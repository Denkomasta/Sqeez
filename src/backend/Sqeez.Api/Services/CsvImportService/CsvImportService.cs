using CsvHelper;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Import;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
    }
}
