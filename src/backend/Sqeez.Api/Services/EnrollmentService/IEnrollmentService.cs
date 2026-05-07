using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines subject enrollment search, enrollment mutation, grading, and cleanup operations.
    /// </summary>
    public interface IEnrollmentService
    {
        /// <summary>
        /// Gets enrollments with paging and optional student, subject, mark, and active/archive filters.
        /// </summary>
        /// <param name="filter">Filtering, sorting, and paging values.</param>
        /// <returns>A paged list of enrollment DTOs.</returns>
        Task<ServiceResult<PagedResponse<EnrollmentDto>>> GetAllEnrollmentsAsync(EnrollmentFilterDto filter);

        /// <summary>
        /// Gets a single enrollment with student, subject, and quiz-attempt count details.
        /// </summary>
        /// <param name="id">The enrollment id.</param>
        /// <returns>The enrollment DTO, or not found when the enrollment does not exist.</returns>
        Task<ServiceResult<EnrollmentDto>> GetEnrollmentByIdAsync(long id);

        /// <summary>
        /// Updates an enrollment mark when requested by the subject's teacher.
        /// </summary>
        /// <param name="id">The enrollment id.</param>
        /// <param name="enrollment">Mark patch data, including optional mark removal.</param>
        /// <param name="currentUserId">The teacher attempting the update.</param>
        /// <returns>
        /// The updated enrollment. Returns not found for a missing enrollment, forbidden when the caller is not
        /// the subject teacher, or validation failed when the mark is outside the supported range.
        /// </returns>
        Task<ServiceResult<EnrollmentDto>> PatchEnrollmentAsync(long id, PatchEnrollmentDto enrollment, long currentUserId);

        /// <summary>
        /// Removes an enrollment, archiving it instead when quiz attempts must be preserved.
        /// </summary>
        /// <param name="id">The enrollment id.</param>
        /// <returns>A successful result when removed or archived, or not found when the enrollment does not exist.</returns>
        Task<ServiceResult<bool>> DeleteEnrollmentAsync(long id);

        /// <summary>
        /// Enrolls multiple students in a subject.
        /// </summary>
        /// <param name="subjectId">The subject id.</param>
        /// <param name="dto">Student ids to enroll.</param>
        /// <returns>
        /// Newly enrolled and already enrolled student ids. Returns not found for a missing subject, forbidden
        /// when the subject is closed, or validation failed for invalid student ids or attempts to enroll the
        /// subject teacher as a student.
        /// </returns>
        Task<ServiceResult<BulkEnrollmentResultDto>> EnrollStudentsInSubjectAsync(long subjectId, AssignStudentsDto dto);

        /// <summary>
        /// Unenrolls multiple students from a subject, archiving enrollments that have quiz attempts.
        /// </summary>
        /// <param name="subjectId">The subject id.</param>
        /// <param name="dto">Student ids to unenroll.</param>
        /// <returns>A successful result. Missing matching active enrollments are treated as a no-op.</returns>
        Task<ServiceResult<bool>> UnenrollStudentsFromSubjectAsync(long subjectId, RemoveStudentsDto dto);
    }
}
