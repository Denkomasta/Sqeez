namespace Sqeez.Api.Models.Config
{
    /// <summary>
    /// SMTP configuration bound from application settings for outbound email delivery.
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>
        /// SMTP server hostname.
        /// </summary>
        public string Server { get; set; } = string.Empty;

        /// <summary>
        /// SMTP server port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Display name used in outgoing email sender headers.
        /// </summary>
        public string SenderName { get; set; } = string.Empty;

        /// <summary>
        /// Email address used in outgoing email sender headers.
        /// </summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>
        /// SMTP username used for authentication.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// SMTP password used for authentication.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether STARTTLS should be used for the SMTP connection.
        /// </summary>
        public bool UseStartTls { get; set; } = true;
    }
}
