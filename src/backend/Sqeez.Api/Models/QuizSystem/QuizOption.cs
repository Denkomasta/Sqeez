using Sqeez.Api.Models.Media;

namespace Sqeez.Api.Models.QuizSystem
{
    /// <summary>
    /// Answer option for a quiz question, or the suggested correct answer for a free-text question.
    /// </summary>
    public class QuizOption
    {
        /// <summary>
        /// Primary identifier of the option.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Option text. For free-text questions this stores the suggested correct answer.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Indicates whether this option represents a free-text expected answer.
        /// </summary>
        public bool IsFreeText { get; set; }

        /// <summary>
        /// Indicates whether this option is considered correct for scoring.
        /// </summary>
        public bool IsCorrect { get; set; }

        /// <summary>
        /// Identifier of the question that owns the option.
        /// </summary>
        public long QuizQuestionId { get; set; }

        /// <summary>
        /// Question that owns the option.
        /// </summary>
        public QuizQuestion QuizQuestion { get; set; } = null!;

        /// <summary>
        /// Identifier of the optional media asset attached to the option.
        /// </summary>
        public long? MediaAssetId { get; set; }

        /// <summary>
        /// Optional media asset attached to the option.
        /// </summary>
        public MediaAsset? Media { get; set; }

        /// <summary>
        /// Responses that selected this option.
        /// </summary>
        public ICollection<QuizQuestionResponse> Responses { get; set; } = new List<QuizQuestionResponse>();
    }
}
