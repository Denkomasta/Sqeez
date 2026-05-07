using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines quiz search, retrieval, creation, patch, and delete operations.
    /// </summary>
    public interface IQuizService
    {
        /// <summary>
        /// Gets quizzes with paging and optional search, teacher, subject, student enrollment, activity, and date filters.
        /// </summary>
        /// <param name="filter">Filtering and paging values.</param>
        /// <returns>A paged list of quiz DTOs.</returns>
        Task<ServiceResult<PagedResponse<QuizDto>>> GetAllQuizzesAsync(QuizFilterDto filter);

        /// <summary>
        /// Gets a quiz by id and optionally counts attempts for a specific student.
        /// </summary>
        /// <param name="id">The quiz id.</param>
        /// <param name="dto">Optional student id used to scope attempt count.</param>
        /// <returns>The quiz DTO, or not found when the quiz does not exist.</returns>
        Task<ServiceResult<QuizDto>> GetQuizByIdAsync(long id, GetQuizDto dto);

        /// <summary>
        /// Creates a quiz in a subject owned by the current teacher.
        /// </summary>
        /// <param name="dto">Quiz metadata, subject id, retry limit, publish date, and closing date.</param>
        /// <param name="currentUserId">The teacher creating the quiz.</param>
        /// <returns>
        /// The created quiz. Returns not found for a missing subject, forbidden when the teacher does not own
        /// the subject or the subject is closed, and validation failed when the closing date exceeds the subject end date.
        /// </returns>
        Task<ServiceResult<QuizDto>> CreateQuizAsync(CreateQuizDto dto, long currentUserId);

        /// <summary>
        /// Patches a quiz owned by the current teacher, optionally moving it to another subject.
        /// </summary>
        /// <param name="id">The quiz id.</param>
        /// <param name="dto">Patch values.</param>
        /// <param name="currentUserId">The teacher modifying the quiz.</param>
        /// <returns>
        /// The updated quiz. Returns not found for a missing quiz or target subject, forbidden for non-owner
        /// teachers or closed target subjects, and validation failed when the resulting closing date exceeds
        /// the target subject end date.
        /// </returns>
        Task<ServiceResult<QuizDto>> PatchQuizAsync(long id, PatchQuizDto dto, long currentUserId);

        /// <summary>
        /// Deletes a quiz or closes it when attempts already exist.
        /// </summary>
        /// <param name="id">The quiz id.</param>
        /// <param name="currentUserId">The requesting user id.</param>
        /// <param name="isAdmin">Whether to bypass teacher ownership checks.</param>
        /// <returns>
        /// A successful result when deleted or soft-deleted by setting the closing date. Returns not found,
        /// forbidden for non-owner non-admin users, or forbidden when the subject has already ended.
        /// </returns>
        Task<ServiceResult<bool>> DeleteQuizAsync(long id, long currentUserId, bool isAdmin);
    }
}
