using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sqeez.Api.Data;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Media;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.Users;

namespace Sqeez.Api.Tests.Integration.Postgres
{
    [Collection(PostgresIntegrationTestCollection.CollectionName)]
    public class QuizContentMediaStatisticsPostgresIntegrationTests
    {
        private readonly PostgresIntegrationTestFixture _fixture;

        public QuizContentMediaStatisticsPostgresIntegrationTests(PostgresIntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [DockerAvailableFact]
        public async Task AddQuizToSubject_AsTeacher_PersistsQuizUsingRouteSubjectId()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedSubjectAsync();
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Teacher);

            var response = await client.PostAsJsonAsync($"/api/subjects/{seed.Subject.Id}/quizzes", new
            {
                title = "Route quiz",
                description = "Created through subject route.",
                subjectId = 999999,
                maxRetries = 2,
                publishDate = DateTime.UtcNow.AddDays(-1)
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, response);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var quiz = await dbContext.Quizzes.SingleAsync();

            Assert.Equal(seed.Subject.Id, quiz.SubjectId);
            Assert.Equal("Route quiz", quiz.Title);
            Assert.Equal(2, quiz.MaxRetries);
        }

        [DockerAvailableFact]
        public async Task CreateQuestionAndOption_AsTeacher_PersistsQuestionTree()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedSubjectAsync(withQuiz: true);
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Teacher);

            var questionResponse = await client.PostAsJsonAsync($"/api/quizzes/{seed.Quiz!.Id}/questions", new
            {
                title = "Live created question",
                difficulty = 7,
                timeLimit = 45,
                quizId = 0,
                hasPenalty = true
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, questionResponse);
            using var questionJson = await JsonDocument.ParseAsync(await questionResponse.Content.ReadAsStreamAsync());
            var questionId = questionJson.RootElement.GetProperty("id").GetInt64();

            var optionResponse = await client.PostAsJsonAsync($"/api/quizzes/{seed.Quiz.Id}/questions/{questionId}/options", new
            {
                text = "Correct option",
                isCorrect = true,
                quizQuestionID = 0
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, optionResponse);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var question = await dbContext.QuizQuestions
                .Include(q => q.Options)
                .SingleAsync(q => q.Id == questionId);

            Assert.Equal(seed.Quiz.Id, question.QuizId);
            Assert.Equal(7, question.Difficulty);
            Assert.Equal(3, question.PenaltyPoints);
            Assert.Equal("Correct option", Assert.Single(question.Options).Text);
        }

        [DockerAvailableFact]
        public async Task PatchOption_BeforeAttempts_UpdatesOptionTextAndCorrectness()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizTreeAsync();
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Teacher);

            var response = await client.PatchAsJsonAsync($"/api/quizzes/{seed.Quiz.Id}/questions/{seed.Question.Id}/options/{seed.WrongOption.Id}", new
            {
                text = "Promoted answer",
                isCorrect = true
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, response);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var option = await dbContext.QuizOptions.SingleAsync(option => option.Id == seed.WrongOption.Id);

            Assert.Equal("Promoted answer", option.Text);
            Assert.True(option.IsCorrect);
        }

