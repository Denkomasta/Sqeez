using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Handles quiz attempt lifecycle operations, answer submission, grading, and attempt cleanup.
    /// </summary>
    [Route("api/quiz-attempts")]
    public class QuizAttemptsController : ApiBaseController
    {
        private readonly IQuizAttemptService _quizAttemptService;

        public QuizAttemptsController(IQuizAttemptService quizAttemptService)
        {
            _quizAttemptService = quizAttemptService;
        }

        /// <summary>
        /// POST /api/quiz-attempts/start
        /// Starts a new attempt for the currently authenticated student.
        /// </summary>
        [Authorize]
        [HttpPost("start")]
        public async Task<ActionResult<QuizAttemptDto>> StartAttempt([FromBody] StartQuizAttemptDto dto)
        {
            var studentIdStr = GetUserIdFromClaims();
            if (!long.TryParse(studentIdStr, out long studentId))
                return Unauthorized("Invalid student ID token.");

            var result = await _quizAttemptService.StartAttemptAsync(studentId, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// POST /api/quiz-attempts/{id}/answers
        /// Submits or updates an answer for a specific question.
        /// </summary>
        [Authorize]
        [HttpPost("{id}/answer")]
        public async Task<ActionResult<QuestionAnsweredDto>> SubmitAnswer(long id, [FromBody] SubmitQuestionResponseDto dto)
        {
            var studentIdStr = GetUserIdFromClaims();
            if (!long.TryParse(studentIdStr, out long studentId))
                return Unauthorized("Invalid student ID token.");

            var result = await _quizAttemptService.SubmitAnswerAsync(id, studentId, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/quiz-attempts/{id}/next-question
        /// Recovers the ID of the next unanswered question if the frontend loses context.
        /// </summary>
        [Authorize]
        [HttpGet("{id}/next-question")]
        public async Task<ActionResult<long?>> GetNextPendingQuestionId(long id)
        {
            var studentIdStr = GetUserIdFromClaims();
            if (!long.TryParse(studentIdStr, out long studentId))
                return Unauthorized("Invalid student ID token.");

            var result = await _quizAttemptService.GetNextPendingQuestionIdAsync(id, studentId);

            // For No Content
            if (result.Success)
            {
                return Ok(result.Data);
            }

            return HandleServiceResult(result);
        }

        /// <summary>
        /// POST /api/quiz-attempts/{id}/complete
        /// Locks the attempt and calculates the final score.
        /// </summary>
        [Authorize]
        [HttpPost("{id}/complete")]
        public async Task<ActionResult<QuizAttemptDto>> CompleteAttempt(long id)
        {
            var studentIdStr = GetUserIdFromClaims();
            if (!long.TryParse(studentIdStr, out long studentId))
                return Unauthorized("Invalid student ID token.");

            var result = await _quizAttemptService.CompleteAttemptAsync(id, studentId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/quiz-attempts/{id}
        /// Gets full attempt details. Students see their own attempts, subject teachers see attempts for their quizzes,
        /// and admins can see any attempt.
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<QuizAttemptDetailDto>> GetAttemptDetails(long id)
        {
            var userIdStr = GetUserIdFromClaims();
            var role = GetUserRoleFromClaims() ?? string.Empty;
            if (!long.TryParse(userIdStr, out long currentUserId))
                return Unauthorized("Invalid user ID token.");

            var result = await _quizAttemptService.GetAttemptDetailsAsync(id, currentUserId, role);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/quiz-attempts/quiz/{quizId}
        /// Gets a paginated list of all attempts for a specific quiz.
        /// </summary>
        [Authorize]
        [HttpGet("quiz/{quizId}")]
        public async Task<ActionResult<PagedResponse<QuizAttemptDto>>> GetAttemptsForQuiz(long quizId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var teacherIdStr = GetUserIdFromClaims();
            if (!long.TryParse(teacherIdStr, out long teacherId))
                return Unauthorized("Invalid teacher ID token.");

            var result = await _quizAttemptService.GetAttemptsForQuizAsync(quizId, teacherId, pageNumber, pageSize);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// PATCH /api/quiz-attempts/responses/{responseId}/grade
        /// Allows a teacher to manually grade a free-text answer and "Like" it.
        /// </summary>
        [Authorize(Roles = "Teacher,Admin")]
        [HttpPatch("responses/{responseId}/grade")]
        public async Task<ActionResult<QuestionResponseDto>> GradeResponse(long responseId, [FromBody] GradeQuestionResponseDto dto)
        {
            var teacherIdStr = GetUserIdFromClaims();
            if (!long.TryParse(teacherIdStr, out long teacherId))
                return Unauthorized("Invalid teacher ID token.");

            var result = await _quizAttemptService.GradeFreeTextResponseAsync(responseId, teacherId, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// DELETE /api/quiz-attempts/{id}
        /// Deletes a specific quiz attempt (Allows a teacher to reset a student's try).
        /// </summary>
        [Authorize(Roles = "Teacher,Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeleteAttempt(long id)
        {
            var teacherIdStr = GetUserIdFromClaims();
            if (!long.TryParse(teacherIdStr, out long teacherId))
                return Unauthorized("Invalid teacher ID token.");

            var result = await _quizAttemptService.DeleteAttemptAsync(id, teacherId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// DELETE /api/quizzes/{quizId}/attempts
        /// Deletes all quiz attempts and student responses for a specific quiz.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpDelete("{quizId}/attempts")]
        public async Task<ActionResult<bool>> DeleteAllAttemptsForQuiz(long quizId)
        {
            var userIdStr = GetUserIdFromClaims();
            if (!long.TryParse(userIdStr, out long currentUserId))
            {
                return Unauthorized();
            }

            bool isAdmin = User.IsInRole("Admin");

            var result = await _quizAttemptService.DeleteAllAttemptsForQuizAsync(quizId, currentUserId, isAdmin);

            return HandleServiceResult(result);
        }
    }
}
