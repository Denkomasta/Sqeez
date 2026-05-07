using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sqeez.Api.Models.Config;
using Sqeez.Api.Services.EmailService;
using Xunit;

namespace Sqeez.Api.Tests.Services
{
    public class EmailServiceTests
    {
        private EmailService CreateService(SmtpSettings settings, out Mock<ILogger<EmailService>> mockLogger)
        {
            var options = Options.Create(settings);
            mockLogger = new Mock<ILogger<EmailService>>();
            return new EmailService(options, mockLogger.Object);
        }

        [Fact]
        public async Task SendVerificationEmailAsync_WhenSmtpFails_LogsError()
        {
            var settings = new SmtpSettings
            {
                Server = "invalid.server.local",
                Port = 12345,
                SenderEmail = "noreply@sqeez.org",
                SenderName = "Sqeez Test"
            };

            var service = CreateService(settings, out var mockLogger);

            // This should not throw since SendEmailAsync catches all exceptions
            await service.SendVerificationEmailAsync("test@example.com", "http://verify");

            // Verify that LogError was called
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains("Failed to send email.") &&
                        !v.ToString()!.Contains("test@example.com")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SendPasswordResetEmailAsync_WhenSmtpFails_LogsError()
        {
            var settings = new SmtpSettings
            {
                Server = "invalid.server.local",
                Port = 12345,
                SenderEmail = "noreply@sqeez.org",
                SenderName = "Sqeez Test"
            };

            var service = CreateService(settings, out var mockLogger);

            await service.SendPasswordResetEmailAsync("test@example.com", "http://reset");

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains("Failed to send email.") &&
                        !v.ToString()!.Contains("test@example.com")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }
    }
}
