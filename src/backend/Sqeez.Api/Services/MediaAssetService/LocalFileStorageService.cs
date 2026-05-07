using FileTypeChecker;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ISystemConfigService _configService;
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mp3", ".pdf" };

        public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger, ISystemConfigService configService)
        {
            _env = env;
            _logger = logger;
            _configService = configService;
        }

        public async Task<ServiceResult<string>> UploadFileAsync(IFormFile file, string subDirectory = "media", bool isPublic = false)
        {
            if (file == null || file.Length == 0)
                return ServiceResult<string>.Failure("No file was uploaded.", ServiceError.ValidationFailed);

            var config = await _configService.GetConfigAsync();

            int maxLimitMB;
            string fileCategoryName;

            if (subDirectory.Equals("avatars", StringComparison.OrdinalIgnoreCase) ||
                subDirectory.Equals("badges", StringComparison.OrdinalIgnoreCase))
            {
                maxLimitMB = config.Data!.MaxAvatarAndBadgeUploadSizeMB;
                fileCategoryName = "avatars and badges";
            }
            else
            {
                maxLimitMB = config.Data!.MaxQuizMediaUploadSizeMB;
                fileCategoryName = "quiz media";
            }

            long maxSizeBytes = maxLimitMB * 1024 * 1024;

            if (file.Length > maxSizeBytes)
            {
                return ServiceResult<string>.Failure(
                    $"File is too large. Maximum allowed size for {fileCategoryName} is {maxLimitMB} MB.",
                    ServiceError.ValidationFailed);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (fileCategoryName == "avatars and badges")
            {
                var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedImageExtensions.Contains(extension))
                {
                    var allowed = string.Join(", ", allowedImageExtensions);
                    return ServiceResult<string>.Failure(
                        $"Invalid file type. Avatars and badges must be images ({allowed}).",
                        ServiceError.ValidationFailed);
                }
            }
            else if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
            {
                var allowed = string.Join(", ", _allowedExtensions);
                return ServiceResult<string>.Failure(
                    $"Invalid file type '{extension}'. Allowed types are: {allowed}",
                    ServiceError.ValidationFailed);
            }

            using var readStream = file.OpenReadStream();
            bool isSignatureValid = false;

            try
            {
                if (FileTypeValidator.IsTypeRecognizable(readStream))
                {
                    var fileType = FileTypeValidator.GetFileType(readStream);

                    var recognizedExt = "." + fileType.Extension.ToLowerInvariant();

                    if (recognizedExt == ".jpeg") recognizedExt = ".jpg";
                    var expectedExt = extension == ".jpeg" ? ".jpg" : extension;

                    if (recognizedExt == expectedExt)
                    {
                        isSignatureValid = true;
                    }
                    else
                    {
                        _logger.LogWarning("File spoofing detected: User claims {UserExt} but file is actually {RealExt}", extension, recognizedExt);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read uploaded file signature.");
            }

            if (!isSignatureValid)
            {
                return ServiceResult<string>.Failure(
                    "The file content does not match its extension or is corrupted. Spoofing suspected.",
                    ServiceError.ValidationFailed);
            }

            readStream.Position = 0;

            try
            {
                string rootFolder;
                string returnedUrlPrefix;

                if (isPublic)
                {
                    rootFolder = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    returnedUrlPrefix = "";
                }
                else
                {
                    rootFolder = Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "SecureStorage");
                    returnedUrlPrefix = "/secure";
                }

                var targetDirectory = Path.Combine(rootFolder, subDirectory);
                if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(targetDirectory, uniqueFileName);

                using (var writeStream = new FileStream(filePath, FileMode.Create))
                {
                    await readStream.CopyToAsync(writeStream);
                }

                var finalUrl = string.IsNullOrEmpty(returnedUrlPrefix)
                    ? $"/{subDirectory}/{uniqueFileName}"
                    : $"{returnedUrlPrefix}/{subDirectory}/{uniqueFileName}";

                return ServiceResult<string>.Ok(finalUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving physical file.");
                return ServiceResult<string>.Failure("An unexpected error occurred while saving the file.", ServiceError.InternalError);
            }
        }

        public Task<ServiceResult<string>> GetPhysicalFilePathAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl) || fileUrl.Contains(".."))
            {
                return Task.FromResult(ServiceResult<string>.Failure("Invalid file path.", ServiceError.ValidationFailed));
            }

            string rootPath;
            string relativePath;

            if (fileUrl.StartsWith("/secure/"))
            {
                relativePath = fileUrl.Replace("/secure/", "").Replace('/', Path.DirectorySeparatorChar);
                rootPath = Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "SecureStorage");
            }
            else
            {
                relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var physicalPath = Path.Combine(rootPath, relativePath);

            var fullRootPath = Path.GetFullPath(rootPath);
            var fullPhysicalPath = Path.GetFullPath(physicalPath);

            if (!fullPhysicalPath.StartsWith(fullRootPath))
            {
                return Task.FromResult(ServiceResult<string>.Failure("Access denied.", ServiceError.Forbidden));
            }

            if (!File.Exists(fullPhysicalPath))
            {
                return Task.FromResult(ServiceResult<string>.Failure(
                    "Stored file was not found.",
                    ServiceError.NotFound));
            }

            return Task.FromResult(ServiceResult<string>.Ok(fullPhysicalPath));
        }

        public Task<ServiceResult<bool>> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileUrl) || fileUrl.Contains(".."))
                    return Task.FromResult(ServiceResult<bool>.Failure("Invalid file path.", ServiceError.ValidationFailed));

                string rootPath;
                string relativePath;

                if (fileUrl.StartsWith("/secure/"))
                {
                    relativePath = fileUrl.Replace("/secure/", "").Replace('/', Path.DirectorySeparatorChar);
                    rootPath = Path.Combine(_env.ContentRootPath ?? Directory.GetCurrentDirectory(), "SecureStorage");
                }
                else
                {
                    relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }

                var physicalPath = Path.Combine(rootPath, relativePath);

                var fullRootPath = Path.GetFullPath(rootPath);
                var fullPhysicalPath = Path.GetFullPath(physicalPath);

                if (!fullPhysicalPath.StartsWith(fullRootPath))
                {
                    _logger.LogWarning("Path traversal attempt detected during file deletion.");
                    return Task.FromResult(ServiceResult<bool>.Failure("Access denied.", ServiceError.Forbidden));
                }

                if (File.Exists(fullPhysicalPath))
                {
                    File.Delete(fullPhysicalPath);
                }

                return Task.FromResult(ServiceResult<bool>.Ok(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting physical file.");
                return Task.FromResult(ServiceResult<bool>.Failure("An unexpected error occurred while deleting the file.", ServiceError.InternalError));
            }
        }
    }
}
