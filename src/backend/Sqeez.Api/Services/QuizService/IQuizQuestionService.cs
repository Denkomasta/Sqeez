using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines quiz question management and student-safe question detail retrieval.
    /// </summary>
    public interface IQuizQuestionService
    {
        /// <summary>
        /// Gets quiz questions with paging and optional quiz, difficulty, media, and text-search filters.
        /// </summary>
        /// <param name="filter">Filtering and paging values.</param>
        /// <param name="currentUserId">The requesting user id.</param>
        /// <param name="isAdmin">Whether to bypass teacher ownership filtering.</param>
        /// <returns>A paged list of questions visible to the requester.</returns>
        Task<ServiceResult<PagedResponse<QuizQuestionDto>>> GetAllQuizQuestionsAsync(QuizQuestionFilterDto filter, long currentUserId, bool isAdmin);

        /// <summary>
        /// Gets a quiz question for a teacher-owned subject.
        /// </summary>
        /// <param name="id">The question id.</param>
        /// <param name="currentUserId">The requesting teacher id.</param>
        /// <returns>The question DTO, not found for a missing question, or forbidden for a non-owner teacher.</returns>
        Task<ServiceResult<QuizQuestionDto>> GetQuizQuestionByIdAsync(long id, long currentUserId);

        /// <summary>
        /// Gets the student-facing detailed question view with answer text hidden for free-text options.
        /// </summary>
        /// <param name="id">The question id.</param>
        /// <param name="quizId">The quiz id that must contain the question.</param>
        /// <param name="currentUserId">The requesting user id.</param>
        /// <param name="role">The requesting role.</param>
        /// <returns>
        /// Detailed question data. Subject teachers can view directly; enrolled students must have an active
        /// attempt for the quiz. Returns not found or forbidden otherwise.
        /// </returns>
        Task<ServiceResult<DetailedQuizQuestionDto>> GetDetailedQuizQuestionByIdAsync(long id, long quizId, long currentUserId, string role);

        /// <summary>
        /// Creates a question on a teacher-owned quiz.
        /// </summary>
        /// <param name="dto">Question content, scoring, timing, quiz id, and optional media id.</param>
        /// <param name="currentUserId">The requesting teacher id.</param>
        /// <returns>
        /// The created question. Returns not found for a missing quiz, forbidden for non-owner teachers or closed
        /// subjects, and conflict when attempts already exist for the quiz.
        /// </returns>
        Task<ServiceResult<QuizQuestionDto>> CreateQuizQuestionAsync(CreateQuizQuestionDto dto, long currentUserId);

        /// <summary>
        /// Patches a question on a teacher-owned quiz.
        /// </summary>
        /// <param name="id">The question id.</param>
        /// <param name="dto">Patch values. A media asset id of 0 removes the media attachment.</param>
        /// <param name="currentUserId">The requesting teacher id.</param>
        /// <returns>
        /// The updated question. Returns not found, forbidden, or conflict when the question cannot be changed.
        /// Replaced media assets are deleted after a successful update.
        /// </returns>
        Task<ServiceResult<QuizQuestionDto>> PatchQuizQuestionAsync(long id, PatchQuizQuestionDto dto, long currentUserId);

        /// <summary>
        /// Deletes a question and any media assets attached to the question or its options.
        /// </summary>
        /// <param name="id">The question id.</param>
        /// <param name="currentUserId">The requesting user id.</param>
        /// <param name="isAdmin">Whether to bypass teacher ownership checks.</param>
        /// <returns>
        /// A successful result when deleted. Returns not found, forbidden, or conflict when deletion would affect
        /// started/completed attempts or submitted responses.
        /// </returns>
        Task<ServiceResult<bool>> DeleteQuizQuestionAsync(long id, long currentUserId, bool isAdmin);
    }
}
