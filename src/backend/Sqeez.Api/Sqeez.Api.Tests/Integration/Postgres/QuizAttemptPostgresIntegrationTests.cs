using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.TokenService;

namespace Sqeez.Api.Tests.Integration.Postgres
{
    [Collection(PostgresIntegrationTestCollection.CollectionName)]
    public class QuizAttemptPostgresIntegrationTests
    {
        private readonly PostgresIntegrationTestFixture _fixture;

        public QuizAttemptPostgresIntegrationTests(PostgresIntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [DockerAvailableFact]
        public async Task StartAttempt_WithPublishedQuiz_CreatesAttemptAndReturnsFirstQuestionId()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizAsync();
            var client = CreateAuthenticatedClient(seed.Student);

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/start", new
            {
                quizId = seed.QuizId,
                enrollmentId = seed.EnrollmentId
            });

            await AssertStatusCodeAsync(HttpStatusCode.OK, response);
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var attemptId = json.RootElement.GetProperty("id").GetInt64();
            var nextQuestionId = json.RootElement.GetProperty("nextQuestionId").GetInt64();

            Assert.Equal(seed.FirstQuestionId, nextQuestionId);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var attempt = await dbContext.QuizAttempts.SingleAsync(attempt => attempt.Id == attemptId);

            Assert.Equal(seed.QuizId, attempt.QuizId);
            Assert.Equal(seed.EnrollmentId, attempt.EnrollmentId);
            Assert.Equal(AttemptStatus.Created, attempt.Status);
            Assert.Equal(0, attempt.TotalScore);
        }

