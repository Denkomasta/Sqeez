using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Services;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Tests.Services
{
    public class QuizQuestionServiceTests
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

        private QuizQuestionService CreateService(SqeezDbContext context, Mock<IMediaAssetService>? mockMediaAssetService = null)
        {
            var mockLogger = new Mock<ILogger<QuizQuestionService>>();
            mockMediaAssetService ??= new Mock<IMediaAssetService>();

            return new QuizQuestionService(context, mockLogger.Object, mockMediaAssetService.Object);
        }

        private Subject CreateActiveSubject(long teacherId)
        {
            return new Subject
            {
                TeacherId = teacherId,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };
        }

        private Subject CreateEndedSubject(long teacherId)
        {
            return new Subject
            {
                TeacherId = teacherId,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(-1)
            };
        }

        [Fact]
        public async Task GetQuizQuestionByIdAsync_WhenValidAndAuthorized_ReturnsQuestion()
        {
            var context = await GetInMemoryDbContext();
            long currentUserId = 1;

            var subject = CreateActiveSubject(currentUserId);
            var quiz = new Quiz { Subject = subject };
            var question = new QuizQuestion { Title = "Test Question", Quiz = quiz };

            context.QuizQuestions.Add(question);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetQuizQuestionByIdAsync(question.Id, currentUserId);

            Assert.True(result.Success);
            Assert.Equal("Test Question", result.Data!.Title);
        }

        [Fact]
        public async Task GetQuizQuestionByIdAsync_WhenUnauthorizedTeacher_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            long currentUserId = 1;
            long unauthorizedUserId = 99;

            var subject = CreateActiveSubject(currentUserId);
            var quiz = new Quiz { Subject = subject };
            var question = new QuizQuestion { Quiz = quiz };

            context.QuizQuestions.Add(question);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetQuizQuestionByIdAsync(question.Id, unauthorizedUserId);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task CreateQuizQuestionAsync_WhenSubjectHasEnded_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            long currentUserId = 1;

            var subject = CreateEndedSubject(currentUserId);
            var quiz = new Quiz { Subject = subject };
            context.Quizzes.Add(quiz);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new CreateQuizQuestionDto(
                "Too late?",
                1,
                30,
                quiz.Id,
                false,
                null,
                false
            );

            var result = await service.CreateQuizQuestionAsync(dto, currentUserId);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
            Assert.Contains("closed subject", result.ErrorMessage);
        }

        [Fact]
        public async Task PatchQuizQuestionAsync_WhenQuizHasAttempts_ReturnsConflict()
        {
            var context = await GetInMemoryDbContext();
            long currentUserId = 1;
            long studentId = 123;

            var subject = CreateActiveSubject(currentUserId);

            var enrollment = new Enrollment { Subject = subject, StudentId = studentId };

            var quiz = new Quiz { Subject = subject };
            var question = new QuizQuestion { Quiz = quiz };

            var attempt = new QuizAttempt { Quiz = quiz, Enrollment = enrollment };

            context.Enrollments.Add(enrollment);
            context.QuizQuestions.Add(question);
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var patchDto = new PatchQuizQuestionDto("Try changing this now", null, null, null, null, null);

            var result = await service.PatchQuizQuestionAsync(question.Id, patchDto, currentUserId);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Conflict, result.ErrorCode);
            Assert.Contains("students have already started", result.ErrorMessage);
        }

        [Fact]
        public async Task GetDetailedQuizQuestionByIdAsync_WhenStudentHasActiveAttempt_ReturnsQuestion()
        {
            var context = await GetInMemoryDbContext();
            long teacherId = 1;
            long studentId = 2;

            var subject = CreateActiveSubject(teacherId);
            var enrollment = new Enrollment { Subject = subject, StudentId = studentId };
            var quiz = new Quiz { Subject = subject, MaxRetries = 1, ClosingDate = DateTime.UtcNow.AddDays(1) };
            var question = new QuizQuestion { Quiz = quiz, Title = "Active attempt question" };
            var option = new QuizOption { QuizQuestion = question, Text = "Visible option", IsCorrect = true };
            var attempt = new QuizAttempt { Quiz = quiz, Enrollment = enrollment, Status = AttemptStatus.Started };

            context.AddRange(enrollment, question, option, attempt);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetDetailedQuizQuestionByIdAsync(question.Id, quiz.Id, studentId, "Student");

            Assert.True(result.Success);
            Assert.Equal("Active attempt question", result.Data!.Title);
        }

        [Fact]
        public async Task GetDetailedQuizQuestionByIdAsync_WhenQuizIsClosed_ReturnsQuestion()
        {
            var context = await GetInMemoryDbContext();
            long teacherId = 1;
            long studentId = 2;

            var subject = CreateActiveSubject(teacherId);
            var enrollment = new Enrollment { Subject = subject, StudentId = studentId };
            var quiz = new Quiz
            {
                Subject = subject,
                ClosingDate = DateTime.UtcNow.AddMinutes(-1)
            };
            var question = new QuizQuestion { Quiz = quiz, Title = "Review question" };
            var correctOption = new QuizOption { QuizQuestion = question, Text = "Review option", IsCorrect = true };

            context.AddRange(enrollment, question, correctOption);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetDetailedQuizQuestionByIdAsync(question.Id, quiz.Id, studentId, "Student");

            Assert.True(result.Success);
            Assert.Equal("Review question", result.Data!.Title);
            Assert.Single(result.Data.Options);
        }

        [Fact]
        public async Task GetDetailedQuizQuestionByIdAsync_WhenQuizStillOpenAndNoActiveAttempt_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            long teacherId = 1;
            long studentId = 2;

            var subject = CreateActiveSubject(teacherId);
            var enrollment = new Enrollment { Subject = subject, StudentId = studentId };
            var quiz = new Quiz
            {
                Subject = subject,
                MaxRetries = 1,
                ClosingDate = DateTime.UtcNow.AddDays(1)
            };
            var question = new QuizQuestion { Quiz = quiz, Title = "Open quiz question" };
            var option = new QuizOption { QuizQuestion = question, Text = "Open option", IsCorrect = true };
            var attempt = new QuizAttempt { Quiz = quiz, Enrollment = enrollment, Status = AttemptStatus.Completed };

            context.AddRange(enrollment, question, option, attempt);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetDetailedQuizQuestionByIdAsync(question.Id, quiz.Id, studentId, "Student");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }
    }
}
