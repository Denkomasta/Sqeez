using Sqeez.Api.Constants;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Current application configuration exposed to the frontend and administrators.
    /// </summary>
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

        [StringLength(ValidationConstants.UrlMaxLength)]
        public string? LogoUrl { get; init; }

        [StringLength(ValidationConstants.EmailMaxLength)]
        [RegularExpression(ValidationConstants.EmailRegex)]
        public string? SupportEmail { get; init; }

        [StringLength(ValidationConstants.LanguageCodeMaxLength)]
        public string? DefaultLanguage { get; init; }

        [StringLength(ValidationConstants.AcademicYearMaxLength)]
        public string? CurrentAcademicYear { get; init; }
        public bool? AllowPublicRegistration { get; init; }
        public bool? RequireEmailVerification { get; init; }

        [Range(1, ValidationConstants.MaxUploadSizeMb)]
        public int? MaxAvatarAndBadgeUploadSizeMB { get; init; }

        [Range(1, ValidationConstants.MaxUploadSizeMb)]
        public int? MaxQuizMediaUploadSizeMB { get; init; }

        [Range(1, ValidationConstants.MaxActiveSessionsPerUser)]
        public int? MaxActiveSessionsPerUser { get; init; }
    }
}
