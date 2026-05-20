using Sqeez.Api.Constants;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Current application configuration exposed to the frontend and administrators.
    /// </summary>
    /// <param name="SchoolName">School name displayed in the application.</param>
    /// <param name="LogoUrl">Public logo URL.</param>
    /// <param name="SupportEmail">Support contact email.</param>
    /// <param name="DefaultLanguage">Default language code used for localized email templates.</param>
    /// <param name="CurrentAcademicYear">Current academic year label.</param>
    /// <param name="AllowPublicRegistration">Whether unauthenticated users can register themselves.</param>
    /// <param name="RequireEmailVerification">Whether users must verify email before login.</param>
    /// <param name="MaxAvatarAndBadgeUploadSizeMB">Upload size limit for avatars and badge icons.</param>
    /// <param name="MaxQuizMediaUploadSizeMB">Upload size limit for quiz media.</param>
    /// <param name="MaxActiveSessionsPerUser">Maximum active refresh-token sessions per user.</param>
    public record SystemConfigDto(
        string SchoolName,
        string LogoUrl,
        string SupportEmail,
        string DefaultLanguage,
        string CurrentAcademicYear,
        bool AllowPublicRegistration,
        bool RequireEmailVerification,
        int MaxAvatarAndBadgeUploadSizeMB,
        int MaxQuizMediaUploadSizeMB,
        int MaxActiveSessionsPerUser
    );

    /// <summary>
    /// Request for partially updating mutable system configuration values.
    /// </summary>
    public record UpdateSystemConfigDto
    {
        public UpdateSystemConfigDto() { }

        public UpdateSystemConfigDto(
            string? SchoolName,
            string? LogoUrl,
            string? SupportEmail,
            string? DefaultLanguage,
            string? CurrentAcademicYear,
            bool? AllowPublicRegistration,
            bool? RequireEmailVerification,
            int? MaxAvatarAndBadgeUploadSizeMB,
            int? MaxQuizMediaUploadSizeMB,
            int? MaxActiveSessionsPerUser)
        {
            this.SchoolName = SchoolName;
            this.LogoUrl = LogoUrl;
            this.SupportEmail = SupportEmail;
            this.DefaultLanguage = DefaultLanguage;
            this.CurrentAcademicYear = CurrentAcademicYear;
            this.AllowPublicRegistration = AllowPublicRegistration;
            this.RequireEmailVerification = RequireEmailVerification;
            this.MaxAvatarAndBadgeUploadSizeMB = MaxAvatarAndBadgeUploadSizeMB;
            this.MaxQuizMediaUploadSizeMB = MaxQuizMediaUploadSizeMB;
            this.MaxActiveSessionsPerUser = MaxActiveSessionsPerUser;
        }

        [StringLength(ValidationConstants.TitleMaxLength)]
        public string? SchoolName { get; init; }

        /// <summary>
        /// Public logo URL.
        /// </summary>
        [StringLength(ValidationConstants.UrlMaxLength)]
        public string? LogoUrl { get; init; }

        /// <summary>
        /// Support contact email.
        /// </summary>
        [StringLength(ValidationConstants.EmailMaxLength)]
        [RegularExpression(ValidationConstants.EmailRegex)]
        public string? SupportEmail { get; init; }

        /// <summary>
        /// Default language code used for localized email templates.
        /// </summary>
        [StringLength(ValidationConstants.LanguageCodeMaxLength)]
        public string? DefaultLanguage { get; init; }

        /// <summary>
        /// Current academic year label.
        /// </summary>
        [StringLength(ValidationConstants.AcademicYearMaxLength)]
        public string? CurrentAcademicYear { get; init; }

        /// <summary>
        /// Whether unauthenticated users can register themselves.
        /// </summary>
        public bool? AllowPublicRegistration { get; init; }

        /// <summary>
        /// Whether users must verify email before login.
        /// </summary>
        public bool? RequireEmailVerification { get; init; }

        /// <summary>
        /// Upload size limit for avatars and badge icons.
        /// </summary>
        [Range(1, ValidationConstants.MaxUploadSizeMb)]
        public int? MaxAvatarAndBadgeUploadSizeMB { get; init; }

        /// <summary>
        /// Upload size limit for quiz media.
        /// </summary>
        [Range(1, ValidationConstants.MaxUploadSizeMb)]
        public int? MaxQuizMediaUploadSizeMB { get; init; }

        /// <summary>
        /// Maximum active refresh-token sessions per user.
        /// </summary>
        [Range(1, ValidationConstants.MaxActiveSessionsPerUser)]
        public int? MaxActiveSessionsPerUser { get; init; }
    }
}
