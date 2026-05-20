using Sqeez.Api.Models.QuizSystem;

namespace Sqeez.Api.Extensions
{
    /// <summary>
    /// Query helpers for quiz availability filtering.
    /// </summary>
    public static class QuizQueryableExtensions
    {
        /// <summary>
        /// Filters quizzes to those currently available for students to take.
        /// </summary>
        /// <remarks>
        /// A quiz is active when it has a publish date in the past, has no closing date in the past,
        /// and its parent subject has not ended.
        /// </remarks>
        public static IQueryable<Quiz> WhereIsActive(this IQueryable<Quiz> query)
        {
            var now = DateTime.UtcNow;
            return query.Where(q => q.PublishDate != null &&
                                    q.PublishDate <= now &&
                                    (q.ClosingDate == null || q.ClosingDate > now) &&
                                    (q.Subject.EndDate == null || q.Subject.EndDate >= now));
        }

        /// <summary>
        /// Filters quizzes to drafts, future quizzes, closed quizzes, or quizzes under ended subjects.
        /// </summary>
        public static IQueryable<Quiz> WhereIsInactive(this IQueryable<Quiz> query)
        {
            var now = DateTime.UtcNow;
            return query.Where(q => q.PublishDate == null ||
                                    q.PublishDate > now ||
                                    (q.ClosingDate != null && q.ClosingDate <= now) ||
                                    (q.Subject.EndDate != null && q.Subject.EndDate < now));
        }
    }
}
