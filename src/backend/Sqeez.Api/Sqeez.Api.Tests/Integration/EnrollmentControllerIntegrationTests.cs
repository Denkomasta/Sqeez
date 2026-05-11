using System.Net;
using System.Net.Http.Json;
using Moq;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;

namespace Sqeez.Api.Tests.Integration
{
    public class EnrollmentControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EnrollmentControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
        }

        [Fact]
        public async Task GetAllEnrollments_PassesFilterToService()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.GetAllEnrollmentsAsync(It.Is<EnrollmentFilterDto>(filter =>
                    filter.Mark == 2 &&
                    filter.StudentId == 7 &&
                    filter.SubjectId == 5 &&
                    filter.IsActive == true &&
                    filter.PageNumber == 2 &&
                    filter.PageSize == 15),
                    42,
                    "Admin"))
                .ReturnsAsync(ServiceResult<PagedResponse<EnrollmentDto>>.Ok(
                    new PagedResponse<EnrollmentDto>
                    {
                        Data = new[]
                        {
                            CreateEnrollment(id: 10, studentId: 7, subjectId: 5, mark: 2)
                        },
                        PageNumber = 2,
                        PageSize = 15,
                        TotalCount = 1
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.GetAsync("/api/enrollments?Mark=2&StudentId=7&SubjectId=5&IsActive=true&PageNumber=2&PageSize=15");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.GetAllEnrollmentsAsync(It.IsAny<EnrollmentFilterDto>(), 42, "Admin"),
                Times.Once);
        }

        [Fact]
        public async Task GetAllEnrollments_AsStudent_PassesUserContextToService()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.GetAllEnrollmentsAsync(It.Is<EnrollmentFilterDto>(filter =>
                    filter.StudentId == 99 &&
                    filter.SubjectId == 5),
                    7,
                    "Student"))
                .ReturnsAsync(ServiceResult<PagedResponse<EnrollmentDto>>.Ok(
                    new PagedResponse<EnrollmentDto>
                    {
                        Data = new[] { CreateEnrollment(id: 10, studentId: 7, subjectId: 5, mark: 2) },
                        PageNumber = 1,
                        PageSize = 10,
                        TotalCount = 1
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/enrollments?StudentId=99&SubjectId=5");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.GetAllEnrollmentsAsync(It.IsAny<EnrollmentFilterDto>(), 7, "Student"),
                Times.Once);
        }

        [Fact]
        public async Task GetAllEnrollments_WhenServiceReturnsForbidden_MapsForbidden()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.GetAllEnrollmentsAsync(
                    It.IsAny<EnrollmentFilterDto>(),
                    42,
                    "Teacher"))
                .ReturnsAsync(ServiceResult<PagedResponse<EnrollmentDto>>.Failure(
                    "Teachers must filter by an owned subject unless they are viewing their own enrollments.",
                    ServiceError.Forbidden));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/enrollments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.GetAllEnrollmentsAsync(It.IsAny<EnrollmentFilterDto>(), 42, "Teacher"),
                Times.Once);
        }

        [Fact]
        public async Task GetAllEnrollments_WithInvalidMarkFilter_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/enrollments?Mark=6");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.GetAllEnrollmentsAsync(
                    It.IsAny<EnrollmentFilterDto>(),
                    It.IsAny<long>(),
                    It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task GetEnrollmentById_WhenServiceReturnsNotFound_MapsNotFound()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.GetEnrollmentByIdAsync(10, 42, "Teacher"))
                .ReturnsAsync(ServiceResult<EnrollmentDto>.Failure("Enrollment not found.", ServiceError.NotFound));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/enrollments/10");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(service => service.GetEnrollmentByIdAsync(10, 42, "Teacher"), Times.Once);
        }

        [Fact]
        public async Task GetEnrollmentById_AsDifferentStudent_ReturnsForbidden()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.GetEnrollmentByIdAsync(10, 7, "Student"))
                .ReturnsAsync(ServiceResult<EnrollmentDto>.Failure("You do not have permission to view this enrollment.", ServiceError.Forbidden));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/enrollments/10");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetEnrollmentById_AsTeacherViewingOwnEnrollment_ReturnsEnrollment()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.GetEnrollmentByIdAsync(10, 42, "Teacher"))
                .ReturnsAsync(ServiceResult<EnrollmentDto>.Ok(CreateEnrollment(id: 10, studentId: 42, subjectId: 5, mark: 2)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/enrollments/10");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.SubjectServiceMock.Verify(
                service => service.GetSubjectByIdAsync(It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task GetEnrollmentById_AsSubjectTeacher_ReturnsEnrollment()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.GetEnrollmentByIdAsync(10, 42, "Teacher"))
                .ReturnsAsync(ServiceResult<EnrollmentDto>.Ok(CreateEnrollment(id: 10, studentId: 8, subjectId: 5, mark: 2)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/enrollments/10");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PatchEnrollment_AsTeacher_PassesEnrollmentIdDtoAndCurrentUserId()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.PatchEnrollmentAsync(
                    10,
                    It.Is<PatchEnrollmentDto>(dto => dto.Mark == 1 && dto.RemoveMark == false),
                    42))
                .ReturnsAsync(ServiceResult<EnrollmentDto>.Ok(
                    CreateEnrollment(id: 10, studentId: 7, subjectId: 5, mark: 1)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PatchAsJsonAsync("/api/enrollments/10", new
            {
                mark = 1,
                removeMark = false
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.PatchEnrollmentAsync(10, It.IsAny<PatchEnrollmentDto>(), 42),
                Times.Once);
        }

        [Fact]
        public async Task PatchEnrollment_RemoveMark_PassesRemoveMarkFlag()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.PatchEnrollmentAsync(
                    10,
                    It.Is<PatchEnrollmentDto>(dto => dto.Mark == null && dto.RemoveMark == true),
                    42))
                .ReturnsAsync(ServiceResult<EnrollmentDto>.Ok(
                    CreateEnrollment(id: 10, studentId: 7, subjectId: 5, mark: null)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PatchAsJsonAsync("/api/enrollments/10", new
            {
                removeMark = true
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.PatchEnrollmentAsync(10, It.IsAny<PatchEnrollmentDto>(), 42),
                Times.Once);
        }

        [Fact]
        public async Task PatchEnrollment_WhenServiceReturnsValidationFailed_MapsBadRequest()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.PatchEnrollmentAsync(10, It.IsAny<PatchEnrollmentDto>(), 42))
                .ReturnsAsync(ServiceResult<EnrollmentDto>.Failure("Invalid mark.", ServiceError.ValidationFailed));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PatchAsJsonAsync("/api/enrollments/10", new
            {
                mark = 3
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEnrollment_AsAdmin_CallsDeleteWithoutOwnershipLookup()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.DeleteEnrollmentAsync(10, 1, "Admin"))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.DeleteAsync("/api/enrollments/10");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(service => service.DeleteEnrollmentAsync(10, 1, "Admin"), Times.Once);
        }

        [Fact]
        public async Task DeleteEnrollment_AsStudentWhenEnrollmentMissing_ReturnsNotFoundBeforeDelete()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.DeleteEnrollmentAsync(10, 7, "Student"))
                .ReturnsAsync(ServiceResult<bool>.Failure("Enrollment not found.", ServiceError.NotFound));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.DeleteAsync("/api/enrollments/10");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(service => service.DeleteEnrollmentAsync(10, 7, "Student"), Times.Once);
        }

        private static EnrollmentDto CreateEnrollment(long id, long studentId, long subjectId, int? mark) =>
            new(id, mark, DateTime.UtcNow, null, studentId, "student", subjectId, "Math", "MATH", 0);
    }
}
