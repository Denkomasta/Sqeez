using System.Net;
using System.Net.Http.Json;
using Moq;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Tests.Integration
{
    public class AuthorizationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthorizationIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
        }

        [Fact]
        public async Task GetEnrollments_WithoutAuthentication_ReturnsUnauthorizedBeforeCallingService()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/enrollments");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.GetAllEnrollmentsAsync(
                    It.IsAny<EnrollmentFilterDto>(),
                    It.IsAny<long>(),
                    It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task GetEnrollments_WithInvalidPageSize_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/enrollments?PageSize=101");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.GetAllEnrollmentsAsync(
                    It.IsAny<EnrollmentFilterDto>(),
                    It.IsAny<long>(),
                    It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task PatchEnrollment_WithStudentRole_ReturnsForbiddenBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PatchAsJsonAsync("/api/enrollments/10", new
            {
                mark = 3,
                removeMark = false
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.PatchEnrollmentAsync(
                    It.IsAny<long>(),
                    It.IsAny<PatchEnrollmentDto>(),
                    It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task PatchEnrollment_WhenServiceReturnsForbidden_MapsToForbidden()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.PatchEnrollmentAsync(
                    10,
                    It.Is<PatchEnrollmentDto>(dto => dto.Mark == 3),
                    42))
                .ReturnsAsync(ServiceResult<EnrollmentDto>.Failure("Not your subject.", ServiceError.Forbidden));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PatchAsJsonAsync("/api/enrollments/10", new
            {
                mark = 3,
                removeMark = false
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(
                service => service.PatchEnrollmentAsync(
                    10,
                    It.Is<PatchEnrollmentDto>(dto => dto.Mark == 3),
                    42),
                Times.Once);
        }

        [Fact]
        public async Task DeleteEnrollment_AsDifferentStudent_ReturnsForbiddenBeforeDelete()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.DeleteEnrollmentAsync(10, 7, "Student"))
                .ReturnsAsync(ServiceResult<bool>.Failure("You do not have permission to delete this enrollment.", ServiceError.Forbidden));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.DeleteAsync("/api/enrollments/10");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(service => service.DeleteEnrollmentAsync(10, 7, "Student"), Times.Once);
        }

        [Fact]
        public async Task DeleteEnrollment_AsOwnerStudent_CallsDeleteService()
        {
            _factory.EnrollmentServiceMock
                .Setup(service => service.DeleteEnrollmentAsync(10, 7, "Student"))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.DeleteAsync("/api/enrollments/10");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.EnrollmentServiceMock.Verify(service => service.DeleteEnrollmentAsync(10, 7, "Student"), Times.Once);
        }
    }
}
