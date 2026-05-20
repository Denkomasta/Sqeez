using Sqeez.Api.Enums;
using Sqeez.Api.Models.Users;

namespace Sqeez.Api.Models.Media
{
    /// <summary>
    /// Stored media file that can be attached to quiz questions, quiz options, avatars, or badges.
    /// </summary>
    public class MediaAsset
    {
        /// <summary>
        /// Primary identifier of the media asset.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Public or application-relative location where the media file can be retrieved.
        /// </summary>
        public string LocationUrl { get; set; } = string.Empty;

        /// <summary>
        /// Categorized media type used for upload limits and validation.
        /// </summary>
        public MediaType MimeType { get; set; }

        /// <summary>
        /// Indicates whether the asset should be treated as private by access checks.
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Optional description or alt text for the media asset.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Identifier of the teacher or admin account that owns the asset.
        /// </summary>
        public long OwnerId { get; set; }

        /// <summary>
        /// Teacher or admin account that owns the asset.
        /// </summary>
        public Teacher Owner { get; set; } = null!;
    }
}
