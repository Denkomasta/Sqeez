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
        /// Gets users with requester-aware email visibility.
        /// </summary>
        /// <param name="filter">Filtering, sorting, and paging values.</param>
        /// <param name="currentUserId">The authenticated user's id.</param>
        /// <param name="currentUserRole">The authenticated user's role.</param>
        /// <returns>A paged list of polymorphic student, teacher, and admin DTOs.</returns>
        Task<ServiceResult<PagedResponse<StudentDto>>> GetAllUsersAsync(UserFilterDto filter, long currentUserId, string? currentUserRole);

        /// <summary>
        /// Gets a user by id with requester-aware email visibility.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <param name="currentUserId">The authenticated user's id.</param>
        /// <param name="currentUserRole">The authenticated user's role.</param>
        /// <returns>The user DTO, or not found when the user does not exist.</returns>
        Task<ServiceResult<StudentDto>> GetUserByIdAsync(long id, long currentUserId, string? currentUserRole);

        /// <summary>
        /// Gets a detailed user profile with requester-aware email visibility.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <param name="currentUserId">The authenticated user's id.</param>
        /// <param name="currentUserRole">The authenticated user's role.</param>
        /// <returns>The detailed user DTO, or not found when the user does not exist.</returns>
        Task<ServiceResult<DetailedUserDto>> GetDetailedUserByIdAsync(long id, long currentUserId, string? currentUserRole);

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
        /// <param name="currentUserId">The authenticated user's id.</param>
        /// <param name="currentUserRole">The authenticated user's role.</param>
        /// <returns>
        /// The updated polymorphic user DTO. Returns not found when the user or provided class reference does not exist,
        /// validation failed when class/teacher ownership assignments conflict, or conflict for duplicate identity fields.
        /// </returns>
        Task<ServiceResult<StudentDto>> PatchUserAsync(long id, PatchStudentDto dto, long currentUserId, string? currentUserRole);

        /// <summary>
        /// Archives a user by setting the archive timestamp according to self, admin, and superadmin rules.
        /// </summary>
        /// <param name="id">The user id to archive.</param>
        /// <param name="currentUserId">The authenticated user's id.</param>
        /// <param name="currentUserRole">The authenticated user's role.</param>
        /// <returns>A successful result when archived, not found when a user does not exist, or forbidden when access is denied.</returns>
        Task<ServiceResult<bool>> ArchiveUserAsync(long id, long currentUserId, string? currentUserRole);

        /// <summary>
        /// Restores an archived user by clearing the archive timestamp according to admin and superadmin rules.
        /// </summary>
        /// <param name="id">The user id to restore.</param>
        /// <param name="currentUserId">The authenticated user's id.</param>
        /// <param name="currentUserRole">The authenticated user's role.</param>
        /// <returns>A successful result when restored, not found when a user does not exist, or forbidden when access is denied.</returns>
        Task<ServiceResult<bool>> RestoreUserAsync(long id, long currentUserId, string? currentUserRole);

        /// <summary>
        /// Permanently deletes an archived student or teacher and the user's dependent data.
        /// </summary>
        /// <param name="id">The archived student or teacher id to delete.</param>
        /// <param name="currentUserId">The authenticated user's id.</param>
        /// <param name="currentUserRole">The authenticated user's role.</param>
        /// <param name="replacementMediaOwnerId">Teacher/admin id that receives media owned by the deleted user.</param>
        /// <returns>
        /// A successful result when deleted, not found when a user does not exist, validation failed when the user
        /// is not archived, or forbidden when access is denied or the target is an admin.
        /// </returns>
        Task<ServiceResult<bool>> DeleteUserAsync(long id, long currentUserId, string? currentUserRole, long? replacementMediaOwnerId = null);

        /// <summary>
        /// Uploads and assigns a user's avatar image.
        /// </summary>
        /// <param name="currentUserId">The authenticated user's id.</param>
        /// <param name="imageFile">Avatar image file.</param>
        /// <param name="targetUserId">Optional user id whose avatar should be changed. Null changes the authenticated user's avatar.</param>
        /// <param name="currentUserRole">The authenticated user's role.</param>
        /// <returns>
        /// The new avatar URL. Returns validation failed for unsupported file extensions, not found for a missing user,
        /// forbidden when access is denied, or propagates storage failures. Existing avatars are deleted before the
        /// replacement upload is attempted.
        /// </returns>
        Task<ServiceResult<string>> UploadAvatarAsync(long currentUserId, IFormFile imageFile, long? targetUserId = null, string? currentUserRole = null);
    }
}
