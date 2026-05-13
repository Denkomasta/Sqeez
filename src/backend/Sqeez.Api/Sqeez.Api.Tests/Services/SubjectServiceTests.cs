using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.SubjectService;
using Xunit;

namespace Sqeez.Api.Tests.Services
{
    public class SubjectServiceTests
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

        private SubjectService CreateService(SqeezDbContext context)
        {
            var mockLogger = new Mock<ILogger<SubjectService>>();
            return new SubjectService(context, mockLogger.Object);
        }

        [Fact]
        public async Task GetSubjectByIdAsync_WhenSubjectExists_ReturnsSubject()
        {
            var context = await GetInMemoryDbContext();
            var subject = new Subject { Name = "Mathematics", Code = "MATH101", StartDate = DateTime.UtcNow };
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetSubjectByIdAsync(subject.Id);

            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.Data);
            Assert.Equal("Mathematics", result.Data.Name);
            Assert.Equal("MATH101", result.Data.Code);
        }

        [Fact]
        public async Task GetSubjectByIdAsync_WhenSubjectDoesNotExist_ReturnsFailure()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);

            var result = await service.GetSubjectByIdAsync(999);

            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(ServiceError.NotFound, result.ErrorCode);
            Assert.Equal("Subject not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateSubjectAsync_CreatesAndReturnsSubject()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);
            var createDto = new CreateSubjectDto
            (
                "Physics",
                "PHYS201",
                "Intro to Physics",
                DateTime.UtcNow
            );

            var result = await service.CreateSubjectAsync(createDto);

            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.Data);
            Assert.Equal("Physics", result.Data.Name);

            var savedSubject = await context.Subjects.FirstOrDefaultAsync(s => s.Code == "PHYS201");
            Assert.NotNull(savedSubject);
            Assert.Equal("Intro to Physics", savedSubject.Description);
        }

        [Fact]
        public async Task CreateSubjectAsync_WithUtcDates_CreatesSubject()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);
            var startDate = new DateTime(2026, 1, 15, 8, 30, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2026, 6, 15, 16, 0, 0, DateTimeKind.Utc);
            var createDto = new CreateSubjectDto(
                "History",
                "HIST203",
                "Subject with UTC dates",
                startDate,
                endDate);

            var result = await service.CreateSubjectAsync(createDto);

            Assert.True(result.Success);
            Assert.Equal(startDate, result.Data!.StartDate);
            Assert.Equal(endDate, result.Data.EndDate);
        }

        [Fact]
        public async Task PatchSubjectAsync_WhenSubjectExists_UpdatesPropertiesAndValidatesTeacher()
        {
            var context = await GetInMemoryDbContext();

            // Seed a Teacher so the validation passes
            var teacher = new Teacher { Username = "Mr. Smith", Email = "smith@sqeez.org", Role = UserRole.Teacher };
            context.Teachers.Add(teacher);

            var subject = new Subject { Name = "Old Name", Code = "OLD1", StartDate = DateTime.UtcNow };
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchSubjectDto
            (
                "New Name",
                TeacherId: teacher.Id
            );

            var result = await service.PatchSubjectAsync(subject.Id, patchDto);

            Assert.Null(result.ErrorMessage);

            var updatedSubject = await context.Subjects.FindAsync(subject.Id);
            Assert.Equal("New Name", updatedSubject!.Name);
            Assert.Equal("OLD1", updatedSubject.Code);
            Assert.Equal(teacher.Id, updatedSubject.TeacherId);
        }

        [Fact]
        public async Task DeleteSubjectAsync_WhenSubjectIsEmpty_HardDeletesSubject()
        {
            var context = await GetInMemoryDbContext();
            var subject = new Subject { Name = "Empty Class", StartDate = DateTime.UtcNow };
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.DeleteSubjectAsync(subject.Id);

            Assert.Null(result.ErrorMessage);

            // Verify it was actually removed from the DB
            var deletedSubject = await context.Subjects.FindAsync(subject.Id);
            Assert.Null(deletedSubject);
        }

        [Fact]
        public async Task DeleteSubjectAsync_WhenSubjectHasEnrollments_SoftDeletesSubject()
        {
            var context = await GetInMemoryDbContext();
            var subject = new Subject { Name = "Populated Class", StartDate = DateTime.UtcNow };
            context.Subjects.Add(subject);

            // Add an enrollment to trigger the soft delete
            var student = new Student { Username = "TestStudent", Email = "student@sqeez.org" };
            context.Enrollments.Add(new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow });

            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.DeleteSubjectAsync(subject.Id);

            Assert.True(result.Success);

            var softDeletedSubject = await context.Subjects.FindAsync(subject.Id);
            Assert.NotNull(softDeletedSubject);
            Assert.NotNull(softDeletedSubject.EndDate);
            Assert.True(softDeletedSubject.EndDate <= DateTime.UtcNow);
        }

        [Fact]
        public async Task GetAllSubjectsAsync_WithSearchTerm_ReturnsFilteredResults()
        {
            var context = await GetInMemoryDbContext();
            context.Subjects.AddRange(
                new Subject { Name = "Biology", Code = "BIO1", StartDate = DateTime.UtcNow },
                new Subject { Name = "Chemistry", Code = "CHEM1", StartDate = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new SubjectFilterDto { SearchTerm = "bio" };

            var result = await service.GetAllSubjectsAsync(filter);

            Assert.Equal(1, result.Data!.TotalCount);
            Assert.Equal("Biology", result.Data.Data.First().Name);
        }

        [Fact]
        public async Task GetAllSubjectsAsync_WhenIsActiveIsTrue_ReturnsOnlyActiveSubjects()
        {
            var context = await GetInMemoryDbContext();
            var now = DateTime.UtcNow;

            context.Subjects.AddRange(
                new Subject { Name = "Active1", StartDate = now.AddDays(-5) },
                new Subject { Name = "Active2", StartDate = now.AddDays(-5), EndDate = now.AddDays(10) },
                new Subject { Name = "Inactive1", StartDate = now.AddDays(-10), EndDate = now.AddDays(-1) },
                new Subject { Name = "Inactive2", StartDate = now.AddDays(5) }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new SubjectFilterDto { IsActive = true };

            var result = await service.GetAllSubjectsAsync(filter);

            Assert.Equal(2, result.Data!.TotalCount);
            Assert.Contains(result.Data.Data, s => s.Name == "Active1");
            Assert.Contains(result.Data.Data, s => s.Name == "Active2");
        }

        [Fact]
        public async Task CreateSubjectsBulkAsync_WhenSubjectsProvided_SkipsExistingAndCreatesNew()
        {
            var context = await GetInMemoryDbContext();
            var existingSubject = new Subject { Name = "Math", Code = "m1", StartDate = DateTime.UtcNow };
            context.Subjects.Add(existingSubject);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var newSubjects = new List<Subject>
            {
                new Subject { Name = "Math", Code = "m1", StartDate = DateTime.UtcNow }, // duplicate by code
                new Subject { Name = "Physics", Code = "p1", StartDate = DateTime.UtcNow } // new
            };

            var result = await service.CreateSubjectsBulkAsync(newSubjects);

            Assert.True(result.Success);
            Assert.Single(result.Data!.Created);
            Assert.Equal("Physics", result.Data.Created.First().Name);
            Assert.Equal("p1", result.Data.Created.First().Code);
            Assert.Single(result.Data.SkippedMessages);
        }

        [Fact]
        public async Task PatchSubjectAsync_WhenTeacherIsEnrolledStudent_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "EnrolledStudent", Role = UserRole.Student };
            var subject = new Subject { Name = "Math", Code = "MATH", StartDate = DateTime.UtcNow };
            context.Students.Add(student);
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();

            context.Enrollments.Add(new Enrollment { StudentId = student.Id, SubjectId = subject.Id, EnrolledAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.PatchSubjectAsync(subject.Id, new PatchSubjectDto(TeacherId: student.Id));

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);

            var dbSubject = await context.Subjects.FindAsync(subject.Id);
            Assert.Null(dbSubject!.TeacherId);
        }

        [Fact]
        public async Task GetAllSubjectsAsync_WithStudentFilter_ExcludesTaughtAndActiveEnrolledSubjects()
        {
            var context = await GetInMemoryDbContext();
            long userId = 7;
            var openSubject = new Subject { Name = "Open", Code = "OPEN", StartDate = DateTime.UtcNow };
            var taughtSubject = new Subject { Name = "Taught", Code = "TEACH", TeacherId = userId, StartDate = DateTime.UtcNow };
            var enrolledSubject = new Subject { Name = "Enrolled", Code = "ENR", StartDate = DateTime.UtcNow };
            var archivedEnrollmentSubject = new Subject { Name = "Archived", Code = "ARCH", StartDate = DateTime.UtcNow };

            context.Subjects.AddRange(openSubject, taughtSubject, enrolledSubject, archivedEnrollmentSubject);
            context.Enrollments.AddRange(
                new Enrollment { StudentId = userId, Subject = enrolledSubject, EnrolledAt = DateTime.UtcNow },
                new Enrollment { StudentId = userId, Subject = archivedEnrollmentSubject, EnrolledAt = DateTime.UtcNow, ArchivedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new SubjectFilterDto { StudentId = userId, PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllSubjectsAsync(filter);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.TotalCount);
            Assert.Contains(result.Data.Data, s => s.Name == "Open");
            Assert.Contains(result.Data.Data, s => s.Name == "Archived");
            Assert.DoesNotContain(result.Data.Data, s => s.Name == "Taught");
            Assert.DoesNotContain(result.Data.Data, s => s.Name == "Enrolled");
        }

        [Fact]
        public async Task CreateSubjectsBulkAsync_WhenIncomingCodesRepeat_CreatesFirstAndSkipsDuplicate()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);
            var subjects = new List<Subject>
            {
                new Subject { Name = "Physics", Code = "PHY", StartDate = DateTime.UtcNow },
                new Subject { Name = "Physics Duplicate", Code = "phy", StartDate = DateTime.UtcNow }
            };

            var result = await service.CreateSubjectsBulkAsync(subjects);

            Assert.True(result.Success);
            Assert.Single(result.Data!.Created);
            Assert.Single(result.Data.SkippedMessages);
            Assert.Equal(1, await context.Subjects.CountAsync());
            Assert.Equal("Physics", result.Data.Created.First().Name);
        }
    }
}
