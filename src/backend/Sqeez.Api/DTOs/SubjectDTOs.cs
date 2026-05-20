using Sqeez.Api.Constants;
using Sqeez.Api.Validation;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Subject summary returned by subject endpoints.
    /// </summary>
    /// <param name="Id">Subject identifier.</param>
    /// <param name="Name">Human-readable subject name.</param>
    /// <param name="Code">Unique subject code used for imports and search.</param>
    /// <param name="Description">Optional subject description.</param>
    /// <param name="StartDate">Subject start date stored as UTC.</param>
    /// <param name="EndDate">Optional subject end date stored as UTC.</param>
    /// <param name="TeacherId">Assigned teacher id, or null when the subject has no teacher.</param>
    /// <param name="TeacherName">Assigned teacher username, or null when no teacher is assigned.</param>
    /// <param name="SchoolClassId">Assigned school class id, or null when no class is assigned.</param>
    /// <param name="SchoolClassName">Assigned school class name, or null when no class is assigned.</param>
    /// <param name="EnrollmentCount">Number of enrollments attached to the subject.</param>
    /// <param name="QuizCount">Number of quizzes attached to the subject.</param>
    public record SubjectDto(
        long Id,
        string Name,
        string Code,
        string? Description,
        DateTime StartDate,
        DateTime? EndDate,
        long? TeacherId,
        string? TeacherName,
        long? SchoolClassId,
        string? SchoolClassName,
        int EnrollmentCount,
        int QuizCount
        );

    /// <summary>
    /// Subject search filters, including teacher, class, student, and UTC start-date filters.
    /// </summary>
    public class SubjectFilterDto : PagedFilterDto
    {
        /// <summary>
        /// Searches subject name and description.
        /// </summary>
        [StringLength(ValidationConstants.SearchTermMaxLength)]
        public string? SearchTerm { get; set; }

        [StringLength(ValidationConstants.SubjectCodeMaxLength)]
        public string? Code { get; set; }

        public long? TeacherId { get; set; }
        public long? SchoolClassId { get; set; }

        /// <summary>
        /// Filters to subjects available to or already connected with a student, depending on service context.
        /// </summary>
        public long? StudentId { get; set; }

        /// <summary>
        /// Filters subjects by current active state. Null leaves activity unfiltered.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Filters subjects whose start date is after the supplied UTC value.
        /// </summary>
        [UtcDateTime]
        public DateTime? StartingAfter { get; set; }

        public bool IsDescending { get; set; } = false;
    }

    /// <summary>
    /// Request for creating a subject with optional UTC dates and assignments.
    /// </summary>
    public record CreateSubjectDto
    {
        public CreateSubjectDto() { }

        public CreateSubjectDto(string Name, string Code, string? Description = null, DateTime? StartDate = null, DateTime? EndDate = null, long? TeacherId = null, long? SchoolClassId = null)
        {
            this.Name = Name;
            this.Code = Code;
            this.Description = Description;
            this.StartDate = StartDate;
            this.EndDate = EndDate;
            this.TeacherId = TeacherId;
            this.SchoolClassId = SchoolClassId;
        }

        [StringLength(ValidationConstants.TitleMaxLength)]
        public string Name { get; init; } = string.Empty;

        [StringLength(ValidationConstants.SubjectCodeMaxLength)]
        public string Code { get; init; } = string.Empty;

        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string? Description { get; init; }
        [UtcDateTime]
        public DateTime? StartDate { get; init; }
        [UtcDateTime]
        public DateTime? EndDate { get; init; }

        /// <summary>
        /// Optional teacher assignment.
        /// </summary>
        public long? TeacherId { get; init; }

        /// <summary>
        /// Optional school class assignment.
        /// </summary>
        public long? SchoolClassId { get; init; }
    }

    /// <summary>
    /// Request for partially updating a subject and its optional assignments.
    /// </summary>
    public record PatchSubjectDto
    {
        public PatchSubjectDto() { }

        public PatchSubjectDto(string? Name = null, string? Code = null, string? Description = null, DateTime? StartDate = null, DateTime? EndDate = null, long? TeacherId = null, long? SchoolClassId = null)
        {
            this.Name = Name;
            this.Code = Code;
            this.Description = Description;
            this.StartDate = StartDate;
            this.EndDate = EndDate;
            this.TeacherId = TeacherId;
            this.SchoolClassId = SchoolClassId;
        }

        [StringLength(ValidationConstants.TitleMaxLength)]
        public string? Name { get; init; }

        [StringLength(ValidationConstants.SubjectCodeMaxLength)]
        public string? Code { get; init; }

        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string? Description { get; init; }
        [UtcDateTime]
        public DateTime? StartDate { get; init; }
        [UtcDateTime]
        public DateTime? EndDate { get; init; }

        /// <summary>
        /// Optional teacher assignment. A value of 0 removes the teacher.
        /// </summary>
        public long? TeacherId { get; init; }

        /// <summary>
        /// Optional school class assignment. A value of 0 removes the class.
        /// </summary>
        public long? SchoolClassId { get; init; }
    }

    /// <summary>
    /// Compact subject view embedded in class and user detail responses.
    /// </summary>
    public record SubjectBasicDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }
}
