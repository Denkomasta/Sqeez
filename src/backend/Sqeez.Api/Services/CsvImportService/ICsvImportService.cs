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
    }
}
