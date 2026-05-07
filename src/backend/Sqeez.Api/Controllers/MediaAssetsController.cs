using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Manages media asset metadata, file uploads, and protected file downloads.
    /// </summary>
    [Authorize]
    [Route("api/media-assets")]
    public class MediaAssetsController : ApiBaseController
    {
        private readonly IMediaAssetService _mediaAssetService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<MediaAssetsController> _logger;

        public MediaAssetsController(
            IMediaAssetService mediaAssetService,
            IFileStorageService fileStorageService,
            ILogger<MediaAssetsController> logger)
        {
            _mediaAssetService = mediaAssetService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/media-assets
        /// Gets a paged list of media asset metadata.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<MediaAssetDto>>> GetAll([FromQuery] MediaAssetFilterDto filter)
        {
            var result = await _mediaAssetService.GetAllMediaAssetsAsync(filter);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/media-assets/{id}
        /// Gets media asset metadata by id.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<MediaAssetDto>> GetById(long id)
        {
            var result = await _mediaAssetService.GetMediaAssetByIdAsync(id);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// POST /api/media-assets
        /// Saves metadata for a new media asset without uploading a file. The owner id is taken from the authenticated user.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost]
        public async Task<ActionResult<MediaAssetDto>> Create([FromBody] CreateMediaAssetDto dto)
        {
            var userIdStr = GetUserIdFromClaims();
            if (!long.TryParse(userIdStr, out long ownerId))
            {
                return Unauthorized();
            }

            var safeDto = new CreateMediaAssetDto(
                dto.LocationUrl,
                dto.MimeType,
                dto.IsPrivate,
                ownerId,
                dto.Description);

            var result = await _mediaAssetService.CreateMediaAssetAsync(safeDto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// PATCH /api/media-assets/{id}
        /// Updates media asset metadata. Admins can patch any asset; teachers can patch only their own assets.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPatch("{id}")]
        public async Task<ActionResult<MediaAssetDto>> Patch(long id, [FromBody] PatchMediaAssetDto dto)
        {
            var assetResult = await _mediaAssetService.GetMediaAssetByIdAsync(id);
            if (!assetResult.Success || assetResult.Data == null)
            {
                return HandleServiceResult(assetResult);
            }

            var userIdStr = GetUserIdFromClaims();
            bool isAdmin = User.IsInRole("Admin");

            if (!long.TryParse(userIdStr, out long currentUserId))
            {
                return Unauthorized();
            }

            if (!isAdmin && assetResult.Data.OwnerId != currentUserId)
            {
                return Forbid();
            }

            var result = await _mediaAssetService.PatchMediaAssetAsync(id, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// POST /api/media-assets/upload
        /// Accepts a physical file, saves it securely, and creates the database record.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<MediaAssetDto>> UploadFile([FromForm] UploadMediaAssetDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file was uploaded.");

            var userIdStr = GetUserIdFromClaims();
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long ownerId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _fileStorageService.UploadFileAsync(dto.File, "media", false);

                if (!response.Success)
                {
                    return HandleServiceResult(response);
                }

                string fileUrl = response.Data!;
                var mediaType = GetMediaTypeFromStoredFileUrl(fileUrl);

                var createDto = new CreateMediaAssetDto(
                    LocationUrl: fileUrl,
                    MimeType: mediaType,
                    IsPrivate: dto.IsPrivate,
                    OwnerId: ownerId,
                    Description: dto.Description
                );

                var dbResult = await _mediaAssetService.CreateMediaAssetAsync(createDto);

                if (!dbResult.Success)
                {
                    await _fileStorageService.DeleteFileAsync(fileUrl);
                    return HandleServiceResult(dbResult);
                }

                return HandleServiceResult(dbResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during media upload for user {OwnerId}.", ownerId);
                return StatusCode(500, "An unexpected error occurred during file upload.");
            }
        }

        private static MediaType GetMediaTypeFromStoredFileUrl(string fileUrl)
        {
            return Path.GetExtension(fileUrl).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" => MediaType.Image,
                ".mp4" => MediaType.Video,
                ".mp3" => MediaType.Audio,
                _ => MediaType.Document
            };
        }

        /// <summary>
        /// DELETE /api/media-assets/{id}
        /// Deletes the database metadata and physical file. Admins can delete any asset; teachers can delete only their own assets.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> Delete(long id)
        {
            var assetResult = await _mediaAssetService.GetMediaAssetByIdAsync(id);
            if (!assetResult.Success || assetResult.Data == null)
            {
                return HandleServiceResult(assetResult);
            }

            var userIdStr = GetUserIdFromClaims();
            bool isAdmin = User.IsInRole("Admin");

            if (!long.TryParse(userIdStr, out long currentUserId))
            {
                return Unauthorized();
            }

            if (!isAdmin && assetResult.Data.OwnerId != currentUserId)
            {
                return Forbid();
            }

            var result = await _mediaAssetService.DeleteMediaAssetAndFileAsync(id);

            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/media-assets/{id}/file
        /// Streams a stored file after checking the asset's privacy and requester access.
        /// </summary>
        [Authorize]
        [HttpGet("{id}/file")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK, "application/octet-stream")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFile(long id)
        {
            var userIdStr = GetUserIdFromClaims();
            var role = GetUserRoleFromClaims() ?? string.Empty;
            long.TryParse(userIdStr, out long currentUserId);

            var metadataResult = await _mediaAssetService.GetDownloadMetadataAsync(id, currentUserId, role);
            if (!metadataResult.Success)
            {
                return HandleServiceResult(metadataResult);
            }

            var pathResult = await _fileStorageService.GetPhysicalFilePathAsync(metadataResult.Data!.LocationUrl);
            if (!pathResult.Success)
            {
                return HandleServiceResult(pathResult);
            }

            var fileName = Path.GetFileName(metadataResult.Data!.LocationUrl);

            return PhysicalFile(pathResult.Data!, metadataResult.Data.MimeType, fileName, enableRangeProcessing: true);
        }
    }
}
