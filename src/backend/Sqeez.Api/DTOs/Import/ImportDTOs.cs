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
    /// One parsed row from a quiz CSV import/export file. Each row represents one answer option.
    /// </summary>
    public class QuizImportDto
    {
        [Required]
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string QuizTitle { get; set; } = string.Empty;

        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string QuizDescription { get; set; } = string.Empty;

        [Range(0, ValidationConstants.MaxQuizRetries)]
        public int MaxRetries { get; set; }

        public string PublishDate { get; set; } = string.Empty;

        public string ClosingDate { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int QuestionOrder { get; set; }

        [Required]
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string QuestionTitle { get; set; } = string.Empty;

        [Range(0, ValidationConstants.MaxQuestionDifficulty)]
        public int Difficulty { get; set; }

        [Range(0, ValidationConstants.MaxQuestionTimeLimitSeconds)]
        public int TimeLimit { get; set; }

        public bool HasPenalty { get; set; }

        public bool IsStrictMultipleChoice { get; set; }

        [Range(1, int.MaxValue)]
        public int OptionOrder { get; set; }

        [StringLength(ValidationConstants.LongTextMaxLength)]
        public string OptionText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public bool IsFreeText { get; set; }
    }

    /// <summary>
    /// CsvHelper mapping between quiz CSV column names and import DTO fields.
    /// </summary>
    public sealed class QuizImportMap : ClassMap<QuizImportDto>
    {
        public QuizImportMap()
        {
            Map(m => m.QuizTitle).Name("Quiz Title");
            Map(m => m.QuizDescription).Name("Quiz Description").Optional();
            Map(m => m.MaxRetries).Name("Max Retries").Optional();
            Map(m => m.PublishDate).Name("Publish Date").Optional();
            Map(m => m.ClosingDate).Name("Closing Date").Optional();
            Map(m => m.QuestionOrder).Name("Question Order");
            Map(m => m.QuestionTitle).Name("Question Title");
            Map(m => m.Difficulty).Name("Difficulty");
            Map(m => m.TimeLimit).Name("Time Limit");
            Map(m => m.HasPenalty).Name("Has Penalty").Optional();
            Map(m => m.IsStrictMultipleChoice).Name("Is Strict Multiple Choice").Optional();
            Map(m => m.OptionOrder).Name("Option Order");
            Map(m => m.OptionText).Name("Option Text").Optional();
            Map(m => m.IsCorrect).Name("Is Correct");
            Map(m => m.IsFreeText).Name("Is Free Text").Optional();
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
