using System.Net;
using Moq;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;

namespace Sqeez.Api.Tests.Integration
{
    public class BadgeControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public BadgeControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
        }

        [Fact]
        public async Task GetBadges_WithAuthenticatedUser_CallsBadgeService()
        {
            _factory.BadgeServiceMock
                .Setup(service => service.GetAllBadgesAsync(It.Is<BadgeFilterDto>(filter =>
                    filter.SearchTerm == "quiz" &&
                    filter.PageSize == 5)))
                .ReturnsAsync(ServiceResult<PagedResponse<BadgeDto>>.Ok(
                    new PagedResponse<BadgeDto>
                    {
                        Data = new[]
                        {
                            new BadgeDto(1, "Quiz master", "Complete quizzes", null, 100, new List<BadgeRuleDto>())
                        },
                        PageNumber = 1,
                        PageSize = 5,
                        TotalCount = 1
                    }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "7");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Student");

            var response = await client.GetAsync("/api/badges?SearchTerm=quiz&PageSize=5");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.BadgeServiceMock.Verify(
                service => service.GetAllBadgesAsync(It.IsAny<BadgeFilterDto>()),
                Times.Once);
        }

        [Fact]
        public async Task AwardBadge_AsAdmin_MapsServiceResult()
        {
            _factory.BadgeServiceMock
                .Setup(service => service.AwardBadgeToStudentAsync(7, 3))
                .ReturnsAsync(ServiceResult<StudentBadgeBasicDto>.Ok(
                    new StudentBadgeBasicDto { BadgeId = 3, Name = "Helper", EarnedAt = DateTime.UtcNow }));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.PostAsync("/api/badges/3/award/7", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.BadgeServiceMock.Verify(
                service => service.AwardBadgeToStudentAsync(7, 3),
                Times.Once);
        }

        [Fact]
        public async Task PatchBadge_WithJsonRuleFormField_BindsRuleAndCallsService()
        {
            _factory.BadgeServiceMock
                .Setup(service => service.UpdateBadgeAsync(3, It.Is<UpdateBadgeDto>(dto =>
                    dto.Name == "Perfect Score" &&
                    dto.XpBonus == 100 &&
                    dto.Rules != null &&
                    dto.Rules.Count == 1 &&
                    dto.Rules[0].Id == null &&
                    dto.Rules[0].Metric == BadgeMetric.ScorePercentage &&
                    dto.Rules[0].Operator == BadgeOperator.Equals &&
                    dto.Rules[0].TargetValue == 100)))
                .ReturnsAsync(ServiceResult<BadgeDto>.Ok(new BadgeDto(
                    3,
                    "Perfect Score",
                    "You scored 100% on a quiz! Outstanding!",
                    null,
                    100,
                    new List<BadgeRuleDto>
                    {
                        new BadgeRuleDto(9, BadgeMetric.ScorePercentage, BadgeOperator.Equals, 100)
                    })));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            using var content = new MultipartFormDataContent
            {
                { new StringContent("Perfect Score"), "Name" },
                { new StringContent("You scored 100% on a quiz! Outstanding!"), "Description" },
                { new StringContent("100"), "XpBonus" },
                { new StringContent("{\"id\":null,\"metric\":\"ScorePercentage\",\"operator\":\"Equals\",\"targetValue\":100}"), "Rules" }
            };

            var response = await client.PatchAsync("/api/badges/3", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.BadgeServiceMock.Verify(
                service => service.UpdateBadgeAsync(3, It.IsAny<UpdateBadgeDto>()),
                Times.Once);
        }

        [Fact]
        public async Task PatchBadge_WithRepeatedJsonRuleFormFields_BindsAllRulesAndCallsService()
        {
            _factory.BadgeServiceMock
                .Setup(service => service.UpdateBadgeAsync(3, It.Is<UpdateBadgeDto>(dto =>
                    dto.Rules != null &&
                    dto.Rules.Count == 2 &&
                    dto.Rules[0].Id == 4 &&
                    dto.Rules[0].Metric == BadgeMetric.ScorePercentage &&
                    dto.Rules[0].Operator == BadgeOperator.Equals &&
                    dto.Rules[0].TargetValue == 100 &&
                    dto.Rules[1].Id == null &&
                    dto.Rules[1].Metric == BadgeMetric.TotalScore &&
                    dto.Rules[1].Operator == BadgeOperator.GreaterThanOrEqual &&
                    dto.Rules[1].TargetValue == 80)))
                .ReturnsAsync(ServiceResult<BadgeDto>.Ok(new BadgeDto(
                    3,
                    "Perfect Score",
                    "You scored 100% on a quiz! Outstanding!",
                    null,
                    100,
                    new List<BadgeRuleDto>
                    {
                        new BadgeRuleDto(4, BadgeMetric.ScorePercentage, BadgeOperator.Equals, 100),
                        new BadgeRuleDto(5, BadgeMetric.TotalScore, BadgeOperator.GreaterThanOrEqual, 80)
                    })));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            using var content = new MultipartFormDataContent
            {
                { new StringContent("Perfect Score"), "Name" },
                { new StringContent("You scored 100% on a quiz! Outstanding!"), "Description" },
                { new StringContent("100"), "XpBonus" },
                { new StringContent("{\"id\":4,\"metric\":\"ScorePercentage\",\"operator\":\"Equals\",\"targetValue\":100}"), "Rules" },
                { new StringContent("{\"id\":null,\"metric\":\"TotalScore\",\"operator\":\"GreaterThanOrEqual\",\"targetValue\":80}"), "Rules" }
            };

            var response = await client.PatchAsync("/api/badges/3", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.BadgeServiceMock.Verify(
                service => service.UpdateBadgeAsync(3, It.IsAny<UpdateBadgeDto>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteBadge_WhenServiceReturnsConflict_MapsToConflict()
        {
            _factory.BadgeServiceMock
                .Setup(service => service.DeleteBadgeAsync(3))
                .ReturnsAsync(ServiceResult<bool>.Failure("Badge has already been awarded.", ServiceError.Conflict));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "1");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

            var response = await client.DeleteAsync("/api/badges/3");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            _factory.BadgeServiceMock.Verify(service => service.DeleteBadgeAsync(3), Times.Once);
        }
    }
}
