using System.Net;
using System.Net.Http.Json;
using Moq;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;

namespace Sqeez.Api.Tests.Integration
{
    public class SubjectAndQuizControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public SubjectAndQuizControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
        }

        [Fact]
        public async Task CreateSubject_AsAdmin_CallsSubjectService()
        {
            _factory.SubjectServiceMock
                .Setup(service => service.CreateSubjectAsync(It.Is<CreateSubjectDto>(dto =>
                    dto.Name == "Mathematics" &&
                    dto.Code == "MATH-1")))
                .ReturnsAsync(ServiceResult<SubjectDto>.Ok(
                    new SubjectDto(5, "Mathematics", "MATH-1", null, DateTime.UtcNow, null, null, null, null, null, 0, 0)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.PostAsJsonAsync("/api/subjects", new
            {
                name = "Mathematics",
                code = "MATH-1"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.SubjectServiceMock.Verify(service => service.CreateSubjectAsync(It.IsAny<CreateSubjectDto>()), Times.Once);
        }

        [Fact]
        public async Task DeleteSubjectEnrollments_WithDeleteAllAsAdmin_CallsEnrollmentService()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.DeleteAllEnrollmentsFromSubjectAsync(5, "Admin"))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.DeleteAsync("/api/subjects/5/enrollments?deleteAll=true");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.DeleteAllEnrollmentsFromSubjectAsync(5, "Admin"),
                Times.Once);
        }

        [Fact]
        public async Task DeleteSubjectEnrollments_WithDeleteAllAsTeacher_ReturnsForbiddenBeforeServiceCall()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.DeleteAsync("/api/subjects/5/enrollments?deleteAll=true");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.DeleteAllEnrollmentsFromSubjectAsync(It.IsAny<long>(), It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteSubjectQuizzes_WithDeleteAllAsAdmin_CallsQuizService()
        {
            _factory.QuizServiceMock
                .Setup(service => service.DeleteAllQuizzesFromSubjectAsync(5, true))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.DeleteAsync("/api/subjects/5/quizzes?deleteAll=true");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizServiceMock.Verify(
                service => service.DeleteAllQuizzesFromSubjectAsync(5, true),
                Times.Once);
        }

        [Fact]
        public async Task DeleteSubjectQuizzes_WithoutDeleteAll_ReturnsBadRequestBeforeServiceCall()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.DeleteAsync("/api/subjects/5/quizzes");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.QuizServiceMock.Verify(
                service => service.DeleteAllQuizzesFromSubjectAsync(It.IsAny<long>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task EnrollStudents_AsStudentForAnotherStudent_ReturnsForbiddenBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/subjects/5/enrollments", new
            {
                studentIds = new[] { 8 }
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.EnrollStudentsInSubjectAsync(It.IsAny<long>(), It.IsAny<AssignStudentsDto>()),
                Times.Never);
        }

        [Fact]
        public async Task EnrollStudents_AsStudentForSelf_CallsEnrollmentService()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.EnrollStudentsInSubjectAsync(
                    5,
                    It.Is<AssignStudentsDto>(dto => dto.StudentIds.SequenceEqual(new[] { 7L }))))
                .ReturnsAsync(ServiceResult<BulkEnrollmentResultDto>.Ok(
                    new BulkEnrollmentResultDto
                    {
                        NewlyEnrolledIds = new List<long> { 7 }
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/subjects/5/enrollments", new
            {
                studentIds = new[] { 7 }
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.EnrollStudentsInSubjectAsync(5, It.IsAny<AssignStudentsDto>()),
                Times.Once);
        }

        [Fact]
        public async Task EnrollStudents_AsAdminCanEnrollMultipleStudents()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.EnrollStudentsInSubjectAsync(
                    5,
                    It.Is<AssignStudentsDto>(dto => dto.StudentIds.SequenceEqual(new[] { 7L, 8L }))))
                .ReturnsAsync(ServiceResult<BulkEnrollmentResultDto>.Ok(
                    new BulkEnrollmentResultDto
                    {
                        NewlyEnrolledIds = new List<long> { 7 },
                        AlreadyEnrolledIds = new List<long> { 8 }
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.PostAsJsonAsync("/api/subjects/5/enrollments", new
            {
                studentIds = new[] { 7, 8 }
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.EnrollStudentsInSubjectAsync(5, It.IsAny<AssignStudentsDto>()),
                Times.Once);
        }

        [Fact]
        public async Task EnrollStudents_WithTooManyIds_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.PostAsJsonAsync("/api/subjects/5/enrollments", new
            {
                studentIds = Enumerable.Range(1, 1001).ToArray()
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.EnrollStudentsInSubjectAsync(It.IsAny<long>(), It.IsAny<AssignStudentsDto>()),
                Times.Never);
        }

        [Fact]
        public async Task EnrollStudents_WhenServiceReturnsForbidden_MapsForbidden()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.EnrollStudentsInSubjectAsync(5, It.IsAny<AssignStudentsDto>()))
                .ReturnsAsync(ServiceResult<BulkEnrollmentResultDto>.Failure("Subject is closed.", ServiceError.Forbidden));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PostAsJsonAsync("/api/subjects/5/enrollments", new
            {
                studentIds = new[] { 7 }
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetEnrollmentsForSubject_ForcesSubjectIdFilter()
        {
            _factory.SubjectServiceMock
                .Setup(service => service.GetSubjectByIdAsync(5))
                .ReturnsAsync(ServiceResult<SubjectDto>.Ok(
                    new SubjectDto(5, "Math", "MATH", null, DateTime.UtcNow, null, 42, "teacher", null, null, 0, 0)));

            _factory.EnrollmentServiceMock
                .Setup(service => service.GetAllEnrollmentsAsync(It.Is<EnrollmentFilterDto>(filter =>
                    filter.SubjectId == 5 &&
                    filter.StudentId == 7 &&
                    filter.Mark == 2),
                    42,
                    "Teacher"))
                .ReturnsAsync(ServiceResult<PagedResponse<EnrollmentDto>>.Ok(
                    new PagedResponse<EnrollmentDto>
                    {
                        Data = Array.Empty<EnrollmentDto>(),
                        PageNumber = 1,
                        PageSize = 10,
                        TotalCount = 0
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/subjects/5/enrollments?SubjectId=999&StudentId=7&Mark=2");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.GetAllEnrollmentsAsync(It.IsAny<EnrollmentFilterDto>(), 42, "Teacher"),
                Times.Once);
        }

        [Fact]
        public async Task GetEnrollmentsForSubject_WhenServiceReturnsForbidden_MapsForbidden()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.GetAllEnrollmentsAsync(
                    It.IsAny<EnrollmentFilterDto>(),
                    42,
                    "Teacher"))
                .ReturnsAsync(ServiceResult<PagedResponse<EnrollmentDto>>.Failure(
                    "You do not have permission to view enrollments for this subject.",
                    ServiceError.Forbidden));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/subjects/5/enrollments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.GetAllEnrollmentsAsync(It.IsAny<EnrollmentFilterDto>(), 42, "Teacher"),
                Times.Once);
        }

        [Fact]
        public async Task GetEnrollmentsForSubject_AsStudent_PassesUserContextToService()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.GetAllEnrollmentsAsync(It.Is<EnrollmentFilterDto>(filter =>
                    filter.SubjectId == 5 &&
                    filter.StudentId == 99),
                    7,
                    "Student"))
                .ReturnsAsync(ServiceResult<PagedResponse<EnrollmentDto>>.Ok(
                    new PagedResponse<EnrollmentDto>
                    {
                        Data = Array.Empty<EnrollmentDto>(),
                        PageNumber = 1,
                        PageSize = 10,
                        TotalCount = 0
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/subjects/5/enrollments?StudentId=99");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.GetAllEnrollmentsAsync(It.IsAny<EnrollmentFilterDto>(), 7, "Student"),
                Times.Once);
        }

        [Fact]
        public async Task UnenrollStudents_AsTeacher_CallsEnrollmentServiceWithRouteSubjectId()
        {
            _factory.SubjectServiceMock
                .Setup(service => service.GetSubjectByIdAsync(5))
                .ReturnsAsync(ServiceResult<SubjectDto>.Ok(
                    new SubjectDto(5, "Math", "MATH", null, DateTime.UtcNow, null, 42, "teacher", null, null, 0, 0)));

            _factory.EnrollmentServiceMock
                .Setup(service => service.UnenrollStudentsFromSubjectAsync(
                    5,
                    It.Is<RemoveStudentsDto>(dto => dto.StudentIds.SequenceEqual(new[] { 7L, 8L }))))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/subjects/5/enrollments")
            {
                Content = JsonContent.Create(new
                {
                    studentIds = new[] { 7, 8 }
                })
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.UnenrollStudentsFromSubjectAsync(5, It.IsAny<RemoveStudentsDto>()),
                Times.Once);
        }

        [Fact]
        public async Task UnenrollStudents_AsStudent_ReturnsForbiddenBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/subjects/5/enrollments")
            {
                Content = JsonContent.Create(new
                {
                    studentIds = new[] { 7 }
                })
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.UnenrollStudentsFromSubjectAsync(It.IsAny<long>(), It.IsAny<RemoveStudentsDto>()),
                Times.Never);
        }

        [Fact]
        public async Task UnenrollStudents_WithTooManyIds_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/subjects/5/enrollments")
            {
                Content = JsonContent.Create(new
                {
                    studentIds = Enumerable.Range(1, 1001).ToArray()
                })
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.UnenrollStudentsFromSubjectAsync(It.IsAny<long>(), It.IsAny<RemoveStudentsDto>()),
                Times.Never);
        }

        [Fact]
        public async Task UnenrollStudents_WhenServiceReturnsNotFound_MapsNotFound()
        {
            _factory.SubjectServiceMock
                .Setup(service => service.GetSubjectByIdAsync(5))
                .ReturnsAsync(ServiceResult<SubjectDto>.Ok(
                    new SubjectDto(5, "Math", "MATH", null, DateTime.UtcNow, null, 42, "teacher", null, null, 0, 0)));

            _factory.EnrollmentServiceMock
                .Setup(service => service.UnenrollStudentsFromSubjectAsync(5, It.IsAny<RemoveStudentsDto>()))
                .ReturnsAsync(ServiceResult<bool>.Failure("Subject not found.", ServiceError.NotFound));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/subjects/5/enrollments")
            {
                Content = JsonContent.Create(new
                {
                    studentIds = new[] { 7 }
                })
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddQuizToSubject_UsesSubjectIdFromRouteAndCurrentUserId()
        {
            _factory.QuizServiceMock
                .Setup(service => service.CreateQuizAsync(
                    It.Is<CreateQuizDto>(dto => dto.SubjectId == 5 && dto.Title == "Intro quiz"),
                    42))
                .ReturnsAsync(ServiceResult<QuizDto>.Ok(
                    new QuizDto(10, "Intro quiz", "Basics", 2, DateTime.UtcNow, null, null, 5, 0, 0)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PostAsJsonAsync("/api/subjects/5/quizzes", new
            {
                title = "Intro quiz",
                description = "Basics",
                subjectId = 999,
                maxRetries = 2
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizServiceMock.Verify(
                service => service.CreateQuizAsync(It.IsAny<CreateQuizDto>(), It.IsAny<long>()),
                Times.Once);
        }

        [Fact]
        public async Task AddQuizToSubject_WithPublishDateWithoutOffset_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PostAsJsonAsync("/api/subjects/5/quizzes", new
            {
                title = "Intro quiz",
                description = "Basics",
                maxRetries = 2,
                publishDate = "2026-01-15T08:30:00"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.QuizServiceMock.Verify(
                service => service.CreateQuizAsync(It.IsAny<CreateQuizDto>(), It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task PatchQuiz_WithClosingDateWithoutOffset_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PatchAsJsonAsync("/api/quizzes/10", new
            {
                closingDate = "2026-01-15T08:30:00"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.QuizServiceMock.Verify(
                service => service.PatchQuizAsync(It.IsAny<long>(), It.IsAny<PatchQuizDto>(), It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task GetQuizzesForSubject_ForcesSubjectIdFilter()
        {
            _factory.QuizServiceMock
                .Setup(service => service.GetAllQuizzesAsync(It.Is<QuizFilterDto>(filter =>
                    filter.SubjectId == 5 &&
                    filter.SearchTerm == "exam")))
                .ReturnsAsync(ServiceResult<PagedResponse<QuizDto>>.Ok(
                    new PagedResponse<QuizDto>
                    {
                        Data = Array.Empty<QuizDto>(),
                        PageNumber = 1,
                        PageSize = 10,
                        TotalCount = 0
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/subjects/5/quizzes?SubjectId=999&SearchTerm=exam");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.QuizServiceMock.Verify(
                service => service.GetAllQuizzesAsync(It.IsAny<QuizFilterDto>()),
                Times.Once);
        }

        [Fact]
        public async Task GetQuizzes_WithPublishDateWithoutOffset_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/quizzes?PublishDate=2026-01-15T08:30:00");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.QuizServiceMock.Verify(
                service => service.GetAllQuizzesAsync(It.IsAny<QuizFilterDto>()),
                Times.Never);
        }
    }
}
