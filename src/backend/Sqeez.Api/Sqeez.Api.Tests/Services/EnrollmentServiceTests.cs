using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services;

namespace Sqeez.Api.Tests.Services
{
    public class EnrollmentServiceTests
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

        private EnrollmentService CreateService(SqeezDbContext context)
        {
            var mockLogger = new Mock<ILogger<EnrollmentService>>();
            return new EnrollmentService(context, mockLogger.Object);
        }

        // --- Helpers for Domain State ---
        private Subject CreateActiveSubject(long teacherId)
        {
            return new Subject
            {
                Name = "Active Subject",
                TeacherId = teacherId,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30) // Future end date
            };
        }

        private Subject CreateEndedSubject(long teacherId)
        {
            return new Subject
            {
                Name = "Ended Subject",
                TeacherId = teacherId,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(-1) // Past end date
            };
        }

        // --- Read Tests ---

        [Fact]
        public async Task GetEnrollmentByIdAsync_WhenExists_ReturnsEnrollmentDto()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "TestStudent" };
            var subject = CreateActiveSubject(1);
            var enrollment = new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow, Mark = 5 };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetEnrollmentByIdAsync(enrollment.Id, 1, "Admin");

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(5, result.Data.Mark);
            Assert.Equal(student.Id, result.Data.StudentId);
        }

        [Fact]
        public async Task GetEnrollmentByIdAsync_WhenStudentOwnsEnrollment_ReturnsEnrollmentDto()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "Student" };
            var subject = CreateActiveSubject(1);
            var enrollment = new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetEnrollmentByIdAsync(enrollment.Id, student.Id, "Student");

            Assert.True(result.Success);
            Assert.Equal(enrollment.Id, result.Data!.Id);
        }

        [Fact]
        public async Task GetEnrollmentByIdAsync_WhenTeacherOwnsEnrollmentAsStudent_ReturnsEnrollmentDto()
        {
            var context = await GetInMemoryDbContext();
            var teacherAsStudent = new Student { Username = "TeacherStudent" };
            var subject = CreateActiveSubject(99);
            var enrollment = new Enrollment { Student = teacherAsStudent, Subject = subject, EnrolledAt = DateTime.UtcNow };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetEnrollmentByIdAsync(enrollment.Id, teacherAsStudent.Id, "Teacher");

            Assert.True(result.Success);
            Assert.Equal(teacherAsStudent.Id, result.Data!.StudentId);
        }

        [Fact]
        public async Task GetEnrollmentByIdAsync_WhenTeacherOwnsSubject_ReturnsEnrollmentDto()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "Student" };
            var subject = CreateActiveSubject(42);
            var enrollment = new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetEnrollmentByIdAsync(enrollment.Id, 42, "Teacher");

            Assert.True(result.Success);
            Assert.Equal(enrollment.Id, result.Data!.Id);
        }

        [Fact]
        public async Task GetEnrollmentByIdAsync_WhenRequesterCannotViewEnrollment_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "Student" };
            var subject = CreateActiveSubject(42);
            var enrollment = new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetEnrollmentByIdAsync(enrollment.Id, 99, "Student");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task GetAllEnrollmentsAsync_WhenIsActiveFilterApplied_ReturnsCorrectRecords()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "TestStudent" };
            var subject = CreateActiveSubject(1);

            context.Enrollments.AddRange(
                new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow, ArchivedAt = null }, // Active
                new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow, ArchivedAt = DateTime.UtcNow } // Inactive
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Test Active Only
            var activeFilter = new EnrollmentFilterDto { IsActive = true, PageNumber = 1, PageSize = 10 };
            var activeResult = await service.GetAllEnrollmentsAsync(activeFilter, 1, "Admin");

            Assert.True(activeResult.Success);
            Assert.Equal(1, activeResult.Data!.TotalCount);
            Assert.Null(activeResult.Data.Data.First().ArchivedAt);

            // Test Inactive Only
            var inactiveFilter = new EnrollmentFilterDto { IsActive = false, PageNumber = 1, PageSize = 10 };
            var inactiveResult = await service.GetAllEnrollmentsAsync(inactiveFilter, 1, "Admin");

            Assert.True(inactiveResult.Success);
            Assert.Equal(1, inactiveResult.Data!.TotalCount);
            Assert.NotNull(inactiveResult.Data.Data.First().ArchivedAt);
        }

        [Fact]
        public async Task GetAllEnrollmentsAsync_WhenStudentRequestsAnotherStudent_ForcesCurrentStudent()
        {
            var context = await GetInMemoryDbContext();
            var currentStudent = new Student { Username = "CurrentStudent" };
            var otherStudent = new Student { Username = "OtherStudent" };
            var subject = CreateActiveSubject(1);

            context.Enrollments.AddRange(
                new Enrollment { Student = currentStudent, Subject = subject, EnrolledAt = DateTime.UtcNow },
                new Enrollment { Student = otherStudent, Subject = subject, EnrolledAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new EnrollmentFilterDto { StudentId = otherStudent.Id, PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllEnrollmentsAsync(filter, currentStudent.Id, "Student");

            Assert.True(result.Success);
            Assert.Single(result.Data!.Data);
            Assert.Equal(currentStudent.Id, result.Data.Data.First().StudentId);
        }

        [Fact]
        public async Task GetAllEnrollmentsAsync_WhenTeacherViewsOwnEnrollments_DoesNotRequireSubjectFilter()
        {
            var context = await GetInMemoryDbContext();
            var teacherAsStudent = new Student { Username = "TeacherStudent" };
            var subject = CreateActiveSubject(99);
            var enrollment = new Enrollment { Student = teacherAsStudent, Subject = subject, EnrolledAt = DateTime.UtcNow };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new EnrollmentFilterDto { StudentId = teacherAsStudent.Id, PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllEnrollmentsAsync(filter, teacherAsStudent.Id, "Teacher");

            Assert.True(result.Success);
            Assert.Single(result.Data!.Data);
            Assert.Equal(teacherAsStudent.Id, result.Data.Data.First().StudentId);
        }

        [Fact]
        public async Task GetAllEnrollmentsAsync_WhenTeacherDoesNotFilterByOwnedSubject_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);

            var result = await service.GetAllEnrollmentsAsync(new EnrollmentFilterDto(), 42, "Teacher");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task GetAllEnrollmentsAsync_WhenTeacherFiltersByOwnedSubject_ReturnsSubjectEnrollments()
        {
            var context = await GetInMemoryDbContext();
            var student = new Student { Username = "Student" };
            var ownedSubject = CreateActiveSubject(42);
            var otherSubject = CreateActiveSubject(99);

            context.Enrollments.AddRange(
                new Enrollment { Student = student, Subject = ownedSubject, EnrolledAt = DateTime.UtcNow },
                new Enrollment { Student = new Student { Username = "Other" }, Subject = otherSubject, EnrolledAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new EnrollmentFilterDto { SubjectId = ownedSubject.Id, PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllEnrollmentsAsync(filter, 42, "Teacher");

            Assert.True(result.Success);
            Assert.Single(result.Data!.Data);
            Assert.Equal(ownedSubject.Id, result.Data.Data.First().SubjectId);
        }

        [Fact]
        public async Task GetAllEnrollmentsAsync_WhenTeacherFiltersByDifferentTeacherSubject_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var subject = CreateActiveSubject(99);
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var filter = new EnrollmentFilterDto { SubjectId = subject.Id, PageNumber = 1, PageSize = 10 };

            var result = await service.GetAllEnrollmentsAsync(filter, 42, "Teacher");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        // --- Patch (Grading) Tests ---

        [Fact]
        public async Task PatchEnrollmentAsync_WhenSettingValidMark_UpdatesMark()
        {
            var context = await GetInMemoryDbContext();
            long currentTeacherId = 1;

            var student = new Student { Username = "Student" };
            var subject = CreateActiveSubject(currentTeacherId);
            var enrollment = new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchEnrollmentDto(Mark: 3);

            // Act - Need to pass the currentUserId!
            var result = await service.PatchEnrollmentAsync(enrollment.Id, patchDto, currentTeacherId);

            Assert.True(result.Success);
            Assert.Equal(3, result.Data!.Mark);

            var dbRecord = await context.Enrollments.FindAsync(enrollment.Id);
            Assert.Equal(3, dbRecord!.Mark);
        }

        [Fact]
        public async Task PatchEnrollmentAsync_WhenUnauthorizedTeacher_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            long currentTeacherId = 1;
            long wrongTeacherId = 99; // Different teacher trying to grade

            var student = new Student { Username = "Student" };
            var subject = CreateActiveSubject(currentTeacherId); // Owned by teacher 1
            var enrollment = new Enrollment { Student = student, Subject = subject };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchEnrollmentDto(Mark: 5);

            // Act
            var result = await service.PatchEnrollmentAsync(enrollment.Id, patchDto, wrongTeacherId);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task PatchEnrollmentAsync_WhenRemovingMark_SetsMarkToNull()
        {
            var context = await GetInMemoryDbContext();
            long currentTeacherId = 1;

            var subject = CreateActiveSubject(currentTeacherId);
            var enrollment = new Enrollment { Student = new Student(), Subject = subject, Mark = 5 };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchEnrollmentDto(Mark: null, RemoveMark: true);

            // Act
            var result = await service.PatchEnrollmentAsync(enrollment.Id, patchDto, currentTeacherId);

            Assert.True(result.Success);
            Assert.Null(result.Data!.Mark);
        }

        [Fact]
        public async Task PatchEnrollmentAsync_WhenMarkIsInvalid_ReturnsValidationFailed()
        {
            var context = await GetInMemoryDbContext();
            long currentTeacherId = 1;

            var subject = CreateActiveSubject(currentTeacherId);
            var enrollment = new Enrollment { Student = new Student(), Subject = subject };

            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var patchDto = new PatchEnrollmentDto(Mark: 99); // Invalid mark, max is 5

            // Act
            var result = await service.PatchEnrollmentAsync(enrollment.Id, patchDto, currentTeacherId);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.ValidationFailed, result.ErrorCode);
        }

        // --- Delete Tests ---

        [Fact]
        public async Task DeleteEnrollmentAsync_WhenNoQuizAttempts_HardDeletes()
        {
            var context = await GetInMemoryDbContext();
            var enrollment = new Enrollment { Student = new Student(), Subject = new Subject() };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.DeleteEnrollmentAsync(enrollment.Id, 1, "Admin");

            Assert.True(result.Success);
            var deletedRecord = await context.Enrollments.FindAsync(enrollment.Id);
            Assert.Null(deletedRecord); // Completely gone
        }

        [Fact]
        public async Task DeleteEnrollmentAsync_WhenHasQuizAttempts_SoftDeletes()
        {
            var context = await GetInMemoryDbContext();
            var enrollment = new Enrollment { Student = new Student(), Subject = new Subject() };

            // Add a mock quiz attempt so it triggers soft deletion
            enrollment.QuizAttempts = new List<QuizAttempt> { new QuizAttempt { TotalScore = 10 } };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.DeleteEnrollmentAsync(enrollment.Id, 1, "Admin");

            Assert.True(result.Success);
            var archivedRecord = await context.Enrollments.FindAsync(enrollment.Id);
            Assert.NotNull(archivedRecord); // Still exists
            Assert.NotNull(archivedRecord.ArchivedAt); // But is archived
        }

        [Fact]
        public async Task DeleteEnrollmentAsync_WhenRequesterCannotDeleteEnrollment_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var enrollment = new Enrollment { Student = new Student(), Subject = new Subject() };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.DeleteEnrollmentAsync(enrollment.Id, 99, "Student");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        // --- Bulk Enroll/Unenroll Tests ---

        [Fact]
        public async Task EnrollStudentsInSubjectAsync_WhenValid_EnrollsNewStudentsOnly()
        {
            var context = await GetInMemoryDbContext();
            long teacherId = 99;

            var subject = CreateActiveSubject(teacherId);
            var existingStudent = new Student { Username = "AlreadyHere" };
            var newStudent = new Student { Username = "NewGuy" };

            context.Subjects.Add(subject);
            context.Students.AddRange(existingStudent, newStudent);

            // Existing student is already enrolled
            context.Enrollments.Add(new Enrollment { Student = existingStudent, Subject = subject });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new AssignStudentsDto(new List<long> { existingStudent.Id, newStudent.Id });

            var result = await service.EnrollStudentsInSubjectAsync(subject.Id, dto);

            Assert.True(result.Success);
            Assert.Contains(newStudent.Id, result.Data!.NewlyEnrolledIds);
            Assert.Contains(existingStudent.Id, result.Data.AlreadyEnrolledIds);

            // Should have 2 enrollments total now, didn't create a duplicate for existingStudent
            var totalEnrollments = await context.Enrollments.CountAsync(e => e.SubjectId == subject.Id);
            Assert.Equal(2, totalEnrollments);
        }

        [Fact]
        public async Task EnrollStudentsInSubjectAsync_WhenSubjectClosed_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            long teacherId = 1;

            var subject = CreateEndedSubject(teacherId); // Subject is closed!
            var newStudent = new Student { Username = "NewGuy" };

            context.Subjects.Add(subject);
            context.Students.Add(newStudent);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new AssignStudentsDto(new List<long> { newStudent.Id });

            var result = await service.EnrollStudentsInSubjectAsync(subject.Id, dto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task EnrollStudentsInSubjectAsync_WhenEnrollingTeacher_ReturnsValidationFailed()
        {
            var context = await GetInMemoryDbContext();
            long teacherId = 1;

            var subject = CreateActiveSubject(teacherId);
            var teacherAsStudent = new Student { Id = teacherId, Username = "SneakyTeacher" };

            context.Subjects.Add(subject);
            context.Students.Add(teacherAsStudent);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new AssignStudentsDto(new List<long> { teacherId });

            var result = await service.EnrollStudentsInSubjectAsync(subject.Id, dto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.ValidationFailed, result.ErrorCode);
            Assert.Contains("cannot be enrolled as a student", result.ErrorMessage);
        }

        [Fact]
        public async Task UnenrollStudentsFromSubjectAsync_WhenValid_ArchivesStudents()
        {
            var context = await GetInMemoryDbContext();
            var subject = CreateActiveSubject(1);
            var student = new Student { Username = "DropOut" };
            var enrollment = new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow };

            // Give them a quiz attempt so they soft-delete (archive)
            enrollment.QuizAttempts = new List<QuizAttempt> { new QuizAttempt { TotalScore = 5 } };

            context.Subjects.Add(subject);
            context.Students.Add(student);
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new RemoveStudentsDto(new List<long> { student.Id });

            var result = await service.UnenrollStudentsFromSubjectAsync(subject.Id, dto);

            Assert.True(result.Success);

            var dbEnrollment = await context.Enrollments.FirstOrDefaultAsync(e => e.Id == enrollment.Id);
            Assert.NotNull(dbEnrollment);
            Assert.NotNull(dbEnrollment.ArchivedAt); // Successfully Soft Deleted
        }

        [Fact]
        public async Task DeleteAllEnrollmentsFromSubjectAsync_WhenNotAdmin_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);

            var result = await service.DeleteAllEnrollmentsFromSubjectAsync(1, "Teacher");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task DeleteAllEnrollmentsFromSubjectAsync_WhenSubjectHasHistory_RemovesEnrollmentsAttemptsAndResponses()
        {
            var context = await GetInMemoryDbContext();
            var subject = CreateActiveSubject(1);
            var student = new Student { Username = "Student", Email = "student@sqeez.org" };
            var enrollment = new Enrollment { Student = student, Subject = subject, EnrolledAt = DateTime.UtcNow };
            var quiz = new Quiz { Title = "Quiz", Subject = subject };
            var question = new QuizQuestion { Title = "Question", Quiz = quiz };
            var option = new QuizOption { Text = "Answer", QuizQuestion = question };
            var attempt = new QuizAttempt { Quiz = quiz, Enrollment = enrollment };
            var response = new QuizQuestionResponse { QuizAttempt = attempt, QuizQuestion = question };
            response.Options.Add(option);

            context.Subjects.Add(subject);
            context.Students.Add(student);
            context.Enrollments.Add(enrollment);
            context.Quizzes.Add(quiz);
            context.QuizQuestions.Add(question);
            context.QuizOptions.Add(option);
            context.QuizAttempts.Add(attempt);
            context.QuizQuestionResponses.Add(response);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.DeleteAllEnrollmentsFromSubjectAsync(subject.Id, "Admin");

            Assert.True(result.Success);
            Assert.False(await context.Enrollments.AnyAsync(enrollment => enrollment.SubjectId == subject.Id));
            Assert.False(await context.QuizAttempts.AnyAsync(attempt => attempt.QuizId == quiz.Id));
            Assert.False(await context.QuizQuestionResponses.AnyAsync());
            Assert.NotNull(await context.Quizzes.FindAsync(quiz.Id));
            Assert.NotNull(await context.QuizQuestions.FindAsync(question.Id));
            Assert.NotNull(await context.QuizOptions.FindAsync(option.Id));
        }
    }
}
