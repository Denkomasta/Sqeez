using Microsoft.AspNetCore.Http;
using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines physical file storage operations for uploaded assets.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Validates and stores an uploaded file in local public or secure storage.
        /// </summary>
        /// <param name="file">Uploaded file to validate and persist.</param>
        /// <param name="subDirectory">Storage subdirectory, such as media, avatars, or badges.</param>
        /// <param name="isPublic">When true, stores below web root; otherwise stores below secure storage.</param>
        /// <returns>
        /// The URL path for the stored file. Returns validation failed for empty, oversized, unsupported,
        /// or signature-mismatched files, and internal error for save failures.
        /// </returns>
        Task<ServiceResult<string>> UploadFileAsync(IFormFile file, string subDirectory = "media", bool isPublic = false);

        /// <summary>
        /// Deletes a stored file by URL path.
        /// </summary>
        /// <param name="fileUrl">Public or secure file URL path.</param>
        /// <returns>
        /// A successful result even when the file is already missing. Returns validation failed or forbidden
        /// for invalid/path-traversal input, and internal error for deletion failures.
        /// </returns>
        Task<ServiceResult<bool>> DeleteFileAsync(string fileUrl);

        /// <summary>
        /// Resolves a public or secure file URL to a physical file path.
        /// </summary>
        /// <param name="fileUrl">Public or secure file URL path.</param>
        /// <returns>
        /// The physical file path. Returns validation failed for invalid paths, forbidden for path traversal,
        /// or not found when the file does not exist.
        /// </returns>
        Task<ServiceResult<string>> GetPhysicalFilePathAsync(string fileUrl);
    }
}
