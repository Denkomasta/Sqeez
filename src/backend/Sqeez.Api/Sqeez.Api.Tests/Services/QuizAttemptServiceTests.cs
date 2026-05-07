using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Sqeez.Api.Constants;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Tests.Services
{
    public class QuizAttemptServiceTests
    {
        private readonly DbContextOptions<SqeezDbContext> _dbContextOptions;
        private readonly Mock<ILogger<QuizAttemptService>> _mockLogger;
        private readonly Mock<IBadgeService> _mockBadgeService;

        public QuizAttemptServiceTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<SqeezDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockLogger = new Mock<ILogger<QuizAttemptService>>();
            _mockBadgeService = new Mock<IBadgeService>();
        }

        private async Task<SqeezDbContext> GetSeededContextAsync()
        {
            var options = new DbContextOptionsBuilder<SqeezDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            var context = new SqeezDbContext(options);

            var student = new Student { Id = 1, Username = "teststudent", Email = "test@test.com", PasswordHash = "hash", CurrentXP = 0 };
            var subject = new Subject { Id = 1, Name = "Math", Code = "M1" };
            var enrollment = new Enrollment { Id = 1, StudentId = 1, SubjectId = 1 };

            var quiz = new Quiz { Id = 1, SubjectId = 1, Title = "Test Quiz", MaxRetries = 5 };

            var question1 = new QuizQuestion { Id = 1, QuizId = 1, Difficulty = 5, Title = "Strict MCQ", IsStrictMultipleChoice = true };
            var opt1 = new QuizOption { Id = 1, QuizQuestionId = 1, IsCorrect = true, Text = "A" };
            var opt2 = new QuizOption { Id = 2, QuizQuestionId = 1, IsCorrect = true, Text = "B" };
            var opt3 = new QuizOption { Id = 3, QuizQuestionId = 1, IsCorrect = false, Text = "C" };

            var question2 = new QuizQuestion { Id = 2, QuizId = 1, Difficulty = 10, Title = "FreeText" };
            var opt4 = new QuizOption { Id = 4, QuizQuestionId = 2, IsCorrect = true, IsFreeText = true, Text = "Earth" };

            var question3 = new QuizQuestion { Id = 3, QuizId = 1, Difficulty = 5, Title = "Non-Strict MCQ", IsStrictMultipleChoice = false };
            var opt5 = new QuizOption { Id = 5, QuizQuestionId = 3, IsCorrect = true, Text = "Correct Option 1" };
            var opt6 = new QuizOption { Id = 6, QuizQuestionId = 3, IsCorrect = true, Text = "Correct Option 2" };
            var opt7 = new QuizOption { Id = 7, QuizQuestionId = 3, IsCorrect = false, Text = "Incorrect Option" };

            context.Students.Add(student);
            context.Subjects.Add(subject);
            context.Enrollments.Add(enrollment);
            context.Quizzes.Add(quiz);
            context.QuizQuestions.AddRange(question1, question2, question3);
            context.QuizOptions.AddRange(opt1, opt2, opt3, opt4, opt5, opt6, opt7);

            await context.SaveChangesAsync();
            return context;
        }

        [Fact]
        public async Task StartAttemptAsync_WhenValid_CreatesAttemptAndReturnsFirstQuestionId()
        {
            await using var context = await GetSeededContextAsync();

            var quiz = await context.Quizzes.FindAsync(1L);
            quiz!.PublishDate = DateTime.UtcNow.AddDays(-1);
            quiz.ClosingDate = null;
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);
            var dto = new StartQuizAttemptDto(QuizId: 1, EnrollmentId: 1);

            var result = await service.StartAttemptAsync(1, dto);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(AttemptStatus.Created, result.Data!.Status);
            Assert.Equal(0, result.Data.TotalScore);
            Assert.Equal(1, await context.QuizAttempts.CountAsync());

            Assert.NotNull(result.Data.NextQuestionId);
            Assert.Equal(1, result.Data.NextQuestionId.Value);
        }

        [Fact]
        public async Task StartAttemptAsync_WhenMaxRetriesReached_ReturnsConflict()
        {
            await using var context = await GetSeededContextAsync();

            var quiz = await context.Quizzes.FirstAsync();

            quiz.PublishDate = DateTime.UtcNow.AddDays(-1);
            quiz.ClosingDate = null;
            quiz.MaxRetries = 1;

            context.QuizAttempts.Add(new QuizAttempt { QuizId = 1, EnrollmentId = 1 });
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);
            var dto = new StartQuizAttemptDto(QuizId: 1, EnrollmentId: 1);

            var result = await service.StartAttemptAsync(1, dto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Conflict, result.ErrorCode);
            Assert.Contains("maximum of 1 retries", result.ErrorMessage);
        }

        [Fact]
        public async Task SubmitAnswerAsync_NotStrict_SelectsOneCorrect_AwardsFullPoints()
        {
            await using var context = await GetSeededContextAsync();

            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Created };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var dto = new SubmitQuestionResponseDto(QuizQuestionId: 3, ResponseTimeMs: 5000, null, new List<long> { 5 });

            var result = await service.SubmitAnswerAsync(1, 1, dto);

            Assert.True(result.Success);
            Assert.Equal(5, result.Data!.Score);
        }

        [Fact]
        public async Task SubmitAnswerAsync_NotStrict_SelectsMultiple_AwardsZeroPoints()
        {
            await using var context = await GetSeededContextAsync();

            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Created };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var dto = new SubmitQuestionResponseDto(QuizQuestionId: 3, ResponseTimeMs: 5000, null, new List<long> { 5, 6 });

            var result = await service.SubmitAnswerAsync(1, 1, dto);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.Score);
        }

        [Fact]
        public async Task SubmitAnswerAsync_NotStrict_SelectsOneIncorrect_AwardsZeroPoints()
        {
            await using var context = await GetSeededContextAsync();

            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Created };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var dto = new SubmitQuestionResponseDto(QuizQuestionId: 3, ResponseTimeMs: 5000, null, new List<long> { 7 });

            var result = await service.SubmitAnswerAsync(1, 1, dto);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.Score);
        }

        [Fact]
        public async Task SubmitAnswerAsync_WithOptionFromDifferentQuestion_ReturnsValidationFailed()
        {
            await using var context = await GetSeededContextAsync();

            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Created };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var dto = new SubmitQuestionResponseDto(QuizQuestionId: 3, ResponseTimeMs: 5000, null, new List<long> { 1 });

            var result = await service.SubmitAnswerAsync(1, 1, dto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.ValidationFailed, result.ErrorCode);
            Assert.Empty(context.QuizQuestionResponses);
        }

        [Fact]
        public async Task SubmitAnswerAsync_StrictMultipleChoice_PerfectMatch_AwardsFullPoints()
        {
            await using var context = await GetSeededContextAsync();
            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Created };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var dto = new SubmitQuestionResponseDto(QuizQuestionId: 1, ResponseTimeMs: 5000, null, new List<long> { 1, 2 });

            var result = await service.SubmitAnswerAsync(1, 1, dto);

            Assert.True(result.Success);
            Assert.Equal(5, result.Data!.Score);
            Assert.Equal(2, result.Data.NextQuestionId);
        }

        [Fact]
        public async Task SubmitAnswerAsync_StrictMultipleChoice_PartialMatch_AwardsZeroPoints()
        {
            await using var context = await GetSeededContextAsync();
            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Created };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var dto = new SubmitQuestionResponseDto(QuizQuestionId: 1, ResponseTimeMs: 5000, null, new List<long> { 1, 3 });

            var result = await service.SubmitAnswerAsync(1, 1, dto);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.Score);
        }

        [Fact]
        public async Task SubmitAnswerAsync_FreeText_LeavesScoreAtNullForManualGrading()
        {
            await using var context = await GetSeededContextAsync();
            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Created };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var dto = new SubmitQuestionResponseDto(QuizQuestionId: 2, ResponseTimeMs: 5000, "earth", new List<long>());

            var result = await service.SubmitAnswerAsync(1, 1, dto);

            Assert.True(result.Success);
            Assert.Null(result.Data!.Score);
            Assert.Equal(3, result.Data.NextQuestionId);
        }

        [Fact]
        public async Task SubmitAnswerAsync_AlreadyAnswered_ReturnsConflict()
        {
            await using var context = await GetSeededContextAsync();
            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Started };
            var existingResponse = new QuizQuestionResponse { QuizAttemptId = 1, QuizQuestionId = 1 };

            context.QuizAttempts.Add(attempt);
            context.QuizQuestionResponses.Add(existingResponse);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);
            var dto = new SubmitQuestionResponseDto(QuizQuestionId: 1, ResponseTimeMs: 5000, null, new List<long> { 1, 2 });

            var result = await service.SubmitAnswerAsync(1, 1, dto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Conflict, result.ErrorCode);
            Assert.Contains("already submitted", result.ErrorMessage);
        }

        [Fact]
        public async Task GetNextPendingQuestionIdAsync_WhenNoAnswers_ReturnsFirstQuestionId()
        {
            await using var context = await GetSeededContextAsync();
            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Started };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var result = await service.GetNextPendingQuestionIdAsync(attempt.Id, 1);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data);
        }

        [Fact]
        public async Task GetNextPendingQuestionIdAsync_WhenPartiallyAnswered_ReturnsNextQuestionId()
        {
            await using var context = await GetSeededContextAsync();
            var attempt = new QuizAttempt
            {
                Id = 1,
                QuizId = 1,
                EnrollmentId = 1,
                Status = AttemptStatus.Started,
                Responses = new List<QuizQuestionResponse> { new QuizQuestionResponse { QuizQuestionId = 1 } }
            };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var result = await service.GetNextPendingQuestionIdAsync(attempt.Id, 1);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data);
        }

        [Fact]
        public async Task GetNextPendingQuestionIdAsync_WhenFullyAnswered_ReturnsNull()
        {
            await using var context = await GetSeededContextAsync();
            var attempt = new QuizAttempt
            {
                Id = 1,
                QuizId = 1,
                EnrollmentId = 1,
                Status = AttemptStatus.Started,
                Responses = new List<QuizQuestionResponse>
                {
                    new QuizQuestionResponse { QuizQuestionId = 1 },
                    new QuizQuestionResponse { QuizQuestionId = 2 },
                    new QuizQuestionResponse { QuizQuestionId = 3 }
                }
            };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var result = await service.GetNextPendingQuestionIdAsync(attempt.Id, 1);

            Assert.True(result.Success);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetNextPendingQuestionIdAsync_WhenAttemptIsCompleted_ReturnsConflict()
        {
            await using var context = await GetSeededContextAsync();
            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Completed };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var result = await service.GetNextPendingQuestionIdAsync(attempt.Id, 1);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Conflict, result.ErrorCode);
        }

        [Fact]
        public async Task GetAttemptDetailsAsync_AsOwningTeacher_ReturnsDetails()
        {
            await using var context = await GetSeededContextAsync();
            var subject = await context.Subjects.FirstAsync();
            subject.TeacherId = 99;
            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Completed };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var result = await service.GetAttemptDetailsAsync(attempt.Id, 99, "Teacher");

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(attempt.Id, result.Data!.Id);
        }

        [Fact]
        public async Task GetAttemptDetailsAsync_AsDifferentTeacher_ReturnsForbidden()
        {
            await using var context = await GetSeededContextAsync();
            var subject = await context.Subjects.FirstAsync();
            subject.TeacherId = 99;
            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Completed };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var result = await service.GetAttemptDetailsAsync(attempt.Id, 42, "Teacher");

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task GetAttemptDetailsAsync_AsAdmin_ReturnsDetails()
        {
            await using var context = await GetSeededContextAsync();
            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Completed };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var result = await service.GetAttemptDetailsAsync(attempt.Id, 123, "Admin");

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(attempt.Id, result.Data!.Id);
        }

        [Fact]
        public async Task GetAttemptsForQuizAsync_ClampsInvalidPagingValues()
        {
            await using var context = await GetSeededContextAsync();
            var subject = await context.Subjects.FirstAsync();
            subject.TeacherId = 99;

            for (int i = 1; i <= ValidationConstants.MaxPageSize + 5; i++)
            {
                context.QuizAttempts.Add(new QuizAttempt
                {
                    QuizId = 1,
                    EnrollmentId = 1,
                    Status = AttemptStatus.Completed,
                    StartTime = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var result = await service.GetAttemptsForQuizAsync(1, 99, pageNumber: -5, pageSize: 10_000);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(1, result.Data!.PageNumber);
            Assert.Equal(ValidationConstants.MaxPageSize, result.Data.PageSize);
            Assert.Equal(ValidationConstants.MaxPageSize, result.Data.Data.Count());
        }

        [Fact]
        public async Task DeleteAttemptAsync_WhenValid_DeletesAttemptAndResponses()
        {
            await using var context = await GetSeededContextAsync();
            var subject = await context.Subjects.FirstAsync();
            subject.TeacherId = 99;

            var attempt = new QuizAttempt
            {
                Id = 1,
                QuizId = 1,
                EnrollmentId = 1,
                Status = AttemptStatus.Started,
                Responses = new List<QuizQuestionResponse> { new QuizQuestionResponse { QuizQuestionId = 1, Score = 5 } }
            };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);
            var result = await service.DeleteAttemptAsync(attempt.Id, 99);

            Assert.True(result.Success);
            Assert.Equal(0, await context.QuizAttempts.CountAsync());
            Assert.Equal(0, await context.QuizQuestionResponses.CountAsync());
        }

        [Fact]
        public async Task DeleteAttemptAsync_WhenAttemptNotFound_ReturnsNotFound()
        {
            await using var context = await GetSeededContextAsync();
            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            var result = await service.DeleteAttemptAsync(9999, 1);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.NotFound, result.ErrorCode);
        }

        [Fact]
        public async Task DeleteAttemptAsync_WhenWrongTeacher_ReturnsForbidden()
        {
            await using var context = await GetSeededContextAsync();
            var subject = await context.Subjects.FirstAsync();
            subject.TeacherId = 99;

            var attempt = new QuizAttempt { Id = 1, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Completed };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);
            var result = await service.DeleteAttemptAsync(attempt.Id, 42);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
            Assert.Equal(1, await context.QuizAttempts.CountAsync());
        }

        [Fact]
        public async Task CompleteAttemptAsync_CalculatesFinalScore_AndTriggersBadgeService()
        {
            await using var context = await GetSeededContextAsync();
            var attempt = new QuizAttempt
            {
                QuizId = 1,
                EnrollmentId = 1,
                Status = AttemptStatus.Started,
                Responses = new List<QuizQuestionResponse>
                {
                    new QuizQuestionResponse { QuizQuestionId = 1, Score = 5 },
                    new QuizQuestionResponse { QuizQuestionId = 2, Score = 10 },
                    new QuizQuestionResponse { QuizQuestionId = 3, Score = 5 },
                }
            };
            context.QuizAttempts.Add(attempt);
            await context.SaveChangesAsync();

            _mockBadgeService
                .Setup(b => b.EvaluateAndAwardBadgesAsync(It.IsAny<long>(), It.IsAny<BadgeEvaluationMetrics>()))
                .ReturnsAsync(ServiceResult<List<StudentBadgeBasicDto>>.Ok(new List<StudentBadgeBasicDto>()));
            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);
            var result = await service.CompleteAttemptAsync(attempt.Id, 1);

            Assert.True(result.Success);
            Assert.Equal(AttemptStatus.Completed, result.Data!.Status);
            Assert.Equal(20, result.Data.TotalScore);

            var student = await context.Students.FindAsync(1L);
            Assert.Equal(20, student!.CurrentXP);

            _mockBadgeService.Verify(b => b.EvaluateAndAwardBadgesAsync(1,
                It.Is<BadgeEvaluationMetrics>(m => m.TotalScore == 20 && m.ScorePercentage == 100m)),
                Times.Once);
        }

        [Fact]
        public async Task CompleteAttemptAsync_WhenNewHighScore_AwardsDeltaXP()
        {
            await using var context = await GetSeededContextAsync();

            // Student previously scored 5 points
            context.QuizAttempts.Add(new QuizAttempt { Id = 10, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Completed, TotalScore = 5 });

            // Current attempt will score 15 points
            var newAttempt = new QuizAttempt
            {
                Id = 11,
                QuizId = 1,
                EnrollmentId = 1,
                Status = AttemptStatus.Started,
                Responses = new List<QuizQuestionResponse> { new QuizQuestionResponse { Score = 15 } }
            };
            context.QuizAttempts.Add(newAttempt);
            await context.SaveChangesAsync();

            _mockBadgeService
                .Setup(b => b.EvaluateAndAwardBadgesAsync(It.IsAny<long>(), It.IsAny<BadgeEvaluationMetrics>()))
                .ReturnsAsync(ServiceResult<List<StudentBadgeBasicDto>>.Ok(new List<StudentBadgeBasicDto>()));

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            await service.CompleteAttemptAsync(newAttempt.Id, 1L);

            var student = await context.Students.FindAsync(1L);
            // Student would normally have 15 points, the 5 points from original are not set so the service correctly adds 10 points.
            Assert.Equal(10, student!.CurrentXP);
        }

        [Fact]
        public async Task CompleteAttemptAsync_WhenLowerScore_DoesNotAwardXP()
        {
            await using var context = await GetSeededContextAsync();

            // Student previously scored an amazing 15 points
            context.QuizAttempts.Add(new QuizAttempt { Id = 10, QuizId = 1, EnrollmentId = 1, Status = AttemptStatus.Completed, TotalScore = 15 });

            // Current attempt will only score 5 points
            var newAttempt = new QuizAttempt
            {
                Id = 11,
                QuizId = 1,
                EnrollmentId = 1,
                Status = AttemptStatus.Started,
                Responses = new List<QuizQuestionResponse> { new QuizQuestionResponse { Score = 5 } }
            };
            context.QuizAttempts.Add(newAttempt);

            // Give the student a baseline 15 XP to start
            var student = await context.Students.FindAsync(1L);
            student!.CurrentXP = 15;
            await context.SaveChangesAsync();

            _mockBadgeService
                .Setup(b => b.EvaluateAndAwardBadgesAsync(It.IsAny<long>(), It.IsAny<BadgeEvaluationMetrics>()))
                .ReturnsAsync(ServiceResult<List<StudentBadgeBasicDto>>.Ok(new List<StudentBadgeBasicDto>()));

            var service = new QuizAttemptService(context, _mockLogger.Object, _mockBadgeService.Object);

            await service.CompleteAttemptAsync(newAttempt.Id, 1);

            var updatedStudent = await context.Students.FindAsync(1L);
            Assert.Equal(15, updatedStudent!.CurrentXP);
        }
    }
}
