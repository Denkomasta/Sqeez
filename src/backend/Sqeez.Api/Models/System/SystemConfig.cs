namespace Sqeez.Api.Models.System
{
    /// <summary>
    /// Singleton row containing global application branding, academic, security, and upload settings.
    /// </summary>
    public class SystemConfig
    {
        /// <summary>
        /// Fixed primary identifier for the singleton configuration row.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display name of the school or application instance.
        /// </summary>
        public string SchoolName { get; set; } = "Sqeez";

        /// <summary>
        /// URL of the configured school logo.
        /// </summary>
        public string LogoUrl { get; set; } = string.Empty;

        /// <summary>
        /// Support contact email shown to users.
        /// </summary>
        public string SupportEmail { get; set; } = "support@sqeez.org";

        /// <summary>
        /// Default language code used when a more specific language is unavailable.
        /// </summary>
        public string DefaultLanguage { get; set; } = "en";

        /// <summary>
        /// Current academic year displayed and used by default in administrative flows.
        /// </summary>
        public string CurrentAcademicYear { get; set; } = "2025/2026";

        /// <summary>
        /// Indicates whether unauthenticated users may register their own accounts.
        /// </summary>
        public bool AllowPublicRegistration { get; set; } = false;

        /// <summary>
        /// Indicates whether accounts must verify their email before login.
        /// </summary>
        public bool RequireEmailVerification { get; set; } = true;

        /// <summary>
        /// Maximum upload size in MB for avatar and badge images.
        /// </summary>
        public int MaxAvatarAndBadgeUploadSizeMB { get; set; } = 5;

        /// <summary>
        /// Maximum upload size in MB for quiz media such as images, audio, and video.
        /// </summary>
        public int MaxQuizMediaUploadSizeMB { get; set; } = 50;

        /// <summary>
        /// Maximum number of active refresh-token sessions allowed per user.
        /// </summary>
        public int MaxActiveSessionsPerUser { get; set; } = 3;
    }
}
