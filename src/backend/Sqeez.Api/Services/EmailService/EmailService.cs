using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Sqeez.Api.Models.Config;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string email, string verificationLink)
        {
            var subject = "Verify your email address for Sqeez";

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                    <h2>Welcome to Sqeez!</h2>
                    <p>Thank you for registering. Please confirm your email address by clicking the button below:</p>
                    <a href='{verificationLink}' style='display: inline-block; padding: 10px 20px; color: white; background-color: #007bff; text-decoration: none; border-radius: 5px; margin-top: 15px;'>Verify Email</a>
                    <p style='margin-top: 20px; font-size: 12px; color: #666;'>
                        If the button doesn't work, copy and paste this link into your browser:<br/>
                        {verificationLink}
                    </p>
                    <p>This link will expire in 24 hours.</p>
                </div>";

            await SendEmailAsync(email, subject, htmlBody);
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var subject = "Reset your Sqeez password";

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                    <h2>Password Reset Request</h2>
                    <p>We received a request to reset your password for your Sqeez account. Click the button below to set a new password:</p>
                    <a href='{resetLink}' style='display: inline-block; padding: 10px 20px; color: white; background-color: #dc3545; text-decoration: none; border-radius: 5px; margin-top: 15px;'>Reset Password</a>
                    <p style='margin-top: 20px; font-size: 12px; color: #666;'>
                        If the button doesn't work, copy and paste this link into your browser:<br/>
                        {resetLink}
                    </p>
                    <p>This link will expire in 15 minutes. If you did not request a password reset, you can safely ignore this email; your password will remain unchanged.</p>
                </div>";

            await SendEmailAsync(email, subject, htmlBody);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;
                email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, SecureSocketOptions.StartTls);

                if (!string.IsNullOrEmpty(_smtpSettings.Username))
                {
                    await smtp.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                }

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Successfully sent email.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email.");
            }
        }
    }
}
