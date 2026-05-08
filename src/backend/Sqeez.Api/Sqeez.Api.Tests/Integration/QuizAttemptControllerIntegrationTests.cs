using System.Net;
using System.Net.Http.Json;
using Moq;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;

namespace Sqeez.Api.Tests.Integration
{
    public class QuizAttemptControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public QuizAttemptControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
        }

        [Fact]
        public async Task StartAttempt_UsesStudentIdFromClaims()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.StartAttemptAsync(
                    7,
                    It.Is<StartQuizAttemptDto>(dto => dto.QuizId == 5 && dto.EnrollmentId == 11)))
                .ReturnsAsync(ServiceResult<QuizAttemptDto>.Ok(
                    new QuizAttemptDto(20, 5, 11, DateTime.UtcNow, null, AttemptStatus.Created, 0, null, 3)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/start", new
            {
                quizId = 5,
                enrollmentId = 11
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.StartAttemptAsync(It.IsAny<long>(), It.IsAny<StartQuizAttemptDto>()),
                Times.Once);
        }

        [Fact]
        public async Task GradeResponse_WithInvalidScore_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PatchAsJsonAsync("/api/quiz-attempts/responses/3/grade", new
            {
                score = 1001,
                isLiked = true
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.GradeFreeTextResponseAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<GradeQuestionResponseDto>()),
                Times.Never);
        }

        [Fact]
        public async Task StartAttempt_WithoutAuthentication_ReturnsUnauthorizedBeforeCallingService()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/start", new
            {
                quizId = 5,
                enrollmentId = 11
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.StartAttemptAsync(It.IsAny<long>(), It.IsAny<StartQuizAttemptDto>()),
                Times.Never);
        }

        [Fact]
        public async Task StartAttempt_WithInvalidUserIdClaim_ReturnsUnauthorizedBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "not-a-number");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/start", new
            {
                quizId = 5,
                enrollmentId = 11
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.StartAttemptAsync(It.IsAny<long>(), It.IsAny<StartQuizAttemptDto>()),
                Times.Never);
        }

        [Fact]
        public async Task StartAttempt_WhenRetryLimitReached_MapsConflict()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.StartAttemptAsync(7, It.IsAny<StartQuizAttemptDto>()))
                .ReturnsAsync(ServiceResult<QuizAttemptDto>.Failure("Retry limit reached.", ServiceError.Conflict));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/start", new
            {
                quizId = 5,
                enrollmentId = 11
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task SubmitAnswer_UsesAttemptIdAndStudentIdFromRouteAndClaims()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.SubmitAnswerAsync(
                    20,
                    7,
                    It.Is<SubmitQuestionResponseDto>(dto =>
                        dto.QuizQuestionId == 3 &&
                        dto.ResponseTimeMs == 1500 &&
                        dto.SelectedOptionIds.SequenceEqual(new[] { 4L, 5L }))))
                .ReturnsAsync(ServiceResult<QuestionAnsweredDto>.Ok(
                    new QuestionAnsweredDto(30, 3, 1500, null, false, 10, new List<long> { 4, 5 }, new List<long> { 4 }, null, 6)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/20/answer", new
            {
                quizQuestionId = 3,
                responseTimeMs = 1500,
                selectedOptionIds = new[] { 4, 5 }
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.SubmitAnswerAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<SubmitQuestionResponseDto>()),
                Times.Once);
        }

        [Fact]
        public async Task SubmitAnswer_WithTooLongFreeTextAnswer_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/20/answer", new
            {
                quizQuestionId = 3,
                responseTimeMs = 1500,
                freeTextAnswer = new string('a', 4001),
                selectedOptionIds = Array.Empty<long>()
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.SubmitAnswerAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<SubmitQuestionResponseDto>()),
                Times.Never);
        }

        [Fact]
        public async Task SubmitAnswer_WithTooManySelectedOptions_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/20/answer", new
            {
                quizQuestionId = 3,
                responseTimeMs = 1500,
                selectedOptionIds = Enumerable.Range(1, 1001).ToArray()
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.SubmitAnswerAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<SubmitQuestionResponseDto>()),
                Times.Never);
        }

        [Fact]
        public async Task SubmitAnswer_WithResponseTimeOverLimit_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/20/answer", new
            {
                quizQuestionId = 3,
                responseTimeMs = 3600001,
                selectedOptionIds = new[] { 4 }
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.SubmitAnswerAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<SubmitQuestionResponseDto>()),
                Times.Never);
        }

        [Fact]
        public async Task SubmitAnswer_WhenAttemptIsCompleted_MapsConflict()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.SubmitAnswerAsync(20, 7, It.IsAny<SubmitQuestionResponseDto>()))
                .ReturnsAsync(ServiceResult<QuestionAnsweredDto>.Failure("Attempt is no longer in progress.", ServiceError.Conflict));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/quiz-attempts/20/answer", new
            {
                quizQuestionId = 3,
                responseTimeMs = 1500,
                selectedOptionIds = new[] { 4 }
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task GetNextPendingQuestion_WhenNoQuestionRemains_ReturnsOkWithNullQuestionIdAndProgress()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.GetNextPendingQuestionIdAsync(20, 7))
                .ReturnsAsync(ServiceResult<NextQuestionProgressDto>.Ok(new NextQuestionProgressDto(null, 4)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/quiz-attempts/20/next-question");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("\"nextQuestionId\":null", body);
            Assert.Contains("\"answeredQuestionsCount\":4", body);
            _factory.QuizAttemptServiceMock.Verify(service => service.GetNextPendingQuestionIdAsync(20, 7), Times.Once);
        }

        [Fact]
        public async Task GetNextPendingQuestion_WhenQuestionExists_ReturnsOkWithQuestionIdAndProgress()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.GetNextPendingQuestionIdAsync(20, 7))
                .ReturnsAsync(ServiceResult<NextQuestionProgressDto>.Ok(new NextQuestionProgressDto(3, 2)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/quiz-attempts/20/next-question");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("\"nextQuestionId\":3", body);
            Assert.Contains("\"answeredQuestionsCount\":2", body);
        }

        [Fact]
        public async Task CompleteAttempt_UsesAttemptIdAndStudentId()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.CompleteAttemptAsync(20, 7))
                .ReturnsAsync(ServiceResult<QuizAttemptDto>.Ok(
                    new QuizAttemptDto(20, 5, 11, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow, AttemptStatus.Completed, 18, 1)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsync("/api/quiz-attempts/20/complete", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(service => service.CompleteAttemptAsync(20, 7), Times.Once);
        }

        [Fact]
        public async Task CompleteAttempt_WhenUngradedFreeTextExists_ReturnsPendingCorrectionAttempt()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.CompleteAttemptAsync(20, 7))
                .ReturnsAsync(ServiceResult<QuizAttemptDto>.Ok(
                    new QuizAttemptDto(20, 5, 11, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow, AttemptStatus.PendingCorrection, 8, null)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsync("/api/quiz-attempts/20/complete", null);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"status\":\"PendingCorrection\"", body);
        }

        [Fact]
        public async Task GetAttemptDetails_UsesCurrentUserIdAndRole()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.GetAttemptDetailsAsync(20, 42, "Teacher"))
                .ReturnsAsync(ServiceResult<QuizAttemptDetailDto>.Ok(
                    new QuizAttemptDetailDto(20, 5, 11, DateTime.UtcNow, null, AttemptStatus.Started, 0, null, new List<QuestionResponseDto>())));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/quiz-attempts/20");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(service => service.GetAttemptDetailsAsync(20, 42, "Teacher"), Times.Once);
        }

        [Fact]
        public async Task GetAttemptDetails_WhenServiceReturnsForbidden_MapsForbidden()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.GetAttemptDetailsAsync(20, 7, "Student"))
                .ReturnsAsync(ServiceResult<QuizAttemptDetailDto>.Failure("Not your attempt.", ServiceError.Forbidden));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/quiz-attempts/20");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetAttemptsForQuiz_PassesPagingToService()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.GetAttemptsForQuizAsync(5, 42, 2, 15))
                .ReturnsAsync(ServiceResult<PagedResponse<QuizAttemptDto>>.Ok(
                    new PagedResponse<QuizAttemptDto>
                    {
                        Data = new[]
                        {
                            new QuizAttemptDto(20, 5, 11, DateTime.UtcNow, null, AttemptStatus.Started, 0, null)
                        },
                        PageNumber = 2,
                        PageSize = 15,
                        TotalCount = 1
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/quiz-attempts/quiz/5?pageNumber=2&pageSize=15");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(service => service.GetAttemptsForQuizAsync(5, 42, 2, 15), Times.Once);
        }

        [Fact]
        public async Task GradeResponse_AsStudent_ReturnsForbiddenBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PatchAsJsonAsync("/api/quiz-attempts/responses/3/grade", new
            {
                score = 5,
                isLiked = true
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.GradeFreeTextResponseAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<GradeQuestionResponseDto>()),
                Times.Never);
        }

        [Fact]
        public async Task GradeResponse_AsTeacher_UsesResponseIdAndTeacherId()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.GradeFreeTextResponseAsync(
                    3,
                    42,
                    It.Is<GradeQuestionResponseDto>(dto => dto.Score == 8 && dto.IsLiked)))
                .ReturnsAsync(ServiceResult<QuestionResponseDto>.Ok(
                    new QuestionResponseDto(3, 9, 2500, "Free answer", true, 8, new List<long>())));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PatchAsJsonAsync("/api/quiz-attempts/responses/3/grade", new
            {
                score = 8,
                isLiked = true
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.GradeFreeTextResponseAsync(3, 42, It.IsAny<GradeQuestionResponseDto>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAttempt_AsStudent_ReturnsForbiddenBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.DeleteAsync("/api/quiz-attempts/20");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.DeleteAttemptAsync(It.IsAny<long>(), It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAttempt_WhenServiceReturnsNotFound_MapsNotFound()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.DeleteAttemptAsync(20, 42))
                .ReturnsAsync(ServiceResult<bool>.Failure("Attempt not found.", ServiceError.NotFound));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.DeleteAsync("/api/quiz-attempts/20");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAllAttemptsForQuiz_AsAdmin_PassesAdminFlag()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.DeleteAllAttemptsForQuizAsync(5, 1, true))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.DeleteAsync("/api/quiz-attempts/5/attempts");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.DeleteAllAttemptsForQuizAsync(5, 1, true),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAllAttemptsForQuiz_AsTeacher_PassesAdminFlagFalse()
        {
            _factory.QuizAttemptServiceMock
                .Setup(service => service.DeleteAllAttemptsForQuizAsync(5, 42, false))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.DeleteAsync("/api/quiz-attempts/5/attempts");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizAttemptServiceMock.Verify(
                service => service.DeleteAllAttemptsForQuizAsync(5, 42, false),
                Times.Once);
        }
    }
}
