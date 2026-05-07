using Sqeez.Api.Constants;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// School class summary returned by class endpoints.
    /// </summary>
    public record SchoolClassDto(
        long Id,
        string Name,
        string AcademicYear,
        string Section,
        long? TeacherId,
        string? TeacherName,
        int StudentCount,
        int SubjectCount);

    /// <summary>
    /// School class search filters.
    /// </summary>
    public class SchoolClassFilterDto : PagedFilterDto
    {
        [StringLength(ValidationConstants.SearchTermMaxLength)]
        public string? SearchTerm { get; set; }  // Search against Name or Section

        [StringLength(ValidationConstants.AcademicYearMaxLength)]
        public string? AcademicYear { get; set; }
        public long? TeacherId { get; set; }
    }

    /// <summary>
    /// Request for creating a school class with an optional managed teacher.
    /// </summary>
    public record CreateSchoolClassDto
    {
        public CreateSchoolClassDto() { }

        public CreateSchoolClassDto(string Name, string AcademicYear, string Section, long? TeacherId)
        {
            this.Name = Name;
            this.AcademicYear = AcademicYear;
            this.Section = Section;
            this.TeacherId = TeacherId;
        }

        [StringLength(ValidationConstants.TitleMaxLength)]
        public string Name { get; init; } = string.Empty;

        [StringLength(ValidationConstants.AcademicYearMaxLength)]
        public string AcademicYear { get; init; } = string.Empty;

        [StringLength(ValidationConstants.SectionMaxLength)]
        public string Section { get; init; } = string.Empty;
        public long? TeacherId { get; init; }
    }

    /// <summary>
    /// Request for partially updating a school class and managed teacher.
    /// </summary>
    public record PatchSchoolClassDto
    {
        public PatchSchoolClassDto() { }

        public PatchSchoolClassDto(string? Name = null, string? AcademicYear = null, string? Section = null, long? TeacherId = null)
        {
            this.Name = Name;
            this.AcademicYear = AcademicYear;
            this.Section = Section;
            this.TeacherId = TeacherId;
        }

        [StringLength(ValidationConstants.TitleMaxLength)]
        public string? Name { get; init; }

        [StringLength(ValidationConstants.AcademicYearMaxLength)]
        public string? AcademicYear { get; init; }

        [StringLength(ValidationConstants.SectionMaxLength)]
        public string? Section { get; init; }
        public long? TeacherId { get; init; }
    }

    /// <summary>
    /// Request containing student ids to assign to a class or subject.
    /// </summary>
    public record AssignStudentsDto
    {
        public AssignStudentsDto() { }

        public AssignStudentsDto(List<long> StudentIds)
        {
            this.StudentIds = StudentIds;
        }

        [MaxLength(ValidationConstants.MaxBulkIds)]
        public List<long> StudentIds { get; init; } = new();
    }

    /// <summary>
    /// Request containing student ids to remove from a class or subject.
    /// </summary>
    public record RemoveStudentsDto
    {
        public RemoveStudentsDto() { }

        public RemoveStudentsDto(List<long> StudentIds)
        {
            this.StudentIds = StudentIds;
        }

        [MaxLength(ValidationConstants.MaxBulkIds)]
        public List<long> StudentIds { get; init; } = new();
    }

    /// <summary>
    /// Detailed school class view with teacher, students, and subjects.
    /// </summary>
    public record SchoolClassDetailDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string AcademicYear { get; init; } = string.Empty;
        public string Section { get; init; } = string.Empty;

        public TeacherBasicDto? Teacher { get; init; }
        public List<ClassmateDto> Students { get; init; } = new();
        public List<SubjectBasicDto> Subjects { get; init; } = new();
    }

    /// <summary>
    /// Compact school class view embedded in user profiles.
    /// </summary>
    public record SchoolClassBasicDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string AcademicYear { get; init; } = string.Empty;
        public string Section { get; init; } = string.Empty;
    }
}
