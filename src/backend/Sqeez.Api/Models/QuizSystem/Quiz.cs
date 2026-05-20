using Sqeez.Api.Models.Academics;

namespace Sqeez.Api.Models.QuizSystem
{
    /// <summary>
    /// Quiz assigned to a subject with availability dates, questions, and attempts.
    /// </summary>
    public class Quiz
    {
        /// <summary>
        /// Primary identifier of the quiz.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Display title of the quiz.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional instructions or description shown before taking the quiz.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of retry attempts allowed after the first attempt.
        /// </summary>
        public int MaxRetries { get; set; } = 0;

        /// <summary>
        /// UTC timestamp when the quiz was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Optional UTC timestamp when the quiz becomes visible for taking.
        /// </summary>
        public DateTime? PublishDate { get; set; }

        /// <summary>
        /// Optional UTC timestamp after which the quiz is closed for taking.
        /// </summary>
        public DateTime? ClosingDate { get; set; }

        /// <summary>
        /// Identifier of the subject that owns the quiz.
        /// </summary>
        public long SubjectId { get; set; }

        /// <summary>
        /// Subject that owns the quiz.
        /// </summary>
        public Subject Subject { get; set; } = null!;

        /// <summary>
        /// Questions that make up the quiz.
        /// </summary>
        public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();

        /// <summary>
        /// Attempts submitted for the quiz.
        /// </summary>
        public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    }
}
