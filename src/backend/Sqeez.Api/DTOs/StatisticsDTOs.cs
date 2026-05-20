namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// High-level quiz performance statistics.
    /// </summary>
    public class QuizSummaryStatDto
    {
        /// <summary>
        /// Quiz identifier.
        /// </summary>
        public long QuizId { get; set; }

        /// <summary>
        /// Number of attempts created for the quiz.
        /// </summary>
        public int TotalAttempts { get; set; }

        /// <summary>
        /// Number of attempts that reached the completed state.
        /// </summary>
        public int CompletedAttempts { get; set; }

        /// <summary>
        /// Average score across completed attempts.
        /// </summary>
        public double AverageScore { get; set; }

        /// <summary>
        /// Highest score across completed attempts.
        /// </summary>
        public int HighestScore { get; set; }

        /// <summary>
        /// Lowest score across completed attempts.
        /// </summary>
        public int LowestScore { get; set; }

        /// <summary>
        /// Average completion time in minutes for completed attempts.
        /// </summary>
        public double AverageCompletionTimeMinutes { get; set; }
    }

    /// <summary>
    /// Aggregated option selection statistics for a question.
    /// </summary>
    public class OptionStatDto
    {
        /// <summary>
        /// Option identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Option text, if the option has text content.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Number of responses that selected this option.
        /// </summary>
        public int PickCount { get; set; }

        /// <summary>
        /// Whether this option is marked as correct.
        /// </summary>
        public bool IsCorrect { get; set; }
    }

    /// <summary>
    /// Aggregated question statistics including response counts, timing, and option/free-text details.
    /// </summary>
    public class QuestionStatDto
    {
        /// <summary>
        /// Question identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Question title or text.
        /// </summary>
        public string? QuestionText { get; set; }

        /// <summary>
        /// Whether the question is answered with free text.
        /// </summary>
        public bool IsFreeText { get; set; }

        /// <summary>
        /// Number of responses submitted for this question.
        /// </summary>
        public int TotalAnswers { get; set; }

        /// <summary>
        /// Average score awarded for this question.
        /// </summary>
        public double AverageScore { get; set; }

        /// <summary>
        /// Average response time in seconds.
        /// </summary>
        public double AverageResponseTimeSeconds { get; set; }

        /// <summary>
        /// Option pick statistics for choice questions.
        /// </summary>
        public List<OptionStatDto> Options { get; set; } = new();

        /// <summary>
        /// Submitted answers for free-text questions.
        /// </summary>
        public List<string> SubmittedFreeTextAnswers { get; set; } = new();
    }
}
