using CsvHelper;
using Sqeez.Api.Data;
using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Constants;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Import;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using BC = BCrypt.Net.BCrypt;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Imports master CSV files and creates related classes, subjects, students, and enrollments.
    /// </summary>
    public class CsvImportService : BaseService<CsvImportService>, ICsvImportService
    {
        private readonly ISchoolClassService _classService;
        private readonly ISubjectService _subjectService;
        private readonly IUserService _userService;

        public CsvImportService(
            SqeezDbContext context,
            ILogger<CsvImportService> logger,
            ISchoolClassService classService,
            ISubjectService subjectService,
            IUserService userService) : base(context, logger)
        {
            _classService = classService;
            _subjectService = subjectService;
            _userService = userService;
        }

        public async Task<ServiceResult<ImportResultDto>> ImportMasterFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ServiceResult<ImportResultDto>.Failure("No file uploaded.", ServiceError.BadRequest);

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return ServiceResult<ImportResultDto>.Failure("Only CSV files are allowed.", ServiceError.BadRequest);

            var result = new ImportResultDto();

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<MasterImportMap>();

                var allRecords = csv.GetRecords<MasterImportDto>().ToList();
                var validRecords = new List<MasterImportDto>();

                for (int i = 0; i < allRecords.Count; i++)
                {
                    var record = allRecords[i];
                    int row = i + 2;    // +2 because of header and 0-based index

                    var validationResults = new List<ValidationResult>();
                    var validationContext = new ValidationContext(record);
                    if (!Validator.TryValidateObject(record, validationContext, validationResults, true))
                    {
                        foreach (var valError in validationResults)
                        {
                            result.Errors.Add($"Row {row}: {valError.ErrorMessage}");
                        }
                        continue;
                    }

                    validRecords.Add(record);
                }

                if (!validRecords.Any())
                {
                    return ServiceResult<ImportResultDto>.Ok(result);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                var distinctClassNames = validRecords
                    .Where(r => !string.IsNullOrWhiteSpace(r.ClassName))
                    .Select(r => r.ClassName.Trim())
                    .Distinct()
                    .ToList();

                var classResult = await _classService.EnsureClassesExistAsync(distinctClassNames);
                if (!classResult.Success || classResult.Data == null)
                    return ServiceResult<ImportResultDto>.Failure(classResult.ErrorMessage ?? "Failed to process classes.", classResult.ErrorCode);

                var allProcessedClasses = classResult.Data.Created.Concat(classResult.Data.Existing);
                var classDictionary = allProcessedClasses.ToDictionary(c => c.Name.ToLower(), c => c.Id);

                result.RecordsImported += classResult.Data.Created.Count;

                var newSubjects = validRecords
                    .Where(r => !string.IsNullOrWhiteSpace(r.SubjectCode))
                    .GroupBy(r => r.SubjectCode.Trim().ToLower())
                    .Select(g => new Subject
                    {
                        Name = g.First().SubjectName.Trim(),
                        Code = g.First().SubjectCode.Trim(),
                        StartDate = DateTime.UtcNow,
                        SchoolClassId = classDictionary.GetValueOrDefault(g.First().ClassName.Trim().ToLower())
                    })
                    .ToList();

                if (newSubjects.Any())
                {
                    var subjectResult = await _subjectService.CreateSubjectsBulkAsync(newSubjects);
                    if (!subjectResult.Success || subjectResult.Data == null)
                    {
                        return ServiceResult<ImportResultDto>.Failure(subjectResult.ErrorMessage ?? "Failed to process subjects.", subjectResult.ErrorCode);
                    }

                    result.RecordsImported += subjectResult.Data.Created.Count;
                    result.Errors.AddRange(subjectResult.Data.SkippedMessages);
                }

                var newStudents = new List<Student>();

                foreach (var record in validRecords)
                {
                    string rawPassword = string.IsNullOrWhiteSpace(record.StudentPassword)
                        ? "Heslo1122*" // Default password if column is empty
                        : record.StudentPassword.Trim();

                    string hashedPassword = BC.HashPassword(rawPassword, BC.GenerateSalt(12));
                    string email = record.StudentEmail.Trim().ToLower();

                    newStudents.Add(new Student
                    {
                        FirstName = record.StudentFirstName.Trim(),
                        LastName = record.StudentLastName.Trim(),
                        Email = email,
                        Username = email.Split('@')[0],
                        PasswordHash = hashedPassword,
                        Role = UserRole.Student,
                        LastSeen = DateTime.UtcNow,
                        SchoolClassId = classDictionary.GetValueOrDefault(record.ClassName.Trim().ToLower()),
                        IsEmailVerified = true,
                        EmailVerificationToken = null,
                        EmailVerificationTokenExpiry = null,
                    });
                }

                if (newStudents.Any())
                {
                    var studentResult = await _userService.CreateStudentsBulkAsync(newStudents);

                    if (!studentResult.Success || studentResult.Data == null)
                    {
                        return ServiceResult<ImportResultDto>.Failure(studentResult.ErrorMessage ?? "Failed to process students.", studentResult.ErrorCode);
                    }

                    result.RecordsImported += studentResult.Data.Created.Count;
                    result.Errors.AddRange(studentResult.Data.SkippedMessages);
                }

                await transaction.CommitAsync();

                return ServiceResult<ImportResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical failure during Master CSV import.");
                return ServiceResult<ImportResultDto>.Failure("An unexpected error occurred during processing.", ServiceError.BadRequest);
            }
        }

        public async Task<ServiceResult<ImportResultDto>> ImportQuizFileAsync(long subjectId, IFormFile file, long currentUserId)
        {
            if (file == null || file.Length == 0)
                return ServiceResult<ImportResultDto>.Failure("No file uploaded.", ServiceError.BadRequest);

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return ServiceResult<ImportResultDto>.Failure("Only CSV files are allowed.", ServiceError.BadRequest);

            var subject = await _context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == subjectId);
            if (subject == null)
                return ServiceResult<ImportResultDto>.Failure("Subject not found.", ServiceError.NotFound);

            if (subject.TeacherId != currentUserId)
                return ServiceResult<ImportResultDto>.Failure("Only the subject teacher can import quizzes.", ServiceError.Forbidden);

            if (subject.HasEnded)
                return ServiceResult<ImportResultDto>.Failure("Cannot import quizzes into a closed subject.", ServiceError.Forbidden);

            var result = new ImportResultDto();

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<QuizImportMap>();

                var allRecords = csv.GetRecords<QuizImportDto>().ToList();
                var validRecords = new List<QuizImportDto>();

                for (int i = 0; i < allRecords.Count; i++)
                {
                    var record = allRecords[i];
                    int row = i + 2;

                    TrimQuizImportRecord(record);

                    var validationResults = new List<ValidationResult>();
                    var validationContext = new ValidationContext(record);
                    if (!Validator.TryValidateObject(record, validationContext, validationResults, true))
                    {
                        foreach (var valError in validationResults)
                        {
                            result.Errors.Add($"Row {row}: {valError.ErrorMessage}");
                        }
                        continue;
                    }

                    validRecords.Add(record);
                }

                if (!validRecords.Any())
                    return ServiceResult<ImportResultDto>.Ok(result);

                var existingQuizTitles = await _context.Quizzes
                    .AsNoTracking()
                    .Where(q => q.SubjectId == subjectId)
                    .Select(q => q.Title.ToLower())
                    .ToHashSetAsync();

                var quizzesToCreate = new List<Quiz>();

                foreach (var quizGroup in validRecords.GroupBy(r => r.QuizTitle, StringComparer.OrdinalIgnoreCase))
                {
                    var quizRows = quizGroup.ToList();
                    var firstQuizRow = quizRows.First();
                    var quizLabel = firstQuizRow.QuizTitle;
                    var groupErrors = new List<string>();

                    if (existingQuizTitles.Contains(quizLabel.ToLower()))
                    {
                        result.Errors.Add($"Quiz '{quizLabel}' skipped: a quiz with this title already exists in the subject.");
                        continue;
                    }

                    if (quizRows.Any(r =>
                        r.QuizDescription != firstQuizRow.QuizDescription ||
                        r.MaxRetries != firstQuizRow.MaxRetries ||
                        r.PublishDate != firstQuizRow.PublishDate ||
                        r.ClosingDate != firstQuizRow.ClosingDate))
                    {
                        groupErrors.Add($"Quiz '{quizLabel}' has inconsistent quiz metadata across rows.");
                    }

                    if (!TryParseUtcDate(firstQuizRow.PublishDate, "Publish Date", out var publishDate, out var publishError))
                        groupErrors.Add($"Quiz '{quizLabel}': {publishError}");

                    if (!TryParseUtcDate(firstQuizRow.ClosingDate, "Closing Date", out var closingDate, out var closingError))
                        groupErrors.Add($"Quiz '{quizLabel}': {closingError}");

                    if (closingDate.HasValue && subject.EndDate.HasValue && closingDate.Value > subject.EndDate.Value)
                    {
                        groupErrors.Add($"Quiz '{quizLabel}': the closing date cannot be later than the subject's end date ({subject.EndDate.Value:yyyy-MM-dd}).");
                    }

                    var quiz = new Quiz
                    {
                        Title = firstQuizRow.QuizTitle,
                        Description = firstQuizRow.QuizDescription,
                        MaxRetries = firstQuizRow.MaxRetries,
                        PublishDate = publishDate,
                        ClosingDate = closingDate,
                        CreatedAt = DateTime.UtcNow,
                        SubjectId = subjectId
                    };

                    foreach (var questionGroup in quizRows.GroupBy(r => r.QuestionOrder).OrderBy(g => g.Key))
                    {
                        var questionRows = questionGroup.OrderBy(r => r.OptionOrder).ToList();
                        var firstQuestionRow = questionRows.First();
                        var questionLabel = $"Quiz '{quizLabel}', question {firstQuestionRow.QuestionOrder}";

                        if (questionRows.Any(r =>
                            r.QuestionTitle != firstQuestionRow.QuestionTitle ||
                            r.Difficulty != firstQuestionRow.Difficulty ||
                            r.TimeLimit != firstQuestionRow.TimeLimit ||
                            r.HasPenalty != firstQuestionRow.HasPenalty ||
                            r.IsStrictMultipleChoice != firstQuestionRow.IsStrictMultipleChoice))
                        {
                            groupErrors.Add($"{questionLabel} has inconsistent question metadata across rows.");
                            continue;
                        }

                        var duplicateOptionOrders = questionRows
                            .GroupBy(r => r.OptionOrder)
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key)
                            .ToList();
                        if (duplicateOptionOrders.Any())
                        {
                            groupErrors.Add($"{questionLabel} has duplicate option order(s): {string.Join(", ", duplicateOptionOrders)}.");
                            continue;
                        }

                        if (questionRows.Count > QuizConstants.MaxOptionsPerQuestion)
                        {
                            groupErrors.Add($"{questionLabel} has more than {QuizConstants.MaxOptionsPerQuestion} options.");
                            continue;
                        }

                        var hasFreeTextOption = questionRows.Any(r => r.IsFreeText);
                        if (hasFreeTextOption)
                        {
                            if (questionRows.Count != 1 || !firstQuestionRow.IsFreeText || !firstQuestionRow.IsCorrect || string.IsNullOrWhiteSpace(firstQuestionRow.OptionText))
                            {
                                groupErrors.Add($"{questionLabel} is a free-text question and must have exactly one correct free-text option with a suggested solution.");
                                continue;
                            }
                        }
                        else
                        {
                            if (questionRows.Count < 2)
                            {
                                groupErrors.Add($"{questionLabel} must have at least two options unless it is a free-text question.");
                                continue;
                            }

                            if (!questionRows.Any(r => r.IsCorrect))
                            {
                                groupErrors.Add($"{questionLabel} must have at least one correct option.");
                                continue;
                            }

                            if (questionRows.Any(r => string.IsNullOrWhiteSpace(r.OptionText)))
                            {
                                groupErrors.Add($"{questionLabel} has an empty option text.");
                                continue;
                            }
                        }

                        var question = new QuizQuestion
                        {
                            Title = firstQuestionRow.QuestionTitle,
                            Difficulty = firstQuestionRow.Difficulty,
                            TimeLimit = firstQuestionRow.TimeLimit,
                            HasPenalty = firstQuestionRow.HasPenalty,
                            IsStrictMultipleChoice = firstQuestionRow.IsStrictMultipleChoice
                        };

                        foreach (var optionRow in questionRows)
                        {
                            question.Options.Add(new QuizOption
                            {
                                Text = optionRow.OptionText,
                                IsCorrect = optionRow.IsCorrect,
                                IsFreeText = optionRow.IsFreeText
                            });
                        }

                        quiz.QuizQuestions.Add(question);
                    }

                    if (!quiz.QuizQuestions.Any())
                    {
                        groupErrors.Add($"Quiz '{quizLabel}' must contain at least one valid question.");
                    }

                    if (groupErrors.Any())
                    {
                        result.Errors.AddRange(groupErrors);
                        continue;
                    }

                    quizzesToCreate.Add(quiz);
                    existingQuizTitles.Add(quizLabel.ToLower());
                }

                if (!quizzesToCreate.Any())
                    return ServiceResult<ImportResultDto>.Ok(result);

                using var transaction = await _context.Database.BeginTransactionAsync();
                _context.Quizzes.AddRange(quizzesToCreate);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.RecordsImported += quizzesToCreate.Count;
                result.RecordsImported += quizzesToCreate.SelectMany(q => q.QuizQuestions).Count();
                result.RecordsImported += quizzesToCreate.SelectMany(q => q.QuizQuestions).SelectMany(q => q.Options).Count();

                return ServiceResult<ImportResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical failure during Quiz CSV import.");
                return ServiceResult<ImportResultDto>.Failure("An unexpected error occurred during processing.", ServiceError.BadRequest);
            }
        }

        public async Task<ServiceResult<string>> ExportQuizFileAsync(long quizId, long currentUserId)
        {
            var quiz = await _context.Quizzes
                .AsNoTracking()
                .Include(q => q.Subject)
                .Include(q => q.QuizQuestions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return ServiceResult<string>.Failure("Quiz not found.", ServiceError.NotFound);

            if (quiz.Subject.TeacherId != currentUserId)
                return ServiceResult<string>.Failure("Only the subject teacher can export this quiz.", ServiceError.Forbidden);

            var rows = quiz.QuizQuestions
                .OrderBy(q => q.Id)
                .SelectMany((question, questionIndex) =>
                {
                    var options = question.Options.OrderBy(o => o.Id).ToList();
                    if (!options.Any())
                    {
                        return new[]
                        {
                            CreateQuizExportRow(quiz, question, questionIndex + 1, null, 1)
                        };
                    }

                    return options.Select((option, optionIndex) =>
                        CreateQuizExportRow(quiz, question, questionIndex + 1, option, optionIndex + 1));
                })
                .ToList();

            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<QuizImportMap>();
            csv.WriteRecords(rows);

            return ServiceResult<string>.Ok(writer.ToString());
        }

        private static QuizImportDto CreateQuizExportRow(Quiz quiz, QuizQuestion question, int questionOrder, QuizOption? option, int optionOrder)
        {
            return new QuizImportDto
            {
                QuizTitle = quiz.Title,
                QuizDescription = quiz.Description,
                MaxRetries = quiz.MaxRetries,
                PublishDate = FormatUtcDate(quiz.PublishDate),
                ClosingDate = FormatUtcDate(quiz.ClosingDate),
                QuestionOrder = questionOrder,
                QuestionTitle = question.Title ?? string.Empty,
                Difficulty = question.Difficulty,
                TimeLimit = question.TimeLimit,
                HasPenalty = question.HasPenalty,
                IsStrictMultipleChoice = question.IsStrictMultipleChoice,
                OptionOrder = optionOrder,
                OptionText = option?.Text ?? string.Empty,
                IsCorrect = option?.IsCorrect ?? false,
                IsFreeText = option?.IsFreeText ?? false
            };
        }

        private static string FormatUtcDate(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static void TrimQuizImportRecord(QuizImportDto record)
        {
            record.QuizTitle = record.QuizTitle.Trim();
            record.QuizDescription = record.QuizDescription.Trim();
            record.PublishDate = record.PublishDate.Trim();
            record.ClosingDate = record.ClosingDate.Trim();
            record.QuestionTitle = record.QuestionTitle.Trim();
            record.OptionText = record.OptionText.Trim();
        }

        private static bool TryParseUtcDate(string value, string fieldName, out DateTime? parsed, out string? error)
        {
            parsed = null;
            error = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (!value.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            {
                error = $"{fieldName} must be a UTC ISO 8601 value ending with 'Z'.";
                return false;
            }

            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime) ||
                dateTime.Kind != DateTimeKind.Utc)
            {
                error = $"{fieldName} must be a valid UTC ISO 8601 value ending with 'Z'.";
                return false;
            }

            parsed = dateTime;
            return true;
        }
    }
}
