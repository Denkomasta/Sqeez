using Sqeez.Api.Constants;
using Sqeez.Api.Validation;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Quiz metadata summary returned by quiz endpoints.
    /// </summary>
    public record QuizDto(
        long Id,
        string Title,
        string Description,
        int MaxRetries,
        DateTime CreatedAt,
        DateTime? PublishDate,
        DateTime? ClosingDate,
        long SubjectId,
        int QuizQuestions,
        int QuizAttempts);

    /// <summary>
    /// Quiz search filters, including subject, student, teacher, and UTC date filters.
    /// </summary>
    public class QuizFilterDto : PagedFilterDto
    {
        [StringLength(ValidationConstants.SearchTermMaxLength)]
        public string? SearchTerm { get; set; }  // Search against Title or Description
        public bool? IsActive { get; set; }
        [UtcDateTime]
        public DateTime? PublishDate { get; set; }
        [UtcDateTime]
        public DateTime? ClosingDate { get; set; }
        public long? SubjectId { get; set; }
        public long? StudentId { get; set; }
        public long? TeacherId { get; set; }
    }

    /// <summary>
    /// Optional query context for loading quiz data for a specific student.
    /// </summary>
    public record GetQuizDto(long? studentId);

    /// <summary>
    /// Request for creating a quiz with optional UTC publish and closing dates.
    /// </summary>
    public record CreateQuizDto
    {
        public CreateQuizDto() { }

        public CreateQuizDto(string Title, string Description, long SubjectId, int MaxRetries = 0, DateTime? PublishDate = null, DateTime? ClosingDate = null)
        {
            this.Title = Title;
            this.Description = Description;
            this.SubjectId = SubjectId;
            this.MaxRetries = MaxRetries;
            this.PublishDate = PublishDate;
            this.ClosingDate = ClosingDate;
        }

        [StringLength(ValidationConstants.TitleMaxLength)]
        public string Title { get; init; } = string.Empty;

        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string Description { get; init; } = string.Empty;
        public long SubjectId { get; init; }

        [Range(0, ValidationConstants.MaxQuizRetries)]
        public int MaxRetries { get; init; } = 0;
        [UtcDateTime]
        public DateTime? PublishDate { get; init; }
        [UtcDateTime]
        public DateTime? ClosingDate { get; init; }
    }

    /// <summary>
    /// Request for partially updating quiz metadata and schedule values.
    /// </summary>
    public record PatchQuizDto
    {
        public PatchQuizDto() { }

        public PatchQuizDto(string? Title = null, string? Description = null, int? MaxRetries = null, long? SubjectId = null, DateTime? PublishDate = null, DateTime? ClosingDate = null)
        {
            this.Title = Title;
            this.Description = Description;
            this.MaxRetries = MaxRetries;
            this.SubjectId = SubjectId;
            this.PublishDate = PublishDate;
            this.ClosingDate = ClosingDate;
        }

        [StringLength(ValidationConstants.TitleMaxLength)]
        public string? Title { get; init; }

        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string? Description { get; init; }

        [Range(0, ValidationConstants.MaxQuizRetries)]
        public int? MaxRetries { get; init; }
        public long? SubjectId { get; init; }
        [UtcDateTime]
        public DateTime? PublishDate { get; init; }
        [UtcDateTime]
        public DateTime? ClosingDate { get; init; }
    }

    /// <summary>
    /// Quiz question summary returned by management endpoints.
    /// </summary>
    public record QuizQuestionDto(
        long Id,
        string Title,
        int Difficulty,
        bool HasPenalty,
        int TimeLimit,
        bool IsStrictMultipleChoice,
        long QuizId,
        long? MediaAssetId,
        int QuizOptions,
        int CalculatedPenalty);

    /// <summary>
    /// Quiz question search filters.
    /// </summary>
    public class QuizQuestionFilterDto : PagedFilterDto
    {
        [StringLength(ValidationConstants.SearchTermMaxLength)]
        public string? SearchTerm { get; set; }  // Search against Title

        [Range(0, ValidationConstants.MaxQuestionDifficulty)]
        public int? Difficulty { get; set; }
        public long? QuizId { get; set; }
        public long? MediaAssetId { get; set; }
    }

    /// <summary>
    /// Request for creating a quiz question.
    /// </summary>
    public record CreateQuizQuestionDto
    {
        public CreateQuizQuestionDto() { }

        public CreateQuizQuestionDto(string Title, int Difficulty, int TimeLimit, long QuizId, bool HasPenalty = false, long? MediaAssetId = null, bool IsStrictMultipleChoice = false)
        {
            this.Title = Title;
            this.Difficulty = Difficulty;
            this.TimeLimit = TimeLimit;
            this.QuizId = QuizId;
            this.HasPenalty = HasPenalty;
            this.MediaAssetId = MediaAssetId;
            this.IsStrictMultipleChoice = IsStrictMultipleChoice;
        }

        [StringLength(ValidationConstants.TitleMaxLength)]
        public string Title { get; init; } = string.Empty;

        [Range(0, ValidationConstants.MaxQuestionDifficulty)]
        public int Difficulty { get; init; }

        [Range(0, ValidationConstants.MaxQuestionTimeLimitSeconds)]
        public int TimeLimit { get; init; }
        public long QuizId { get; init; }
        public bool HasPenalty { get; init; } = false;
        public long? MediaAssetId { get; init; }
        public bool IsStrictMultipleChoice { get; init; } = false;
    }

    /// <summary>
    /// Request for partially updating a quiz question.
    /// </summary>
    public record PatchQuizQuestionDto
    {
        public PatchQuizQuestionDto() { }

        public PatchQuizQuestionDto(string? Title = null, int? Difficulty = null, bool? HasPenalty = null, int? TimeLimit = null, long? MediaAssetId = null, bool? IsStrictMultipleChoice = null)
        {
            this.Title = Title;
            this.Difficulty = Difficulty;
            this.HasPenalty = HasPenalty;
            this.TimeLimit = TimeLimit;
            this.MediaAssetId = MediaAssetId;
            this.IsStrictMultipleChoice = IsStrictMultipleChoice;
        }

        [StringLength(ValidationConstants.TitleMaxLength)]
        public string? Title { get; init; }

        [Range(0, ValidationConstants.MaxQuestionDifficulty)]
        public int? Difficulty { get; init; }
        public bool? HasPenalty { get; init; }

        [Range(0, ValidationConstants.MaxQuestionTimeLimitSeconds)]
        public int? TimeLimit { get; init; }
        public long? MediaAssetId { get; init; }
        public bool? IsStrictMultipleChoice { get; init; }
    }

    /// <summary>
    /// Question detail used for quiz taking and management views.
    /// </summary>
    public record DetailedQuizQuestionDto(
        long Id,
        string Title,
        int Difficulty,
        bool HasPenalty,
        int CalculatedPenalty,
        int TimeLimit,
        bool IsStrictMultipleChoice,
        long QuizId,
        long? MediaAssetId,
        List<StudentQuizOptionDto> Options
    );

    /// <summary>
    /// Quiz option summary returned by management endpoints.
    /// </summary>
    public record QuizOptionDto(
        long Id,
        string? Text,
        bool IsFreeText,
        bool IsCorrect,
        long QuizQuestionId,
        long? MediaAssetId,
        int Responses);

    /// <summary>
    /// Quiz option search filters.
    /// </summary>
    public class QuizOptionFilterDto : PagedFilterDto
    {
        [StringLength(ValidationConstants.SearchTermMaxLength)]
        public string? SearchTerm { get; set; }  // Search against Text
        public bool? IsFreeText { get; set; }
        public bool? IsCorrect { get; set; }
        public long? QuizQuestionId { get; set; }
        public long? MediaAssetId { get; set; }
    }

    /// <summary>
    /// Request for creating a quiz option.
    /// </summary>
    public record CreateQuizOptionDto
    {
        public CreateQuizOptionDto() { }

        public CreateQuizOptionDto(bool IsCorrect, long QuizQuestionID, string? Text = null, bool IsFreeText = false, long? MediaAssetId = null)
        {
            this.IsCorrect = IsCorrect;
            this.QuizQuestionID = QuizQuestionID;
            this.Text = Text;
            this.IsFreeText = IsFreeText;
            this.MediaAssetId = MediaAssetId;
        }

        public bool IsCorrect { get; init; }
        public long QuizQuestionID { get; init; }

        [StringLength(ValidationConstants.LongTextMaxLength)]
        public string? Text { get; init; }
        public bool IsFreeText { get; init; } = false;
        public long? MediaAssetId { get; init; }
    }

    /// <summary>
    /// Request for partially updating a quiz option.
    /// </summary>
    public record PatchQuizOptionDto
    {
        public PatchQuizOptionDto() { }

        public PatchQuizOptionDto(string? Text = null, bool? IsFreeText = null, bool? IsCorrect = null, long? MediaAssetId = null)
        {
            this.Text = Text;
            this.IsFreeText = IsFreeText;
            this.IsCorrect = IsCorrect;
            this.MediaAssetId = MediaAssetId;
        }

        [StringLength(ValidationConstants.LongTextMaxLength)]
        public string? Text { get; init; }
        public bool? IsFreeText { get; init; }
        public bool? IsCorrect { get; init; }
        public long? MediaAssetId { get; init; }
    }

    /// <summary>
    /// Student-safe quiz option DTO that hides correctness.
    /// </summary>
    public record StudentQuizOptionDto(
        long Id,
        string? Text,
        bool IsFreeText,
        long QuizQuestionId,
        long? MediaAssetId
    );
}
