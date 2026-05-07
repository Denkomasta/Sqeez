using Sqeez.Api.DTOs;
using Sqeez.Api.Models.Import;
using Sqeez.Api.Models.Users;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines user search, profile retrieval, account management, and avatar operations.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Gets users with paging, role, search, online, class, subject, archive, department, phone, and assignment filters.
        /// </summary>
        /// <param name="filter">Filtering, sorting, and paging values.</param>
        /// <returns>A paged list of polymorphic student, teacher, and admin DTOs.</returns>
        Task<ServiceResult<PagedResponse<StudentDto>>> GetAllUsersAsync(UserFilterDto filter);

        /// <summary>
        /// Gets a user by id as the appropriate polymorphic DTO.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns>The user DTO, or not found when the user does not exist.</returns>
        Task<ServiceResult<StudentDto>> GetUserByIdAsync(long id);

        /// <summary>
        /// Gets a detailed user profile with class details, enrollments, and recent badges.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns>The detailed user DTO, or not found when the user does not exist.</returns>
        Task<ServiceResult<DetailedUserDto>> GetDetailedUserByIdAsync(long id);

        /// <summary>
        /// Creates a student, teacher, or admin based on the concrete create DTO type.
        /// </summary>
        /// <param name="dto">User creation data.</param>
        /// <returns>
        /// The created polymorphic user DTO. Returns conflict for duplicate email or username, or not found when a
        /// provided school class does not exist.
        /// </returns>
        Task<ServiceResult<StudentDto>> CreateUserAsync(CreateStudentDto dto);

        /// <summary>
        /// Creates multiple students while skipping duplicate emails or usernames.
        /// </summary>
        /// <param name="students">Prepared student entities to insert.</param>
        /// <returns>Created students plus skipped-record messages for duplicates.</returns>
        Task<ServiceResult<BulkOperationResult<StudentDto>>> CreateStudentsBulkAsync(IEnumerable<Student> students);

        /// <summary>
        /// Patches base user data and role-specific teacher/admin fields.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <param name="dto">Patch values. Class ids of 0 remove the relevant class assignment.</param>
        /// <returns>
        /// The updated polymorphic user DTO. Returns not found when the user or provided class reference does not exist,
        /// validation failed when class/teacher ownership assignments conflict, or conflict for duplicate identity fields.
        /// </returns>
        Task<ServiceResult<StudentDto>> PatchUserAsync(long id, PatchStudentDto dto);

        /// <summary>
        /// Archives a user by setting the archive timestamp.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns>A successful result when archived, or not found when the user does not exist.</returns>
        Task<ServiceResult<bool>> ArchiveUserAsync(long id);

        /// <summary>
        /// Uploads and assigns a user's avatar image.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="imageFile">Avatar image file.</param>
        /// <returns>
        /// The new avatar URL. Returns validation failed for unsupported file extensions, not found for a missing user,
        /// or propagates storage failures. Existing avatars are deleted before the replacement upload is attempted.
        /// </returns>
        Task<ServiceResult<string>> UploadAvatarAsync(long userId, IFormFile imageFile);
    }
}
