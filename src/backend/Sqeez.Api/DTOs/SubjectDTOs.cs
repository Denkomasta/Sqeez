using Sqeez.Api.Constants;
using Sqeez.Api.Validation;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Subject summary returned by subject endpoints.
    /// </summary>
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
        int EnrollmentCount,    // Enrollements are added in Enrollment service
        int QuizCount           // Quizzes are added in Quiz service
        );

    /// <summary>
    /// Subject search filters, including teacher, class, student, and UTC start-date filters.
    /// </summary>
    public class SubjectFilterDto : PagedFilterDto
    {
        [StringLength(ValidationConstants.SearchTermMaxLength)]
        public string? SearchTerm { get; set; }

        [StringLength(ValidationConstants.SubjectCodeMaxLength)]
        public string? Code { get; set; }

        public long? TeacherId { get; set; }
        public long? SchoolClassId { get; set; }
        public long? StudentId { get; set; }

        public bool? IsActive { get; set; }
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
        public long? TeacherId { get; init; }
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
        public long? TeacherId { get; init; }     // Pass 0 to remove the teacher
        public long? SchoolClassId { get; init; } // Pass 0 to remove the class
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
