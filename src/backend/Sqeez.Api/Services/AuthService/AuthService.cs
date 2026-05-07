using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.Interfaces;
using Sqeez.Api.Services.TokenService;
using BC = BCrypt.Net.BCrypt;

namespace Sqeez.Api.Services.AuthService
{
    public class AuthService : BaseService<AuthService>, IAuthService
    {
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ISystemConfigService _configService;
        private readonly string _superUserEmail;
        private readonly string _frontendUrl;

        public AuthService(SqeezDbContext context, IConfiguration config, ITokenService tokenService, IEmailService emailService,
            ISystemConfigService configService, ILogger<AuthService> logger) : base(context, logger)
        {
            _emailService = emailService;
            _tokenService = tokenService;
            _configService = configService;
            _superUserEmail = config["SUPER_USER_EMAIL"]?.Trim().ToLower() ?? string.Empty;
            _frontendUrl = config["FrontendUrl"]?.Trim().TrimEnd('/') ?? "http://localhost:3000";
        }

        private async Task<ServiceResult<AuthResponseDto>> GenerateAuthResponseAndSessionAsync(Student user, bool rememberMe = false)
        {
            var tokenResult = _tokenService.CreateToken(user);
            if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Data))
            {
                _logger.LogError("Failed to create access token for user {UserId}: {Error}", user.Id, tokenResult.ErrorMessage);
                return ServiceResult<AuthResponseDto>.Failure("Failed to create authentication token.", ServiceError.InternalError);
            }

