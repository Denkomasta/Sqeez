using Sqeez.Api.DTOs;
using Sqeez.Api.Models.Import;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines school class search, management, and student assignment operations.
    /// </summary>
    public interface ISchoolClassService
    {
        /// <summary>
        /// Gets school classes with paging and optional search, academic-year, and teacher filters.
        /// </summary>
        /// <param name="filter">Filtering and paging values.</param>
        /// <returns>A paged list of school class DTOs with teacher, student, and subject counts.</returns>
        Task<ServiceResult<PagedResponse<SchoolClassDto>>> GetAllClassesAsync(SchoolClassFilterDto filter);

        /// <summary>
        /// Gets detailed class information including teacher, students, and subjects.
        /// </summary>
        /// <param name="id">The class id.</param>
        /// <returns>The class detail DTO, or not found when the class does not exist.</returns>
        Task<ServiceResult<SchoolClassDetailDto>> GetClassByIdAsync(long id);

        /// <summary>
        /// Creates a school class and optionally assigns a teacher.
        /// </summary>
        /// <param name="dto">Class metadata and optional teacher id.</param>
        /// <returns>
        /// The created class DTO. Returns validation failed when a provided teacher id does not belong to a teacher.
        /// </returns>
        Task<ServiceResult<SchoolClassDto>> CreateClassAsync(CreateSchoolClassDto dto);

        /// <summary>
        /// Ensures a set of class names exists, creating missing classes with imported defaults.
        /// </summary>
        /// <param name="classNames">Class names to normalize, deduplicate, look up, and create when missing.</param>
        /// <returns>Created and existing class DTOs grouped in a bulk operation result.</returns>
        Task<ServiceResult<BulkOperationResult<SchoolClassDto>>> EnsureClassesExistAsync(IEnumerable<string> classNames);

        /// <summary>
        /// Patches class metadata and teacher assignment.
        /// </summary>
        /// <param name="id">The class id.</param>
        /// <param name="dto">Patch values. A teacher id of 0 removes the teacher assignment.</param>
        /// <returns>The updated class DTO, not found for a missing class, or validation failed for an invalid teacher id.</returns>
        Task<ServiceResult<SchoolClassDto>> PatchClassAsync(long id, PatchSchoolClassDto dto);

        /// <summary>
        /// Deletes a school class.
        /// </summary>
        /// <param name="id">The class id.</param>
        /// <returns>A successful result when deleted, not found for a missing class, or internal error on deletion failure.</returns>
        Task<ServiceResult<bool>> DeleteClassAsync(long id);

        /// <summary>
        /// Assigns multiple students to a class.
        /// </summary>
        /// <param name="classId">The target class id.</param>
        /// <param name="dto">Student ids to assign.</param>
        /// <returns>
        /// A successful result when all students are assigned. Returns not found for a missing class or validation
        /// failed when any provided student id does not exist.
        /// </returns>
        Task<ServiceResult<bool>> AssignStudentsToClassAsync(long classId, AssignStudentsDto dto);

        /// <summary>
        /// Removes multiple students from a class by clearing their class assignment.
        /// </summary>
        /// <param name="classId">The class id.</param>
        /// <param name="dto">Student ids to remove from the class.</param>
        /// <returns>
        /// A successful result when matching students are removed. Missing matching student assignments are treated
        /// as a no-op. Returns not found for a missing class.
        /// </returns>
        Task<ServiceResult<bool>> RemoveStudentsFromClassAsync(long classId, RemoveStudentsDto dto);
    }
}
