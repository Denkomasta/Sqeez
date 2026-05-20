using Sqeez.Api.Constants;
using Sqeez.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Quiz attempt summary returned when an attempt is started, completed, or listed.
    /// </summary>
    /// <param name="Id">Attempt identifier.</param>
    /// <param name="QuizId">Quiz being attempted.</param>
    /// <param name="EnrollmentId">Enrollment that proves the student's access to the quiz subject.</param>
    /// <param name="StartTime">UTC timestamp when the attempt started.</param>
    /// <param name="EndTime">UTC timestamp when the attempt was completed or closed.</param>
    /// <param name="Status">Attempt lifecycle state.</param>
    /// <param name="TotalScore">Total score currently awarded for the attempt.</param>
    /// <param name="Mark">Optional derived mark for completed attempts.</param>
    /// <param name="NextQuestionId">Next unanswered question id, or null when no pending question remains.</param>
    /// <param name="EarnedBadges">Badges awarded while completing the attempt.</param>
    /// <param name="StudentName">Student display name, included only for privileged quiz-owner views.</param>
    /// <param name="StudentId">Student id, included only for privileged quiz-owner views.</param>
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
    /// Quiz attempt navigation state used to recover the next question and render progress.
    /// </summary>
    /// <param name="NextQuestionId">Next unanswered question id, or null when all questions are answered.</param>
    /// <param name="AnsweredQuestionsCount">Number of distinct questions already answered in the attempt.</param>
    public record NextQuestionProgressDto(
        long? NextQuestionId,
        int AnsweredQuestionsCount
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

        /// <summary>
        /// Question being answered. It must belong to the quiz attempt.
        /// </summary>
        public long QuizQuestionId { get; init; }

        /// <summary>
        /// Time spent on the question in milliseconds.
        /// </summary>
        [Range(0, ValidationConstants.MaxResponseTimeMs)]
        public long ResponseTimeMs { get; init; }

        /// <summary>
        /// Free-text answer for free-text questions.
        /// </summary>
        [StringLength(ValidationConstants.LongTextMaxLength)]
        public string? FreeTextAnswer { get; init; }

        /// <summary>
        /// Selected option ids for single-choice or multiple-choice questions.
        /// </summary>
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

        /// <summary>
        /// Teacher feedback flag for highlighting an answer.
        /// </summary>
        public bool IsLiked { get; init; }
    }
}
