using Sqeez.Api.Constants;
using Sqeez.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Media asset metadata returned by media endpoints.
    /// </summary>
    public record MediaAssetDto(
        long Id,
        string LocationUrl,
        MediaType MimeType,
        bool IsPrivate,
        string? Description,
        long OwnerId,
        string? OwnerUsername);

    /// <summary>
    /// Media asset search filters.
    /// </summary>
    public class MediaAssetFilterDto : PagedFilterDto
    {
        [StringLength(ValidationConstants.SearchTermMaxLength)]
        public string? SearchTerm { get; set; } // Search Description or LocationUrl
        public MediaType? MimeType { get; set; }
        public bool? IsPrivate { get; set; }
        public long? OwnerId { get; set; }
    }

    /// <summary>
    /// Request for creating media asset metadata.
    /// </summary>
    public record CreateMediaAssetDto
    {
        public CreateMediaAssetDto() { }

        public CreateMediaAssetDto(string LocationUrl, MediaType MimeType, bool IsPrivate, long OwnerId, string? Description = null)
        {
            this.LocationUrl = LocationUrl;
            this.MimeType = MimeType;
            this.IsPrivate = IsPrivate;
            this.OwnerId = OwnerId;
            this.Description = Description;
        }

        [StringLength(ValidationConstants.UrlMaxLength)]
        public string LocationUrl { get; init; } = string.Empty;
        public MediaType MimeType { get; init; }
        public bool IsPrivate { get; init; }
        public long OwnerId { get; init; }

        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string? Description { get; init; }
    }

    /// <summary>
    /// Request for partially updating media asset metadata.
    /// </summary>
    public record PatchMediaAssetDto
    {
        public PatchMediaAssetDto() { }

        public PatchMediaAssetDto(string? LocationUrl = null, MediaType? MimeType = null, bool? IsPrivate = null, string? Description = null)
        {
            this.LocationUrl = LocationUrl;
            this.MimeType = MimeType;
            this.IsPrivate = IsPrivate;
            this.Description = Description;
        }

        [StringLength(ValidationConstants.UrlMaxLength)]
        public string? LocationUrl { get; init; }
        public MediaType? MimeType { get; init; }
        public bool? IsPrivate { get; init; }

        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string? Description { get; init; }
    }

    /// <summary>
    /// Multipart upload request for storing a media file and creating its metadata.
    /// </summary>
    public class UploadMediaAssetDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
        public long OwnerId { get; set; }
        public bool IsPrivate { get; set; } = false;
        [StringLength(ValidationConstants.DescriptionMaxLength)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Internal download metadata used to resolve and stream stored files.
    /// </summary>
    public record MediaDownloadDto(string LocationUrl, string MimeType);
}
