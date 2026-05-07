using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Import;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace Sqeez.Api.Services.UserService
{
    public class UserService : BaseService<UserService>, IUserService
    {
        private readonly IFileStorageService _fileStorageService;

        public UserService(
            SqeezDbContext context,
            ILogger<UserService> logger,
            IFileStorageService fileStorageService) : base(context, logger)
        {
            _fileStorageService = fileStorageService;
        }

        private static StudentDto MapUserToDto(Student user)
        {
            if (user == null) return null!;

            return user switch
            {
                Admin a => new AdminDto
                {
                    Id = a.Id,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Username = a.Username,
                    Email = a.Email,
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
                    Email = t.Email,
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
                    Email = user.Email,
                    CurrentXP = user.CurrentXP,
                    Role = user.Role,
                    LastSeen = user.LastSeen,
                    AvatarUrl = user.AvatarUrl,
                    SchoolClassId = user.SchoolClassId
                }
            };
        }

        public async Task<ServiceResult<PagedResponse<StudentDto>>> GetAllUsersAsync(UserFilterDto filter)
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
                query = query.Where(u => u.Username.ToLower().Contains(searchTerm) ||
                                         u.Email.ToLower().Contains(searchTerm));
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

            var mappedUsers = users.Select(MapUserToDto).ToList();

            var response = new PagedResponse<StudentDto>
            {
                Data = mappedUsers,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ServiceResult<PagedResponse<StudentDto>>.Ok(response);
        }

        public async Task<ServiceResult<StudentDto>> GetUserByIdAsync(long id)
        {
            var user = await _context.Students.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return ServiceResult<StudentDto>.Failure("User not found.", ServiceError.NotFound);

            return ServiceResult<StudentDto>.Ok(MapUserToDto(user));
        }

        public async Task<ServiceResult<DetailedUserDto>> GetDetailedUserByIdAsync(long id)
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

            var baseDto = MapUserToDto(user);

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

                // Takes only the last 5 earned.
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

        public async Task<ServiceResult<StudentDto>> CreateUserAsync(CreateStudentDto dto)
        {
            var email = dto.Email.Trim().ToLower();
            var username = dto.Username.Trim();
            var usernameLower = username.ToLower();

            if (await _context.Students.AnyAsync(u => u.Email == email))
                return ServiceResult<StudentDto>.Failure("Email already in use.", ServiceError.Conflict);

            if (await _context.Students.AnyAsync(u => u.Username.ToLower() == usernameLower))
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
                    string reason = isDuplicateEmail ? "Email already exists" : "Derived username already exists";
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

                bulkResult.Created = validStudentsToInsert.Select(MapUserToDto).ToList();
            }

            return ServiceResult<BulkOperationResult<StudentDto>>.Ok(bulkResult);
        }

        public async Task<ServiceResult<bool>> ArchiveUserAsync(long id)
        {
            var user = await _context.Students.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return ServiceResult<bool>.Failure("User not found.", ServiceError.NotFound);

            user.ArchivedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Delete must be with file deletion

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<StudentDto>> PatchUserAsync(long id, PatchStudentDto dto)
        {
            var user = await _context.Students.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return ServiceResult<StudentDto>.Failure("User not found.", ServiceError.NotFound);

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

        public async Task<ServiceResult<string>> UploadAvatarAsync(long userId, IFormFile imageFile)
        {
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            if (!allowedImageExtensions.Contains(extension))
            {
                return ServiceResult<string>.Failure("Avatars must be an image file (.jpg, .png, .gif).", ServiceError.ValidationFailed);
            }

            var user = await _context.Students.FindAsync(userId);
            if (user == null)
            {
                return ServiceResult<string>.Failure("User not found.", ServiceError.NotFound);
            }

            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                _logger.LogInformation("Deleting old avatar for user {UserId}.", userId);
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
