using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Media;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Implements media asset metadata management, access checks, and coordinated file cleanup.
    /// </summary>
    public class MediaAssetService : BaseService<MediaAssetService>, IMediaAssetService
    {
        private readonly IFileStorageService _fileStorageService;
        public MediaAssetService(SqeezDbContext context, ILogger<MediaAssetService> logger, IFileStorageService fileStorageService) : base(context, logger)
        {
            _fileStorageService = fileStorageService;
        }

        public async Task<ServiceResult<PagedResponse<MediaAssetDto>>> GetAllMediaAssetsAsync(MediaAssetFilterDto filter)
        {
            var query = _context.MediaAssets.AsNoTracking();

            if (filter.OwnerId.HasValue)
            {
                query = query.Where(m => m.OwnerId == filter.OwnerId.Value);
            }

            if (filter.MimeType.HasValue)
            {
                query = query.Where(m => m.MimeType == filter.MimeType.Value);
            }

            if (filter.IsPrivate.HasValue)
            {
                query = query.Where(m => m.IsPrivate == filter.IsPrivate.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.Trim().ToLower();
                query = query.Where(m =>
                    (m.Description != null && m.Description.ToLower().Contains(search)) ||
                    m.LocationUrl.ToLower().Contains(search));
            }

            int totalCount = await query.CountAsync();

            var assets = await query
                .OrderByDescending(m => m.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(m => new MediaAssetDto(
                    m.Id,
                    m.LocationUrl,
                    m.MimeType,
                    m.IsPrivate,
                    m.Description,
                    m.OwnerId,
                    m.Owner.Username
                ))
                .ToListAsync();

            return ServiceResult<PagedResponse<MediaAssetDto>>.Ok(new PagedResponse<MediaAssetDto>
            {
                Data = assets,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            });
        }

        public async Task<ServiceResult<MediaAssetDto>> GetMediaAssetByIdAsync(long id)
        {
            var asset = await _context.MediaAssets
                .Where(m => m.Id == id)
                .Select(m => new MediaAssetDto(
                    m.Id,
                    m.LocationUrl,
                    m.MimeType,
                    m.IsPrivate,
                    m.Description,
                    m.OwnerId,
                    m.Owner.Username
                ))
                .FirstOrDefaultAsync();

            if (asset == null)
                return ServiceResult<MediaAssetDto>.Failure("Media asset not found.", ServiceError.NotFound);

            return ServiceResult<MediaAssetDto>.Ok(asset);
        }

        public async Task<ServiceResult<MediaDownloadDto>> GetDownloadMetadataAsync(long mediaId, long currentUserId, string currentUserRole)
        {
            var asset = await _context.MediaAssets.FirstOrDefaultAsync(m => m.Id == mediaId);
            if (asset == null)
            {
                return ServiceResult<MediaDownloadDto>.Failure("Media not found.", ServiceError.NotFound);
            }

            if (asset.IsPrivate)
            {
                if (asset.OwnerId != currentUserId && currentUserRole != "Admin")
                {
                    return ServiceResult<MediaDownloadDto>.Failure("You do not have permission to view this private file.", ServiceError.Forbidden);
                }
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(asset.LocationUrl, out string? mimeTypeStr))
            {
                // If it's a super weird file type it doesn't recognize, fall back to binary
                mimeTypeStr = "application/octet-stream";
            }

            return ServiceResult<MediaDownloadDto>.Ok(new MediaDownloadDto(asset.LocationUrl, mimeTypeStr));
        }

        public async Task<ServiceResult<MediaAssetDto>> CreateMediaAssetAsync(CreateMediaAssetDto dto)
        {
            var ownerExists = await _context.Teachers.AnyAsync(t => t.Id == dto.OwnerId);
            if (!ownerExists)
                return ServiceResult<MediaAssetDto>.Failure("The specified Teacher does not exist.", ServiceError.NotFound);

            var asset = new MediaAsset
            {
                LocationUrl = dto.LocationUrl,
                MimeType = dto.MimeType,
                IsPrivate = dto.IsPrivate,
                Description = dto.Description,
                OwnerId = dto.OwnerId
            };

            _context.MediaAssets.Add(asset);
            await _context.SaveChangesAsync();

            var ownerUsername = await _context.Teachers
                .Where(t => t.Id == asset.OwnerId)
                .Select(t => t.Username)
                .FirstOrDefaultAsync();

            return ServiceResult<MediaAssetDto>.Ok(new MediaAssetDto(
                asset.Id, asset.LocationUrl, asset.MimeType, asset.IsPrivate, asset.Description, asset.OwnerId, ownerUsername));
        }

        public async Task<ServiceResult<MediaAssetDto>> PatchMediaAssetAsync(long id, PatchMediaAssetDto dto)
        {
            var asset = await _context.MediaAssets
                .Include(m => m.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset == null)
                return ServiceResult<MediaAssetDto>.Failure("Media asset not found.", ServiceError.NotFound);

            if (dto.LocationUrl != null) asset.LocationUrl = dto.LocationUrl;
            if (dto.MimeType.HasValue) asset.MimeType = dto.MimeType.Value;
            if (dto.IsPrivate.HasValue) asset.IsPrivate = dto.IsPrivate.Value;
            if (dto.Description != null) asset.Description = dto.Description;

            await _context.SaveChangesAsync();

            return ServiceResult<MediaAssetDto>.Ok(new MediaAssetDto(
                asset.Id, asset.LocationUrl, asset.MimeType, asset.IsPrivate, asset.Description, asset.OwnerId, asset.Owner.Username));
        }

        public async Task<ServiceResult<bool>> DeleteMediaAssetAsync(long id)
        {
            var asset = await _context.MediaAssets.FindAsync(id);

            if (asset == null)
                return ServiceResult<bool>.Failure("Media asset not found.", ServiceError.NotFound);

            string fileUrlToDelete = asset.LocationUrl;

            try
            {
                await _fileStorageService.DeleteFileAsync(fileUrlToDelete);

                _context.MediaAssets.Remove(asset);

                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Ok(true);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete MediaAsset {Id} due to database constraints.", id);
                return ServiceResult<bool>.Failure(
                    "Cannot delete this media asset because it is currently attached to a quiz question or option.",
                    ServiceError.Conflict);
            }
        }

        public async Task<ServiceResult<bool>> DeleteMediaAssetAndFileAsync(long id)
        {
            var asset = await _context.MediaAssets.FindAsync(id);
            if (asset == null) return ServiceResult<bool>.Ok(true);

            string fileUrl = asset.LocationUrl;

            if (!string.IsNullOrWhiteSpace(fileUrl))
            {
                await _fileStorageService.DeleteFileAsync(fileUrl);
            }

            _context.MediaAssets.Remove(asset);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }
    }
}
