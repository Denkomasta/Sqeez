using Sqeez.Api.Models.Users;
using Sqeez.Api.Models.QuizSystem;

namespace Sqeez.Api.Models.Academics
{
    /// <summary>
    /// Connects a student account to a subject and stores the final subject mark.
    /// </summary>
    public class Enrollment
    {
        /// <summary>
        /// Primary identifier of the enrollment.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Optional final mark assigned by a teacher or administrator.
        /// </summary>
        public int? Mark { get; set; }

        /// <summary>
        /// UTC timestamp when the student enrolled in the subject.
        /// </summary>
        public DateTime EnrolledAt { get; set; }

        /// <summary>
        /// UTC timestamp when the enrollment was archived, or null for active enrollments.
        /// </summary>
        public DateTime? ArchivedAt { get; set; }

        /// <summary>
        /// Identifier of the enrolled student.
        /// </summary>
        public long StudentId { get; set; }

        /// <summary>
        /// Student account that owns the enrollment.
        /// </summary>
        public Student Student { get; set; } = null!;

        /// <summary>
        /// Identifier of the subject the student is enrolled in.
        /// </summary>
        public long SubjectId { get; set; }

        /// <summary>
        /// Subject attached to this enrollment.
        /// </summary>
        public Subject Subject { get; set; } = null!;

        /// <summary>
        /// Quiz attempts made through this enrollment.
        /// </summary>
        public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    }
}
