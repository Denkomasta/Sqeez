using Sqeez.Api.Models.Academics;

namespace Sqeez.Api.Extensions
{
    /// <summary>
    /// Query helpers for subject availability filtering.
    /// </summary>
    public static class SubjectQueryableExtensions
    {
        /// <summary>
        /// Filters subjects to those whose availability window contains the current UTC time.
        /// </summary>
        public static IQueryable<Subject> WhereIsActive(this IQueryable<Subject> query)
        {
            var now = DateTime.UtcNow;
            return query.Where(s => s.StartDate <= now &&
                                   (!s.EndDate.HasValue || s.EndDate >= now));
        }

        /// <summary>
        /// Filters subjects to those scheduled for the future or already ended.
        /// </summary>
        public static IQueryable<Subject> WhereIsInactive(this IQueryable<Subject> query)
        {
            var now = DateTime.UtcNow;
            return query.Where(s => s.StartDate > now ||
                                   (s.EndDate.HasValue && s.EndDate < now));
        }
    }
}
