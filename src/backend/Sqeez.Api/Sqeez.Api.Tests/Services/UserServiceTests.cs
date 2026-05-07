using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Gamification;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.Interfaces;
using Sqeez.Api.Services.UserService;
using BC = BCrypt.Net.BCrypt;

namespace Sqeez.Api.Tests.Services
{
    public class UserServiceTests
    {
        private async Task<SqeezDbContext> GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SqeezDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new SqeezDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return context;
        }

        private UserService CreateService(SqeezDbContext context)
        {
            var mockLogger = new Mock<ILogger<UserService>>();
            var mockedFileService = new Mock<IFileStorageService>();
            return new UserService(context, mockLogger.Object, mockedFileService.Object);
        }

        // ==========================================
        // 1. GET BY ID TESTS
        // ==========================================

        [Fact]
        public async Task GetUserByIdAsync_WhenUserExists_ReturnsCorrectPolymorphicDto()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "TestAdmin", Email = "admin@sqeez.org", Role = UserRole.Admin, PhoneNumber = "123456789" };
            context.Students.Add(admin);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetUserByIdAsync(admin.Id);

            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.Data);

            var adminDto = Assert.IsType<AdminDto>(result.Data);
            Assert.Equal("TestAdmin", adminDto.Username);
            Assert.Equal("123456789", adminDto.PhoneNumber);
        }

        [Fact]
        public async Task GetUserByIdAsync_WhenUserDoesNotExist_ReturnsFailure()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);

            var result = await service.GetUserByIdAsync(999);

            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(ServiceError.NotFound, result.ErrorCode);
        }

        // ==========================================
        // 2. CREATE TESTS
        // ==========================================

        [Fact]
        public async Task CreateUserAsync_WithAdminDto_CreatesAdminInDatabase()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);

            var createDto = new CreateAdminDto
            {
                Username = "NewAdmin",
                Email = "new@sqeez.org",
                Password = "StrongPassword123!",
                Department = "IT",
                PhoneNumber = "+1234567890"
            };

            var result = await service.CreateUserAsync(createDto);

            Assert.Null(result.ErrorMessage);
            var resultDto = Assert.IsType<AdminDto>(result.Data);
            Assert.Equal("IT", resultDto.Department);

            var savedUser = await context.Students.FirstOrDefaultAsync(a => a.Email == "new@sqeez.org");
            var savedAdmin = Assert.IsType<Admin>(savedUser);
            Assert.Equal("+1234567890", savedAdmin.PhoneNumber);
            Assert.Equal(UserRole.Admin, savedAdmin.Role);
            Assert.NotEqual("StrongPassword123!", savedAdmin.PasswordHash);
            Assert.True(BC.Verify("StrongPassword123!", savedAdmin.PasswordHash));
        }

        [Fact]
        public async Task CreateUserAsync_WhenEmailAlreadyExists_ReturnsConflict()
        {
            var context = await GetInMemoryDbContext();
            context.Students.Add(new Student { Username = "Existing", Email = "conflict@sqeez.org", Role = UserRole.Student });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var createDto = new CreateTeacherDto { Username = "Duplicate", Email = "conflict@sqeez.org", Password = "pwd" };

            var result = await service.CreateUserAsync(createDto);

            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(ServiceError.Conflict, result.ErrorCode);
        }

        [Fact]
        public async Task CreateUserAsync_WhenUsernameAlreadyExistsIgnoringCase_ReturnsConflict()
        {
            var context = await GetInMemoryDbContext();
            context.Students.Add(new Student { Username = "ExistingUser", Email = "first@sqeez.org", Role = UserRole.Student });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var createDto = new CreateStudentDto
            {
                Username = "existinguser",
                Email = "second@sqeez.org",
                Password = "StrongPassword123!"
            };

            var result = await service.CreateUserAsync(createDto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Conflict, result.ErrorCode);
            Assert.Contains("Username", result.ErrorMessage);
        }

        // ==========================================
        // 3. PATCH TESTS
        // ==========================================

        [Fact]
        public async Task PatchUserAsync_WhenAdminExists_UpdatesBaseAndDerivedProperties()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "OldName", Email = "old@sqeez.org", Role = UserRole.Admin, Department = "OldDept" };
            context.Students.Add(admin);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchAdminDto { Username = "UpdatedName", Department = "HR", PhoneNumber = "999-999-9999" };

            var result = await service.PatchUserAsync(admin.Id, patchDto);

            Assert.Null(result.ErrorMessage);
            var updatedAdmin = Assert.IsType<AdminDto>(result.Data);

            Assert.Equal("UpdatedName", updatedAdmin.Username);
            Assert.Equal("HR", updatedAdmin.Department);
            Assert.Equal("999-999-9999", updatedAdmin.PhoneNumber);
        }

        // ==========================================
        // 4. ARCHIVE TESTS
        // ==========================================

        [Fact]
        public async Task ArchiveUserAsync_WhenUserExists_SoftDeletesUser()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "ToBeDeleted", Email = "delete@sqeez.org", Role = UserRole.Student };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.ArchiveUserAsync(student.Id);

            Assert.Null(result.ErrorMessage);
            var deletedStudent = await context.Students.FindAsync(student.Id);
            Assert.NotNull(deletedStudent!.ArchivedAt);
        }

        // ==========================================
        // 5. GET ALL TESTS (FILTERS & PAGINATION)
        // ==========================================

        [Fact]
        public async Task GetAllUsersAsync_WhenRoleIsTeacherAndStrictIsFalse_IncludesTeachersAndAdmins()
        {
            var context = await GetInMemoryDbContext();
            context.Students.AddRange(
                new Student { Username = "JustStudent", Role = UserRole.Student },
                new Teacher { Username = "MathTeacher", Role = UserRole.Teacher },
                new Admin { Username = "SystemAdmin", Role = UserRole.Admin }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new UserFilterDto { Role = UserRole.Teacher, StrictRoleOnly = false, PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllUsersAsync(filter);

            Assert.Equal(2, result.Data!.TotalCount);
            Assert.Contains(result.Data.Data, u => u.Username == "MathTeacher");
            Assert.Contains(result.Data.Data, u => u.Username == "SystemAdmin");
            Assert.DoesNotContain(result.Data.Data, u => u.Username == "JustStudent");
        }

        [Fact]
        public async Task GetAllUsersAsync_WhenRoleIsTeacherAndStrictIsTrue_IncludesOnlyTeachers()
        {
            var context = await GetInMemoryDbContext();
            context.Students.AddRange(
                new Student { Username = "JustStudent", Role = UserRole.Student },
                new Teacher { Username = "MathTeacher", Role = UserRole.Teacher },
                new Admin { Username = "SystemAdmin", Role = UserRole.Admin }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new UserFilterDto { Role = UserRole.Teacher, StrictRoleOnly = true, PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllUsersAsync(filter);

            Assert.Equal(1, result.Data!.TotalCount);
            Assert.Equal("MathTeacher", result.Data.Data.First().Username);
        }

        [Fact]
        public async Task GetAllUsersAsync_WithSearchTerm_ReturnsFilteredResults()
        {
            var context = await GetInMemoryDbContext();
            context.Students.AddRange(
                new Student { Username = "JohnDoe", Email = "john@sqeez.org", Role = UserRole.Student },
                new Teacher { Username = "JaneSmith", Email = "jane@sqeez.org", Role = UserRole.Teacher }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new UserFilterDto { SearchTerm = "smith", PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllUsersAsync(filter);

            Assert.Single(result.Data!.Data);
            Assert.Equal("JaneSmith", result.Data.Data.First().Username);
        }

        [Fact]
        public async Task GetAllUsersAsync_WithPagination_ReturnsCorrectPageAndCount()
        {
            var context = await GetInMemoryDbContext();
            // Create 5 students named A through E
            for (int i = 0; i < 5; i++)
            {
                context.Students.Add(new Student
                {
                    Username = $"Student{(char)('A' + i)}",
                    Role = UserRole.Student
                });
            }
            await context.SaveChangesAsync();

            var service = CreateService(context);
            // Get page 2, size 2 (should return StudentC and StudentD)
            var filter = new UserFilterDto { PageNumber = 2, PageSize = 2 };

            var result = await service.GetAllUsersAsync(filter);

            Assert.Equal(5, result.Data!.TotalCount);
            Assert.Equal(2, result.Data.Data.Count());
            Assert.Equal("StudentC", result.Data.Data.First().Username);
        }

        [Fact]
        public async Task GetAllUsersAsync_WithIsOnline_FiltersByLastSeen()
        {
            var context = await GetInMemoryDbContext();
            context.Students.AddRange(
                new Student { Username = "OnlineUser", Role = UserRole.Student, LastSeen = DateTime.UtcNow.AddMinutes(-5) },
                new Student { Username = "OfflineUser", Role = UserRole.Student, LastSeen = DateTime.UtcNow.AddMinutes(-30) }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new UserFilterDto { IsOnline = true, PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllUsersAsync(filter);

            Assert.Single(result.Data!.Data);
            Assert.Equal("OnlineUser", result.Data.Data.First().Username);
        }

        [Fact]
        public async Task GetAllUsersAsync_WithIsArchived_FiltersCorrectly()
        {
            var context = await GetInMemoryDbContext();
            context.Students.AddRange(
                new Student { Username = "ActiveUser", Role = UserRole.Student, ArchivedAt = null },
                new Student { Username = "ArchivedUser", Role = UserRole.Student, ArchivedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // 1. Test IsArchived = true
            var archivedFilter = new UserFilterDto { IsArchived = true, PageNumber = 1, PageSize = 10 };
            var archivedResult = await service.GetAllUsersAsync(archivedFilter);
            Assert.Single(archivedResult.Data!.Data);
            Assert.Equal("ArchivedUser", archivedResult.Data.Data.First().Username);

            // 2. Test IsArchived = false
            var activeFilter = new UserFilterDto { IsArchived = false, PageNumber = 1, PageSize = 10 };
            var activeResult = await service.GetAllUsersAsync(activeFilter);
            Assert.Single(activeResult.Data!.Data);
            Assert.Equal("ActiveUser", activeResult.Data.Data.First().Username);
        }

        [Fact]
        public async Task GetAllUsersAsync_WithDepartment_OnlyReturnsMatchingTeachersAndAdmins()
        {
            var context = await GetInMemoryDbContext();
            context.Students.AddRange(
                new Student { Username = "NormalStudent", Role = UserRole.Student },
                new Teacher { Username = "MathTeacher", Role = UserRole.Teacher, Department = "Math" },
                new Teacher { Username = "ScienceTeacher", Role = UserRole.Teacher, Department = "Science" },
                new Admin { Username = "MathAdmin", Role = UserRole.Admin, Department = "Math" }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new UserFilterDto { Department = "Math", PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllUsersAsync(filter);

            Assert.Equal(2, result.Data!.TotalCount);
            Assert.Contains(result.Data.Data, u => u.Username == "MathTeacher");
            Assert.Contains(result.Data.Data, u => u.Username == "MathAdmin");
            Assert.DoesNotContain(result.Data.Data, u => u.Username == "ScienceTeacher");
        }

        [Fact]
        public async Task GetAllUsersAsync_WithSchoolClassId_FiltersCorrectly()
        {
            var context = await GetInMemoryDbContext();
            context.Students.AddRange(
                new Student { Username = "Class1Student", Role = UserRole.Student, SchoolClassId = 1 },
                new Teacher { Username = "Class1Teacher", Role = UserRole.Teacher, SchoolClassId = 1 },
                new Student { Username = "Class2Student", Role = UserRole.Student, SchoolClassId = 2 }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new UserFilterDto { SchoolClassId = 1, PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllUsersAsync(filter);

            Assert.Equal(2, result.Data!.TotalCount);
            Assert.DoesNotContain(result.Data.Data, u => u.Username == "Class2Student");
        }

        // ==========================================
        // 6. DETAILED USER PROFILE TESTS
        // ==========================================

        [Fact]
        public async Task GetDetailedUserByIdAsync_WhenUserExists_ReturnsDetailedDtoWithAllRelations()
        {
            var context = await GetInMemoryDbContext();

            var schoolClass = new SchoolClass { Name = "Class 1A", AcademicYear = "2025/2026" };
            var subject = new Subject { Name = "Advanced Mathematics" };
            var badge = new Badge { Name = "Math Whiz", IconUrl = "/icons/math.png" };

            context.SchoolClasses.Add(schoolClass);
            context.Subjects.Add(subject);
            context.Badges.Add(badge);
            await context.SaveChangesAsync();

            var student = new Student
            {
                Username = "DetailedStudent",
                Role = UserRole.Student,
                SchoolClassId = schoolClass.Id,
                Enrollments = new List<Enrollment>
                {
                    new Enrollment
                    {
                        SubjectId = subject.Id,
                        Mark = 95,
                        EnrolledAt = DateTime.UtcNow.AddMonths(-1)
                    }
                },
                StudentBadges = new List<StudentBadge>
                {
                    new StudentBadge
                    {
                        BadgeId = badge.Id,
                        EarnedAt = DateTime.UtcNow.AddDays(-5)
                    }
                }
            };

            context.Students.Add(student);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetDetailedUserByIdAsync(student.Id);

            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.Data);

            Assert.Equal("DetailedStudent", result.Data.Username);

            Assert.NotNull(result.Data.SchoolClassDetails);
            Assert.Equal("Class 1A", result.Data.SchoolClassDetails.Name);

            Assert.Single(result.Data.Enrollments);
            var enrollmentDto = result.Data.Enrollments.First();
            Assert.Equal("Advanced Mathematics", enrollmentDto.SubjectName);
            Assert.Equal(95, enrollmentDto.Mark);

            Assert.Single(result.Data.Badges);
            var badgeDto = result.Data.Badges.First();
            Assert.Equal("Math Whiz", badgeDto.Name);
            Assert.Equal("/icons/math.png", badgeDto.IconUrl);
        }

        [Fact]
        public async Task GetDetailedUserByIdAsync_WhenUserHasNoRelations_ReturnsDetailedDtoWithEmptyCollections()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student
            {
                Username = "LonelyStudent",
                Role = UserRole.Student
            };

            context.Students.Add(student);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetDetailedUserByIdAsync(student.Id);

            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.Data);

            Assert.Equal("LonelyStudent", result.Data.Username);

            Assert.Null(result.Data.SchoolClassDetails);
            Assert.Empty(result.Data.Enrollments);
            Assert.Empty(result.Data.Badges);
        }

        [Fact]
        public async Task GetDetailedUserByIdAsync_WhenUserDoesNotExist_ReturnsFailure()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);

            var result = await service.GetDetailedUserByIdAsync(999);

            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(ServiceError.NotFound, result.ErrorCode);
            Assert.Equal("User not found.", result.ErrorMessage);
        }

        private Mock<IFormFile> CreateMockFile(string fileName)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(1024);
            return mockFile;
        }

        [Fact]
        public async Task UploadAvatarAsync_WithValidImage_UploadsAndSavesUrl()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "TestUser", Role = UserRole.Student };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var mockFileService = new Mock<IFileStorageService>();
            mockFileService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "avatars", true))
                .ReturnsAsync(ServiceResult<string>.Ok("/avatars/new-avatar.png"));

            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object);
            var mockFile = CreateMockFile("profile.png");

            var result = await service.UploadAvatarAsync(student.Id, mockFile.Object);

            Assert.True(result.Success);
            Assert.Equal("/avatars/new-avatar.png", result.Data);

            var updatedUser = await context.Students.FindAsync(student.Id);
            Assert.Equal("/avatars/new-avatar.png", updatedUser!.AvatarUrl);
        }

        [Fact]
        public async Task UploadAvatarAsync_WhenUserHasExistingAvatar_DeletesOldAvatar()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "TestUser", Role = UserRole.Student, AvatarUrl = "/avatars/old.png" };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var mockFileService = new Mock<IFileStorageService>();
            mockFileService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "avatars", true))
                .ReturnsAsync(ServiceResult<string>.Ok("/avatars/new.png"));

            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object);
            var mockFile = CreateMockFile("new.png");

            await service.UploadAvatarAsync(student.Id, mockFile.Object);

            mockFileService.Verify(s => s.DeleteFileAsync("/avatars/old.png"), Times.Once);
        }

        [Fact]
        public async Task UploadAvatarAsync_WithInvalidExtension_ReturnsValidationFailure()
        {
            var context = await GetInMemoryDbContext();
            var mockFileService = new Mock<IFileStorageService>();
            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object);

            var mockFile = CreateMockFile("document.pdf");

            var result = await service.UploadAvatarAsync(1, mockFile.Object);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.ValidationFailed, result.ErrorCode);
            Assert.Contains("must be an image file", result.ErrorMessage);
        }

        [Fact]
        public async Task UploadAvatarAsync_WhenUserDoesNotExist_ReturnsNotFound()
        {
            var context = await GetInMemoryDbContext();
            var mockFileService = new Mock<IFileStorageService>();
            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object);

            var mockFile = CreateMockFile("profile.jpg");

            var result = await service.UploadAvatarAsync(999, mockFile.Object);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.NotFound, result.ErrorCode);
        }

        [Fact]
        public async Task CreateStudentsBulkAsync_WhenStudentsProvided_SkipsExistingAndCreatesNew()
        {
            var context = await GetInMemoryDbContext();
            var existingStudent = new Student { Username = "existing", Email = "exist@sqeez.org", Role = UserRole.Student, FirstName = "E", LastName = "S" };
            context.Students.Add(existingStudent);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var newStudents = new List<Student>
            {
                new Student { Username = "dupe", Email = "exist@sqeez.org", PasswordHash = "hash", FirstName = "D", LastName = "U" }, // Duplicate by Email
                new Student { Username = "newstudent", Email = "new@sqeez.org", PasswordHash = "hash", FirstName = "N", LastName = "W" } // New
            };

            var result = await service.CreateStudentsBulkAsync(newStudents);

            Assert.True(result.Success);
            Assert.Single(result.Data!.Created);
            Assert.Equal("newstudent", result.Data.Created.First().Username);
            Assert.Single(result.Data.SkippedMessages);
            
            var dbStudent = await context.Students.FirstOrDefaultAsync(s => s.Email == "new@sqeez.org");
            Assert.NotNull(dbStudent);
            Assert.Equal(UserRole.Student, dbStudent.Role);
            Assert.NotNull(dbStudent.PasswordHash);
        }

        [Fact]
        public async Task GetAllUsersAsync_WithPhoneNumber_ReturnsOnlyMatchingAdmins()
        {
            var context = await GetInMemoryDbContext();
            context.Students.AddRange(
                new Admin { Username = "MatchingAdmin", Role = UserRole.Admin, PhoneNumber = "001234567890" },
                new Admin { Username = "OtherAdmin", Role = UserRole.Admin, PhoneNumber = "009876543210" },
                new Teacher { Username = "Teacher", Role = UserRole.Teacher, Department = "001234567890" }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new UserFilterDto { PhoneNumber = "001234567890", PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllUsersAsync(filter);

            Assert.True(result.Success);
            Assert.Single(result.Data!.Data);
            Assert.Equal("MatchingAdmin", result.Data.Data.First().Username);
            Assert.IsType<AdminDto>(result.Data.Data.First());
        }

        [Fact]
        public async Task PatchUserAsync_WhenTeacherManagedClassIdIsZero_RemovesManagedClass()
        {
            var context = await GetInMemoryDbContext();
            var schoolClass = new SchoolClass { Name = "Managed" };
            var teacher = new Teacher { Username = "Teacher", Role = UserRole.Teacher, ManagedClass = schoolClass };
            context.Teachers.Add(teacher);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchTeacherDto { ManagedClassId = 0 };

            var result = await service.PatchUserAsync(teacher.Id, patchDto);

            Assert.True(result.Success);
            var teacherDto = Assert.IsType<TeacherDto>(result.Data);
            Assert.Null(teacherDto.ManagedClassId);

            var dbTeacher = await context.Teachers.FindAsync(teacher.Id);
            Assert.Null(dbTeacher!.ManagedClassId);
        }

        [Fact]
        public async Task PatchUserAsync_WhenTeacherAlreadyStudentOfManagedClass_ReturnsValidationFailed()
        {
            var context = await GetInMemoryDbContext();
            var schoolClass = new SchoolClass { Name = "Student class" };
            var teacher = new Teacher
            {
                Username = "TeacherStudent",
                Role = UserRole.Teacher,
                SchoolClass = schoolClass
            };

            context.SchoolClasses.Add(schoolClass);
            context.Teachers.Add(teacher);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchTeacherDto { ManagedClassId = schoolClass.Id };

            var result = await service.PatchUserAsync(teacher.Id, patchDto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.ValidationFailed, result.ErrorCode);
            Assert.Contains("already assigned as a student", result.ErrorMessage);

            var dbTeacher = await context.Teachers.FindAsync(teacher.Id);
            Assert.Null(dbTeacher!.ManagedClassId);
        }

        [Fact]
        public async Task PatchUserAsync_WhenAssigningTeacherAsStudentToManagedClass_ReturnsValidationFailed()
        {
            var context = await GetInMemoryDbContext();
            var schoolClass = new SchoolClass { Name = "Managed class" };
            var teacher = new Teacher
            {
                Username = "TeacherManager",
                Role = UserRole.Teacher,
                ManagedClass = schoolClass
            };

            context.SchoolClasses.Add(schoolClass);
            context.Teachers.Add(teacher);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchTeacherDto { SchoolClassId = schoolClass.Id };

            var result = await service.PatchUserAsync(teacher.Id, patchDto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.ValidationFailed, result.ErrorCode);
            Assert.Contains("class they manage", result.ErrorMessage);

            var dbTeacher = await context.Teachers.FindAsync(teacher.Id);
            Assert.Null(dbTeacher!.SchoolClassId);
        }

        [Fact]
        public async Task UploadAvatarAsync_WhenStorageUploadFails_DoesNotChangeAvatar()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "TestUser", Role = UserRole.Student, AvatarUrl = "/avatars/current.png" };
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var mockFileService = new Mock<IFileStorageService>();
            mockFileService.Setup(s => s.DeleteFileAsync("/avatars/current.png"))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));
            mockFileService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "avatars", true))
                .ReturnsAsync(ServiceResult<string>.Failure("Upload failed.", ServiceError.InternalError));

            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object);
            var mockFile = CreateMockFile("profile.png");

            var result = await service.UploadAvatarAsync(student.Id, mockFile.Object);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.InternalError, result.ErrorCode);

            var dbStudent = await context.Students.FindAsync(student.Id);
            Assert.Equal("/avatars/current.png", dbStudent!.AvatarUrl);
        }

        [Fact]
        public async Task CreateStudentsBulkAsync_WhenIncomingUsernameRepeats_SkipsSecondStudent()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);
            var students = new List<Student>
            {
                new Student { Username = "duplicate", Email = "first@sqeez.org", PasswordHash = "hash", FirstName = "First", LastName = "Student" },
                new Student { Username = "Duplicate", Email = "second@sqeez.org", PasswordHash = "hash", FirstName = "Second", LastName = "Student" }
            };

            var result = await service.CreateStudentsBulkAsync(students);

            Assert.True(result.Success);
            Assert.Single(result.Data!.Created);
            Assert.Single(result.Data.SkippedMessages);
            Assert.Equal(1, await context.Students.CountAsync());
            Assert.Equal("duplicate", result.Data.Created.First().Username);
        }
    }
}
