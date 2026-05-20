namespace Sqeez.Api.Models.QuizSystem
{
    /// <summary>
    /// Student response to a quiz question within a quiz attempt.
    /// </summary>
    public class QuizQuestionResponse
    {
        /// <summary>
        /// Primary identifier of the response.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Time spent answering the question in milliseconds.
        /// </summary>
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// Submitted free-text answer, when the question is free text.
        /// </summary>
        public string? FreeTextAnswer { get; set; }

        /// <summary>
        /// Indicates whether the student marked the question as liked.
        /// </summary>
        public bool IsLiked { get; set; }

        /// <summary>
        /// Score awarded for this response, when evaluated.
        /// </summary>
        public int? Score { get; set; }

        /// <summary>
        /// Identifier of the attempt containing this response.
        /// </summary>
        public long QuizAttemptId { get; set; }

        /// <summary>
        /// Attempt containing this response.
        /// </summary>
        public QuizAttempt QuizAttempt { get; set; } = null!;

        /// <summary>
        /// Identifier of the answered question.
        /// </summary>
        public long QuizQuestionId { get; set; }

        /// <summary>
        /// Question answered by this response.
        /// </summary>
        public QuizQuestion QuizQuestion { get; set; } = null!;

        /// <summary>
        /// Options selected for the response.
        /// </summary>
        public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
    }
}
