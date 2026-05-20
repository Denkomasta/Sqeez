namespace Sqeez.Api.Models.Gamification
{
    /// <summary>
    /// Achievement awarded to students when configured badge rules are satisfied.
    /// </summary>
    public class Badge
    {
        /// <summary>
        /// Primary identifier of the badge.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Display name of the badge.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description shown to users.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional URL of the badge icon.
        /// </summary>
        public string? IconUrl { get; set; }

        /// <summary>
        /// Experience points granted when the badge is awarded.
        /// </summary>
        public int XpBonus { get; set; }

        /// <summary>
        /// Rules that must be satisfied for the badge to be awarded.
        /// </summary>
        public ICollection<BadgeRule> Rules { get; set; } = new List<BadgeRule>();

        /// <summary>
        /// Student badge awards created from this badge.
        /// </summary>
        public ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();
    }
}
