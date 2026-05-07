using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines quiz and question statistics retrieval operations.
    /// </summary>
    public interface IQuizStatisticsService
    {
        /// <summary>
        /// Calculates aggregate attempt statistics for a teacher-owned quiz.
        /// </summary>
        /// <param name="quizId">The quiz id.</param>
        /// <param name="teacherId">The teacher requesting statistics.</param>
        /// <returns>
        /// Summary statistics including attempts, completed attempts, score range, average score, and completion
        /// time. Returns not found for a missing quiz or forbidden when the teacher does not own the quiz subject.
        /// </returns>
        Task<ServiceResult<QuizSummaryStatDto>> GetQuizSummaryStatsAsync(long quizId, long teacherId);

        /// <summary>
        /// Calculates per-question statistics for completed and pending-correction attempts in a teacher-owned quiz.
        /// </summary>
        /// <param name="quizId">The quiz id.</param>
        /// <param name="teacherId">The teacher requesting statistics.</param>
        /// <returns>
        /// Question-level answer counts, option pick counts, free-text submissions, average score, and average
        /// response time. Returns not found for a missing quiz or forbidden when the teacher does not own the quiz subject.
        /// </returns>
        Task<ServiceResult<IEnumerable<QuestionStatDto>>> GetQuestionStatsAsync(long quizId, long teacherId);
    }
}
