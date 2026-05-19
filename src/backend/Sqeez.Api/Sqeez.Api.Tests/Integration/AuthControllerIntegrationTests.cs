using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;

namespace Sqeez.Api.Tests.Integration
{
    public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
        }

        [Fact]
        public async Task Register_WhenServiceSucceeds_ReturnsOkAndCallsService()
        {
            _factory.AuthServiceMock
                .Setup(service => service.RegisterAsync(It.Is<RegisterDTO>(dto =>
                    dto.FirstName == "Jana" &&
                    dto.LastName == "Novakova" &&
                    dto.Username == "jana" &&
                    dto.Email == "jana@sqeez.test" &&
                    dto.RememberMe)))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Jana",
                lastName = "Novakova",
                username = "jana",
                email = "jana@sqeez.test",
                password = "StrongPassword123!",
                rememberMe = true
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.RegisterAsync(It.IsAny<RegisterDTO>()), Times.Once);
        }

        [Fact]
        public async Task Register_WhenEmailExists_MapsConflict()
        {
            _factory.AuthServiceMock
                .Setup(service => service.RegisterAsync(It.IsAny<RegisterDTO>()))
                .ReturnsAsync(ServiceResult<bool>.Failure("Email already exists.", ServiceError.Conflict));

            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Jana",
                lastName = "Novakova",
                username = "jana",
                email = "jana@sqeez.test",
                password = "StrongPassword123!",
                rememberMe = false
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Register_WhenPublicRegistrationDisabled_MapsForbidden()
        {
            _factory.AuthServiceMock
                .Setup(service => service.RegisterAsync(It.IsAny<RegisterDTO>()))
                .ReturnsAsync(ServiceResult<bool>.Failure("Public registration is disabled.", ServiceError.Forbidden));

            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                firstName = "Jana",
                lastName = "Novakova",
                username = "jana",
                email = "jana@sqeez.test",
                password = "StrongPassword123!",
                rememberMe = false
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Login_WhenServiceSucceeds_ReturnsOkAndSetsAuthCookies()
        {
            _factory.AuthServiceMock
                .Setup(service => service.LoginAsync(It.Is<LoginDTO>(dto =>
                    dto.Email == "student@sqeez.test" &&
                    dto.Password == "StrongPassword123!" &&
                    dto.RememberMe)))
                .ReturnsAsync(ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto("access-token", "refresh-token")));

            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "student@sqeez.test",
                password = "StrongPassword123!",
                rememberMe = true
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
            Assert.Contains(cookies, cookie => cookie.StartsWith("sqeez_access_token=access-token"));
            Assert.Contains(cookies, cookie => cookie.StartsWith("sqeez_refresh_token=refresh-token"));
            _factory.AuthServiceMock.Verify(service => service.LoginAsync(It.IsAny<LoginDTO>()), Times.Once);
        }

        [Fact]
        public async Task Login_WhenServiceReturnsUnauthorized_MapsToUnauthorized()
        {
            _factory.AuthServiceMock
                .Setup(service => service.LoginAsync(It.IsAny<LoginDTO>()))
                .ReturnsAsync(ServiceResult<AuthResponseDto>.Failure("Invalid credentials.", ServiceError.Unauthorized));

            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "student@sqeez.test",
                password = "StrongPassword123!",
                rememberMe = false
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WhenEmailInvalid_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "not-email",
                password = "StrongPassword123!",
                rememberMe = false
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.LoginAsync(It.IsAny<LoginDTO>()), Times.Never);
        }

        [Fact]
        public async Task VerifyEmail_WithoutToken_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsync("/api/auth/verify-email", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.AuthServiceMock.Verify(
                service => service.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task VerifyEmail_WhenServiceSucceeds_SetsCookiesAndCallsService()
        {
            _factory.AuthServiceMock
                .Setup(service => service.VerifyEmailAsync("verify-token", true))
                .ReturnsAsync(ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto("verified-access", "verified-refresh")));

            var client = _factory.CreateClient();

            var response = await client.PostAsync("/api/auth/verify-email?token=verify-token&rememberMe=true", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
            Assert.Contains(cookies, cookie => cookie.StartsWith("sqeez_access_token=verified-access"));
            Assert.Contains(cookies, cookie => cookie.StartsWith("sqeez_refresh_token=verified-refresh"));
            _factory.AuthServiceMock.Verify(service => service.VerifyEmailAsync("verify-token", true), Times.Once);
        }

        [Fact]
        public async Task VerifyEmail_WhenTokenExpired_MapsUnauthorized()
        {
            _factory.AuthServiceMock
                .Setup(service => service.VerifyEmailAsync("expired-token", false))
                .ReturnsAsync(ServiceResult<AuthResponseDto>.Failure("Token expired.", ServiceError.Unauthorized));

            var client = _factory.CreateClient();

            var response = await client.PostAsync("/api/auth/verify-email?token=expired-token", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ResendVerification_WithValidEmail_CallsService()
        {
            _factory.AuthServiceMock
                .Setup(service => service.ResendVerificationEmailAsync(It.Is<ResendVerificationDto>(dto =>
                    dto.Email == "jana@sqeez.test" &&
                    dto.RememberMe)))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/resend-verification", new
            {
                email = "jana@sqeez.test",
                rememberMe = true
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.ResendVerificationEmailAsync(It.IsAny<ResendVerificationDto>()), Times.Once);
        }

        [Fact]
        public async Task ResendVerification_WhenThrottled_MapsTooManyRequests()
        {
            _factory.AuthServiceMock
                .Setup(service => service.ResendVerificationEmailAsync(It.IsAny<ResendVerificationDto>()))
                .ReturnsAsync(ServiceResult<bool>.Failure("Please wait before retrying.", ServiceError.TooManyRequests));

            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/resend-verification", new
            {
                email = "jana@sqeez.test"
            });

            Assert.Equal((HttpStatusCode)429, response.StatusCode);
        }

        [Fact]
        public async Task ResendVerification_WithInvalidEmail_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/resend-verification", new
            {
                email = "not-email"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.ResendVerificationEmailAsync(It.IsAny<ResendVerificationDto>()), Times.Never);
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_PassesEmailToService()
        {
            _factory.AuthServiceMock
                .Setup(service => service.ForgotPasswordAsync("jana@sqeez.test", "cs"))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new
            {
                email = "jana@sqeez.test",
                language = "cs"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.ForgotPasswordAsync("jana@sqeez.test", "cs"), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_WithInvalidEmail_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new
            {
                email = "not-email"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.ForgotPasswordAsync(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task ResetPassword_WithValidDto_CallsService()
        {
            _factory.AuthServiceMock
                .Setup(service => service.ResetPasswordAsync(It.Is<ResetPasswordDto>(dto =>
                    dto.Token == "reset-token" &&
                    dto.NewPassword == "NewStrongPassword123!")))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/reset-password", new
            {
                token = "reset-token",
                newPassword = "NewStrongPassword123!"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.ResetPasswordAsync(It.IsAny<ResetPasswordDto>()), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_WithWeakPassword_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/reset-password", new
            {
                token = "reset-token",
                newPassword = "weak"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.ResetPasswordAsync(It.IsAny<ResetPasswordDto>()), Times.Never);
        }

        [Fact]
        public async Task ResetPassword_WhenTokenExpired_MapsUnauthorized()
        {
            _factory.AuthServiceMock
                .Setup(service => service.ResetPasswordAsync(It.IsAny<ResetPasswordDto>()))
                .ReturnsAsync(ServiceResult<bool>.Failure("Token expired.", ServiceError.Unauthorized));

            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/reset-password", new
            {
                token = "reset-token",
                newPassword = "NewStrongPassword123!"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_WithoutRefreshCookie_ReturnsUnauthorizedBeforeCallingService()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsync("/api/auth/refresh", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            _factory.AuthServiceMock.Verify(
                service => service.RefreshTokenAsync(It.IsAny<RefreshTokenDto>()),
                Times.Never);
        }

        [Fact]
        public async Task Refresh_WithRefreshCookie_RotatesTokenAndSetsCookies()
        {
            _factory.AuthServiceMock
                .Setup(service => service.RefreshTokenAsync(It.Is<RefreshTokenDto>(dto => dto.RefreshToken == "old-refresh")))
                .ReturnsAsync(ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto("new-access", "new-refresh")));

            var client = _factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            request.Headers.Add("Cookie", "sqeez_refresh_token=old-refresh");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
            Assert.Contains(cookies, cookie => cookie.StartsWith("sqeez_access_token=new-access"));
            Assert.Contains(cookies, cookie => cookie.StartsWith("sqeez_refresh_token=new-refresh"));
            _factory.AuthServiceMock.Verify(service => service.RefreshTokenAsync(It.IsAny<RefreshTokenDto>()), Times.Once);
        }

        [Fact]
        public async Task Refresh_WhenServiceReturnsUnauthorized_ClearsCookiesAndMapsUnauthorized()
        {
            _factory.AuthServiceMock
                .Setup(service => service.RefreshTokenAsync(It.IsAny<RefreshTokenDto>()))
                .ReturnsAsync(ServiceResult<AuthResponseDto>.Failure("Refresh token invalid.", ServiceError.Unauthorized));

            var client = _factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            request.Headers.Add("Cookie", "sqeez_refresh_token=old-refresh");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
            Assert.Contains(cookies, cookie => cookie.StartsWith("sqeez_access_token=;"));
            Assert.Contains(cookies, cookie => cookie.StartsWith("sqeez_refresh_token=;"));
        }

        [Fact]
        public async Task Logout_WithAuthenticatedUser_CallsServiceWithRefreshCookieAndClearsCookies()
        {
            _factory.AuthServiceMock
                .Setup(service => service.LogoutAsync(7, "refresh-to-revoke"))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
            request.Headers.Add(TestAuthenticationHandler.UserIdHeader, "7");
            request.Headers.Add(TestAuthenticationHandler.RoleHeader, "Student");
            request.Headers.Add("Cookie", "sqeez_refresh_token=refresh-to-revoke");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
            Assert.Contains(cookies, cookie => cookie.StartsWith("sqeez_access_token=;"));
            Assert.Contains(cookies, cookie => cookie.StartsWith("sqeez_refresh_token=;"));
            _factory.AuthServiceMock.Verify(service => service.LogoutAsync(7, "refresh-to-revoke"), Times.Once);
        }

        [Fact]
        public async Task Logout_WithoutAuthentication_ReturnsUnauthorizedBeforeCallingService()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsync("/api/auth/logout", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.LogoutAsync(It.IsAny<long>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentUser_WithAuthenticatedUser_ReturnsEnumAsStringAndUsesClaims()
        {
            _factory.AuthServiceMock
                .Setup(service => service.GetCurrentUserAsync(7, "Teacher"))
                .ReturnsAsync(ServiceResult<UserDTO>.Ok(
                    new UserDTO(7, "teacher", "teacher@sqeez.test", "120", UserRole.Teacher, null)));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Teacher", document.RootElement.GetProperty("role").GetString());
            _factory.AuthServiceMock.Verify(service => service.GetCurrentUserAsync(7, "Teacher"), Times.Once);
        }

        [Fact]
        public async Task GetCurrentUser_WhenServiceReturnsNotFound_MapsNotFound()
        {
            _factory.AuthServiceMock
                .Setup(service => service.GetCurrentUserAsync(7, "Student"))
                .ReturnsAsync(ServiceResult<UserDTO>.Failure("User not found.", ServiceError.NotFound));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Elevate_AsAdmin_CallsServiceWithAdminIdAndDto()
        {
            _factory.AuthServiceMock
                .Setup(service => service.UpdateUserRoleAsync(
                    1,
                    It.Is<UpdateRoleDTO>(dto =>
                        dto.Id == 7 &&
                        dto.Role == UserRole.Teacher &&
                        dto.Department == "Math" &&
                        dto.PhoneNumber == "+420 123456789")))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.PatchAsJsonAsync("/api/auth/elevate", new
            {
                id = 7,
                role = "Teacher",
                department = "Math",
                phoneNumber = "+420 123456789"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.UpdateUserRoleAsync(1, It.IsAny<UpdateRoleDTO>()), Times.Once);
        }

        [Fact]
        public async Task Elevate_AsTeacher_ReturnsForbiddenBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PatchAsJsonAsync("/api/auth/elevate", new
            {
                id = 7,
                role = "Admin"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.UpdateUserRoleAsync(It.IsAny<long>(), It.IsAny<UpdateRoleDTO>()), Times.Never);
        }

        [Fact]
        public async Task Elevate_WithInvalidPhone_ReturnsBadRequestBeforeCallingService()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.PatchAsJsonAsync("/api/auth/elevate", new
            {
                id = 7,
                role = "Teacher",
                phoneNumber = "<script>"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            _factory.AuthServiceMock.Verify(service => service.UpdateUserRoleAsync(It.IsAny<long>(), It.IsAny<UpdateRoleDTO>()), Times.Never);
        }

        [Fact]
        public async Task Elevate_WhenServiceReturnsConflict_MapsConflict()
        {
            _factory.AuthServiceMock
                .Setup(service => service.UpdateUserRoleAsync(1, It.IsAny<UpdateRoleDTO>()))
                .ReturnsAsync(ServiceResult<bool>.Failure("Teacher manages active class.", ServiceError.Conflict));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.PatchAsJsonAsync("/api/auth/elevate", new
            {
                id = 7,
                role = "Student"
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }
}
