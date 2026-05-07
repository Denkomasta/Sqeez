using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Provides quiz-level and question-level statistics for teachers and admins.
    /// </summary>
    [Route("api/quizzes/{quizId}/statistics")]
    public class QuizStatisticsController : ApiBaseController
    {
        private readonly IQuizStatisticsService _statisticsService;

        public QuizStatisticsController(IQuizStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// GET /api/quizzes/{quizId}/statistics/summary
        /// Gets high-level summary statistics for a specific quiz.
        /// </summary>
        [Authorize(Roles = "Teacher,Admin")]
        [HttpGet("summary")]
        public async Task<ActionResult<QuizSummaryStatDto>> GetQuizSummaryStats(long quizId)
        {
            var result = await _statisticsService.GetQuizSummaryStatsAsync(quizId, CurrentUserId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/quizzes/{quizId}/statistics/questions
        /// Gets detailed statistics for each question within a quiz to identify difficult or popular questions.
        /// </summary>
        [Authorize(Roles = "Teacher,Admin")]
        [HttpGet("questions")]
        public async Task<ActionResult<IEnumerable<QuestionStatDto>>> GetQuestionStats(long quizId)
        {
            var result = await _statisticsService.GetQuestionStatsAsync(quizId, CurrentUserId);
            return HandleServiceResult(result);
        }
    }
}
