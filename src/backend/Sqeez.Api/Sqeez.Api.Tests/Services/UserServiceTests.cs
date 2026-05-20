using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Gamification;
using Sqeez.Api.Models.Media;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.Interfaces;
using Sqeez.Api.Services.UserService;
using BC = BCrypt.Net.BCrypt;

namespace Sqeez.Api.Tests.Services
{
    public class UserServiceTests
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

        private UserService CreateService(SqeezDbContext context)
        {
            var mockLogger = new Mock<ILogger<UserService>>();
            var mockedFileService = new Mock<IFileStorageService>();
            return new UserService(context, mockLogger.Object, mockedFileService.Object, CreateConfiguration());
        }

        private static IConfiguration CreateConfiguration()
        {
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(configuration => configuration["SUPER_USER_EMAIL"]).Returns(SuperUserEmail);
            return mockConfiguration.Object;
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
            var result = await service.GetUserByIdAsync(admin.Id, 0, "Admin");

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

            var result = await service.GetUserByIdAsync(999, 0, "Admin");

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
        public async Task CreateUserAsync_WhenEmailBelongsToArchivedUser_ReturnsEmailConflict()
        {
            var context = await GetInMemoryDbContext();
            context.Students.Add(new Student
            {
                Username = "Archived",
                Email = "archived@sqeez.org",
                Role = UserRole.Student,
                ArchivedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var createDto = new CreateTeacherDto { Username = "Replacement", Email = "archived@sqeez.org", Password = "pwd" };

            var result = await service.CreateUserAsync(createDto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Conflict, result.ErrorCode);
            Assert.Equal("Email already in use.", result.ErrorMessage);
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
            var admin = new Admin { Username = "OldName", Email = SuperUserEmail, Role = UserRole.Admin, Department = "OldDept" };
            context.Students.Add(admin);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchAdminDto { Username = "UpdatedName", Department = "HR", PhoneNumber = "999-999-9999" };

            var result = await service.PatchUserAsync(admin.Id, patchDto, admin.Id, "Admin");

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
            var result = await service.ArchiveUserAsync(student.Id, student.Id, "Student");

            Assert.Null(result.ErrorMessage);
            var deletedStudent = await context.Students.FindAsync(student.Id);
            Assert.NotNull(deletedStudent!.ArchivedAt);
        }

        [Fact]
        public async Task ArchiveUserAsync_WhenAdminArchivesStudent_Succeeds()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var student = new Student { Username = "Student", Email = "student@sqeez.org", Role = UserRole.Student };
            context.Students.AddRange(admin, student);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.ArchiveUserAsync(student.Id, admin.Id, "Admin");

            Assert.True(result.Success);
            Assert.NotNull((await context.Students.FindAsync(student.Id))!.ArchivedAt);
        }

        [Fact]
        public async Task ArchiveUserAsync_WhenAdminArchivesAdmin_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var targetAdmin = new Admin { Username = "TargetAdmin", Email = "target-admin@sqeez.org", Role = UserRole.Admin };
            context.Students.AddRange(admin, targetAdmin);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.ArchiveUserAsync(targetAdmin.Id, admin.Id, "Admin");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
            Assert.Null((await context.Students.FindAsync(targetAdmin.Id))!.ArchivedAt);
        }

        [Fact]
        public async Task ArchiveUserAsync_WhenSuperAdminArchivesAdmin_Succeeds()
        {
            var context = await GetInMemoryDbContext();
            var superAdmin = new Admin { Username = "SuperAdmin", Email = SuperUserEmail, Role = UserRole.Admin };
            var targetAdmin = new Admin { Username = "TargetAdmin", Email = "target-admin@sqeez.org", Role = UserRole.Admin };
            context.Students.AddRange(superAdmin, targetAdmin);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.ArchiveUserAsync(targetAdmin.Id, superAdmin.Id, "Admin");

            Assert.True(result.Success);
            Assert.NotNull((await context.Students.FindAsync(targetAdmin.Id))!.ArchivedAt);
        }

        [Fact]
        public async Task ArchiveUserAsync_WhenSuperAdminArchivesSelf_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var superAdmin = new Admin { Username = "SuperAdmin", Email = SuperUserEmail, Role = UserRole.Admin };
            context.Students.Add(superAdmin);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.ArchiveUserAsync(superAdmin.Id, superAdmin.Id, "Admin");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
            Assert.Null((await context.Students.FindAsync(superAdmin.Id))!.ArchivedAt);
        }

