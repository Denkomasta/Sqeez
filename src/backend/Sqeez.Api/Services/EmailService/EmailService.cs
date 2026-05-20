using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Sqeez.Api.Models.Config;
using Sqeez.Api.Services.Interfaces;
using System.Net;

namespace Sqeez.Api.Services.EmailService
{
    /// <summary>
    /// Sends application emails for verification and password-reset flows.
    /// </summary>
    public class EmailService : IEmailService
    {
        private const string DefaultLanguage = "en";
        private const string TemplatesRoot = "EmailTemplates";

        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendVerificationEmailAsync(string email, string verificationLink)
        {
            await SendVerificationEmailAsync(email, verificationLink, DefaultLanguage);
        }

        /// <inheritdoc />
        public async Task SendVerificationEmailAsync(string email, string verificationLink, string? language)
        {
            var template = LoadTemplate("verification", language);
            var values = new Dictionary<string, string>
            {
                ["VerificationLink"] = verificationLink
            };

            await SendEmailAsync(email, RenderTemplate(template.Subject, values), RenderTemplate(template.HtmlBody, values));
        }

        /// <inheritdoc />
        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            await SendPasswordResetEmailAsync(email, resetLink, DefaultLanguage);
        }

        /// <inheritdoc />
        public async Task SendPasswordResetEmailAsync(string email, string resetLink, string? language)
        {
            var template = LoadTemplate("password-reset", language);
            var values = new Dictionary<string, string>
            {
                ["ResetLink"] = resetLink
            };

            await SendEmailAsync(email, RenderTemplate(template.Subject, values), RenderTemplate(template.HtmlBody, values));
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

                var socketOptions = _smtpSettings.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

                await smtp.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, socketOptions);

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

        private EmailTemplate LoadTemplate(string templateName, string? language)
        {
            foreach (var languageCode in GetLanguageFallbacks(language))
            {
                var templateDirectory = Path.Combine(AppContext.BaseDirectory, TemplatesRoot, languageCode);
                var subjectPath = Path.Combine(templateDirectory, $"{templateName}.subject.txt");
                var htmlPath = Path.Combine(templateDirectory, $"{templateName}.html");

                if (File.Exists(subjectPath) && File.Exists(htmlPath))
                {
                    return new EmailTemplate(
                        File.ReadAllText(subjectPath),
                        File.ReadAllText(htmlPath));
                }
            }

            _logger.LogWarning("Email template {TemplateName} for language {Language} was not found. Using built-in English fallback.", templateName, language);
            return GetBuiltInEnglishTemplate(templateName);
        }

        private static IEnumerable<string> GetLanguageFallbacks(string? language)
        {
            var normalizedLanguage = NormalizeLanguageCode(language);
            if (!string.IsNullOrWhiteSpace(normalizedLanguage))
            {
                yield return normalizedLanguage;

                // Prefer a specific locale first, then its neutral language, then English.
                var neutralLanguage = normalizedLanguage.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(neutralLanguage) && neutralLanguage != normalizedLanguage)
                {
                    yield return neutralLanguage;
                }
            }

            if (normalizedLanguage != DefaultLanguage)
            {
                yield return DefaultLanguage;
            }
        }

        private static string NormalizeLanguageCode(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return DefaultLanguage;
            }

            return language.Trim().Replace('_', '-').ToLowerInvariant();
        }

        private static string RenderTemplate(string template, IReadOnlyDictionary<string, string> values)
        {
            var result = template;

            foreach (var value in values)
            {
                result = result.Replace(
                    $"{{{{{value.Key}}}}}",
                    WebUtility.HtmlEncode(value.Value),
                    StringComparison.Ordinal);
            }

            return result;
        }

        private static EmailTemplate GetBuiltInEnglishTemplate(string templateName)
        {
            return templateName switch
            {
                "verification" => new EmailTemplate(
                    "Verify your email address for Sqeez",
                    """
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                        <h2>Welcome to Sqeez!</h2>
                        <p>Thank you for registering. Please confirm your email address by clicking the button below:</p>
                        <a href='{{VerificationLink}}' style='display: inline-block; padding: 10px 20px; color: white; background-color: #007bff; text-decoration: none; border-radius: 5px; margin-top: 15px;'>Verify Email</a>
                        <p style='margin-top: 20px; font-size: 12px; color: #666;'>
                            If the button doesn't work, copy and paste this link into your browser:<br/>
                            {{VerificationLink}}
                        </p>
                        <p>This link will expire in 24 hours.</p>
                    </div>
                    """),
                "password-reset" => new EmailTemplate(
                    "Reset your Sqeez password",
                    """
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                        <h2>Password Reset Request</h2>
                        <p>We received a request to reset your password for your Sqeez account. Click the button below to set a new password:</p>
                        <a href='{{ResetLink}}' style='display: inline-block; padding: 10px 20px; color: white; background-color: #dc3545; text-decoration: none; border-radius: 5px; margin-top: 15px;'>Reset Password</a>
                        <p style='margin-top: 20px; font-size: 12px; color: #666;'>
                            If the button doesn't work, copy and paste this link into your browser:<br/>
                            {{ResetLink}}
                        </p>
                        <p>This link will expire in 15 minutes. If you did not request a password reset, you can safely ignore this email; your password will remain unchanged.</p>
                    </div>
                    """),
                _ => throw new InvalidOperationException($"Unknown email template '{templateName}'.")
            };
        }

        private sealed record EmailTemplate(string Subject, string HtmlBody);
    }
}
