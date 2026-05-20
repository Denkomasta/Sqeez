using Sqeez.Api.Constants;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Enrollment summary joining a student with a subject and optional mark.
    /// </summary>
    /// <param name="Id">Enrollment identifier.</param>
    /// <param name="Mark">Optional teacher-assigned mark.</param>
    /// <param name="EnrolledAt">UTC timestamp when the enrollment was created.</param>
    /// <param name="ArchivedAt">UTC timestamp when the enrollment was archived; null means active.</param>
    /// <param name="StudentId">Enrolled student id.</param>
    /// <param name="StudentUserName">Enrolled student's username.</param>
    /// <param name="SubjectId">Subject id.</param>
    /// <param name="SubjectName">Subject name.</param>
    /// <param name="SubjectCode">Subject code.</param>
    /// <param name="QuizAttemptsCount">Number of quiz attempts connected to this enrollment.</param>
    public record EnrollmentDto(
        long Id,
        int? Mark,
        DateTime EnrolledAt,
        DateTime? ArchivedAt,
        long StudentId,
        string StudentUserName,
        long SubjectId,
        string SubjectName,
        string SubjectCode,
        int QuizAttemptsCount
        );

    /// <summary>
    /// Enrollment search filters.
    /// </summary>
    public class EnrollmentFilterDto : PagedFilterDto
    {
        /// <summary>
        /// Filters enrollments by exact mark.
        /// </summary>
        [Range(ValidationConstants.MinMark, ValidationConstants.MaxMark)]
        public int? Mark { get; set; }

        /// <summary>
        /// Filters enrollments by student id. Student callers are forced to their own id by the service.
        /// </summary>
        public long? StudentId { get; set; }

        /// <summary>
        /// Filters enrollments by subject id. Teacher callers must use an owned subject unless viewing their own enrollment.
        /// </summary>
        public long? SubjectId { get; set; }

        /// <summary>
        /// Filters active or archived enrollments. Null leaves archive state unfiltered.
        /// </summary>
        public bool? IsActive { get; set; }
        public bool IsDescending { get; set; } = false;
    }

    /// <summary>
    /// Request for updating or clearing an enrollment mark.
    /// </summary>
    public record PatchEnrollmentDto
    {
        public PatchEnrollmentDto() { }

        public PatchEnrollmentDto(int? Mark = null, bool? RemoveMark = null)
        {
            this.Mark = Mark;
            this.RemoveMark = RemoveMark;
        }

        [Range(ValidationConstants.MinMark, ValidationConstants.MaxMark)]
        public int? Mark { get; init; }

        /// <summary>
        /// When true, clears the current mark. This distinguishes mark removal from an omitted Mark value.
        /// </summary>
        public bool? RemoveMark { get; init; }
    }

    /// <summary>
    /// Compact enrollment view embedded in detailed user profiles.
    /// </summary>
    public record EnrollmentBasicDto
    {
        public long Id { get; init; }
        public long SubjectId { get; init; }
        public string SubjectName { get; init; } = string.Empty;
        public int? Mark { get; init; }
        public DateTime EnrolledAt { get; init; }
        public DateTime? ArchivedAt { get; init; }
    }

    /// <summary>
    /// Result of a bulk enrollment operation, split by newly enrolled and already enrolled student ids.
    /// </summary>
    public class BulkEnrollmentResultDto
    {
        /// <summary>
        /// Student ids that were newly enrolled by the request.
        /// </summary>
        public List<long> NewlyEnrolledIds { get; set; } = new();

        /// <summary>
        /// Student ids that already had an enrollment for the subject.
        /// </summary>
        public List<long> AlreadyEnrolledIds { get; set; } = new();
    }
}