        [DockerAvailableFact]
        public async Task GetDetailedQuestion_AsStudentWithActiveAttempt_ReturnsStudentSafeOptions()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizTreeAsync(enrollStudent: true, createAttempt: true);
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Student!);

            var response = await client.GetAsync($"/api/quizzes/{seed.Quiz.Id}/questions/{seed.Question.Id}/detailed");

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, response);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("Correct answer", body);
            Assert.DoesNotContain("isCorrect", body, StringComparison.OrdinalIgnoreCase);
        }

        [DockerAvailableFact]
        public async Task GetDetailedQuestion_AsStudentAfterClosedQuiz_ReturnsStudentSafeOptions()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizTreeAsync(enrollStudent: true);

            using (var scope = _fixture.Factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
                var quiz = await dbContext.Quizzes.SingleAsync(quiz => quiz.Id == seed.Quiz.Id);
                quiz.ClosingDate = DateTime.UtcNow.AddMinutes(-1);

                await dbContext.SaveChangesAsync();
            }

            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Student!);

            var response = await client.GetAsync($"/api/quizzes/{seed.Quiz.Id}/questions/{seed.Question.Id}/detailed");

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, response);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("Correct answer", body);
            Assert.DoesNotContain("isCorrect", body, StringComparison.OrdinalIgnoreCase);
        }

        [DockerAvailableFact]
        public async Task DeleteQuiz_WithAttempts_SoftClosesQuiz()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizTreeAsync(enrollStudent: true, createAttempt: true);
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Teacher);

            var response = await client.DeleteAsync($"/api/quizzes/{seed.Quiz.Id}");

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, response);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var quiz = await dbContext.Quizzes.SingleAsync(quiz => quiz.Id == seed.Quiz.Id);

            Assert.NotNull(quiz.ClosingDate);
        }

        [DockerAvailableFact]
        public async Task CreateAndPatchMediaAsset_AsTeacher_PersistsMetadata()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedSubjectAsync();
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Teacher);

            var createResponse = await client.PostAsJsonAsync("/api/media-assets", new
            {
                locationUrl = "/media/live-image.png",
                mimeType = "Image",
                isPrivate = true,
                ownerId = seed.Teacher.Id,
                description = "Initial media"
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, createResponse);
            using var json = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync());
            var mediaId = json.RootElement.GetProperty("id").GetInt64();

            var patchResponse = await client.PatchAsJsonAsync($"/api/media-assets/{mediaId}", new
            {
                description = "Updated media",
                isPrivate = false
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, patchResponse);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var media = await dbContext.MediaAssets.SingleAsync(asset => asset.Id == mediaId);

            Assert.Equal("Updated media", media.Description);
            Assert.False(media.IsPrivate);
        }

        [DockerAvailableFact]
        public async Task UploadFile_AsTeacher_UsesStorageAndCreatesMediaAsset()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedSubjectAsync();
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Teacher);
            using var content = new MultipartFormDataContent
            {
                { new StringContent("true"), "IsPrivate" },
                { new StringContent("Uploaded live image"), "Description" }
            };
            content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("fake image")), "File", "live.png");
            content.Last().Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

            var response = await client.PostAsync("/api/media-assets/upload", content);

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, response);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var media = await dbContext.MediaAssets.SingleAsync();

            Assert.Equal(MediaType.Image, media.MimeType);
            Assert.Equal(seed.Teacher.Id, media.OwnerId);
            Assert.Equal("/test-storage/live.png", media.LocationUrl);
        }

        [DockerAvailableFact]
        public async Task GetPrivateMediaFile_AsDifferentTeacher_ReturnsForbiddenBeforeStorageLookup()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedSubjectAsync();
            var otherTeacher = await AddTeacherAsync("other-teacher");
            var media = await AddMediaAssetAsync(seed.Teacher, isPrivate: true);
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, otherTeacher);

            var response = await client.GetAsync($"/api/media-assets/{media.Id}/file");

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.Forbidden, response);
        }

        [DockerAvailableFact]
        public async Task GetQuizStatistics_AsTeacher_ReturnsSummaryAndQuestionStats()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedQuizTreeAsync(enrollStudent: true, createAttempt: true, completedAttempt: true);
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Teacher);

            var summaryResponse = await client.GetAsync($"/api/quizzes/{seed.Quiz.Id}/statistics/summary");
            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, summaryResponse);
            var summaryBody = await summaryResponse.Content.ReadAsStringAsync();

            var questionsResponse = await client.GetAsync($"/api/quizzes/{seed.Quiz.Id}/statistics/questions");
            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, questionsResponse);
            var questionsBody = await questionsResponse.Content.ReadAsStringAsync();

            Assert.Contains("\"totalAttempts\":1", summaryBody);
            Assert.Contains("\"completedAttempts\":1", summaryBody);
            Assert.Contains("\"averageScore\":5", summaryBody);
            Assert.Contains("\"totalAnswers\":1", questionsBody);
            Assert.Contains("\"pickCount\":1", questionsBody);
        }

        private async Task<SubjectSeed> SeedSubjectAsync(bool withQuiz = false)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var teacher = await PostgresTestHelpers.SeedTeacherAsync(dbContext);

            var subject = new Subject
            {
                Name = "Quiz content subject",
                Code = PostgresTestHelpers.UniqueValue("quiz-subject", 30),
                StartDate = DateTime.UtcNow.AddDays(-7),
                TeacherId = teacher.Id
            };
            dbContext.Subjects.Add(subject);
            await dbContext.SaveChangesAsync();

            Quiz? quiz = null;
            if (withQuiz)
            {
                quiz = new Quiz
                {
                    Title = "Seeded quiz",
                    Description = "Seeded quiz for content tests.",
                    SubjectId = subject.Id,
                    CreatedAt = DateTime.UtcNow,
                    PublishDate = DateTime.UtcNow.AddDays(-1)
                };
                dbContext.Quizzes.Add(quiz);
                await dbContext.SaveChangesAsync();
            }

            return new SubjectSeed(teacher, subject, quiz);
        }

        private async Task<QuizTreeSeed> SeedQuizTreeAsync(
            bool enrollStudent = false,
            bool createAttempt = false,
            bool completedAttempt = false)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var teacher = await PostgresTestHelpers.SeedTeacherAsync(dbContext);
            Student? student = enrollStudent || createAttempt
                ? await PostgresTestHelpers.SeedStudentAsync(dbContext)
                : null;

            var subject = new Subject
            {
                Name = "Quiz tree subject",
                Code = PostgresTestHelpers.UniqueValue("tree-subject", 30),
                StartDate = DateTime.UtcNow.AddDays(-7),
                TeacherId = teacher.Id
            };
            dbContext.Subjects.Add(subject);
            await dbContext.SaveChangesAsync();

            Enrollment? enrollment = null;
            if (student != null)
            {
                enrollment = new Enrollment
                {
                    StudentId = student.Id,
                    SubjectId = subject.Id,
                    EnrolledAt = DateTime.UtcNow
                };
                dbContext.Enrollments.Add(enrollment);
                await dbContext.SaveChangesAsync();
            }

            var quiz = new Quiz
            {
                Title = "Quiz tree",
                Description = "Seeded quiz tree.",
                SubjectId = subject.Id,
                CreatedAt = DateTime.UtcNow,
                PublishDate = DateTime.UtcNow.AddDays(-1)
            };
            dbContext.Quizzes.Add(quiz);
            await dbContext.SaveChangesAsync();

            var question = new QuizQuestion
            {
                QuizId = quiz.Id,
                Title = "Seeded question",
                Difficulty = 5,
                HasPenalty = true,
                TimeLimit = 30
            };
            dbContext.QuizQuestions.Add(question);
            await dbContext.SaveChangesAsync();

            var correctOption = new QuizOption
            {
                QuizQuestionId = question.Id,
                Text = "Correct answer",
                IsCorrect = true
            };
            var wrongOption = new QuizOption
            {
                QuizQuestionId = question.Id,
                Text = "Wrong answer",
                IsCorrect = false
            };
            dbContext.QuizOptions.AddRange(correctOption, wrongOption);
            await dbContext.SaveChangesAsync();

            QuizAttempt? attempt = null;
            if (createAttempt)
            {
                attempt = new QuizAttempt
                {
                    QuizId = quiz.Id,
                    EnrollmentId = enrollment!.Id,
                    StartTime = DateTime.UtcNow.AddMinutes(-5),
                    EndTime = completedAttempt ? DateTime.UtcNow : null,
                    Status = completedAttempt ? AttemptStatus.Completed : AttemptStatus.Started,
                    TotalScore = completedAttempt ? 5 : 0
                };
                dbContext.QuizAttempts.Add(attempt);
                await dbContext.SaveChangesAsync();

                if (completedAttempt)
                {
                    var response = new QuizQuestionResponse
                    {
                        QuizAttemptId = attempt.Id,
                        QuizQuestionId = question.Id,
                        ResponseTimeMs = 2000,
                        Score = 5
                    };
                    response.Options.Add(correctOption);
                    dbContext.QuizQuestionResponses.Add(response);
                    await dbContext.SaveChangesAsync();
                }
            }

            return new QuizTreeSeed(teacher, student, subject, enrollment, quiz, question, correctOption, wrongOption, attempt);
        }

        private async Task<Teacher> AddTeacherAsync(string prefix)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();

            return await PostgresTestHelpers.SeedTeacherAsync(dbContext, prefix);
        }

        private async Task<MediaAsset> AddMediaAssetAsync(Teacher teacher, bool isPrivate)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var media = new MediaAsset
            {
                OwnerId = teacher.Id,
                LocationUrl = "/media/private.png",
                MimeType = MediaType.Image,
                IsPrivate = isPrivate,
                Description = "Private media"
            };

            dbContext.MediaAssets.Add(media);
            await dbContext.SaveChangesAsync();
            return media;
        }

        private sealed record SubjectSeed(Teacher Teacher, Subject Subject, Quiz? Quiz);

        private sealed record QuizTreeSeed(
            Teacher Teacher,
            Student? Student,
            Subject Subject,
            Enrollment? Enrollment,
            Quiz Quiz,
            QuizQuestion Question,
            QuizOption CorrectOption,
            QuizOption WrongOption,
            QuizAttempt? Attempt);
    }
}
