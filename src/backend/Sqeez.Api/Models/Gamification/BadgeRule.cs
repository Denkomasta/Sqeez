using Sqeez.Api.Enums;

namespace Sqeez.Api.Models.Gamification
{
    /// <summary>
    /// Single metric comparison used to decide whether a badge should be awarded.
    /// </summary>
    public class BadgeRule
    {
        /// <summary>
        /// Primary identifier of the rule.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Identifier of the badge that owns this rule.
        /// </summary>
        public long BadgeId { get; set; }

        /// <summary>
        /// Badge that owns this rule.
        /// </summary>
        public Badge Badge { get; set; } = null!;

        /// <summary>
        /// Attempt or student metric evaluated by the rule.
        /// </summary>
        public BadgeMetric Metric { get; set; }

        /// <summary>
        /// Comparison operator used against <see cref="TargetValue"/>.
        /// </summary>
        public BadgeOperator Operator { get; set; }

        /// <summary>
        /// Threshold or exact value used by the rule comparison.
        /// </summary>
        public decimal TargetValue { get; set; }
    }
}
