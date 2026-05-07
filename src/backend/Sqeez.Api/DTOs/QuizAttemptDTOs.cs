using Sqeez.Api.Constants;
using Sqeez.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Quiz attempt summary returned when an attempt is started, completed, or listed.
    /// </summary>
    public record QuizAttemptDto(
        long Id,
        long QuizId,
        long EnrollmentId,
        DateTime? StartTime,
        DateTime? EndTime,
        AttemptStatus Status,
        int TotalScore,
        int? Mark,
        long? NextQuestionId = null,
        List<StudentBadgeBasicDto>? EarnedBadges = null,
        string? StudentName = null,
        long? StudentId = null
    );

    /// <summary>
    /// Stored answer response returned in attempt details.
    /// </summary>
    public record QuestionResponseDto(
        long Id,
        long QuizQuestionId,
        long ResponseTimeMs,
        string? FreeTextAnswer,
        bool IsLiked,
        int? Score,
        List<long> SelectedOptionIds
    );

    /// <summary>
    /// Answer-submission response with correctness hints and next-question navigation.
    /// </summary>
    public record QuestionAnsweredDto(
        long Id,
        long QuizQuestionId,
        long ResponseTimeMs,
        string? FreeTextAnswer,
        bool IsLiked,
        int? Score,
        List<long> SelectedOptionIds,
        List<long>? CorrectOptionIds = null,
        string? CorrectFreeTextAnswer = null,
        long? NextQuestionId = null
    );

    /// <summary>
    /// Full quiz attempt view including all submitted responses.
    /// </summary>
    public record QuizAttemptDetailDto(
        long Id,
        long QuizId,
        long EnrollmentId,
        DateTime? StartTime,
        DateTime? EndTime,
        AttemptStatus Status,
        int TotalScore,
        int? Mark,
        List<QuestionResponseDto> Responses
    );

    /// <summary>
    /// Request for starting a quiz attempt for a student's enrollment.
    /// </summary>
    public record StartQuizAttemptDto(
        long QuizId,
        long EnrollmentId
    );

    /// <summary>
    /// Request for submitting a response to one quiz question.
    /// </summary>
    public record SubmitQuestionResponseDto
    {
        public SubmitQuestionResponseDto() { }

        public SubmitQuestionResponseDto(long QuizQuestionId, long ResponseTimeMs, string? FreeTextAnswer, List<long> SelectedOptionIds)
        {
            this.QuizQuestionId = QuizQuestionId;
            this.ResponseTimeMs = ResponseTimeMs;
            this.FreeTextAnswer = FreeTextAnswer;
            this.SelectedOptionIds = SelectedOptionIds;
        }

        public long QuizQuestionId { get; init; }

        [Range(0, ValidationConstants.MaxResponseTimeMs)]
        public long ResponseTimeMs { get; init; }

        [StringLength(ValidationConstants.LongTextMaxLength)]
        public string? FreeTextAnswer { get; init; }

        [MaxLength(ValidationConstants.MaxBulkIds)]
        public List<long> SelectedOptionIds { get; init; } = new();
    }

    /// <summary>
    /// Teacher/admin request for manually grading a free-text response.
    /// </summary>
    public record GradeQuestionResponseDto
    {
        public GradeQuestionResponseDto() { }

        public GradeQuestionResponseDto(int Score, bool IsLiked)
        {
            this.Score = Score;
            this.IsLiked = IsLiked;
        }

        [Range(-ValidationConstants.MaxQuestionDifficulty, ValidationConstants.MaxQuestionDifficulty)]
        public int Score { get; init; }
        public bool IsLiked { get; init; }
    }
}
