using Sqeez.Api.Enums;

using Sqeez.Api.Constants;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Persisted rule that determines when a badge can be awarded.
    /// </summary>
    /// <param name="Id">Rule identifier.</param>
    /// <param name="Metric">Metric inspected during badge evaluation.</param>
    /// <param name="Operator">Comparison operator used against TargetValue.</param>
    /// <param name="TargetValue">Value that the selected metric is compared with.</param>
    public record BadgeRuleDto(
        long Id,
        BadgeMetric Metric,
        BadgeOperator Operator,
        [Range(0, ValidationConstants.MaxBadgeRuleTarget)]
        decimal TargetValue
    );

    /// <summary>
    /// Request for creating one badge rule.
    /// </summary>
    public record CreateBadgeRuleDto
    {
        public CreateBadgeRuleDto() { }

        public CreateBadgeRuleDto(BadgeMetric Metric, BadgeOperator Operator, decimal TargetValue)
        {
            this.Metric = Metric;
            this.Operator = Operator;
            this.TargetValue = TargetValue;
        }

        /// <summary>
        /// Metric inspected during badge evaluation.
        /// </summary>
        public BadgeMetric Metric { get; init; }

        /// <summary>
        /// Comparison operator used against TargetValue.
        /// </summary>
        public BadgeOperator Operator { get; init; }

        /// <summary>
        /// Value that the selected metric is compared with.
        /// </summary>
        [Range(0, ValidationConstants.MaxBadgeRuleTarget)]
        public decimal TargetValue { get; init; }
    }

    /// <summary>
    /// Request for updating an existing badge rule or creating a new rule when id is omitted.
    /// </summary>
    public record UpdateBadgeRuleDto
    {
        public UpdateBadgeRuleDto() { }

        public UpdateBadgeRuleDto(long? Id, BadgeMetric Metric, BadgeOperator Operator, decimal TargetValue)
        {
            this.Id = Id;
            this.Metric = Metric;
            this.Operator = Operator;
            this.TargetValue = TargetValue;
        }

        /// <summary>
        /// Existing rule id. Null creates a new rule attached to the badge.
        /// </summary>
        public long? Id { get; init; }

        /// <summary>
        /// Metric inspected during badge evaluation.
        /// </summary>
        public BadgeMetric Metric { get; init; }

        /// <summary>
        /// Comparison operator used against TargetValue.
        /// </summary>
        public BadgeOperator Operator { get; init; }

        /// <summary>
        /// Value that the selected metric is compared with.
        /// </summary>
        [Range(0, ValidationConstants.MaxBadgeRuleTarget)]
        public decimal TargetValue { get; init; }
    }

    /// <summary>
    /// Multipart request for creating a badge with optional icon and rules.
    /// </summary>
    public class CreateBadgeDto
    {
        /// <summary>
        /// Badge display name.
        /// </summary>
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Badge description shown to users.
        /// </summary>
        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// XP added to the student when the badge is awarded.
        /// </summary>
        [Range(0, ValidationConstants.MaxXpBonus)]
        public int XpBonus { get; set; }

        /// <summary>
        /// Optional badge icon uploaded as multipart form data.
        /// </summary>
        public IFormFile? IconFile { get; set; } = null;

        /// <summary>
        /// Rules that must be satisfied before the badge can be awarded.
        /// </summary>
        [MaxLength(ValidationConstants.MaxBulkIds)]
        public List<CreateBadgeRuleDto> Rules { get; set; } = new List<CreateBadgeRuleDto>();
    }

    /// <summary>
    /// Multipart request for patching badge metadata, icon, and rules.
    /// </summary>
    public class UpdateBadgeDto
    {
        /// <summary>
        /// Replacement badge display name.
        /// </summary>
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string? Name { get; set; }

        /// <summary>
        /// Replacement badge description.
        /// </summary>
        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string? Description { get; set; }

        /// <summary>
        /// Replacement XP bonus.
        /// </summary>
        [Range(0, ValidationConstants.MaxXpBonus)]
        public int? XpBonus { get; set; }

        /// <summary>
        /// Optional replacement icon uploaded as multipart form data.
        /// </summary>
        public IFormFile? NewIconFile { get; set; }

        /// <summary>
        /// Full desired rule set. Existing rules omitted from this list are removed.
        /// </summary>
        [MaxLength(ValidationConstants.MaxBulkIds)]
        public List<UpdateBadgeRuleDto>? Rules { get; set; }
    }

    /// <summary>
    /// Badge definition returned by badge endpoints.
    /// </summary>
    /// <param name="Id">Badge identifier.</param>
    /// <param name="Name">Badge display name.</param>
    /// <param name="Description">Badge description.</param>
    /// <param name="IconUrl">Public icon URL, or null when no icon is assigned.</param>
    /// <param name="XpBonus">XP added when the badge is awarded.</param>
    /// <param name="Rules">Rules attached to this badge.</param>
    public record BadgeDto(
        long Id,
        string Name,
        string Description,
        string? IconUrl,
        int XpBonus,
        List<BadgeRuleDto> Rules
    );

    /// <summary>
    /// Badge award view returned for a specific student.
    /// </summary>
    /// <param name="BadgeId">Awarded badge id.</param>
    /// <param name="Name">Badge display name.</param>
    /// <param name="Description">Badge description.</param>
    /// <param name="IconUrl">Public icon URL, or null when no icon is assigned.</param>
    /// <param name="XpBonus">XP added when the badge was awarded.</param>
    /// <param name="EarnedAt">UTC timestamp when the student earned the badge.</param>
    public record StudentBadgeDto(
        long BadgeId,
        string Name,
        string Description,
        string? IconUrl,
        int XpBonus,
        DateTime EarnedAt
    );

    /// <summary>
    /// Compact badge award view embedded in user and quiz attempt responses.
    /// </summary>
    public record StudentBadgeBasicDto
    {
        /// <summary>
        /// Awarded badge id.
        /// </summary>
        public long BadgeId { get; init; }

        /// <summary>
        /// Badge display name.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Public icon URL, or null when no icon is assigned.
        /// </summary>
        public string? IconUrl { get; init; }

        /// <summary>
        /// UTC timestamp when the student earned the badge.
        /// </summary>
        public DateTime EarnedAt { get; init; }
    }

    /// <summary>
    /// Computed values used when evaluating whether badge rules are satisfied.
    /// </summary>
    /// <param name="ScorePercentage">Completed quiz score as a percentage.</param>
    /// <param name="TotalScore">Total score awarded for the completed activity.</param>
    /// <param name="PerfectAnswersCount">Number of answers that received full credit.</param>
    /// <param name="TotalAttempts">Number of attempts completed by the student.</param>
    public record BadgeEvaluationMetrics(
        decimal ScorePercentage,
        int TotalScore,
        int PerfectAnswersCount,
        int TotalAttempts
    );

    /// <summary>
    /// Badge search filters.
    /// </summary>
    public class BadgeFilterDto : PagedFilterDto
    {
        /// <summary>
        /// Searches badge name and description.
        /// </summary>
        [StringLength(ValidationConstants.SearchTermMaxLength)]
        public string? SearchTerm { get; init; }

        /// <summary>
        /// Filters badges by whether StudentId has earned them.
        /// </summary>
        public bool? isEarned { get; init; }

        /// <summary>
        /// Student id used together with isEarned.
        /// </summary>
        public long? StudentId { get; init; }
    }
}
