using Sqeez.Api.Enums;

using Sqeez.Api.Constants;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Service-layer result wrapper that keeps success state, payload, and typed error information together.
    /// </summary>
    public record ServiceResult<T>(
        bool Success,
        T? Data,
        string? ErrorMessage,
        ServiceError ErrorCode = ServiceError.None)
    {
        /// <summary>
        /// Creates a successful service result.
        /// </summary>
        public static ServiceResult<T> Ok(T data) =>
            new(true, data, null, ServiceError.None);

        /// <summary>
        /// Creates a failed service result with a user-safe message and error code.
        /// </summary>
        public static ServiceResult<T> Failure(string message, ServiceError code) =>
            new(false, default, message, code);
    }

    /// <summary>
    /// Standard paged response shape used by list endpoints.
    /// </summary>
    public class PagedResponse<T>
    {
        /// <summary>
        /// Items for the requested page.
        /// </summary>
        public IEnumerable<T> Data { get; set; } = new List<T>();

        /// <summary>
        /// Total number of items matching the filter before paging is applied.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// One-based page number returned by the query.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Maximum number of items requested for this page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of pages calculated from TotalCount and PageSize.
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    /// <summary>
    /// Base query DTO for one-based paging with repository-wide page-size limits.
    /// </summary>
    public class PagedFilterDto
    {
        /// <summary>
        /// One-based page number.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Requested page size, capped by ValidationConstants.MaxPageSize.
        /// </summary>
        [Range(1, ValidationConstants.MaxPageSize)]
        public int PageSize { get; init; } = 10;
    }
}
