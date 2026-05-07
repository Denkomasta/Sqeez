using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.AuthService;
using Sqeez.Api.Services.Interfaces;
using Sqeez.Api.Services.TokenService;
using BC = BCrypt.Net.BCrypt;

namespace Sqeez.Api.Tests.Services
{
    public class AuthServiceTests
    {
        private const string SuperUserEmail = "founder@sqeez.org";

        private async Task<SqeezDbContext> GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SqeezDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new SqeezDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return context;
        }

        private AuthService CreateService(SqeezDbContext context, Mock<ITokenService>? mockTokenService = null, Mock<ISystemConfigService>? mockConfigService = null, Mock<IEmailService>? mockEmailService = null)
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["SUPER_USER_EMAIL"]).Returns(SuperUserEmail);

            if (mockTokenService == null)
            {
                mockTokenService = new Mock<ITokenService>();
                mockTokenService.Setup(t => t.CreateToken(It.IsAny<Student>()))
                                .Returns(ServiceResult<string>.Ok("fake-jwt-token"));
                mockTokenService.Setup(t => t.GenerateRefreshToken())
                                .Returns("fake-refresh-token");
            }

            if (mockConfigService == null)
            {
                mockConfigService = new Mock<ISystemConfigService>();
                // Fake the global config so Registration is open and Max Sessions = 3
                mockConfigService.Setup(c => c.GetConfigAsync())
                    .ReturnsAsync(ServiceResult<SystemConfigDto>.Ok(
                        new SystemConfigDto("Sqeez", "", "", "en", "24/25", true, true, 10, 10, 3)
                    ));
            }
            
            mockEmailService ??= new Mock<IEmailService>();

            var mockLogger = new Mock<ILogger<AuthService>>();

            return new AuthService(context, mockConfig.Object, mockTokenService.Object, mockEmailService.Object, mockConfigService.Object, mockLogger.Object);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndSetsLastSeen()
        {
            var context = await GetInMemoryDbContext();
            string password = "MySecretPassword123!";
            var user = new Student
            {
                Username = "LoginUser",
                Email = "login@sqeez.org",
                PasswordHash = BC.HashPassword(password),
                LastSeen = DateTime.UtcNow.AddMinutes(-2),
                IsEmailVerified = true
            };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var loginDto = new LoginDTO("login@sqeez.org", password);

            var result = await service.LoginAsync(loginDto);

            Assert.True(result.Success);
            Assert.Equal("fake-jwt-token", result.Data!.AccessToken);
            Assert.Equal("fake-refresh-token", result.Data.RefreshToken);

            var dbUser = await context.Students.FindAsync(user.Id);
            Assert.True(dbUser!.LastSeen >= DateTime.UtcNow.AddMinutes(-1));

            // Verify a session was saved
            var sessionCount = await context.UserSessions.CountAsync(s => s.UserId == user.Id);
            Assert.Equal(1, sessionCount);
        }

        [Fact]
        public async Task LoginAsync_WhenTokenCreationFails_ReturnsInternalError()
        {
            var context = await GetInMemoryDbContext();
            string password = "MySecretPassword123!";
            var user = new Student
            {
                Username = "LoginUser",
                Email = "login@sqeez.org",
                PasswordHash = BC.HashPassword(password),
                IsEmailVerified = true
            };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            var tokenService = new Mock<ITokenService>();
            tokenService.Setup(t => t.CreateToken(It.IsAny<Student>()))
                .Returns(ServiceResult<string>.Failure("Token key is invalid.", ServiceError.InternalError));

            var service = CreateService(context, tokenService);

            var result = await service.LoginAsync(new LoginDTO("login@sqeez.org", password));

            Assert.False(result.Success);
            Assert.Equal(ServiceError.InternalError, result.ErrorCode);
            Assert.Equal(0, await context.UserSessions.CountAsync());
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ReturnsUnauthorized()
        {
            var context = await GetInMemoryDbContext();
            var user = new Student { Email = "login@sqeez.org", PasswordHash = BC.HashPassword("CorrectPassword") };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var loginDto = new LoginDTO("login@sqeez.org", "WrongPassword");

            var result = await service.LoginAsync(loginDto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Unauthorized, result.ErrorCode);
        }

        [Fact]
        public async Task RegisterAsync_WithNormalEmail_CreatesStudent()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);
            var registerDto = new RegisterDTO("Tonda", "Svoboda", "NewUser", "normal@sqeez.org", "pwd");

            var result = await service.RegisterAsync(registerDto);

            Assert.True(result.Success);
            Assert.True(result.Data);

            var savedUser = await context.Students.FirstOrDefaultAsync(u => u.Email == "normal@sqeez.org");
            Assert.NotNull(savedUser);
            Assert.Equal(UserRole.Student, savedUser.Role);
            Assert.True(BC.Verify("pwd", savedUser.PasswordHash));
        }

        [Fact]
        public async Task RegisterAsync_WithSuperUserEmail_CreatesAdmin()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);
            var registerDto = new RegisterDTO("Super", "Borec", "Founder", SuperUserEmail, "pwd");

            var result = await service.RegisterAsync(registerDto);

            Assert.True(result.Success);

            var savedUser = await context.Admins.FirstOrDefaultAsync(u => u.Email == SuperUserEmail);
            Assert.NotNull(savedUser);
            Assert.Equal(UserRole.Admin, savedUser.Role);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ReturnsConflict()
        {
            var context = await GetInMemoryDbContext();
            context.Students.Add(new Student { Username = "FirstUser", Email = "taken@sqeez.org" });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var registerDto = new RegisterDTO("Tonda", "Druhy", "SecondUser", "taken@sqeez.org", "pwd");

            var result = await service.RegisterAsync(registerDto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Conflict, result.ErrorCode);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenValid_ReturnsNewTokensAndRevokesOld()
        {
            var context = await GetInMemoryDbContext();
            var user = new Student { Username = "RefreshUser", Email = "refresh@sqeez.org", LastSeen = DateTime.UtcNow.AddMinutes(-2) };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            var oldSession = new UserSession
            {
                UserId = user.Id,
                RefreshToken = "old-refresh-token",
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                IsRevoked = false
            };
            context.UserSessions.Add(oldSession);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.RefreshTokenAsync(new RefreshTokenDto("old-refresh-token"));

            Assert.True(result.Success);
            Assert.Equal("fake-jwt-token", result.Data!.AccessToken);
            Assert.Equal("fake-refresh-token", result.Data.RefreshToken);

            // Assert that the old session was properly revoked
            var dbOldSession = await context.UserSessions.FindAsync(oldSession.Id);
            Assert.True(dbOldSession!.IsRevoked);
        }

        [Fact]
        public async Task LogoutAsync_WhenUserExists_SetsLastSeenAndRevokesSessions()
        {
            var context = await GetInMemoryDbContext();
            var user = new Student { Username = "OnlineUser", Email = "online@sqeez.org", LastSeen = DateTime.UtcNow.AddMinutes(-2) };
            context.Students.Add(user);

            var session = new UserSession { UserId = user.Id, RefreshToken = "my-token", IsRevoked = false };
            context.UserSessions.Add(session);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.LogoutAsync(user.Id, "my-token");

            Assert.True(result.Success);
            Assert.True(result.Data);

            var dbUser = await context.Students.FindAsync(user.Id);
            Assert.True(dbUser!.LastSeen >= DateTime.UtcNow.AddMinutes(-1));

            var dbSession = await context.UserSessions.FindAsync(session.Id);
            Assert.True(dbSession!.IsRevoked);
        }


        [Fact]
        public async Task UpdateUserRoleAsync_WhenModifyingSuperUser_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();

            var performingAdmin = new Admin { Username = "Admin2", Email = "admin2@sqeez.org" };
            var superUser = new Admin { Username = "Founder", Email = SuperUserEmail, Role = UserRole.Admin };

            context.Admins.AddRange(performingAdmin, superUser);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new UpdateRoleDTO(superUser.Id, UserRole.Teacher);

            var result = await service.UpdateUserRoleAsync(performingAdmin.Id, dto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task UpdateUserRoleAsync_WhenNormalAdminCreatesAdmin_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var performingAdmin = new Admin { Username = "Admin2", Email = "admin2@sqeez.org" };
            var targetStudent = new Student { Username = "Student", Email = "student@sqeez.org", Role = UserRole.Student };

            context.Students.AddRange(performingAdmin, targetStudent);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new UpdateRoleDTO(targetStudent.Id, UserRole.Admin);

            var result = await service.UpdateUserRoleAsync(performingAdmin.Id, dto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task UpdateUserRoleAsync_WhenValidRequest_ReachesSqlExecutionAndCatchesInMemoryError()
        {
            var context = await GetInMemoryDbContext();

            var founder = new Admin { Username = "Founder", Email = SuperUserEmail };
            var targetStudent = new Student { Username = "Student", Email = "student@sqeez.org", Role = UserRole.Student };

            context.Students.AddRange(founder, targetStudent);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new UpdateRoleDTO(targetStudent.Id, UserRole.Admin);

            var result = await service.UpdateUserRoleAsync(founder.Id, dto);

            // Because EF Core InMemory DOES NOT support raw SQL (ExecuteSqlInterpolatedAsync), 
            // it throws an InvalidOperationException, which your catch block handles.
            Assert.False(result.Success);
            Assert.Equal(ServiceError.InternalError, result.ErrorCode);
            Assert.Equal("Internal error.", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginAsync_WhenRememberMeIsTrue_SetsSessionExpirationToSevenDays()
        {
            var context = await GetInMemoryDbContext();
            string password = "Password123!";
            var user = new Student { Username = "RememberMeTrue", Email = "true@sqeez.org", PasswordHash = BC.HashPassword(password), IsEmailVerified = true };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            // RememberMe = true
            var loginDto = new LoginDTO("true@sqeez.org", password, true);

            await service.LoginAsync(loginDto);

            var session = await context.UserSessions.FirstOrDefaultAsync(s => s.UserId == user.Id);
            Assert.NotNull(session);

            // Check if expiration is roughly 7 days from now (allowing a 1-minute buffer for test execution time)
            var expectedExpiration = DateTime.UtcNow.AddDays(7);
            Assert.True(session!.ExpiresAt > expectedExpiration.AddMinutes(-1) && session.ExpiresAt <= expectedExpiration);
        }

        [Fact]
        public async Task LoginAsync_WhenRememberMeIsFalse_SetsSessionExpirationToOneDay()
        {
            var context = await GetInMemoryDbContext();
            string password = "Password123!";
            var user = new Student { Username = "RememberMeFalse", Email = "false@sqeez.org", PasswordHash = BC.HashPassword(password), IsEmailVerified = true };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            // RememberMe = false
            var loginDto = new LoginDTO("false@sqeez.org", password, false);

            await service.LoginAsync(loginDto);

            var session = await context.UserSessions.FirstOrDefaultAsync(s => s.UserId == user.Id);
            Assert.NotNull(session);

            // Check if expiration is roughly 24 hours from now
            var expectedExpiration = DateTime.UtcNow.AddHours(24);
            Assert.True(session!.ExpiresAt > expectedExpiration.AddMinutes(-1) && session.ExpiresAt <= expectedExpiration);
        }

        [Fact]
        public async Task VerifyEmailAsync_WhenValidToken_VerifiesUser()
        {
            var context = await GetInMemoryDbContext();
            var user = new Student { Username = "Unverified", Email = "verify@sqeez.org", IsEmailVerified = false, EmailVerificationToken = "valid-token", EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1) };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.VerifyEmailAsync("valid-token", false);

            Assert.True(result.Success);
            var dbUser = await context.Students.FindAsync(user.Id);
            Assert.True(dbUser!.IsEmailVerified);
            Assert.Null(dbUser.EmailVerificationToken);
        }

        [Fact]
        public async Task VerifyEmailAsync_WhenTokenExpired_ReturnsBadRequest()
        {
            var context = await GetInMemoryDbContext();
            var user = new Student { Username = "Unverified", Email = "verify@sqeez.org", IsEmailVerified = false, EmailVerificationToken = "expired-token", EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1) };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.VerifyEmailAsync("expired-token", false);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Unauthorized, result.ErrorCode);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WhenUserDoesNotExist_ReturnsSuccessWithoutSendingEmail()
        {
            var context = await GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var service = CreateService(context, null, null, mockEmailService);

            var result = await service.ResendVerificationEmailAsync(new ResendVerificationDto("missing@sqeez.org"));

            Assert.True(result.Success);
            mockEmailService.Verify(
                service => service.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WhenUserIsAlreadyVerified_ReturnsSuccessWithoutSendingEmail()
        {
            var context = await GetInMemoryDbContext();
            context.Students.Add(new Student
            {
                Username = "Verified",
                Email = "verified@sqeez.org",
                IsEmailVerified = true
            });
            await context.SaveChangesAsync();

            var mockEmailService = new Mock<IEmailService>();
            var service = CreateService(context, null, null, mockEmailService);

            var result = await service.ResendVerificationEmailAsync(new ResendVerificationDto("verified@sqeez.org"));

            Assert.True(result.Success);
            mockEmailService.Verify(
                service => service.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ForgotPasswordAsync_WhenUserExists_GeneratesTokenAndSendsEmail()
        {
            var context = await GetInMemoryDbContext();
            var user = new Student { Username = "Forgetful", Email = "forgot@sqeez.org" };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            var mockEmailService = new Mock<IEmailService>();
            var service = CreateService(context, null, null, mockEmailService);

            var result = await service.ForgotPasswordAsync("forgot@sqeez.org");

            Assert.True(result.Success);
            
            var dbUser = await context.Students.FindAsync(user.Id);
            Assert.NotNull(dbUser!.PasswordResetToken);
            Assert.True(dbUser.PasswordResetTokenExpiry > DateTime.UtcNow);

            // Wait a bit for the background Task.Run to execute
            await Task.Delay(200);

            mockEmailService.Verify(e => e.SendPasswordResetEmailAsync(
                It.Is<string>(s => s.Equals("forgot@sqeez.org", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenValidToken_ResetsPassword()
        {
            var context = await GetInMemoryDbContext();
            var oldHash = BC.HashPassword("OldPassword");
            var user = new Student { Username = "Resetter", Email = "reset@sqeez.org", PasswordHash = oldHash, PasswordResetToken = "valid-reset-token", PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1) };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.ResetPasswordAsync(new ResetPasswordDto("valid-reset-token", "NewPassword123!"));

            Assert.True(result.Success);
            
            var dbUser = await context.Students.FindAsync(user.Id);
            Assert.True(BC.Verify("NewPassword123!", dbUser!.PasswordHash));
            Assert.Null(dbUser.PasswordResetToken);
        }

        [Fact]
        public async Task LoginAsync_WhenMaxSessionsReached_RemovesOldestActiveSession()
        {
            var context = await GetInMemoryDbContext();
            string password = "Password123!";
            var user = new Student { Username = "SessionCap", Email = "cap@sqeez.org", PasswordHash = BC.HashPassword(password), IsEmailVerified = true };
            context.Students.Add(user);
            await context.SaveChangesAsync();

            context.UserSessions.AddRange(
                new UserSession { UserId = user.Id, RefreshToken = "oldest", CreatedAt = DateTime.UtcNow.AddHours(-3), ExpiresAt = DateTime.UtcNow.AddDays(1) },
                new UserSession { UserId = user.Id, RefreshToken = "middle", CreatedAt = DateTime.UtcNow.AddHours(-2), ExpiresAt = DateTime.UtcNow.AddDays(1) },
                new UserSession { UserId = user.Id, RefreshToken = "newest", CreatedAt = DateTime.UtcNow.AddHours(-1), ExpiresAt = DateTime.UtcNow.AddDays(1) }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.LoginAsync(new LoginDTO("cap@sqeez.org", password));

            Assert.True(result.Success);
            var sessions = await context.UserSessions.Where(s => s.UserId == user.Id).ToListAsync();
            Assert.Equal(3, sessions.Count);
            Assert.DoesNotContain(sessions, s => s.RefreshToken == "oldest");
            Assert.Contains(sessions, s => s.RefreshToken == "middle");
            Assert.Contains(sessions, s => s.RefreshToken == "newest");
            Assert.Contains(sessions, s => s.RefreshToken == "fake-refresh-token");
        }

        [Fact]
        public async Task RegisterAsync_WhenPublicRegistrationClosed_ReturnsForbiddenAndDoesNotCreateUser()
        {
            var context = await GetInMemoryDbContext();
            var mockConfigService = new Mock<ISystemConfigService>();
            mockConfigService.Setup(c => c.GetConfigAsync())
                .ReturnsAsync(ServiceResult<SystemConfigDto>.Ok(
                    new SystemConfigDto("Sqeez", "", "", "en", "24/25", false, true, 10, 10, 3)
                ));

            var service = CreateService(context, mockConfigService: mockConfigService);

            var result = await service.RegisterAsync(new RegisterDTO("Closed", "User", "ClosedUser", "closed@sqeez.org", "pwd"));

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
            Assert.False(await context.Students.AnyAsync(u => u.Email == "closed@sqeez.org"));
        }
    }
}
