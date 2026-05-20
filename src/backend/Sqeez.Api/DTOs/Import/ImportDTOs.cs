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
        /// <summary>
        /// Name of the class that should exist or be created for the imported student.
        /// </summary>
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// Academic year assigned to the class. Optional in the CSV file.
        /// </summary>
        [StringLength(ValidationConstants.AcademicYearMaxLength)]
        public string AcademicYear { get; set; } = string.Empty;

        /// <summary>
        /// Subject name that should exist or be created and linked with the class. Optional in the CSV file.
        /// </summary>
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string SubjectName { get; set; } = string.Empty;

        /// <summary>
        /// Subject code used to identify or create the imported subject. Optional in the CSV file.
        /// </summary>
        [StringLength(ValidationConstants.SubjectCodeMaxLength)]
        public string SubjectCode { get; set; } = string.Empty;

        /// <summary>
        /// First name of the imported student.
        /// </summary>
        [StringLength(ValidationConstants.NameMaxLength)]
        [RegularExpression(ValidationConstants.PersonNameRegex, ErrorMessage = "First name can only contain letters, spaces, and dashes.")]
        public string StudentFirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name of the imported student.
        /// </summary>
        [StringLength(ValidationConstants.NameMaxLength)]
        [RegularExpression(ValidationConstants.PersonNameRegex, ErrorMessage = "Last name can only contain letters, spaces, and dashes.")]
        public string StudentLastName { get; set; } = string.Empty;

        /// <summary>
        /// Unique email address for the imported student.
        /// </summary>
        [StringLength(ValidationConstants.EmailMaxLength)]
        [RegularExpression(ValidationConstants.EmailRegex, ErrorMessage = "Invalid email format.")]
        public string StudentEmail { get; set; } = string.Empty;

        /// <summary>
        /// Optional initial password. When omitted, the import service supplies its configured default behavior.
        /// </summary>
        [StringLength(ValidationConstants.PasswordMaxLength, MinimumLength = 8)]
        [RegularExpression(ValidationConstants.PasswordComplexityRegex, ErrorMessage = "Password does not meet complexity requirements.")]
        public string StudentPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// CsvHelper mapping between master CSV column names and import DTO fields.
    /// </summary>
    public sealed class MasterImportMap : ClassMap<MasterImportDto>
    {
        /// <summary>
        /// Creates the CsvHelper mapping for the master import CSV headers.
        /// </summary>
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
        /// <summary>
        /// Title of the quiz. Rows with the same title belong to the same imported quiz within the file.
        /// </summary>
        [Required]
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string QuizTitle { get; set; } = string.Empty;

        /// <summary>
        /// Optional quiz description or instructions.
        /// </summary>
        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string QuizDescription { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of retry attempts allowed after the first attempt.
        /// </summary>
        [Range(0, ValidationConstants.MaxQuizRetries)]
        public int MaxRetries { get; set; }

        /// <summary>
        /// Optional publish date text parsed by the import service.
        /// </summary>
        public string PublishDate { get; set; } = string.Empty;

        /// <summary>
        /// Optional closing date text parsed by the import service.
        /// </summary>
        public string ClosingDate { get; set; } = string.Empty;

        /// <summary>
        /// One-based order of the question within the quiz.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int QuestionOrder { get; set; }

        /// <summary>
        /// Question prompt shown to the student.
        /// </summary>
        [Required]
        [StringLength(ValidationConstants.TitleMaxLength)]
        public string QuestionTitle { get; set; } = string.Empty;

        /// <summary>
        /// Point value used for scoring the question.
        /// </summary>
        [Range(0, ValidationConstants.MaxQuestionDifficulty)]
        public int Difficulty { get; set; }

        /// <summary>
        /// Time limit for the question in seconds.
        /// </summary>
        [Range(0, ValidationConstants.MaxQuestionTimeLimitSeconds)]
        public int TimeLimit { get; set; }

        /// <summary>
        /// Indicates whether wrong answers should apply penalty points.
        /// </summary>
        public bool HasPenalty { get; set; }

        /// <summary>
        /// Indicates whether multiple-choice scoring requires an exact match of all correct options.
        /// </summary>
        public bool IsStrictMultipleChoice { get; set; }

        /// <summary>
        /// One-based order of the option within the question.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int OptionOrder { get; set; }

        /// <summary>
        /// Option text. For free-text questions this is the suggested correct answer.
        /// </summary>
        [StringLength(ValidationConstants.LongTextMaxLength)]
        public string OptionText { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the option is correct.
        /// </summary>
        public bool IsCorrect { get; set; }

        /// <summary>
        /// Indicates whether the row represents a free-text answer rather than a selectable option.
        /// </summary>
        public bool IsFreeText { get; set; }
    }

    /// <summary>
    /// CsvHelper mapping between quiz CSV column names and import DTO fields.
    /// </summary>
    public sealed class QuizImportMap : ClassMap<QuizImportDto>
    {
        /// <summary>
        /// Creates the CsvHelper mapping for quiz import/export CSV headers.
        /// </summary>
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
        /// <summary>
        /// Number of records successfully imported.
        /// </summary>
        public int RecordsImported { get; set; }

        /// <summary>
        /// Row-level or file-level import errors.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Indicates whether any row errors were collected during import.
        /// </summary>
        public bool HasRowErrors => Errors.Any();
    }

    /// <summary>
    /// Bulk operation result that separates created records, existing records, and skipped-row messages.
    /// </summary>
    public class BulkOperationResult<T>
    {
        /// <summary>
        /// Records created by the operation.
        /// </summary>
        public List<T> Created { get; set; } = new();

        /// <summary>
        /// Records that already existed and were reused or skipped.
        /// </summary>
        public List<T> Existing { get; set; } = new();

        /// <summary>
        /// Human-readable messages for rows that could not be processed.
        /// </summary>
        public List<string> SkippedMessages { get; set; } = new();
    }
}
