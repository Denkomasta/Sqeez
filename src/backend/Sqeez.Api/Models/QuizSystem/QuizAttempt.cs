using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;

namespace Sqeez.Api.Models.QuizSystem
{
    /// <summary>
    /// Single attempt by an enrolled student to complete a quiz.
    /// </summary>
    public class QuizAttempt
    {
        /// <summary>
        /// Primary identifier of the attempt.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// UTC timestamp when the attempt started.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// UTC timestamp when the attempt finished.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Current lifecycle status of the attempt.
        /// </summary>
        public AttemptStatus Status { get; set; }

        /// <summary>
        /// Total score awarded for all submitted responses.
        /// </summary>
        public int TotalScore { get; set; }

        /// <summary>
        /// Optional mark derived from the score.
        /// </summary>
        public int? Mark { get; set; }

        /// <summary>
        /// Identifier of the quiz being attempted.
        /// </summary>
        public long QuizId { get; set; }

        /// <summary>
        /// Quiz being attempted.
        /// </summary>
        public Quiz Quiz { get; set; } = null!;

        /// <summary>
        /// Identifier of the enrollment through which the student takes the quiz.
        /// </summary>
        public long EnrollmentId { get; set; }

        /// <summary>
        /// Enrollment through which the student takes the quiz.
        /// </summary>
        public Enrollment Enrollment { get; set; } = null!;

        /// <summary>
        /// Responses submitted during this attempt.
        /// </summary>
        public ICollection<QuizQuestionResponse> Responses { get; set; } = new List<QuizQuestionResponse>();
    }
}
