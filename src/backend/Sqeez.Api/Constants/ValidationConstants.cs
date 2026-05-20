namespace Sqeez.Api.Constants
{
    /// <summary>
    /// Shared validation limits and regular expressions used by DTO validation attributes.
    /// Keep these values stable for OpenAPI clients because generated frontend validation can depend on them.
    /// </summary>
    public static class ValidationConstants
    {
        /// <summary>
        /// Maximum length for personal first and last names.
        /// </summary>
        public const int NameMaxLength = 50;
        /// <summary>
        /// Minimum length for usernames.
        /// </summary>
        public const int UsernameMinLength = 3;
        /// <summary>
        /// Maximum length for usernames.
        /// </summary>
        public const int UsernameMaxLength = 20;
        /// <summary>
        /// Maximum length for email addresses, aligned with the practical SMTP address limit.
        /// </summary>
        public const int EmailMaxLength = 254;
        /// <summary>
        /// Maximum accepted plain-text password length before hashing.
        /// </summary>
        public const int PasswordMaxLength = 128;
        /// <summary>
        /// Maximum length for verification, reset, and session token payloads accepted through DTOs.
        /// </summary>
        public const int TokenMaxLength = 512;
        /// <summary>
        /// Maximum length for search terms used in list filters.
        /// </summary>
        public const int SearchTermMaxLength = 100;
        /// <summary>
        /// Maximum length for short titles such as quizzes, questions, badges, and classes.
        /// </summary>
        public const int TitleMaxLength = 150;
        /// <summary>
        /// Maximum length for ordinary descriptions shown in the application UI.
        /// </summary>
        public const int DescriptionMaxLength = 1000;
        /// <summary>
        /// Maximum length for long free-form text such as free-text answers.
        /// </summary>
        public const int LongTextMaxLength = 4000;
        /// <summary>
        /// Maximum length for URLs stored or accepted by the API.
        /// </summary>
        public const int UrlMaxLength = 2048;
        /// <summary>
        /// Maximum length for academic year labels.
        /// </summary>
        public const int AcademicYearMaxLength = 20;
        /// <summary>
        /// Maximum length for class section labels.
        /// </summary>
        public const int SectionMaxLength = 20;
        /// <summary>
        /// Maximum length for subject codes.
        /// </summary>
        public const int SubjectCodeMaxLength = 30;
        /// <summary>
        /// Maximum length for teacher department names.
        /// </summary>
        public const int DepartmentMaxLength = 100;
        /// <summary>
        /// Maximum length for administrative phone numbers.
        /// </summary>
        public const int PhoneNumberMaxLength = 20;
        /// <summary>
        /// Maximum length for BCP-47-like language codes used by localized emails and settings.
        /// </summary>
        public const int LanguageCodeMaxLength = 10;
        /// <summary>
        /// Maximum accepted page size for paginated list endpoints.
        /// </summary>
        public const int MaxPageSize = 100;
        /// <summary>
        /// Maximum number of ids accepted by bulk assignment or removal DTOs.
        /// </summary>
        public const int MaxBulkIds = 1000;
        /// <summary>
        /// Maximum number of quiz retries accepted by quiz DTOs.
        /// </summary>
        public const int MaxQuizRetries = 100;
        /// <summary>
        /// Maximum score value that can be assigned to a single quiz question.
        /// </summary>
        public const int MaxQuestionDifficulty = 1000;
        /// <summary>
        /// Maximum per-question time limit in seconds.
        /// </summary>
        public const int MaxQuestionTimeLimitSeconds = 86400;
        /// <summary>
        /// Maximum response duration in milliseconds accepted for submitted quiz answers.
        /// </summary>
        public const int MaxResponseTimeMs = 3600000;
        /// <summary>
        /// Maximum XP bonus that a badge can award.
        /// </summary>
        public const int MaxXpBonus = 100000;
        /// <summary>
        /// Maximum numeric target value for badge rule comparisons.
        /// </summary>
        public const double MaxBadgeRuleTarget = 1000000;
        /// <summary>
        /// Maximum configurable upload size in megabytes.
        /// </summary>
        public const int MaxUploadSizeMb = 100;
        /// <summary>
        /// Maximum configurable active refresh-token sessions per user.
        /// </summary>
        public const int MaxActiveSessionsPerUser = 20;
        /// <summary>
        /// Best mark in the supported grading scale.
        /// </summary>
        public const int MinMark = 1;
        /// <summary>
        /// Worst mark in the supported grading scale.
        /// </summary>
        public const int MaxMark = 5;

        /// <summary>
        /// Regular expression for basic email address shape validation.
        /// </summary>
        public const string EmailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        /// <summary>
        /// Regular expression for personal names with spaces, hyphens, and Czech diacritics.
        /// </summary>
        public const string PersonNameRegex = @"^[a-zA-Z \-\u00E1\u00E9\u00ED\u00F3\u00FA\u00FD\u010D\u010F\u011B\u0148\u0159\u0161\u0165\u017E\u00C1\u00C9\u00CD\u00D3\u00DA\u00DD\u010C\u010E\u011A\u0147\u0158\u0160\u0164\u017D]+$";
        /// <summary>
        /// Regular expression for usernames with letters, numbers, underscores, hyphens, and Czech diacritics.
        /// </summary>
        public const string UsernameRegex = @"^[a-zA-Z0-9_\-\u00E1\u00E9\u00ED\u00F3\u00FA\u00FD\u010D\u010F\u011B\u0148\u0159\u0161\u0165\u017E\u00C1\u00C9\u00CD\u00D3\u00DA\u00DD\u010C\u010E\u011A\u0147\u0158\u0160\u0164\u017D]+$";
        /// <summary>
        /// Regular expression for department names with common punctuation and Czech diacritics.
        /// </summary>
        public const string DepartmentRegex = @"^[a-zA-Z0-9_ \-\u00E1\u00E9\u00ED\u00F3\u00FA\u00FD\u010D\u010F\u011B\u0148\u0159\u0161\u0165\u017E\u00C1\u00C9\u00CD\u00D3\u00DA\u00DD\u010C\u010E\u011A\u0147\u0158\u0160\u0164\u017D.,&]+$";
        /// <summary>
        /// Regular expression for flexible phone input with optional plus sign, spaces, dashes, and parentheses.
        /// </summary>
        public const string FlexiblePhoneRegex = @"^\+?[0-9\s\-()]{7,15}$";
        /// <summary>
        /// Regular expression for international phone numbers using a leading 00 country prefix.
        /// </summary>
        public const string InternationalPhoneRegex = @"^00[1-9][0-9]{0,2}[0-9]{7,12}$";
        /// <summary>
        /// Regular expression requiring lower-case, upper-case, digit, special character, and at least eight characters.
        /// </summary>
        public const string PasswordComplexityRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$";
    }
}
