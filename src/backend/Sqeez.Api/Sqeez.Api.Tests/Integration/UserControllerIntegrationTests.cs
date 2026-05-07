using System.Net;
using System.Net.Http.Json;
using Moq;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;

namespace Sqeez.Api.Tests.Integration
{
    public class UserControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public UserControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
        }

        [Fact]
        public async Task GetUsers_WithAuthenticatedUser_CallsUserService()
        {
            _factory.UserServiceMock
                .Setup(service => service.GetAllUsersAsync(It.Is<UserFilterDto>(filter =>
                    filter.SearchTerm == "anna" &&
                    filter.PageSize == 5)))
                .ReturnsAsync(ServiceResult<PagedResponse<StudentDto>>.Ok(
                    new PagedResponse<StudentDto>
                    {
                        Data = new[]
                        {
                            new StudentDto
                            {
                                Id = 7,
                                Username = "anna",
                                Email = "anna@sqeez.test",
                                Role = UserRole.Student,
                                LastSeen = DateTime.UtcNow
                            }
                        },
                        PageNumber = 1,
                        PageSize = 5,
                        TotalCount = 1
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.GetAsync("/api/users?SearchTerm=anna&PageSize=5");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.UserServiceMock.Verify(
                service => service.GetAllUsersAsync(It.IsAny<UserFilterDto>()),
                Times.Once);
        }

        [Fact]
        public async Task PatchUser_ForAnotherUserAsStudent_ReturnsForbiddenBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PatchAsJsonAsync("/api/users/8", new
            {
                username = "changed"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.UserServiceMock.Verify(
                service => service.PatchUserAsync(It.IsAny<long>(), It.IsAny<PatchStudentDto>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateUser_WithTeacherDiscriminator_CallsUserServiceWithTeacherDto()
        {
            _factory.UserServiceMock
                .Setup(service => service.CreateUserAsync(It.Is<CreateTeacherDto>(dto =>
                    dto.Username == "teacher" &&
                    dto.Department == "Math")))
                .ReturnsAsync(ServiceResult<StudentDto>.Ok(
                    new TeacherDto
                    {
                        Id = 12,
                        Username = "teacher",
                        Email = "teacher@sqeez.test",
                        Role = UserRole.Teacher,
                        Department = "Math",
                        LastSeen = DateTime.UtcNow
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.PostAsJsonAsync("/api/users", new
            {
                role = "teacher",
                firstName = "Tereza",
                lastName = "Novakova",
                username = "teacher",
                email = "teacher@sqeez.test",
                password = "StrongPassword123!",
                department = "Math"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            _factory.UserServiceMock.Verify(
                service => service.CreateUserAsync(It.IsAny<CreateTeacherDto>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateUser_WithoutAdminRole_ReturnsUnauthorizedBeforeCallingService()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/users", new
            {
                role = "student",
                firstName = "Anna",
                lastName = "Nova",
                username = "anna",
                email = "anna@sqeez.test",
                password = "StrongPassword123!"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            _factory.UserServiceMock.Verify(
                service => service.CreateUserAsync(It.IsAny<CreateStudentDto>()),
                Times.Never);
        }

        [Fact]
        public async Task GetUsers_AsStudent_ReturnsForbiddenBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/users");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.UserServiceMock.Verify(
                service => service.GetAllUsersAsync(It.IsAny<UserFilterDto>()),
                Times.Never);
        }

        [Fact]
        public async Task GetUserById_AsDifferentAuthenticatedStudent_CallsUserService()
        {
            _factory.UserServiceMock
                .Setup(service => service.GetUserByIdAsync(8))
                .ReturnsAsync(ServiceResult<StudentDto>.Ok(
                    new StudentDto
                    {
                        Id = 8,
                        Username = "other-student",
                        Email = "other@sqeez.test",
                        Role = UserRole.Student,
                        LastSeen = DateTime.UtcNow
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/users/8");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.UserServiceMock.Verify(
                service => service.GetUserByIdAsync(8),
                Times.Once);
        }

        [Fact]
        public async Task GetDetailedUserById_AsDifferentAuthenticatedStudent_CallsUserService()
        {
            _factory.UserServiceMock
                .Setup(service => service.GetDetailedUserByIdAsync(8))
                .ReturnsAsync(ServiceResult<DetailedUserDto>.Ok(
                    new DetailedUserDto
                    {
                        Id = 8,
                        Username = "other-student",
                        Email = "other@sqeez.test",
                        Role = UserRole.Student,
                        LastSeen = DateTime.UtcNow
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/users/8/details");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.UserServiceMock.Verify(
                service => service.GetDetailedUserByIdAsync(8),
                Times.Once);
        }

        [Fact]
        public async Task PatchUser_AsSelfChangingSchoolClass_ReturnsForbiddenBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.PatchAsJsonAsync("/api/users/7", new
            {
                role = "student",
                schoolClassId = 12
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.UserServiceMock.Verify(
                service => service.PatchUserAsync(It.IsAny<long>(), It.IsAny<PatchStudentDto>()),
                Times.Never);
        }

        [Fact]
        public async Task ArchiveUser_AsOwner_ReturnsNoContentWhenServiceSucceeds()
        {
            _factory.UserServiceMock
                .Setup(service => service.ArchiveUserAsync(7))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.DeleteAsync("/api/users/7");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            _factory.UserServiceMock.Verify(service => service.ArchiveUserAsync(7), Times.Once);
        }
    }
}
