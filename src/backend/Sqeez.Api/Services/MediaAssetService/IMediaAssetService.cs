using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines media asset metadata, access-control, and file cleanup operations.
    /// </summary>
    public interface IMediaAssetService
    {
        /// <summary>
        /// Gets media assets with paging and optional owner, media type, privacy, and text-search filters.
        /// </summary>
        /// <param name="filter">Filtering and paging values.</param>
        /// <returns>A paged list of media asset DTOs.</returns>
        Task<ServiceResult<PagedResponse<MediaAssetDto>>> GetAllMediaAssetsAsync(MediaAssetFilterDto filter);

        /// <summary>
        /// Gets a single media asset by id.
        /// </summary>
        /// <param name="id">The media asset id.</param>
        /// <returns>The media asset DTO, or not found when the asset does not exist.</returns>
        Task<ServiceResult<MediaAssetDto>> GetMediaAssetByIdAsync(long id);

        /// <summary>
        /// Creates a database record for an already uploaded media asset.
        /// </summary>
        /// <param name="dto">Media URL, type, privacy flag, owner teacher id, and optional description.</param>
        /// <returns>The created media asset, or not found when the specified owner teacher does not exist.</returns>
        Task<ServiceResult<MediaAssetDto>> CreateMediaAssetAsync(CreateMediaAssetDto dto);

        /// <summary>
        /// Patches media asset metadata.
        /// </summary>
        /// <param name="id">The media asset id.</param>
        /// <param name="dto">Patch values for URL, media type, privacy, and description.</param>
        /// <returns>The updated media asset, or not found when the asset does not exist.</returns>
        Task<ServiceResult<MediaAssetDto>> PatchMediaAssetAsync(long id, PatchMediaAssetDto dto);

        /// <summary>
        /// Deletes a media asset record and its stored file.
        /// </summary>
        /// <param name="id">The media asset id.</param>
        /// <returns>
        /// A successful result when deleted. Returns not found for a missing asset or conflict when database
        /// constraints prevent deleting an asset attached to quiz content.
        /// </returns>
        Task<ServiceResult<bool>> DeleteMediaAssetAsync(long id);

        /// <summary>
        /// Gets file URL and content type metadata for downloading a media asset.
        /// </summary>
        /// <param name="mediaId">The media asset id.</param>
        /// <param name="currentUserId">The requesting user id.</param>
        /// <param name="currentUserRole">The requesting user role.</param>
        /// <returns>
        /// Download metadata. Returns not found for a missing asset or forbidden when a non-owner, non-admin
        /// user requests a private asset.
        /// </returns>
        Task<ServiceResult<MediaDownloadDto>> GetDownloadMetadataAsync(long mediaId, long currentUserId, string currentUserRole);

        /// <summary>
        /// Deletes a media asset and file, treating missing records as an idempotent success.
        /// </summary>
        /// <param name="id">The media asset id.</param>
        /// <returns>A successful result whether the asset existed or was already gone.</returns>
        Task<ServiceResult<bool>> DeleteMediaAssetAndFileAsync(long id);
    }
}
