using Sqeez.Api.Models.Media;

namespace Sqeez.Api.Models.QuizSystem
{
    /// <summary>
    /// Question in a quiz, including scoring difficulty, optional media, and answer options.
    /// </summary>
    public class QuizQuestion
    {
        /// <summary>
        /// Primary identifier of the quiz question.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Question text or prompt.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Point value used for scoring the question.
        /// </summary>
        public int Difficulty { get; set; }

        /// <summary>
        /// Indicates whether wrong answers subtract penalty points.
        /// </summary>
        public bool HasPenalty { get; set; }

        /// <summary>
        /// Per-question time limit in seconds.
        /// </summary>
        public int TimeLimit { get; set; }

        /// <summary>
        /// Indicates whether all correct options must be selected exactly for a multiple-choice answer.
        /// </summary>
        public bool IsStrictMultipleChoice { get; set; }

        /// <summary>
        /// Identifier of the quiz that owns this question.
        /// </summary>
        public long QuizId { get; set; }

        /// <summary>
        /// Quiz that owns this question.
        /// </summary>
        public Quiz Quiz { get; set; } = null!;

        /// <summary>
        /// Identifier of the optional media asset attached to the question.
        /// </summary>
        public long? MediaAssetId { get; set; }

        /// <summary>
        /// Optional media asset attached to the question.
        /// </summary>
        public MediaAsset? Media { get; set; }

        /// <summary>
        /// Answer options or free-text expected answers for this question.
        /// </summary>
        public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();

        /// <summary>
        /// Penalty points subtracted for wrong answers when penalties are enabled.
        /// </summary>
        public int PenaltyPoints => HasPenalty ? (int)Math.Floor(Difficulty / 2.0) : 0;
    }
}
