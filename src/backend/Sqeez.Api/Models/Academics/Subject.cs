using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.Users;

namespace Sqeez.Api.Models.Academics
{
    /// <summary>
    /// Teaching subject with optional class assignment, teacher ownership, quizzes, and enrollments.
    /// </summary>
    public class Subject
    {
        /// <summary>
        /// Primary identifier of the subject.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Human-readable subject name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Short subject code used for identification in lists and imports.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Optional subject description visible to users with subject access.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// UTC date and time when the subject becomes active.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Optional UTC date and time when the subject ends.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Identifier of the teacher responsible for the subject, when assigned.
        /// </summary>
        public long? TeacherId { get; set; }

        /// <summary>
        /// Teacher responsible for the subject.
        /// </summary>
        public Teacher? Teacher { get; set; }

        /// <summary>
        /// Identifier of the class this subject belongs to, when assigned.
        /// </summary>
        public long? SchoolClassId { get; set; }

        /// <summary>
        /// Class this subject belongs to.
        /// </summary>
        public SchoolClass? SchoolClass { get; set; }

        /// <summary>
        /// Enrollments of students in this subject.
        /// </summary>
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

        /// <summary>
        /// Quizzes published under this subject.
        /// </summary>
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

        /// <summary>
        /// Indicates whether the configured end date has already passed.
        /// </summary>
        public bool HasEnded =>
            EndDate.HasValue && EndDate.Value < DateTime.UtcNow;

        /// <summary>
        /// Indicates whether the current UTC time falls within the subject availability window.
        /// </summary>
        public bool IsActive =>
            StartDate <= DateTime.UtcNow &&
            (!EndDate.HasValue || EndDate.Value >= DateTime.UtcNow);
    }
}
