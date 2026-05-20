using Sqeez.Api.Models.Users;

namespace Sqeez.Api.Models.Gamification
{
    /// <summary>
    /// Join entity representing a badge earned by a student.
    /// </summary>
    public class StudentBadge
    {
        /// <summary>
        /// UTC timestamp when the badge was earned.
        /// </summary>
        public DateTime EarnedAt { get; set; }

        /// <summary>
        /// Identifier of the student who earned the badge.
        /// </summary>
        public long StudentId { get; set; }

        /// <summary>
        /// Student who earned the badge.
        /// </summary>
        public Student Student { get; set; } = null!;

        /// <summary>
        /// Identifier of the earned badge.
        /// </summary>
        public long BadgeId { get; set; }

        /// <summary>
        /// Earned badge.
        /// </summary>
        public Badge Badge { get; set; } = null!;
    }
}
