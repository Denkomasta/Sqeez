using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines quiz option management operations.
    /// </summary>
    public interface IQuizOptionService
    {
        /// <summary>
        /// Gets quiz options with paging and optional question, correctness, free-text, media, and search filters.
        /// </summary>
        /// <param name="filter">Filtering and paging values.</param>
        /// <param name="currentUserId">The requesting user id.</param>
        /// <param name="isAdmin">Whether to bypass teacher ownership filtering.</param>
        /// <returns>A paged list of options visible to the requester.</returns>
        Task<ServiceResult<PagedResponse<QuizOptionDto>>> GetAllQuizOptionsAsync(QuizOptionFilterDto filter, long currentUserId, bool isAdmin);

        /// <summary>
        /// Gets a single quiz option for a teacher-owned subject.
        /// </summary>
        /// <param name="id">The option id.</param>
        /// <param name="currentUserId">The requesting teacher id.</param>
        /// <returns>The option DTO, not found for a missing option, or forbidden when the teacher does not own the subject.</returns>
        Task<ServiceResult<QuizOptionDto>> GetQuizOptionByIdAsync(long id, long currentUserId);

        /// <summary>
        /// Creates a quiz option on a teacher-owned question.
        /// </summary>
        /// <param name="dto">Option content, correctness, free-text flag, question id, and optional media id.</param>
        /// <param name="currentUserId">The requesting teacher id.</param>
        /// <returns>
        /// The created option. Returns not found for missing question or media, forbidden for non-owner teachers
        /// or ended subjects, conflict when attempts already exist, or validation failed when the question already
        /// has the maximum number of options.
        /// </returns>
        Task<ServiceResult<QuizOptionDto>> CreateQuizOptionAsync(CreateQuizOptionDto dto, long currentUserId);

        /// <summary>
        /// Patches a quiz option on a teacher-owned question.
        /// </summary>
        /// <param name="id">The option id.</param>
        /// <param name="dto">Patch values. A media asset id of 0 removes the media attachment.</param>
        /// <param name="currentUserId">The requesting teacher id.</param>
        /// <returns>
        /// The updated option. Returns not found, forbidden, or conflict when the option cannot be changed.
        /// Replaced media assets are deleted after a successful update.
        /// </returns>
        Task<ServiceResult<QuizOptionDto>> PatchQuizOptionAsync(long id, PatchQuizOptionDto dto, long currentUserId);

        /// <summary>
        /// Deletes a quiz option and any media asset attached to it.
        /// </summary>
        /// <param name="id">The option id.</param>
        /// <param name="currentUserId">The requesting user id.</param>
        /// <param name="isAdmin">Whether to bypass teacher ownership checks.</param>
        /// <returns>
        /// A successful result when deleted. Returns not found, forbidden, or conflict when deletion would affect
        /// existing attempts or responses.
        /// </returns>
        Task<ServiceResult<bool>> DeleteQuizOptionAsync(long id, long currentUserId, bool isAdmin);
    }
}
