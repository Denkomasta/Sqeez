using CsvHelper.Configuration;
using Sqeez.Api.Constants;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.Models.Import
{
    /// <summary>
    /// One parsed row from the master CSV import file.
    /// </summary>
    public class MasterImportDto
    {
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string ClassName { get; set; } = string.Empty;

        [StringLength(ValidationConstants.AcademicYearMaxLength)]
        public string AcademicYear { get; set; } = string.Empty;

        [StringLength(ValidationConstants.TitleMaxLength)]
        public string SubjectName { get; set; } = string.Empty;

        [StringLength(ValidationConstants.SubjectCodeMaxLength)]
        public string SubjectCode { get; set; } = string.Empty;
        [StringLength(ValidationConstants.NameMaxLength)]
        [RegularExpression(ValidationConstants.PersonNameRegex, ErrorMessage = "First name can only contain letters, spaces, and dashes.")]
        public string StudentFirstName { get; set; } = string.Empty;
        [StringLength(ValidationConstants.NameMaxLength)]
        [RegularExpression(ValidationConstants.PersonNameRegex, ErrorMessage = "Last name can only contain letters, spaces, and dashes.")]
        public string StudentLastName { get; set; } = string.Empty;
        [StringLength(ValidationConstants.EmailMaxLength)]
        [RegularExpression(ValidationConstants.EmailRegex, ErrorMessage = "Invalid email format.")]
        public string StudentEmail { get; set; } = string.Empty;
        [StringLength(ValidationConstants.PasswordMaxLength, MinimumLength = 8)]
        [RegularExpression(ValidationConstants.PasswordComplexityRegex, ErrorMessage = "Password does not meet complexity requirements.")]
        public string StudentPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// CsvHelper mapping between master CSV column names and import DTO fields.
    /// </summary>
    public sealed class MasterImportMap : ClassMap<MasterImportDto>
    {
        public MasterImportMap()
        {
            Map(m => m.ClassName).Name("Class Name");
            Map(m => m.AcademicYear).Name("Academic Year").Optional();

            Map(m => m.SubjectName).Name("Subject Name").Optional();
            Map(m => m.SubjectCode).Name("Subject Code").Optional();

            Map(m => m.StudentFirstName).Name("First Name");
            Map(m => m.StudentLastName).Name("Last Name");
            Map(m => m.StudentEmail).Name("Email");
            Map(m => m.StudentPassword).Name("Password").Optional();
        }
    }

    /// <summary>
    /// Import summary with number of imported records and row-level errors.
    /// </summary>
    public class ImportResultDto
    {
        public int RecordsImported { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool HasRowErrors => Errors.Any();
    }

    /// <summary>
    /// Bulk operation result that separates created records, existing records, and skipped-row messages.
    /// </summary>
    public class BulkOperationResult<T>
    {
        public List<T> Created { get; set; } = new();
        public List<T> Existing { get; set; } = new();
        public List<string> SkippedMessages { get; set; } = new();
    }
}
