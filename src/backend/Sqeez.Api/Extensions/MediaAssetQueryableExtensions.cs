using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Models.Media;

namespace Sqeez.Api.Extensions
{
    /// <summary>
    /// Query helpers for media asset list and assignment filtering.
    /// </summary>
    public static class MediaAssetQueryableExtensions
    {
        /// <summary>
        /// Applies media asset search filters including owner, type, privacy, text search, and assignment state.
        /// </summary>
        /// <remarks>
        /// When <see cref="MediaAssetFilterDto.UnassignedOnly"/> is true, the result contains only assets that are
        /// not referenced by any quiz question or quiz option.
        /// </remarks>
        public static IQueryable<MediaAsset> ApplyFilters(
            this IQueryable<MediaAsset> query,
            MediaAssetFilterDto filter,
            SqeezDbContext context)
        {
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

            if (filter.UnassignedOnly == true)
            {
                query = query.WhereIsUnassigned(context);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.Trim().ToLower();
                query = query.Where(m =>
                    (m.Description != null && m.Description.ToLower().Contains(search)) ||
                    m.LocationUrl.ToLower().Contains(search));
            }

            return query;
        }

        /// <summary>
        /// Filters media assets to those that are not attached to any quiz question or quiz option.
        /// </summary>
        /// <remarks>
        /// Avatar and badge icon references are intentionally not considered quiz-content assignments here.
        /// </remarks>
        public static IQueryable<MediaAsset> WhereIsUnassigned(
            this IQueryable<MediaAsset> query,
            SqeezDbContext context)
        {
            return query.Where(m =>
                !context.QuizQuestions.Any(q => q.MediaAssetId == m.Id) &&
                !context.QuizOptions.Any(o => o.MediaAssetId == m.Id));
        }
    }
}