        [Fact]
        public async Task RestoreUserAsync_WhenAdminRestoresStudent_Succeeds()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var student = new Student { Username = "Student", Email = "student@sqeez.org", Role = UserRole.Student, ArchivedAt = DateTime.UtcNow };
            context.Students.AddRange(admin, student);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.RestoreUserAsync(student.Id, admin.Id, "Admin");

            Assert.True(result.Success);
            Assert.Null((await context.Students.FindAsync(student.Id))!.ArchivedAt);
        }

        [Fact]
        public async Task RestoreUserAsync_WhenAdminRestoresAdmin_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var targetAdmin = new Admin { Username = "TargetAdmin", Email = "target-admin@sqeez.org", Role = UserRole.Admin, ArchivedAt = DateTime.UtcNow };
            context.Students.AddRange(admin, targetAdmin);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.RestoreUserAsync(targetAdmin.Id, admin.Id, "Admin");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
            Assert.NotNull((await context.Students.FindAsync(targetAdmin.Id))!.ArchivedAt);
        }

        [Fact]
        public async Task RestoreUserAsync_WhenSuperAdminRestoresAdmin_Succeeds()
        {
            var context = await GetInMemoryDbContext();
            var superAdmin = new Admin { Username = "SuperAdmin", Email = SuperUserEmail, Role = UserRole.Admin };
            var targetAdmin = new Admin { Username = "TargetAdmin", Email = "target-admin@sqeez.org", Role = UserRole.Admin, ArchivedAt = DateTime.UtcNow };
            context.Students.AddRange(superAdmin, targetAdmin);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.RestoreUserAsync(targetAdmin.Id, superAdmin.Id, "Admin");

            Assert.True(result.Success);
            Assert.Null((await context.Students.FindAsync(targetAdmin.Id))!.ArchivedAt);
        }

        [Fact]
        public async Task DeleteUserAsync_WhenUserIsNotArchived_ReturnsValidationFailed()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var student = new Student { Username = "Student", Email = "student@sqeez.org", Role = UserRole.Student };
            context.Students.AddRange(admin, student);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.DeleteUserAsync(student.Id, admin.Id, "Admin");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.ValidationFailed, result.ErrorCode);
            Assert.NotNull(await context.Students.FindAsync(student.Id));
        }

        [Fact]
        public async Task DeleteUserAsync_WhenTargetIsAdmin_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var targetAdmin = new Admin { Username = "TargetAdmin", Email = "target-admin@sqeez.org", Role = UserRole.Admin, ArchivedAt = DateTime.UtcNow };
            context.Students.AddRange(admin, targetAdmin);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.DeleteUserAsync(targetAdmin.Id, admin.Id, "Admin");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
            Assert.NotNull(await context.Students.FindAsync(targetAdmin.Id));
        }

        [Fact]
        public async Task DeleteUserAsync_WhenArchivedStudentHasHistory_DeletesStudentAndHistory()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var student = new Student
            {
                Username = "Student",
                Email = "student@sqeez.org",
                Role = UserRole.Student,
                ArchivedAt = DateTime.UtcNow,
                AvatarUrl = "/avatars/student.png"
            };
            var subject = new Subject { Name = "Math", Code = "MATH", StartDate = DateTime.UtcNow };
            var quiz = new Quiz { Title = "Quiz", Description = "Desc", Subject = subject };
            var question = new QuizQuestion { Title = "Q", Difficulty = 1, Quiz = quiz };
            var enrollment = new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow };
            var attempt = new QuizAttempt { Enrollment = enrollment, Quiz = quiz, Status = AttemptStatus.Completed };
            var response = new QuizQuestionResponse { QuizAttempt = attempt, QuizQuestion = question, ResponseTimeMs = 100 };
            var badge = new Badge { Name = "Badge", Description = "Desc" };
            var studentBadge = new StudentBadge { Student = student, Badge = badge, EarnedAt = DateTime.UtcNow };
            var session = new UserSession { User = student, RefreshToken = "refresh", ExpiresAt = DateTime.UtcNow.AddDays(1) };

            context.Students.AddRange(admin, student);
            context.Subjects.Add(subject);
            context.Quizzes.Add(quiz);
            context.QuizQuestions.Add(question);
            context.Enrollments.Add(enrollment);
            context.QuizAttempts.Add(attempt);
            context.QuizQuestionResponses.Add(response);
            context.Badges.Add(badge);
            context.StudentBadges.Add(studentBadge);
            context.UserSessions.Add(session);
            await context.SaveChangesAsync();

            var mockFileStorage = new Mock<IFileStorageService>();
            mockFileStorage
                .Setup(storage => storage.DeleteFileAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));
            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileStorage.Object, CreateConfiguration());

            var result = await service.DeleteUserAsync(student.Id, admin.Id, "Admin");

            Assert.True(result.Success);
            Assert.Null(await context.Students.FindAsync(student.Id));
            Assert.Empty(context.Enrollments.Where(e => e.StudentId == student.Id));
            Assert.Empty(context.QuizAttempts.Where(a => a.EnrollmentId == enrollment.Id));
            Assert.Empty(context.QuizQuestionResponses.Where(r => r.QuizAttemptId == attempt.Id));
            Assert.Empty(context.StudentBadges.Where(sb => sb.StudentId == student.Id));
            Assert.Empty(context.UserSessions.Where(s => s.UserId == student.Id));
            mockFileStorage.Verify(storage => storage.DeleteFileAsync("/avatars/student.png"), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_WhenArchivedTeacherOwnsMediaWithoutReplacement_ReturnsConflict()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var teacher = new Teacher
            {
                Username = "Teacher",
                Email = "teacher@sqeez.org",
                Role = UserRole.Teacher,
                ArchivedAt = DateTime.UtcNow
            };
            var subject = new Subject { Name = "Math", Code = "MATH", StartDate = DateTime.UtcNow, Teacher = teacher };
            var quiz = new Quiz { Title = "Quiz", Description = "Desc", Subject = subject };
            var mediaAsset = new MediaAsset
            {
                Owner = teacher,
                LocationUrl = "/secure/media/teacher.png",
                MimeType = MediaType.Image
            };
            var question = new QuizQuestion { Title = "Q", Difficulty = 1, Quiz = quiz, Media = mediaAsset };
            var option = new QuizOption { Text = "A", QuizQuestion = question, Media = mediaAsset };

            context.Students.AddRange(admin, teacher);
            context.Subjects.Add(subject);
            context.Quizzes.Add(quiz);
            context.MediaAssets.Add(mediaAsset);
            context.QuizQuestions.Add(question);
            context.QuizOptions.Add(option);
            await context.SaveChangesAsync();

            var questionId = question.Id;
            var optionId = option.Id;
            var mockFileStorage = new Mock<IFileStorageService>();
            mockFileStorage
                .Setup(storage => storage.DeleteFileAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));
            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileStorage.Object, CreateConfiguration());

            var result = await service.DeleteUserAsync(teacher.Id, admin.Id, "Admin");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Conflict, result.ErrorCode);
            Assert.NotNull(await context.Students.FindAsync(teacher.Id));
            Assert.Equal(teacher.Id, (await context.MediaAssets.FindAsync(mediaAsset.Id))!.OwnerId);
            Assert.Equal(mediaAsset.Id, (await context.QuizQuestions.FindAsync(questionId))!.MediaAssetId);
            Assert.Equal(mediaAsset.Id, (await context.QuizOptions.FindAsync(optionId))!.MediaAssetId);
            mockFileStorage.Verify(storage => storage.DeleteFileAsync("/secure/media/teacher.png"), Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_WhenArchivedTeacherOwnsMediaWithReplacement_TransfersMediaAndKeepsReferences()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var replacementTeacher = new Teacher { Username = "Replacement", Email = "replacement@sqeez.org", Role = UserRole.Teacher };
            var teacher = new Teacher
            {
                Username = "Teacher",
                Email = "teacher@sqeez.org",
                Role = UserRole.Teacher,
                ArchivedAt = DateTime.UtcNow
            };
            var subject = new Subject { Name = "Math", Code = "MATH", StartDate = DateTime.UtcNow, Teacher = teacher };
            var quiz = new Quiz { Title = "Quiz", Description = "Desc", Subject = subject };
            var mediaAsset = new MediaAsset
            {
                Owner = teacher,
                LocationUrl = "/secure/media/teacher.png",
                MimeType = MediaType.Image
            };
            var question = new QuizQuestion { Title = "Q", Difficulty = 1, Quiz = quiz, Media = mediaAsset };
            var option = new QuizOption { Text = "A", QuizQuestion = question, Media = mediaAsset };

            context.Students.AddRange(admin, replacementTeacher, teacher);
            context.Subjects.Add(subject);
            context.Quizzes.Add(quiz);
            context.MediaAssets.Add(mediaAsset);
            context.QuizQuestions.Add(question);
            context.QuizOptions.Add(option);
            await context.SaveChangesAsync();

            var questionId = question.Id;
            var optionId = option.Id;
            var mockFileStorage = new Mock<IFileStorageService>();
            mockFileStorage
                .Setup(storage => storage.DeleteFileAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));
            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileStorage.Object, CreateConfiguration());

            var result = await service.DeleteUserAsync(teacher.Id, admin.Id, "Admin", replacementTeacher.Id);

            Assert.True(result.Success);
            Assert.Null(await context.Students.FindAsync(teacher.Id));
            Assert.Null((await context.Subjects.FindAsync(subject.Id))!.TeacherId);
            Assert.Empty(context.MediaAssets.Where(media => media.OwnerId == teacher.Id));
            Assert.Equal(replacementTeacher.Id, (await context.MediaAssets.FindAsync(mediaAsset.Id))!.OwnerId);
            Assert.Equal(mediaAsset.Id, (await context.QuizQuestions.FindAsync(questionId))!.MediaAssetId);
            Assert.Equal(mediaAsset.Id, (await context.QuizOptions.FindAsync(optionId))!.MediaAssetId);
            mockFileStorage.Verify(storage => storage.DeleteFileAsync("/secure/media/teacher.png"), Times.Never);
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

            var result = await service.GetAllUsersAsync(filter, 0, "Admin");

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

            var result = await service.GetAllUsersAsync(filter, 0, "Admin");

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

            var result = await service.GetAllUsersAsync(filter, 0, "Admin");

            Assert.Single(result.Data!.Data);
            Assert.Equal("JaneSmith", result.Data.Data.First().Username);
        }

        [Fact]
        public async Task PatchUserAsync_WhenNormalAdminPatchesOtherAdmin_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var currentAdmin = new Admin { Username = "CurrentAdmin", Email = "current-admin@sqeez.org", Role = UserRole.Admin };
            var targetAdmin = new Admin { Username = "TargetAdmin", Email = "target-admin@sqeez.org", Role = UserRole.Admin };
            context.Admins.AddRange(currentAdmin, targetAdmin);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchAdminDto { PhoneNumber = "001234567890" };

            var result = await service.PatchUserAsync(targetAdmin.Id, patchDto, currentAdmin.Id, "Admin");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task PatchUserAsync_WhenNormalAdminPatchesSuperAdmin_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var currentAdmin = new Admin { Username = "CurrentAdmin", Email = "current-admin@sqeez.org", Role = UserRole.Admin };
            var superAdmin = new Admin { Username = "SuperAdmin", Email = SuperUserEmail, Role = UserRole.Admin };
            context.Admins.AddRange(currentAdmin, superAdmin);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchStudentDto { Username = "ChangedSuperAdmin" };

            var result = await service.PatchUserAsync(superAdmin.Id, patchDto, currentAdmin.Id, "Admin");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task PatchUserAsync_WhenSuperAdminPatchesOtherAdmin_Succeeds()
        {
            var context = await GetInMemoryDbContext();
            var superAdmin = new Admin { Username = "SuperAdmin", Email = SuperUserEmail, Role = UserRole.Admin };
            var targetAdmin = new Admin { Username = "TargetAdmin", Email = "target-admin@sqeez.org", Role = UserRole.Admin };
            context.Admins.AddRange(superAdmin, targetAdmin);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchAdminDto { PhoneNumber = "001234567890" };

            var result = await service.PatchUserAsync(targetAdmin.Id, patchDto, superAdmin.Id, "Admin");

            Assert.True(result.Success, result.ErrorMessage);
            var adminDto = Assert.IsType<AdminDto>(result.Data);
            Assert.Equal("001234567890", adminDto.PhoneNumber);
        }

        [Fact]
        public async Task PatchUserAsync_WhenNormalAdminPatchesOwnRoleSpecificData_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            context.Admins.Add(admin);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchAdminDto { PhoneNumber = "001234567890" };

            var result = await service.PatchUserAsync(admin.Id, patchDto, admin.Id, "Admin");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task PatchUserAsync_WhenNormalAdminPatchesOwnBasicData_Succeeds()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            context.Admins.Add(admin);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchStudentDto { Username = "UpdatedAdmin" };

            var result = await service.PatchUserAsync(admin.Id, patchDto, admin.Id, "Admin");

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal("UpdatedAdmin", result.Data!.Username);
        }

        [Fact]
        public async Task GetAllUsersAsync_AsStudent_HidesOtherStudentEmailsButShowsOwnAndAdminEmails()
        {
            var context = await GetInMemoryDbContext();
            var currentStudent = new Student { Username = "CurrentStudent", Email = "current@sqeez.org", Role = UserRole.Student };
            var otherStudent = new Student { Username = "OtherStudent", Email = "other@sqeez.org", Role = UserRole.Student };
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            context.Students.AddRange(currentStudent, otherStudent, admin);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new UserFilterDto { PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllUsersAsync(filter, currentStudent.Id, "Student");

            Assert.True(result.Success);
            Assert.Equal("current@sqeez.org", result.Data!.Data.Single(user => user.Id == currentStudent.Id).Email);
            Assert.Equal("o***@s***.org", result.Data.Data.Single(user => user.Id == otherStudent.Id).Email);
            Assert.Equal("admin@sqeez.org", result.Data.Data.Single(user => user.Id == admin.Id).Email);
        }

        [Fact]
        public async Task GetAllUsersAsync_AsStudent_DoesNotSearchHiddenEmails()
        {
            var context = await GetInMemoryDbContext();
            var currentStudent = new Student { Username = "CurrentStudent", Email = "current@sqeez.org", Role = UserRole.Student };
            var otherStudent = new Student { Username = "VisibleUsername", Email = "hidden-match@sqeez.org", Role = UserRole.Student };
            context.Students.AddRange(currentStudent, otherStudent);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new UserFilterDto { SearchTerm = "hidden-match", PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllUsersAsync(filter, currentStudent.Id, "Student");

            Assert.True(result.Success);
            Assert.Empty(result.Data!.Data);
        }

        [Fact]
        public async Task GetUserByIdAsync_AsStudent_ShowsTeacherEmailWhenTeacherOwnsStudentsSubject()
        {
            var context = await GetInMemoryDbContext();
            var teacher = new Teacher { Username = "Teacher", Email = "teacher@sqeez.org", Role = UserRole.Teacher };
            var student = new Student { Username = "Student", Email = "student@sqeez.org", Role = UserRole.Student };
            var subject = new Subject { Name = "Math", Code = "MATH", StartDate = DateTime.UtcNow, Teacher = teacher };
            var enrollment = new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow };
            context.Students.AddRange(teacher, student);
            context.Subjects.Add(subject);
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetUserByIdAsync(teacher.Id, student.Id, "Student");

            Assert.True(result.Success);
            Assert.Equal("teacher@sqeez.org", result.Data!.Email);
        }

        [Fact]
        public async Task GetUserByIdAsync_AsTeacher_ShowsStudentEmailWhenTeacherOwnsStudentsSubject()
        {
            var context = await GetInMemoryDbContext();
            var teacher = new Teacher { Username = "Teacher", Email = "teacher@sqeez.org", Role = UserRole.Teacher };
            var student = new Student { Username = "Student", Email = "student@sqeez.org", Role = UserRole.Student };
            var subject = new Subject { Name = "Math", Code = "MATH", StartDate = DateTime.UtcNow, Teacher = teacher };
            var enrollment = new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow };
            context.Students.AddRange(teacher, student);
            context.Subjects.Add(subject);
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetUserByIdAsync(student.Id, teacher.Id, "Teacher");

            Assert.True(result.Success);
            Assert.Equal("student@sqeez.org", result.Data!.Email);
        }

        [Fact]
        public async Task GetDetailedUserByIdAsync_AsStudent_HidesOtherStudentEmail()
        {
            var context = await GetInMemoryDbContext();
            var currentStudent = new Student { Username = "CurrentStudent", Email = "current@sqeez.org", Role = UserRole.Student };
            var otherStudent = new Student { Username = "OtherStudent", Email = "other@sqeez.org", Role = UserRole.Student };
            context.Students.AddRange(currentStudent, otherStudent);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetDetailedUserByIdAsync(otherStudent.Id, currentStudent.Id, "Student");

            Assert.True(result.Success);
            Assert.Equal("o***@s***.org", result.Data!.Email);
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

            var result = await service.GetAllUsersAsync(filter, 0, "Admin");

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

            var result = await service.GetAllUsersAsync(filter, 0, "Admin");

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
            var archivedResult = await service.GetAllUsersAsync(archivedFilter, 0, "Admin");
            Assert.Single(archivedResult.Data!.Data);
            Assert.Equal("ArchivedUser", archivedResult.Data.Data.First().Username);

            // 2. Test IsArchived = false
            var activeFilter = new UserFilterDto { IsArchived = false, PageNumber = 1, PageSize = 10 };
            var activeResult = await service.GetAllUsersAsync(activeFilter, 0, "Admin");
            Assert.Single(activeResult.Data!.Data);
            Assert.Equal("ActiveUser", activeResult.Data.Data.First().Username);
        }

        [Fact]
        public async Task GetAllUsersAsync_WithIsEmailVerified_FiltersCorrectly()
        {
            var context = await GetInMemoryDbContext();
            context.Students.AddRange(
                new Student { Username = "VerifiedUser", Role = UserRole.Student, IsEmailVerified = true },
                new Student { Username = "UnverifiedUser", Role = UserRole.Student, IsEmailVerified = false }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var verifiedFilter = new UserFilterDto { IsEmailVerified = true, PageNumber = 1, PageSize = 10 };
            var verifiedResult = await service.GetAllUsersAsync(verifiedFilter, 0, "Admin");
            Assert.Single(verifiedResult.Data!.Data);
            Assert.Equal("VerifiedUser", verifiedResult.Data.Data.First().Username);

            var unverifiedFilter = new UserFilterDto { IsEmailVerified = false, PageNumber = 1, PageSize = 10 };
            var unverifiedResult = await service.GetAllUsersAsync(unverifiedFilter, 0, "Admin");
            Assert.Single(unverifiedResult.Data!.Data);
            Assert.Equal("UnverifiedUser", unverifiedResult.Data.Data.First().Username);
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

            var result = await service.GetAllUsersAsync(filter, 0, "Admin");

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

            var result = await service.GetAllUsersAsync(filter, 0, "Admin");

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

            var result = await service.GetDetailedUserByIdAsync(student.Id, 0, "Admin");

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

            var result = await service.GetDetailedUserByIdAsync(student.Id, 0, "Admin");

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

            var result = await service.GetDetailedUserByIdAsync(999, 0, "Admin");

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

            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object, CreateConfiguration());
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

            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object, CreateConfiguration());
            var mockFile = CreateMockFile("new.png");

            await service.UploadAvatarAsync(student.Id, mockFile.Object);

            mockFileService.Verify(s => s.DeleteFileAsync("/avatars/old.png"), Times.Once);
        }

        [Fact]
        public async Task UploadAvatarAsync_WithInvalidExtension_ReturnsValidationFailure()
        {
            var context = await GetInMemoryDbContext();
            var mockFileService = new Mock<IFileStorageService>();
            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object, CreateConfiguration());

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
            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object, CreateConfiguration());

            var mockFile = CreateMockFile("profile.jpg");

            var result = await service.UploadAvatarAsync(999, mockFile.Object);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.NotFound, result.ErrorCode);
        }

        [Fact]
        public async Task UploadAvatarAsync_WhenAdminTargetsStudent_UpdatesStudentAvatar()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var student = new Student { Username = "Student", Email = "student@sqeez.org", Role = UserRole.Student };
            context.Students.AddRange(admin, student);
            await context.SaveChangesAsync();

            var mockFileService = new Mock<IFileStorageService>();
            mockFileService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), "avatars", true))
                .ReturnsAsync(ServiceResult<string>.Ok("/avatars/student.png"));

            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object, CreateConfiguration());

            var result = await service.UploadAvatarAsync(admin.Id, CreateMockFile("student.png").Object, student.Id, "Admin");

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal("/avatars/student.png", (await context.Students.FindAsync(student.Id))!.AvatarUrl);
            Assert.Null((await context.Students.FindAsync(admin.Id))!.AvatarUrl);
        }

        [Fact]
        public async Task UploadAvatarAsync_WhenStudentTargetsAnotherUser_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var currentStudent = new Student { Username = "CurrentStudent", Email = "current@sqeez.org", Role = UserRole.Student };
            var otherStudent = new Student { Username = "OtherStudent", Email = "other@sqeez.org", Role = UserRole.Student };
            context.Students.AddRange(currentStudent, otherStudent);
            await context.SaveChangesAsync();

            var mockFileService = new Mock<IFileStorageService>();
            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object, CreateConfiguration());

            var result = await service.UploadAvatarAsync(currentStudent.Id, CreateMockFile("avatar.png").Object, otherStudent.Id, "Student");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
            Assert.Null((await context.Students.FindAsync(otherStudent.Id))!.AvatarUrl);
            mockFileService.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task UploadAvatarAsync_WhenNormalAdminTargetsAdmin_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var currentAdmin = new Admin { Username = "CurrentAdmin", Email = "current-admin@sqeez.org", Role = UserRole.Admin };
            var targetAdmin = new Admin { Username = "TargetAdmin", Email = "target-admin@sqeez.org", Role = UserRole.Admin };
            context.Admins.AddRange(currentAdmin, targetAdmin);
            await context.SaveChangesAsync();

            var mockFileService = new Mock<IFileStorageService>();
            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object, CreateConfiguration());

            var result = await service.UploadAvatarAsync(currentAdmin.Id, CreateMockFile("avatar.png").Object, targetAdmin.Id, "Admin");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
            Assert.Null((await context.Students.FindAsync(targetAdmin.Id))!.AvatarUrl);
            mockFileService.Verify(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
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

            var result = await service.GetAllUsersAsync(filter, 0, "Admin");

            Assert.True(result.Success);
            Assert.Single(result.Data!.Data);
            Assert.Equal("MatchingAdmin", result.Data.Data.First().Username);
            Assert.IsType<AdminDto>(result.Data.Data.First());
        }

        [Fact]
        public async Task PatchUserAsync_WhenTeacherManagedClassIdIsZero_RemovesManagedClass()
        {
            var context = await GetInMemoryDbContext();
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var schoolClass = new SchoolClass { Name = "Managed" };
            var teacher = new Teacher { Username = "Teacher", Role = UserRole.Teacher, ManagedClass = schoolClass };
            context.Admins.Add(admin);
            context.Teachers.Add(teacher);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchTeacherDto { ManagedClassId = 0 };

            var result = await service.PatchUserAsync(teacher.Id, patchDto, admin.Id, "Admin");

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
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var schoolClass = new SchoolClass { Name = "Student class" };
            var teacher = new Teacher
            {
                Username = "TeacherStudent",
                Role = UserRole.Teacher,
                SchoolClass = schoolClass
            };

            context.Admins.Add(admin);
            context.SchoolClasses.Add(schoolClass);
            context.Teachers.Add(teacher);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchTeacherDto { ManagedClassId = schoolClass.Id };

            var result = await service.PatchUserAsync(teacher.Id, patchDto, admin.Id, "Admin");

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
            var admin = new Admin { Username = "Admin", Email = "admin@sqeez.org", Role = UserRole.Admin };
            var schoolClass = new SchoolClass { Name = "Managed class" };
            var teacher = new Teacher
            {
                Username = "TeacherManager",
                Role = UserRole.Teacher,
                ManagedClass = schoolClass
            };

            context.Admins.Add(admin);
            context.SchoolClasses.Add(schoolClass);
            context.Teachers.Add(teacher);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchTeacherDto { SchoolClassId = schoolClass.Id };

            var result = await service.PatchUserAsync(teacher.Id, patchDto, admin.Id, "Admin");

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

            var service = new UserService(context, new Mock<ILogger<UserService>>().Object, mockFileService.Object, CreateConfiguration());
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