        [DockerAvailableFact]
        public async Task StartAttempt_WithUnpublishedQuiz_ReturnsBadRequestAndDoesNotPersistAttempt()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizAsync(isPublished: false);
            var client = CreateAuthenticatedClient(seed.Student);

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/start", new
            {
                quizId = seed.QuizId,
                enrollmentId = seed.EnrollmentId
            });

            await AssertStatusCodeAsync(HttpStatusCode.BadRequest, response);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();

            Assert.False(await dbContext.QuizAttempts.AnyAsync());
        }

        [DockerAvailableFact]
        public async Task SubmitAnswer_WithCorrectSingleChoice_PersistsResponseAndStartsAttempt()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizAsync();
            var client = CreateAuthenticatedClient(seed.Student);
            var attemptId = await StartAttemptAsync(client, seed);

            var response = await client.PostAsJsonAsync($"/api/quiz-attempts/{attemptId}/answer", new
            {
                quizQuestionId = seed.FirstQuestionId,
                responseTimeMs = 1234,
                selectedOptionIds = new[] { seed.CorrectOptionId }
            });

            await AssertStatusCodeAsync(HttpStatusCode.OK, response);
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var nextQuestionId = json.RootElement.GetProperty("nextQuestionId").GetInt64();

            Assert.Equal(seed.SecondQuestionId, nextQuestionId);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var attempt = await dbContext.QuizAttempts
                .Include(attempt => attempt.Responses)
                    .ThenInclude(response => response.Options)
                .SingleAsync(attempt => attempt.Id == attemptId);
            var savedResponse = Assert.Single(attempt.Responses);

            Assert.Equal(AttemptStatus.Started, attempt.Status);
            Assert.Equal(seed.FirstQuestionId, savedResponse.QuizQuestionId);
            Assert.Equal(1234, savedResponse.ResponseTimeMs);
            Assert.Equal(5, savedResponse.Score);
            Assert.Equal(seed.CorrectOptionId, Assert.Single(savedResponse.Options).Id);
        }

        [DockerAvailableFact]
        public async Task GetNextQuestion_AfterAnswer_ReturnsNextQuestionIdAndAnsweredCount()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizAsync();
            var client = CreateAuthenticatedClient(seed.Student);
            var attemptId = await StartAttemptAsync(client, seed);

            var answerResponse = await client.PostAsJsonAsync($"/api/quiz-attempts/{attemptId}/answer", new
            {
                quizQuestionId = seed.FirstQuestionId,
                responseTimeMs = 1234,
                selectedOptionIds = new[] { seed.CorrectOptionId }
            });
            await AssertStatusCodeAsync(HttpStatusCode.OK, answerResponse);

            var response = await client.GetAsync($"/api/quiz-attempts/{attemptId}/next-question");

            await AssertStatusCodeAsync(HttpStatusCode.OK, response);
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            Assert.Equal(seed.SecondQuestionId, json.RootElement.GetProperty("nextQuestionId").GetInt64());
            Assert.Equal(1, json.RootElement.GetProperty("answeredQuestionsCount").GetInt32());
        }

        [DockerAvailableFact]
        public async Task CompleteAttempt_WithAutoGradedResponses_CompletesAttemptAndAwardsXpDelta()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizAsync(secondQuestion: false);
            var client = CreateAuthenticatedClient(seed.Student);
            var attemptId = await StartAttemptAsync(client, seed);

            var answerResponse = await client.PostAsJsonAsync($"/api/quiz-attempts/{attemptId}/answer", new
            {
                quizQuestionId = seed.FirstQuestionId,
                responseTimeMs = 900,
                selectedOptionIds = new[] { seed.CorrectOptionId }
            });
            await AssertStatusCodeAsync(HttpStatusCode.OK, answerResponse);

            var response = await client.PostAsync($"/api/quiz-attempts/{attemptId}/complete", null);

            await AssertStatusCodeAsync(HttpStatusCode.OK, response);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var attempt = await dbContext.QuizAttempts.SingleAsync(attempt => attempt.Id == attemptId);
            var student = await dbContext.Students.SingleAsync(student => student.Id == seed.Student.Id);

            Assert.Equal(AttemptStatus.Completed, attempt.Status);
            Assert.NotNull(attempt.EndTime);
            Assert.Equal(5, attempt.TotalScore);
            Assert.Equal(5, student.CurrentXP);
        }

        [DockerAvailableFact]
        public async Task GradeFreeTextResponse_WhenLastUngradedResponse_CompletesAttemptAndAwardsScore()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizAsync(secondQuestion: false, firstQuestionIsFreeText: true);
            var studentClient = CreateAuthenticatedClient(seed.Student);
            var teacherClient = CreateAuthenticatedClient(seed.Teacher);
            var attemptId = await StartAttemptAsync(studentClient, seed);

            var answerResponse = await studentClient.PostAsJsonAsync($"/api/quiz-attempts/{attemptId}/answer", new
            {
                quizQuestionId = seed.FirstQuestionId,
                responseTimeMs = 1500,
                freeTextAnswer = "PostgreSQL integration answer",
                selectedOptionIds = Array.Empty<long>()
            });
            await AssertStatusCodeAsync(HttpStatusCode.OK, answerResponse);

            var completeResponse = await studentClient.PostAsync($"/api/quiz-attempts/{attemptId}/complete", null);
            await AssertStatusCodeAsync(HttpStatusCode.OK, completeResponse);

            long responseId;
            using (var scope = _fixture.Factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
                var attempt = await dbContext.QuizAttempts
                    .Include(attempt => attempt.Responses)
                    .SingleAsync(attempt => attempt.Id == attemptId);

                Assert.Equal(AttemptStatus.PendingCorrection, attempt.Status);
                responseId = Assert.Single(attempt.Responses).Id;
            }

            var gradeResponse = await teacherClient.PatchAsJsonAsync($"/api/quiz-attempts/responses/{responseId}/grade", new
            {
                score = 4,
                isLiked = true
            });

            await AssertStatusCodeAsync(HttpStatusCode.OK, gradeResponse);

            using var verificationScope = _fixture.Factory.Services.CreateScope();
            var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var gradedAttempt = await verificationDbContext.QuizAttempts.SingleAsync(attempt => attempt.Id == attemptId);
            var gradedResponse = await verificationDbContext.QuizQuestionResponses.SingleAsync(response => response.Id == responseId);
            var student = await verificationDbContext.Students.SingleAsync(student => student.Id == seed.Student.Id);

            Assert.Equal(AttemptStatus.Completed, gradedAttempt.Status);
            Assert.Equal(4, gradedAttempt.TotalScore);
            Assert.Equal(4, gradedResponse.Score);
            Assert.True(gradedResponse.IsLiked);
            Assert.Equal(4, student.CurrentXP);
        }

        private async Task<QuizSeed> SeedQuizAsync(
            bool isPublished = true,
            DateTime? publishDate = null,
            bool secondQuestion = true,
            bool firstQuestionIsFreeText = false)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();

            var student = new Student
            {
                FirstName = "Live",
                LastName = "Student",
                Username = $"student-{Guid.NewGuid():N}"[..20],
                Email = $"student-{Guid.NewGuid():N}@sqeez.test",
                PasswordHash = "not-used",
                Role = UserRole.Student,
                IsEmailVerified = true,
                LastSeen = DateTime.UtcNow
            };

            var teacher = new Teacher
            {
                FirstName = "Live",
                LastName = "Teacher",
                Username = $"teacher-{Guid.NewGuid():N}"[..20],
                Email = $"teacher-{Guid.NewGuid():N}@sqeez.test",
                PasswordHash = "not-used",
                Role = UserRole.Teacher,
                IsEmailVerified = true,
                LastSeen = DateTime.UtcNow,
                Department = "Integration"
            };

            var subject = new Subject
            {
                Name = "Live integration subject",
                Code = $"LIVE-{Guid.NewGuid():N}"[..30],
                Description = "Subject for live PostgreSQL integration tests.",
                StartDate = DateTime.UtcNow.AddDays(-7),
                Teacher = teacher
            };

            var enrollment = new Enrollment
            {
                Student = student,
                Subject = subject,
                EnrolledAt = DateTime.UtcNow
            };

            var quiz = new Quiz
            {
                Title = "Live integration quiz",
                Description = "Quiz for live PostgreSQL integration tests.",
                CreatedAt = DateTime.UtcNow,
                PublishDate = isPublished ? publishDate ?? DateTime.UtcNow.AddHours(-1) : null,
                Subject = subject
            };

            var firstQuestion = new QuizQuestion
            {
                Quiz = quiz,
                Title = firstQuestionIsFreeText ? "Explain transactions" : "Pick the correct answer",
                Difficulty = 5,
                HasPenalty = true,
                TimeLimit = 60,
                IsStrictMultipleChoice = false
            };

            QuizOption? correctOption;
            QuizOption? wrongOption = null;

            if (firstQuestionIsFreeText)
            {
                correctOption = new QuizOption
                {
                    QuizQuestion = firstQuestion,
                    Text = "Expected answer",
                    IsCorrect = true,
                    IsFreeText = true
                };
            }
            else
            {
                correctOption = new QuizOption
                {
                    QuizQuestion = firstQuestion,
                    Text = "Correct",
                    IsCorrect = true
                };
                wrongOption = new QuizOption
                {
                    QuizQuestion = firstQuestion,
                    Text = "Wrong",
                    IsCorrect = false
                };
            }

            dbContext.Students.Add(student);
            dbContext.Teachers.Add(teacher);
            dbContext.Subjects.Add(subject);
            dbContext.Enrollments.Add(enrollment);
            dbContext.Quizzes.Add(quiz);
            dbContext.QuizQuestions.Add(firstQuestion);
            dbContext.QuizOptions.Add(correctOption);
            if (wrongOption != null)
            {
                dbContext.QuizOptions.Add(wrongOption);
            }

            QuizQuestion? secondQuizQuestion = null;
            if (secondQuestion)
            {
                secondQuizQuestion = new QuizQuestion
                {
                    Quiz = quiz,
                    Title = "Second question",
                    Difficulty = 3,
                    HasPenalty = false,
                    TimeLimit = 60,
                    IsStrictMultipleChoice = false,
                    Options =
                    {
                        new QuizOption { Text = "Second correct", IsCorrect = true },
                        new QuizOption { Text = "Second wrong", IsCorrect = false }
                    }
                };

                dbContext.QuizQuestions.Add(secondQuizQuestion);
            }

            await dbContext.SaveChangesAsync();

            return new QuizSeed(
                student,
                teacher,
                enrollment.Id,
                quiz.Id,
                firstQuestion.Id,
                secondQuizQuestion?.Id,
                correctOption.Id,
                wrongOption?.Id);
        }

        private HttpClient CreateAuthenticatedClient(Student user)
        {
            var client = _fixture.Factory.CreateClient();

            using var scope = _fixture.Factory.Services.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var tokenResult = tokenService.CreateToken(user);

            Assert.True(tokenResult.Success, tokenResult.ErrorMessage);
            client.DefaultRequestHeaders.Add("Cookie", $"sqeez_access_token={tokenResult.Data}");

            return client;
        }

        private static async Task<long> StartAttemptAsync(HttpClient client, QuizSeed seed)
        {
            var startResponse = await client.PostAsJsonAsync("/api/quiz-attempts/start", new
            {
                quizId = seed.QuizId,
                enrollmentId = seed.EnrollmentId
            });
            await AssertStatusCodeAsync(HttpStatusCode.OK, startResponse);

            using var json = await JsonDocument.ParseAsync(await startResponse.Content.ReadAsStreamAsync());
            return json.RootElement.GetProperty("id").GetInt64();
        }

        private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response)
        {
            if (response.StatusCode == expected)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected status code {expected}, got {response.StatusCode}. Response body: {body}");
        }

        private sealed record QuizSeed(
            Student Student,
            Teacher Teacher,
            long EnrollmentId,
            long QuizId,
            long FirstQuestionId,
            long? SecondQuestionId,
            long CorrectOptionId,
            long? WrongOptionId);
    }
}