            string accessToken = tokenResult.Data;
            string refreshToken = _tokenService.GenerateRefreshToken();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogError("Failed to create refresh token for user {UserId}.", user.Id);
                return ServiceResult<AuthResponseDto>.Failure("Failed to create authentication token.", ServiceError.InternalError);
            }

            // TODO move the deletion to background service
            var deadSessions = await _context.UserSessions
                .Where(s => s.UserId == user.Id && (s.IsRevoked || s.ExpiresAt <= DateTime.UtcNow))
                .ToListAsync();

            if (deadSessions.Any())
            {
                _context.UserSessions.RemoveRange(deadSessions);
            }

            var configResult = await _configService.GetConfigAsync();
            if (!configResult.Success || configResult.Data == null)
            {
                _logger.LogError("Failed to load system config while creating session for user {UserId}: {Error}", user.Id, configResult.ErrorMessage);
                return ServiceResult<AuthResponseDto>.Failure("Failed to load authentication settings.", ServiceError.InternalError);
            }

            int maxSessions = configResult.Data!.MaxActiveSessionsPerUser;

            var activeSessions = await _context.UserSessions
                .Where(s => s.UserId == user.Id && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();

            if (activeSessions.Count >= maxSessions)
            {
                int sessionsToKill = (activeSessions.Count - maxSessions) + 1;
                var doomedSessions = activeSessions.Take(sessionsToKill);
                foreach (var session in doomedSessions)
                {
                    _context.UserSessions.Remove(session);
                }
            }

            var expirationDate = rememberMe
                ? DateTime.UtcNow.AddDays(7)
                : DateTime.UtcNow.AddHours(24);

            var newSession = new UserSession
            {
                UserId = user.Id,
                RefreshToken = refreshToken,
                ExpiresAt = expirationDate
            };

            _context.UserSessions.Add(newSession);
            await _context.SaveChangesAsync();

            return ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto(accessToken, refreshToken));
        }

        public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDTO dto)
        {
            _logger.LogInformation("Attempting user login.");
            var user = await _context.Students.FirstOrDefaultAsync(u => u.Email == dto.Email.Trim().ToLower());

            if (user == null) return ServiceResult<AuthResponseDto>.Failure("Invalid email or password.", ServiceError.NotFound);

            var config = await _configService.GetConfigAsync();

            if (!user.IsEmailVerified && (config.Data?.RequireEmailVerification ?? true))
                return ServiceResult<AuthResponseDto>.Failure("Please verify your email address before logging in.", ServiceError.Unauthorized);

            bool isValid = BC.Verify(dto.Password.Trim(), user.PasswordHash);
            if (!isValid) return ServiceResult<AuthResponseDto>.Failure("Invalid email or password.", ServiceError.Unauthorized);

            user.LastSeen = DateTime.UtcNow;

            return await GenerateAuthResponseAndSessionAsync(user, dto.RememberMe);
        }

        public async Task<ServiceResult<bool>> RegisterAsync(RegisterDTO dto)
        {
            _logger.LogInformation("Attempting user registration.");

            var config = await _configService.GetConfigAsync();
            if (!config.Data!.AllowPublicRegistration)
            {
                return ServiceResult<bool>.Failure(
                    "Public registration is currently closed. Please contact your administrator for an invite.",
                    ServiceError.Forbidden);
            }

            string email = dto.Email.Trim().ToLower();
            string username = string.IsNullOrWhiteSpace(dto.Username) ? email.Split('@')[0] : dto.Username.Trim();

            if (await _context.Students.AnyAsync(x => x.Email == email))
                return ServiceResult<bool>.Failure("Email already exists.", ServiceError.Conflict);
            if (await _context.Students.AnyAsync(x => x.Username == username))
                return ServiceResult<bool>.Failure("Username already exists", ServiceError.Conflict);

            string salt = BC.GenerateSalt(12);
            string hashedPassword = BC.HashPassword(dto.Password.Trim(), salt);
            bool isSuperUser = email == _superUserEmail;

            string verificationToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

            Student user = isSuperUser
                ? new Admin { FirstName = dto.FirstName.Trim(), LastName = dto.LastName.Trim(), Username = username, Email = email, PasswordHash = hashedPassword, Role = UserRole.Admin, LastSeen = DateTime.UtcNow }
                : new Student { FirstName = dto.FirstName.Trim(), LastName = dto.LastName.Trim(), Username = username, Email = email, PasswordHash = hashedPassword, Role = UserRole.Student, LastSeen = DateTime.UtcNow };

            if (isSuperUser)
            {
                user.IsEmailVerified = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiry = null;
            }
            else
            {
                user.IsEmailVerified = false;
                user.EmailVerificationToken = verificationToken;
                user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
            }

            _context.Students.Add(user);
            await _context.SaveChangesAsync();

            if (!isSuperUser)
            {
                string verificationLink = $"{_frontendUrl}/verify-email?token={verificationToken}&rememberMe={dto.RememberMe.ToString().ToLower()}";

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send background email.");
                    }
                });
            }

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<AuthResponseDto>> VerifyEmailAsync(string token, bool rememberMe)
        {
            var user = await _context.Students.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null)
                return ServiceResult<AuthResponseDto>.Failure("Invalid verification token.", ServiceError.NotFound);

            if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
                return ServiceResult<AuthResponseDto>.Failure("Verification token has expired. Please request a new one.", ServiceError.Unauthorized);

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            user.LastSeen = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GenerateAuthResponseAndSessionAsync(user, rememberMe);
        }

        public async Task<ServiceResult<bool>> ResendVerificationEmailAsync(ResendVerificationDto dto)
        {
            var user = await _context.Students.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return ServiceResult<bool>.Failure("User not found.", ServiceError.NotFound);

            if (user.IsEmailVerified)
                return ServiceResult<bool>.Failure("This email is already verified.", ServiceError.BadRequest);

            string verificationToken;

            if (!string.IsNullOrEmpty(user.EmailVerificationToken) && user.EmailVerificationTokenExpiry > DateTime.UtcNow)
            {
                var timeSinceLastEmail = DateTime.UtcNow.AddHours(24) - user.EmailVerificationTokenExpiry.Value;

                if (timeSinceLastEmail < TimeSpan.FromMinutes(5))
                {
                    return ServiceResult<bool>.Failure("Please wait a couple of minutes before requesting another email.", ServiceError.TooManyRequests);
                }

                verificationToken = user.EmailVerificationToken;

                user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
            }
            else
            {
                verificationToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
                user.EmailVerificationToken = verificationToken;
                user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
            }

            await _context.SaveChangesAsync();

            string verificationLink = $"{_frontendUrl}/verify-email?token={verificationToken}&rememberMe={dto.RememberMe.ToString().ToLower()}";

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send background email.");
                }
            });

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> ForgotPasswordAsync(string email)
        {
            var user = await _context.Students.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return ServiceResult<bool>.Ok(true);

            if (user.PasswordResetTokenExpiry.HasValue &&
                user.PasswordResetTokenExpiry.Value > DateTime.UtcNow.AddMinutes(10))
            {
                return ServiceResult<bool>.Ok(true);
            }

            string resetToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

            await _context.SaveChangesAsync();

            string resetLink = $"{_frontendUrl}/reset-password?token={resetToken}";

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send background reset email.");
                }
            });

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _context.Students.FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

            if (user == null)
                return ServiceResult<bool>.Failure("Invalid password reset token.", ServiceError.BadRequest);

            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
                return ServiceResult<bool>.Failure("This password reset link has expired. Please request a new one.", ServiceError.Unauthorized);

            string salt = BC.GenerateSalt(12);
            user.PasswordHash = BC.HashPassword(dto.NewPassword.Trim(), salt);

            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto)
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshToken == dto.RefreshToken);

            if (session == null || session.IsRevoked || session.ExpiresAt <= DateTime.UtcNow)
            {
                return ServiceResult<AuthResponseDto>.Failure("Invalid or expired refresh token. Please login again.", ServiceError.Unauthorized);
            }

            var user = session.User;

            user.LastSeen = DateTime.UtcNow;

            session.IsRevoked = true;

            return await GenerateAuthResponseAndSessionAsync(user, true);
        }

        public async Task<ServiceResult<bool>> LogoutAsync(long userId, string? refreshToken = null)
        {
            _logger.LogInformation("Attempting to logout user: {id}", userId);
            var user = await _context.Students.FindAsync(userId);
            if (user == null) return ServiceResult<bool>.Failure("User not found.", ServiceError.NotFound);

            user.LastSeen = DateTime.UtcNow;

            var activeSessions = await _context.UserSessions
                .Where(s => s.UserId == userId && !s.IsRevoked)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                if (refreshToken == null || session.RefreshToken == refreshToken)
                {
                    session.IsRevoked = true;
                }
            }

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<UserDTO>> GetCurrentUserAsync(long userId, string? role)
        {
            var user = role switch
            {
                "Teacher" => await _context.Teachers.FindAsync(userId),
                "Admin" => await _context.Admins.FindAsync(userId),
                _ => await _context.Students.FindAsync(userId),
            };

            if (user == null) return ServiceResult<UserDTO>.Failure("User not found", ServiceError.NotFound);

            var result = new UserDTO(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                CurrentXP: user.CurrentXP.ToString(),
                Role: user.Role,
                AvatarUrl: user.AvatarUrl
            );
            return ServiceResult<UserDTO>.Ok(result);
        }

        public async Task<ServiceResult<bool>> UpdateUserRoleAsync(long adminId, UpdateRoleDTO dto)
        {
            var performingAdmin = await _context.Admins.FindAsync(adminId);
            if (performingAdmin == null)
            {
                _logger.LogWarning("Unauthorized role update attempt by user {Id}", adminId);
                return ServiceResult<bool>.Failure("Unauthorized admin user.", ServiceError.Unauthorized);
            }

            UserRole newRole = dto.Role;
            long userId = dto.Id;
            var user = await _context.Students.FindAsync(userId);
            if (user == null) return ServiceResult<bool>.Failure("User not found", ServiceError.NotFound);

            if (user.Role == newRole) return ServiceResult<bool>.Ok(true);

            bool isSuperUser = performingAdmin.Email == _superUserEmail;

            // 1. Prevent role modifications to the Super User account (Already Handled)
            if (user.Email == _superUserEmail)
            {
                _logger.LogWarning("Admin {AdminId} attempted to modify the super user account.", performingAdmin.Id);
                return ServiceResult<bool>.Failure("Forbidden operation.", ServiceError.Forbidden);
            }

            // 2. Only Super User can assign Admin role, and no one can create another Admin
            if (newRole == UserRole.Admin && !isSuperUser)
            {
                _logger.LogWarning("Admin {AdminId} attempted to assign another admin role.", performingAdmin.Id);
                return ServiceResult<bool>.Failure("Forbidden operation.", ServiceError.Forbidden);
            }

            // 3. Prevent modifying a Teacher's/Admin's role if they are tied to Subjects or Classes
            if (user.Role != UserRole.Student && newRole == UserRole.Student)
            {
                // Safely cast the base Student entity to a Teacher entity to access Teacher-specific properties
                if (user is Teacher teacher)
                {
                    bool hasClasses = teacher.ManagedClassId.HasValue;
                    bool hasSubjects = await _context.Subjects.AnyAsync(s => s.TeacherId == userId);

                    if (hasSubjects || hasClasses)
                    {
                        _logger.LogWarning("Attempted to downgrade Teacher/Admin {Id} who has active dependencies.", userId);
                        return ServiceResult<bool>.Failure(
                            "Cannot change this user to a Student because they are currently assigned to a subject or manage a school class. Please reassign their duties first.",
                            ServiceError.Conflict);
                    }
                }
            }

            try
            {
                // We use ExecuteSqlInterpolatedAsync to update the 'Role' (User discriminator)
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users""
            SET ""Role"" = {(int)newRole},
                ""Department"" = {dto.Department},
                ""PhoneNumber"" = {dto.PhoneNumber}
            WHERE ""Id"" = {userId}");

                _logger.LogInformation("Successfully updated user {Id} to {Role}", userId, newRole);
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user {Id}", userId);
                return ServiceResult<bool>.Failure("Internal error.", ServiceError.InternalError);
            }
        }
    }
}
