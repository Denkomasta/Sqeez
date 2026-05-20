using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Gamification;

namespace Sqeez.Api.Models.Users
{
    /// <summary>
    /// Base persisted user account. Teacher and admin accounts inherit from this entity.
    /// </summary>
    public class Student
    {
        /// <summary>
        /// Primary identifier shared by all user roles.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// User's given name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's family name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Unique login name used together with email for account lookup.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email address used for login, verification, and account recovery.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password value. Plain text passwords must never be stored here.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Current experience points accumulated through quizzes and badge rewards.
        /// </summary>
        public int CurrentXP { get; set; }

        /// <summary>
        /// Effective role for authorization and discriminator-based account behavior.
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// Last known activity timestamp in UTC.
        /// </summary>
        public DateTime LastSeen { get; set; }

        /// <summary>
        /// UTC timestamp when the account was archived, or null for active accounts.
        /// </summary>
        public DateTime? ArchivedAt { get; set; }

        /// <summary>
        /// Public URL of the user's avatar image, when one has been uploaded.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Indicates whether the current email address has been verified.
        /// </summary>
        public bool IsEmailVerified { get; set; } = false;

        /// <summary>
        /// Token used to verify the current or pending email address.
        /// </summary>
        public string? EmailVerificationToken { get; set; }

        /// <summary>
        /// UTC expiry time for the email verification token.
        /// </summary>
        public DateTime? EmailVerificationTokenExpiry { get; set; }

        /// <summary>
        /// Requested replacement email that becomes active after verification.
        /// </summary>
        public string? PendingNewEmail { get; set; }

        /// <summary>
        /// Token used by the password reset flow.
        /// </summary>
        public string? PasswordResetToken { get; set; }

        /// <summary>
        /// UTC expiry time for the password reset token.
        /// </summary>
        public DateTime? PasswordResetTokenExpiry { get; set; }

        /// <summary>
        /// Optional class membership for students and teacher accounts acting as students.
        /// </summary>
        public long? SchoolClassId { get; set; }

        /// <summary>
        /// School class the user attends, when assigned.
        /// </summary>
        public SchoolClass? SchoolClass { get; set; }

        /// <summary>
        /// Badges earned by the user.
        /// </summary>
        public ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();

        /// <summary>
        /// Subject enrollments owned by the user.
        /// </summary>
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
