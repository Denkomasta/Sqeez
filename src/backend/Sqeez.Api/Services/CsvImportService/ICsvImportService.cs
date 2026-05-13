using Sqeez.Api.DTOs;
using Sqeez.Api.Models.Import;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines CSV import operations for administrative bulk data loading.
    /// </summary>
    public interface ICsvImportService
    {
        /// <summary>
        /// Imports a master CSV file containing classes, subjects, and students.
        /// </summary>
        /// <param name="file">CSV file using the configured master-import headers.</param>
        /// <returns>
        /// Import counts and row-level validation or skipped-record messages. Returns bad request for missing,
        /// non-CSV, malformed, or otherwise unprocessable files. Valid rows are processed even when other rows
        /// contain validation errors.
        /// </returns>
        Task<ServiceResult<ImportResultDto>> ImportMasterFileAsync(IFormFile file);

        /// <summary>
        /// Imports quizzes, questions, and options from a CSV file into a subject owned by the current teacher.
        /// </summary>
        /// <param name="subjectId">The target subject id from the route.</param>
        /// <param name="file">CSV file using the quiz-import headers.</param>
        /// <param name="currentUserId">The authenticated teacher id.</param>
        /// <returns>Import counts and row/group-level validation errors.</returns>
        Task<ServiceResult<ImportResultDto>> ImportQuizFileAsync(long subjectId, IFormFile file, long currentUserId);

        /// <summary>
        /// Exports a teacher-owned quiz into the quiz-import CSV format.
        /// </summary>
        /// <param name="quizId">The quiz id.</param>
        /// <param name="currentUserId">The authenticated teacher id.</param>
        /// <returns>CSV content for the quiz.</returns>
        Task<ServiceResult<string>> ExportQuizFileAsync(long quizId, long currentUserId);
    }
}
