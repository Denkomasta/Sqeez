using Sqeez.Api.DTOs;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Import;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines subject search, management, and assignment operations.
    /// </summary>
    public interface ISubjectService
    {
        /// <summary>
        /// Gets subjects with paging and optional activity, teacher, student availability, and text-search filters.
        /// </summary>
        /// <param name="filter">Filtering, sorting, and paging values.</param>
        /// <returns>A paged list of subject DTOs with teacher, class, enrollment, and quiz counts.</returns>
        Task<ServiceResult<PagedResponse<SubjectDto>>> GetAllSubjectsAsync(SubjectFilterDto filter);

        /// <summary>
        /// Gets a single subject by id.
        /// </summary>
        /// <param name="id">The subject id.</param>
        /// <returns>The subject DTO, or not found when the subject does not exist.</returns>
        Task<ServiceResult<SubjectDto>> GetSubjectByIdAsync(long id);

        /// <summary>
        /// Creates a subject and optionally assigns a teacher and school class.
        /// </summary>
        /// <param name="dto">Subject metadata, optional dates, teacher id, and class id.</param>
        /// <returns>
        /// The created subject. Returns validation failed when a provided teacher or class id is invalid.
        /// </returns>
        Task<ServiceResult<SubjectDto>> CreateSubjectAsync(CreateSubjectDto dto);

        /// <summary>
        /// Creates multiple subjects while skipping duplicate subject codes.
        /// </summary>
        /// <param name="subjects">Subjects prepared for insertion.</param>
        /// <returns>Created subjects plus skipped-record messages for duplicate codes.</returns>
        Task<ServiceResult<BulkOperationResult<SubjectDto>>> CreateSubjectsBulkAsync(IEnumerable<Subject> subjects);

        /// <summary>
        /// Patches subject metadata, dates, teacher assignment, and class assignment.
        /// </summary>
        /// <param name="id">The subject id.</param>
        /// <param name="dto">Patch values. Teacher id or class id of 0 removes the assignment.</param>
        /// <returns>
        /// The updated subject. Returns not found for a missing subject, validation failed for invalid teacher
        /// or class ids, and forbidden when assigning a teacher who is already enrolled as a student in the subject.
        /// </returns>
        Task<ServiceResult<SubjectDto>> PatchSubjectAsync(long id, PatchSubjectDto dto);

        /// <summary>
        /// Deletes an empty subject or archives it when enrollments or quizzes must be preserved.
        /// </summary>
        /// <param name="id">The subject id.</param>
        /// <returns>A successful result when deleted or archived, not found for a missing subject, or internal error on failure.</returns>
        Task<ServiceResult<bool>> DeleteSubjectAsync(long id);
    }
}
