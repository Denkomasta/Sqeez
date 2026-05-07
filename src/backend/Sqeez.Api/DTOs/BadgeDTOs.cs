using Sqeez.Api.Enums;

using Sqeez.Api.Constants;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Persisted rule that determines when a badge can be awarded.
    /// </summary>
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

        public BadgeMetric Metric { get; init; }
        public BadgeOperator Operator { get; init; }

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

        public long? Id { get; init; } // Null means "Create a new rule attached to this badge"
        public BadgeMetric Metric { get; init; }
        public BadgeOperator Operator { get; init; }

        [Range(0, ValidationConstants.MaxBadgeRuleTarget)]
        public decimal TargetValue { get; init; }
    }

    // Using classes here because model binding IFormFile with Lists in records can be tricky in ASP.NET
    /// <summary>
    /// Multipart request for creating a badge with optional icon and rules.
    /// </summary>
    public class CreateBadgeDto
    {
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string Name { get; set; } = string.Empty;

        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string Description { get; set; } = string.Empty;

        [Range(0, ValidationConstants.MaxXpBonus)]
        public int XpBonus { get; set; }
        public IFormFile? IconFile { get; set; } = null;

        [MaxLength(ValidationConstants.MaxBulkIds)]
        public List<CreateBadgeRuleDto> Rules { get; set; } = new List<CreateBadgeRuleDto>();
    }

    /// <summary>
    /// Multipart request for patching badge metadata, icon, and rules.
    /// </summary>
    public class UpdateBadgeDto
    {
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string? Name { get; set; }

        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string? Description { get; set; }

        [Range(0, ValidationConstants.MaxXpBonus)]
        public int? XpBonus { get; set; }
        public IFormFile? NewIconFile { get; set; }

        [MaxLength(ValidationConstants.MaxBulkIds)]
        public List<UpdateBadgeRuleDto>? Rules { get; set; }
    }

    /// <summary>
    /// Badge definition returned by badge endpoints.
    /// </summary>
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
        public long BadgeId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? IconUrl { get; init; }
        public DateTime EarnedAt { get; init; }
    }

    /// <summary>
    /// Computed values used when evaluating whether badge rules are satisfied.
    /// </summary>
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
        [StringLength(ValidationConstants.SearchTermMaxLength)]
        public string? SearchTerm { get; init; }
        public bool? isEarned { get; init; }
        public long? StudentId { get; init; }
    }
}
