using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Import;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace Sqeez.Api.Services.UserService
{
    /// <summary>
    /// Implements polymorphic user search, profile retrieval, account management, and avatar updates.
    /// </summary>
    public class UserService : BaseService<UserService>, IUserService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly string _superUserEmail;

        public UserService(
            SqeezDbContext context,
            ILogger<UserService> logger,
            IFileStorageService fileStorageService,
            IConfiguration configuration) : base(context, logger)
        {
            _fileStorageService = fileStorageService;
            _superUserEmail = configuration["SUPER_USER_EMAIL"]?.Trim().ToLower() ?? string.Empty;
        }

        private static StudentDto MapUserToDto(Student user, IReadOnlySet<long>? visibleEmailUserIds = null)
        {
            if (user == null) return null!;

            var email = visibleEmailUserIds == null || visibleEmailUserIds.Contains(user.Id)
                ? user.Email
                : PseudonymizeEmail(user.Email);

            return user switch
            {
                Admin a => new AdminDto
                {
                    Id = a.Id,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Username = a.Username,
                    Email = email,
                    CurrentXP = a.CurrentXP,
                    Role = a.Role,
                    LastSeen = a.LastSeen,
                    AvatarUrl = a.AvatarUrl,
                    SchoolClassId = a.SchoolClassId,
                    Department = a.Department,
                    ManagedClassId = a.ManagedClassId,
                    PhoneNumber = a.PhoneNumber
                },
                Teacher t => new TeacherDto
                {
                    Id = t.Id,
                    FirstName = t.FirstName,
                    LastName = t.LastName,
                    Username = t.Username,
                    Email = email,
                    CurrentXP = t.CurrentXP,
                    Role = t.Role,
                    LastSeen = t.LastSeen,
                    AvatarUrl = t.AvatarUrl,
                    SchoolClassId = t.SchoolClassId,
                    Department = t.Department,
                    ManagedClassId = t.ManagedClassId
                },
                _ => new StudentDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username,
                    Email = email,
                    CurrentXP = user.CurrentXP,
                    Role = user.Role,
                    LastSeen = user.LastSeen,
                    AvatarUrl = user.AvatarUrl,
                    SchoolClassId = user.SchoolClassId
                }
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<PagedResponse<StudentDto>>> GetAllUsersAsync(UserFilterDto filter, long currentUserId, string? currentUserRole)
        {
            IQueryable<Student> query = _context.Students.AsNoTracking();

            if (filter.Role.HasValue)
            {
                if (filter.StrictRoleOnly)
                {
                    query = query.Where(u => u.Role == filter.Role.Value);
                }
                else
                {
                    switch (filter.Role.Value)
                    {
                        case UserRole.Admin:
                            query = query.Where(u => u.Role == UserRole.Admin);
                            break;
                        case UserRole.Teacher:
                            query = query.Where(u => u.Role == UserRole.Teacher || u.Role == UserRole.Admin);
                            break;
                        case UserRole.Student:
                            break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.Trim().ToLower();
                query = currentUserRole == "Admin"
                    ? query.Where(u => u.Username.ToLower().Contains(searchTerm) ||
                                       u.Email.ToLower().Contains(searchTerm))
                    : query.Where(u => u.Username.ToLower().Contains(searchTerm));
            }

            if (filter.IsOnline is bool isOnline)
            {
                var threshold = DateTime.UtcNow.AddMinutes(-15);
                query = query.Where(u => isOnline ? u.LastSeen >= threshold : u.LastSeen < threshold);
            }

            if (filter.SchoolClassId.HasValue)
            {
                query = query.Where(u => u.SchoolClassId == filter.SchoolClassId.Value);
            }

            if (filter.SubjectId.HasValue)
            {
                query = query.Where(u => u.Enrollments.Any(e => e.SubjectId == filter.SubjectId.Value));
            }

            if (filter.IsArchived is true)
            {
                query = query.Where(u => u.ArchivedAt != null);
            }
            else if (filter.IsArchived is false)
            {
                query = query.Where(u => u.ArchivedAt == null);
            }

            if (filter.IsEmailVerified is bool isEmailVerified)
            {
                query = query.Where(u => u.IsEmailVerified == isEmailVerified);
            }

            if (!string.IsNullOrWhiteSpace(filter.Department))
            {
                query = query.OfType<Teacher>().Where(t => t.Department == filter.Department).Cast<Student>();
            }

            if (!string.IsNullOrWhiteSpace(filter.PhoneNumber))
            {
                query = query.OfType<Admin>().Where(a => a.PhoneNumber == filter.PhoneNumber).Cast<Student>();
            }

            if (filter.HasAssignedClass.HasValue)
            {
                if (filter.HasAssignedClass.Value)
                {
                    query = query.Where(u => u is Teacher && ((Teacher)u).ManagedClassId != null);
                }
                else
                {
                    query = query.Where(u => u is Teacher && ((Teacher)u).ManagedClassId == null);
                }
            }

            int totalCount = await query.CountAsync();

            query = filter.SortBy switch
            {
                UserSortField.XP => filter.IsDescending
                    ? query.OrderByDescending(u => u.CurrentXP).ThenBy(u => u.Username)
                    : query.OrderBy(u => u.CurrentXP).ThenBy(u => u.Username),

                UserSortField.LastSeen => filter.IsDescending
                    ? query.OrderByDescending(u => u.LastSeen).ThenBy(u => u.Username)
                    : query.OrderBy(u => u.LastSeen).ThenBy(u => u.Username),

                _ => filter.IsDescending
                    ? query.OrderByDescending(u => u.Username)
                    : query.OrderBy(u => u.Username)
            };

            var users = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var visibleEmailUserIds = await GetVisibleEmailUserIdsAsync(users, currentUserId, currentUserRole);
            var mappedUsers = users.Select(user => MapUserToDto(user, visibleEmailUserIds)).ToList();

            var response = new PagedResponse<StudentDto>
            {
                Data = mappedUsers,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ServiceResult<PagedResponse<StudentDto>>.Ok(response);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<StudentDto>> GetUserByIdAsync(long id, long currentUserId, string? currentUserRole)
        {
            var user = await _context.Students.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return ServiceResult<StudentDto>.Failure("User not found.", ServiceError.NotFound);

            var visibleEmailUserIds = await GetVisibleEmailUserIdsAsync(new[] { user }, currentUserId, currentUserRole);
            return ServiceResult<StudentDto>.Ok(MapUserToDto(user, visibleEmailUserIds));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<DetailedUserDto>> GetDetailedUserByIdAsync(long id, long currentUserId, string? currentUserRole)
        {
            var user = await _context.Students
                .AsNoTracking()
                .Include(u => u.SchoolClass)
                .Include(u => u.Enrollments)
                    .ThenInclude(e => e.Subject)
                .Include(u => u.StudentBadges)
                    .ThenInclude(sb => sb.Badge)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ServiceResult<DetailedUserDto>.Failure("User not found.", ServiceError.NotFound);

            var visibleEmailUserIds = await GetVisibleEmailUserIdsAsync(new[] { user }, currentUserId, currentUserRole);
            var baseDto = MapUserToDto(user, visibleEmailUserIds);

            var detailedDto = new DetailedUserDto
            {
                Id = baseDto.Id,
                FirstName = baseDto.FirstName,
                LastName = baseDto.LastName,
                Username = baseDto.Username,
                Email = baseDto.Email,
                CurrentXP = baseDto.CurrentXP,
                Role = baseDto.Role,
                LastSeen = baseDto.LastSeen,
                AvatarUrl = baseDto.AvatarUrl,
                SchoolClassId = baseDto.SchoolClassId,

                Department = baseDto is TeacherDto t ? t.Department : null,
                ManagedClassId = baseDto is TeacherDto tm ? tm.ManagedClassId : null,
                PhoneNumber = baseDto is AdminDto a ? a.PhoneNumber : string.Empty,

                SchoolClassDetails = user.SchoolClass == null ? null : new SchoolClassBasicDto
                {
                    Id = user.SchoolClass.Id,
                    Name = user.SchoolClass.Name,
                    AcademicYear = user.SchoolClass.AcademicYear
                },

                Enrollments = user.Enrollments.Select(e => new EnrollmentBasicDto
                {
                    Id = e.Id,
                    SubjectId = e.SubjectId,
                    SubjectName = e.Subject?.Name ?? "Unknown Subject",
                    Mark = e.Mark,
                    EnrolledAt = e.EnrolledAt,
                    ArchivedAt = e.ArchivedAt
                }).ToList(),

                // Detailed profiles show only the most recent earned badges to keep payloads compact.
                Badges = user.StudentBadges
                    .OrderByDescending(sb => sb.EarnedAt)
                    .Take(5)
                    .Select(sb => new StudentBadgeBasicDto
                    {
                        BadgeId = sb.BadgeId,
                        Name = sb.Badge?.Name ?? "Unknown Badge",
                        IconUrl = sb.Badge?.IconUrl,
                        EarnedAt = sb.EarnedAt
                    }).ToList()
            };

            return ServiceResult<DetailedUserDto>.Ok(detailedDto);
        }

        private async Task<HashSet<long>> GetVisibleEmailUserIdsAsync(IReadOnlyCollection<Student> targetUsers, long currentUserId, string? currentUserRole)
        {
            var visibleIds = new HashSet<long>();
            if (!targetUsers.Any())
            {
                return visibleIds;
            }

            if (currentUserRole == "Admin")
            {
                return targetUsers.Select(user => user.Id).ToHashSet();
            }

            // Admin emails are intentionally public so ordinary users can find a support contact.
            foreach (var admin in targetUsers.Where(user => user.Role == UserRole.Admin))
            {
                visibleIds.Add(admin.Id);
            }

            if (currentUserId <= 0)
            {
                return visibleIds;
            }

            visibleIds.Add(currentUserId);

            var currentUser = await _context.Students
                .AsNoTracking()
                .Where(user => user.Id == currentUserId)
                .Select(user => new { user.Id, user.Role, user.SchoolClassId })
                .FirstOrDefaultAsync();

            if (currentUser == null)
            {
                return visibleIds;
            }

            var targetTeacherIds = targetUsers
                .Where(user => user.Role == UserRole.Teacher)
                .Select(user => user.Id)
                .ToHashSet();

            if (targetTeacherIds.Any())
            {
                if (currentUser.SchoolClassId.HasValue)
                {
                    // Students can see the email of the teacher responsible for their class.
                    foreach (var teacher in targetUsers.OfType<Teacher>().Where(teacher => teacher.ManagedClassId == currentUser.SchoolClassId))
                    {
                        visibleIds.Add(teacher.Id);
                    }
                }

                // Students can see teacher emails for subjects where they have an enrollment.
                var subjectTeacherIds = await _context.Enrollments
                    .AsNoTracking()
                    .Where(enrollment =>
                        enrollment.StudentId == currentUserId &&
                        enrollment.Subject.TeacherId.HasValue &&
                        targetTeacherIds.Contains(enrollment.Subject.TeacherId.Value))
                    .Select(enrollment => enrollment.Subject.TeacherId!.Value)
                    .Distinct()
                    .ToListAsync();

                foreach (var teacherId in subjectTeacherIds)
                {
                    visibleIds.Add(teacherId);
                }
            }

            if (currentUserRole == "Teacher")
            {
                var targetNonAdminIds = targetUsers
                    .Where(user => user.Role != UserRole.Admin)
                    .Select(user => user.Id)
                    .ToHashSet();

                var managedClassId = await _context.Teachers
                    .AsNoTracking()
                    .Where(teacher => teacher.Id == currentUserId)
                    .Select(teacher => teacher.ManagedClassId)
                    .FirstOrDefaultAsync();

                if (managedClassId.HasValue)
                {
                    // Teachers can see emails of students in their managed class.
                    foreach (var student in targetUsers.Where(user => user.Role != UserRole.Admin && user.SchoolClassId == managedClassId))
                    {
                        visibleIds.Add(student.Id);
                    }
                }

                // Teachers can see emails of students enrolled in their subjects.
                var enrolledStudentIds = await _context.Enrollments
                    .AsNoTracking()
                    .Where(enrollment =>
                        targetNonAdminIds.Contains(enrollment.StudentId) &&
                        enrollment.Subject.TeacherId == currentUserId)
                    .Select(enrollment => enrollment.StudentId)
                    .Distinct()
                    .ToListAsync();

                foreach (var studentId in enrolledStudentIds)
                {
                    visibleIds.Add(studentId);
                }
            }

            return visibleIds;
        }

        private static string PseudonymizeEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "***@***";
            }

            var parts = email.Split('@', 2);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                return "***@***";
            }

            var local = parts[0].Trim();
            var domain = parts[1].Trim();
            var domainParts = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var maskedLocal = $"{local[0]}***";
            if (domainParts.Length == 0)
            {
                return $"{maskedLocal}@***";
            }

            var maskedDomain = $"{domainParts[0][0]}***";
            var suffix = domainParts.Length > 1
                ? $".{domainParts[^1]}"
                : string.Empty;

            return $"{maskedLocal}@{maskedDomain}{suffix}";
        }

        /// <inheritdoc />
        public async Task<ServiceResult<StudentDto>> CreateUserAsync(CreateStudentDto dto)
        {
            var email = dto.Email.Trim().ToLower();
            var username = dto.Username.Trim();
            var usernameLower = username.ToLower();

            var existingIdentityMatches = await _context.Students
                .AsNoTracking()
                .Where(u => u.Email == email || u.Username.ToLower() == usernameLower)
                .Select(u => new { u.Email, Username = u.Username.ToLower() })
                .ToListAsync();

            if (existingIdentityMatches.Any(u => u.Email == email))
                return ServiceResult<StudentDto>.Failure("Email already in use.", ServiceError.Conflict);

            if (existingIdentityMatches.Any(u => u.Username == usernameLower))
                return ServiceResult<StudentDto>.Failure("Username is already taken.", ServiceError.Conflict);

            if (dto.SchoolClassId.HasValue && dto.SchoolClassId.Value != 0)
            {
                var classExists = await _context.SchoolClasses.AnyAsync(c => c.Id == dto.SchoolClassId.Value);
                if (!classExists)
                    return ServiceResult<StudentDto>.Failure("School Class does not exist.", ServiceError.NotFound);
            }

            Student newUser = dto switch
            {
                CreateAdminDto adminDto => new Admin
                {
                    Role = UserRole.Admin,
                    Department = adminDto.Department,
                    ManagedClassId = adminDto.ManagedClassId,
                    PhoneNumber = string.IsNullOrWhiteSpace(adminDto.PhoneNumber) ? "-" : adminDto.PhoneNumber
                },
                CreateTeacherDto teacherDto => new Teacher
                {
                    Role = UserRole.Teacher,
                    Department = teacherDto.Department,
                    ManagedClassId = teacherDto.ManagedClassId
                },
                _ => new Student { Role = UserRole.Student }
            };

            newUser.FirstName = dto.FirstName;
            newUser.LastName = dto.LastName;
            newUser.Username = username;
            newUser.Email = email;
            newUser.PasswordHash = BC.HashPassword(dto.Password.Trim(), BC.GenerateSalt(12));
            newUser.LastSeen = DateTime.UtcNow;
            newUser.SchoolClassId = dto.SchoolClassId == 0 ? null : dto.SchoolClassId;

            _context.Students.Add(newUser);
            await _context.SaveChangesAsync();

            return ServiceResult<StudentDto>.Ok(MapUserToDto(newUser));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<BulkOperationResult<StudentDto>>> CreateStudentsBulkAsync(IEnumerable<Student> students)
        {
            var bulkResult = new BulkOperationResult<StudentDto>();
            var studentList = students.ToList();

            if (!studentList.Any())
                return ServiceResult<BulkOperationResult<StudentDto>>.Ok(bulkResult);

            var incomingEmails = studentList.Select(s => s.Email.ToLower()).ToList();
            var incomingUsernames = studentList.Select(s => s.Username.ToLower()).ToList();

            var existingRecords = await _context.Students
                .Where(s => incomingEmails.Contains(s.Email.ToLower()) ||
                            incomingUsernames.Contains(s.Username.ToLower()))
                .Select(s => new
                {
                    Email = s.Email.ToLower(),
                    Username = s.Username.ToLower()
                })
                .ToListAsync();

            var existingEmails = existingRecords.Select(r => r.Email).ToHashSet();
            var existingUsernames = existingRecords.Select(r => r.Username).ToHashSet();

            var validStudentsToInsert = new List<Student>();

            foreach (var student in studentList)
            {
                bool isDuplicateEmail = existingEmails.Contains(student.Email.ToLower());
                bool isDuplicateUsername = existingUsernames.Contains(student.Username.ToLower());

                if (isDuplicateEmail || isDuplicateUsername)
                {
                    string reason = isDuplicateEmail ? "Email already exists or is archived" : "Derived username already exists";
                    bulkResult.SkippedMessages.Add($"Student '{student.Email}' skipped: {reason}.");
                    continue;
                }

                validStudentsToInsert.Add(student);
                existingEmails.Add(student.Email.ToLower());
                existingUsernames.Add(student.Username.ToLower());
            }

            if (validStudentsToInsert.Any())
            {
                await _context.Students.AddRangeAsync(validStudentsToInsert);
                await _context.SaveChangesAsync();

                bulkResult.Created = validStudentsToInsert.Select(user => MapUserToDto(user)).ToList();
            }

            return ServiceResult<BulkOperationResult<StudentDto>>.Ok(bulkResult);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> ArchiveUserAsync(long id, long currentUserId, string? currentUserRole)
        {
            var targetUser = await _context.Students.FirstOrDefaultAsync(u => u.Id == id);
            if (targetUser == null)
                return ServiceResult<bool>.Failure("User not found.", ServiceError.NotFound);

            var currentUser = await _context.Students.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (currentUser == null)
                return ServiceResult<bool>.Failure("Current user not found.", ServiceError.Unauthorized);

            bool targetIsSuperAdmin = IsSuperAdmin(targetUser);
            bool currentUserIsSuperAdmin = IsSuperAdmin(currentUser);
            bool canArchive =
                !targetIsSuperAdmin &&
                (targetUser.Id == currentUserId ||
                 (currentUserRole == "Admin" &&
                  (currentUserIsSuperAdmin || targetUser.Role != UserRole.Admin)));

            if (!canArchive)
            {
                return ServiceResult<bool>.Failure("You do not have permission to archive this user.", ServiceError.Forbidden);
            }

            targetUser.ArchivedAt ??= DateTime.UtcNow;

            var activeSessions = await _context.UserSessions
                .Where(session => session.UserId == targetUser.Id && !session.IsRevoked)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.IsRevoked = true;
            }

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> RestoreUserAsync(long id, long currentUserId, string? currentUserRole)
        {
            var targetUser = await _context.Students.FirstOrDefaultAsync(u => u.Id == id);
            if (targetUser == null)
                return ServiceResult<bool>.Failure("User not found.", ServiceError.NotFound);

            var currentUser = await _context.Students.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (currentUser == null)
                return ServiceResult<bool>.Failure("Current user not found.", ServiceError.Unauthorized);

            bool targetIsSuperAdmin = IsSuperAdmin(targetUser);
            bool currentUserIsSuperAdmin = IsSuperAdmin(currentUser);
            bool canRestore =
                currentUserRole == "Admin" &&
                !targetIsSuperAdmin &&
                (currentUserIsSuperAdmin || targetUser.Role != UserRole.Admin);

            if (!canRestore)
            {
                return ServiceResult<bool>.Failure("You do not have permission to restore this user.", ServiceError.Forbidden);
            }

            targetUser.ArchivedAt = null;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteUserAsync(
            long id,
            long currentUserId,
            string? currentUserRole,
            long? replacementMediaOwnerId = null)
        {
            if (currentUserRole != "Admin")
            {
                return ServiceResult<bool>.Failure("Only admins can permanently delete users.", ServiceError.Forbidden);
            }

            var currentUserExists = await _context.Students
                .AsNoTracking()
                .AnyAsync(user => user.Id == currentUserId && user.Role == UserRole.Admin);

            if (!currentUserExists)
            {
                return ServiceResult<bool>.Failure("Current user not found.", ServiceError.Unauthorized);
            }

            var targetUser = await _context.Students.FirstOrDefaultAsync(u => u.Id == id);
            if (targetUser == null)
                return ServiceResult<bool>.Failure("User not found.", ServiceError.NotFound);

            // Admin accounts must be explicitly demoted before deletion so elevated accounts are never removed by accident.
            if (targetUser.Role == UserRole.Admin)
            {
                return ServiceResult<bool>.Failure(
                    "Admins must be changed to teacher before they can be permanently deleted.",
                    ServiceError.Forbidden);
            }

            if (targetUser.ArchivedAt == null)
            {
                return ServiceResult<bool>.Failure(
                    "User must be archived before permanent deletion.",
                    ServiceError.ValidationFailed);
            }

            // Database FK restrictions require dependent student history to be removed before the user row.
            var fileUrlsToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(targetUser.AvatarUrl))
            {
                fileUrlsToDelete.Add(targetUser.AvatarUrl);
            }

            var sessions = await _context.UserSessions
                .Where(session => session.UserId == id)
                .ToListAsync();
            _context.UserSessions.RemoveRange(sessions);

            var studentBadges = await _context.StudentBadges
                .Where(studentBadge => studentBadge.StudentId == id)
                .ToListAsync();
            _context.StudentBadges.RemoveRange(studentBadges);

            var enrollments = await _context.Enrollments
                .Where(enrollment => enrollment.StudentId == id)
                .ToListAsync();
            var enrollmentIds = enrollments.Select(enrollment => enrollment.Id).ToList();

            if (enrollmentIds.Any())
            {
                var attempts = await _context.QuizAttempts
                    .Include(attempt => attempt.Responses)
                    .Where(attempt => enrollmentIds.Contains(attempt.EnrollmentId))
                    .ToListAsync();
                var responses = attempts.SelectMany(attempt => attempt.Responses).ToList();

                _context.QuizQuestionResponses.RemoveRange(responses);
                _context.QuizAttempts.RemoveRange(attempts);
                _context.Enrollments.RemoveRange(enrollments);
            }

            if (targetUser.Role == UserRole.Teacher)
            {
                // Subjects survive teacher deletion; they become unassigned and can be reassigned by an admin later.
                var subjects = await _context.Subjects
                    .Where(subject => subject.TeacherId == id)
                    .ToListAsync();
                foreach (var subject in subjects)
                {
                    subject.TeacherId = null;
                }

                var ownedMediaAssets = await _context.MediaAssets
                    .Where(mediaAsset => mediaAsset.OwnerId == id)
                    .ToListAsync();

                if (ownedMediaAssets.Any())
                {
                    // Quiz media can still be referenced by content, so ownership is transferred instead of deleting the assets.
                    var replacementOwnerValidation = await ValidateReplacementMediaOwnerAsync(id, replacementMediaOwnerId);
                    if (replacementOwnerValidation != null)
                    {
                        return replacementOwnerValidation;
                    }

                    foreach (var mediaAsset in ownedMediaAssets)
                    {
                        mediaAsset.OwnerId = replacementMediaOwnerId!.Value;
                    }
                }
            }

            _context.Students.Remove(targetUser);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to permanently delete user {UserId}.", id);
                return ServiceResult<bool>.Failure("Failed to permanently delete user.", ServiceError.InternalError);
            }

            foreach (var fileUrl in fileUrlsToDelete)
            {
                var deleteFileResult = await _fileStorageService.DeleteFileAsync(fileUrl);
                if (!deleteFileResult.Success)
                {
                    _logger.LogWarning(
                        "Failed to delete file {FileUrl} while permanently deleting user {UserId}: {Error}",
                        fileUrl,
                        id,
                        deleteFileResult.ErrorMessage);
                }
            }

            return ServiceResult<bool>.Ok(true);
        }

        private async Task<ServiceResult<bool>?> ValidateReplacementMediaOwnerAsync(long targetUserId, long? replacementMediaOwnerId)
        {
            if (!replacementMediaOwnerId.HasValue)
            {
                return ServiceResult<bool>.Failure(
                    "Replacement media owner is required because this user owns media assets.",
                    ServiceError.Conflict);
            }

            if (replacementMediaOwnerId.Value == targetUserId)
            {
                return ServiceResult<bool>.Failure(
                    "Replacement media owner cannot be the user being removed.",
                    ServiceError.Conflict);
            }

            var replacementOwnerExists = await _context.Teachers
                .AsNoTracking()
                .AnyAsync(user => user.Id == replacementMediaOwnerId.Value && user.ArchivedAt == null);

            if (!replacementOwnerExists)
            {
                return ServiceResult<bool>.Failure(
                    "Replacement media owner must be an active teacher or admin.",
                    ServiceError.NotFound);
            }

            return null;
        }

        private bool IsSuperAdmin(Student user)
        {
            return user.Role == UserRole.Admin &&
                   !string.IsNullOrWhiteSpace(_superUserEmail) &&
                   user.Email.Equals(_superUserEmail, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<StudentDto>> PatchUserAsync(long id, PatchStudentDto dto, long currentUserId, string? currentUserRole)
        {
            var user = await _context.Students.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return ServiceResult<StudentDto>.Failure("User not found.", ServiceError.NotFound);

            var accessResult = await ValidatePatchUserAccessAsync(user, dto, currentUserId, currentUserRole);
            if (accessResult != null)
                return accessResult;

            if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.Username)
            {
                var usernameTaken = await _context.Students.AnyAsync(u => u.Username == dto.Username && u.Id != id);
                if (usernameTaken)
                    return ServiceResult<StudentDto>.Failure("Username is already taken.", ServiceError.Conflict);

                user.Username = dto.Username;
            }

            if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;

            if (dto.SchoolClassId.HasValue)
            {
                if (dto.SchoolClassId.Value != 0 && !await _context.SchoolClasses.AnyAsync(c => c.Id == dto.SchoolClassId.Value))
                    return ServiceResult<StudentDto>.Failure("School Class not found.", ServiceError.NotFound);

                if (user is Teacher teacherUser &&
                    dto.SchoolClassId.Value != 0 &&
                    teacherUser.ManagedClassId == dto.SchoolClassId.Value)
                {
                    return ServiceResult<StudentDto>.Failure(
                        "A teacher cannot be assigned as a student of the class they manage.",
                        ServiceError.ValidationFailed);
                }

                user.SchoolClassId = dto.SchoolClassId.Value == 0 ? null : dto.SchoolClassId.Value;
            }

            if (user is Teacher teacher && dto is PatchTeacherDto teacherDto)
            {
                if (teacherDto.Department != null) teacher.Department = teacherDto.Department;

                if (teacherDto.ManagedClassId.HasValue)
                {
                    if (teacherDto.ManagedClassId.Value != 0 && !await _context.SchoolClasses.AnyAsync(c => c.Id == teacherDto.ManagedClassId.Value))
                        return ServiceResult<StudentDto>.Failure("Managed Class not found.", ServiceError.NotFound);

                    if (teacherDto.ManagedClassId.Value != 0 &&
                        teacher.SchoolClassId == teacherDto.ManagedClassId.Value)
                    {
                        return ServiceResult<StudentDto>.Failure(
                            "A teacher cannot manage a class where they are already assigned as a student.",
                            ServiceError.ValidationFailed);
                    }

                    teacher.ManagedClassId = teacherDto.ManagedClassId.Value == 0 ? null : teacherDto.ManagedClassId.Value;
                }
            }

            if (user is Admin admin && dto is PatchAdminDto adminDto)
            {
                if (adminDto.PhoneNumber != null)
                    admin.PhoneNumber = string.IsNullOrWhiteSpace(adminDto.PhoneNumber) ? "-" : adminDto.PhoneNumber;
            }

            await _context.SaveChangesAsync();

            return ServiceResult<StudentDto>.Ok(MapUserToDto(user));
        }

        private async Task<ServiceResult<StudentDto>?> ValidatePatchUserAccessAsync(
            Student targetUser,
            PatchStudentDto dto,
            long currentUserId,
            string? currentUserRole)
        {
            var isSelf = targetUser.Id == currentUserId;
            if (currentUserRole != "Admin")
            {
                if (!isSelf)
                {
                    return ServiceResult<StudentDto>.Failure(
                        "You do not have permission to modify another user's profile.",
                        ServiceError.Forbidden);
                }

                return HasOnlyBasicPatchFields(dto)
                    ? null
                    : ServiceResult<StudentDto>.Failure(
                        "You do not have permission to change assignments or role-specific profile data.",
                        ServiceError.Forbidden);
            }

            var currentUser = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null || currentUser.Role != UserRole.Admin)
            {
                return ServiceResult<StudentDto>.Failure("Current user not found.", ServiceError.Unauthorized);
            }

            var currentUserIsSuperAdmin = IsSuperAdmin(currentUser);
            var targetUserIsSuperAdmin = IsSuperAdmin(targetUser);

            // The configured superadmin account is protected from every patch except its own self-edit.
            if (targetUserIsSuperAdmin)
            {
                return currentUserIsSuperAdmin && isSelf
                    ? null
                    : ServiceResult<StudentDto>.Failure(
                        "Only the superadmin can modify their own profile.",
                        ServiceError.Forbidden);
            }

            if (targetUser.Role == UserRole.Admin)
            {
                // Ordinary admins may update only their own basic profile fields; superadmin manages other admins.
                if (currentUserIsSuperAdmin)
                {
                    return null;
                }

                return isSelf && HasOnlyBasicPatchFields(dto)
                    ? null
                    : ServiceResult<StudentDto>.Failure(
                        "Only the superadmin can modify admin profile data.",
                        ServiceError.Forbidden);
            }

            return null;
        }

        private static bool HasOnlyBasicPatchFields(PatchStudentDto dto)
        {
            if (dto.SchoolClassId.HasValue)
            {
                return false;
            }

            if (dto is PatchTeacherDto teacherDto &&
                (teacherDto.Department != null || teacherDto.ManagedClassId.HasValue))
            {
                return false;
            }

            if (dto is PatchAdminDto adminDto && adminDto.PhoneNumber != null)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> UploadAvatarAsync(long currentUserId, IFormFile imageFile, long? targetUserId = null, string? currentUserRole = null)
        {
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            if (!allowedImageExtensions.Contains(extension))
            {
                return ServiceResult<string>.Failure("Avatars must be an image file (.jpg, .png, .gif).", ServiceError.ValidationFailed);
            }

            var userId = targetUserId ?? currentUserId;
            var user = await _context.Students.FindAsync(userId);
            if (user == null)
            {
                return ServiceResult<string>.Failure("User not found.", ServiceError.NotFound);
            }

            var accessResult = await ValidatePatchUserAccessAsync(user, new PatchStudentDto(), currentUserId, currentUserRole);
            if (accessResult != null)
            {
                return ServiceResult<string>.Failure(accessResult.ErrorMessage ?? "Forbidden", accessResult.ErrorCode);
            }

            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                _logger.LogInformation("Deleting old avatar for user {UserId}.", user.Id);
                await _fileStorageService.DeleteFileAsync(user.AvatarUrl);
            }

            var uploadResult = await _fileStorageService.UploadFileAsync(imageFile, "avatars", true);
            if (!uploadResult.Success)
            {
                return ServiceResult<string>.Failure(uploadResult.ErrorMessage ?? "Internal error", uploadResult.ErrorCode);
            }

            user.AvatarUrl = uploadResult.Data!;
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Ok(user.AvatarUrl);
        }
    }
}
