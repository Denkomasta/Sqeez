using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Manages quizzes and nested quiz questions/options.
    /// </summary>
    [Authorize]
    [Route("api/quizzes")]
    public class QuizzesController : ApiBaseController
    {
        private readonly IQuizService _quizService;
        private readonly IQuizQuestionService _questionService;
        private readonly IQuizOptionService _optionService;

        public QuizzesController(
            IQuizService quizService,
            IQuizQuestionService questionService,
            IQuizOptionService optionService)
        {
            _quizService = quizService;
            _questionService = questionService;
            _optionService = optionService;
        }

        #region --- 1. QUIZZES ---

        /// <summary>
        /// Gets a paged quiz list using the supplied filters.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<QuizDto>>> GetQuizzesForSubject([FromQuery] QuizFilterDto filter)
        {
            var result = await _quizService.GetAllQuizzesAsync(filter);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Gets one quiz by id, optionally including student-specific attempt context.
        /// </summary>
        [HttpGet("{quizId}")]
        public async Task<ActionResult<QuizDto>> GetQuiz(long quizId, [FromQuery] GetQuizDto dto)
        {
            var result = await _quizService.GetQuizByIdAsync(quizId, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Updates quiz metadata. Admins can update any quiz; teachers can update quizzes for their subjects.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPatch("{quizId}")]
        public async Task<ActionResult<QuizDto>> PatchQuiz(long quizId, [FromBody] PatchQuizDto dto)
        {
            var result = await _quizService.PatchQuizAsync(quizId, dto, CurrentUserId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Deletes a quiz. Admins can delete any quiz; teachers can delete quizzes for their subjects.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpDelete("{quizId}")]
        public async Task<ActionResult<bool>> DeleteQuiz(long quizId)
        {
            var result = await _quizService.DeleteQuizAsync(quizId, CurrentUserId, IsCurrentUserAdmin);
            return HandleServiceResult(result);
        }

        #endregion

        #region --- 2. QUIZ QUESTIONS ---

        /// <summary>
        /// Gets questions for a quiz visible to the admin or subject teacher.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpGet("{quizId}/questions")]
        public async Task<ActionResult<PagedResponse<QuizQuestionDto>>> GetQuestions(long quizId, [FromQuery] QuizQuestionFilterDto filter)
        {
            filter.QuizId = quizId;
            var result = await _questionService.GetAllQuizQuestionsAsync(filter, CurrentUserId, IsCurrentUserAdmin);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Gets one quiz question visible to the admin or subject teacher.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpGet("{quizId}/questions/{questionId}")]
        public async Task<ActionResult<QuizQuestionDto>> GetQuestion(long quizId, long questionId)
        {
            var result = await _questionService.GetQuizQuestionByIdAsync(questionId, CurrentUserId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Creates a question in the route quiz. The route quiz id is authoritative.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost("{quizId}/questions")]
        public async Task<ActionResult<QuizQuestionDto>> CreateQuestion(long quizId, [FromBody] CreateQuizQuestionDto dto)
        {
            var safeDto = dto with { QuizId = quizId };
            var result = await _questionService.CreateQuizQuestionAsync(safeDto, CurrentUserId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Updates a quiz question visible to the admin or subject teacher.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPatch("{quizId}/questions/{questionId}")]
        public async Task<ActionResult<QuizQuestionDto>> PatchQuestion(long quizId, long questionId, [FromBody] PatchQuizQuestionDto dto)
        {
            var result = await _questionService.PatchQuizQuestionAsync(questionId, dto, CurrentUserId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Deletes a quiz question visible to the admin or subject teacher.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpDelete("{quizId}/questions/{questionId}")]
        public async Task<ActionResult<bool>> DeleteQuestion(long quizId, long questionId)
        {
            var result = await _questionService.DeleteQuizQuestionAsync(questionId, CurrentUserId, IsCurrentUserAdmin);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Gets a detailed quiz question for quiz taking or quiz management, according to requester role.
        /// </summary>
        [Authorize]
        [HttpGet("{quizId}/questions/{questionId}/detailed")]
        public async Task<ActionResult<DetailedQuizQuestionDto>> GetDetailedQuestion(long quizId, long questionId)
        {
            var result = await _questionService.GetDetailedQuizQuestionByIdAsync(questionId, quizId, CurrentUserId, GetUserRoleFromClaims() ?? "");
            return HandleServiceResult(result);
        }

        #endregion

        #region --- 3. QUIZ OPTIONS ---

        /// <summary>
        /// Gets answer options for a question visible to the admin or subject teacher.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpGet("{quizId}/questions/{questionId}/options")]
        public async Task<ActionResult<PagedResponse<QuizOptionDto>>> GetOptions(long quizId, long questionId, [FromQuery] QuizOptionFilterDto filter)
        {
            filter.QuizQuestionId = questionId;
            var result = await _optionService.GetAllQuizOptionsAsync(filter, CurrentUserId, IsCurrentUserAdmin);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Gets one answer option visible to the admin or subject teacher.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpGet("{quizId}/questions/{questionId}/options/{optionId}")]
        public async Task<ActionResult<QuizOptionDto>> GetOption(long quizId, long questionId, long optionId)
        {
            var result = await _optionService.GetQuizOptionByIdAsync(optionId, CurrentUserId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Creates an answer option in the route question. The route question id is authoritative.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost("{quizId}/questions/{questionId}/options")]
        public async Task<ActionResult<QuizOptionDto>> CreateOption(long quizId, long questionId, [FromBody] CreateQuizOptionDto dto)
        {
            var safeDto = dto with { QuizQuestionID = questionId };
            var result = await _optionService.CreateQuizOptionAsync(safeDto, CurrentUserId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Updates an answer option visible to the admin or subject teacher.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPatch("{quizId}/questions/{questionId}/options/{optionId}")]
        public async Task<ActionResult<QuizOptionDto>> PatchOption(long quizId, long questionId, long optionId, [FromBody] PatchQuizOptionDto dto)
        {
            var result = await _optionService.PatchQuizOptionAsync(optionId, dto, CurrentUserId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Deletes an answer option visible to the admin or subject teacher.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpDelete("{quizId}/questions/{questionId}/options/{optionId}")]
        public async Task<ActionResult<bool>> DeleteOption(long quizId, long questionId, long optionId)
        {
            var result = await _optionService.DeleteQuizOptionAsync(optionId, CurrentUserId, IsCurrentUserAdmin);
            return HandleServiceResult(result);
        }

        #endregion
    }
}
