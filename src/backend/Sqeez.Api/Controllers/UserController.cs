using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Exposes user search, profile details, administrative user management, and avatar upload endpoints.
    /// </summary>
    [Route("api/users")]
    [ApiController]
    public class UserController : ApiBaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Gets a paged list of users using the supplied filters. Any authenticated user may search users.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<PagedResponse<StudentDto>>> GetAllUsers([FromQuery] UserFilterDto filter)
        {
            var result = await _userService.GetAllUsersAsync(filter, CurrentUserId, GetUserRoleFromClaims());

            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        /// <summary>
        /// Gets a lightweight user profile. Any authenticated user may read the profile.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<StudentDto>> GetUserById(long id)
        {
            var result = await _userService.GetUserByIdAsync(id, CurrentUserId, GetUserRoleFromClaims());

            if (!result.Success) return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }

        /// <summary>
        /// Gets a detailed user profile with class, enrollment, and badge data. Any authenticated user may read it.
        /// </summary>
        [HttpGet("{id}/details")]
        [Authorize]
        public async Task<ActionResult<DetailedUserDto>> GetDetailedUserById(long id)
        {
            var result = await _userService.GetDetailedUserByIdAsync(id, CurrentUserId, GetUserRoleFromClaims());

            if (!result.Success)
                return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }

        /// <summary>
        /// Creates a student, teacher, or admin account. Admin-only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<StudentDto>> CreateUser([FromBody] CreateStudentDto dto)
        {
            var result = await _userService.CreateUserAsync(dto);

            if (!result.Success) return BadRequest(result.ErrorMessage);

            return CreatedAtAction(nameof(GetUserById), new { id = result.Data!.Id }, result.Data);
        }

        /// <summary>
        /// Updates a user profile. Users can update their own basic fields; admins can update assignments and role data.
        /// </summary>
        [HttpPatch("{id}")]
        [Authorize]
        public async Task<ActionResult<StudentDto>> PatchUser(long id, [FromBody] PatchStudentDto dto)
        {
            var role = GetUserRoleFromClaims();
            var result = await _userService.PatchUserAsync(id, dto, CurrentUserId, role);

            if (!result.Success) return HandleServiceResult(result);

            return Ok(result.Data);
        }

        /// <summary>
        /// Archives a user. Users can archive themselves; admins can archive non-admin users; the superadmin can archive admins too.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> ArchiveUser(long id)
        {
            var result = await _userService.ArchiveUserAsync(id, CurrentUserId, GetUserRoleFromClaims());

            if (!result.Success) return HandleServiceResult(result);

            return NoContent();
        }

        /// <summary>
        /// Restores an archived user. Admins can restore non-admin users; the superadmin can restore admins too.
        /// </summary>
        [HttpPatch("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RestoreUser(long id)
        {
            var result = await _userService.RestoreUserAsync(id, CurrentUserId, GetUserRoleFromClaims());

            if (!result.Success) return HandleServiceResult(result);

            return NoContent();
        }

        /// <summary>
        /// Permanently deletes an archived student or teacher. Admin targets must be changed to teacher first.
        /// If the deleted user owns media assets, provide a replacement teacher/admin owner id.
        /// </summary>
        [HttpDelete("{id}/hard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(long id, [FromQuery] long? replacementMediaOwnerId = null)
        {
            var result = await _userService.DeleteUserAsync(id, CurrentUserId, GetUserRoleFromClaims(), replacementMediaOwnerId);

            if (!result.Success) return HandleServiceResult(result);

            return NoContent();
        }

        /// <summary>
        /// Uploads and replaces a user's avatar image. Without targetUserId, the current user's avatar is changed.
        /// </summary>
        [Authorize]
        [HttpPost("me/avatar")]
        public async Task<ActionResult<AvatarUploadResponseDto>> UploadAvatar(IFormFile file, [FromQuery] long? targetUserId = null)
        {
            var userIdClaim = GetUserIdFromClaims();
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            long userId = long.Parse(userIdClaim);

            var result = await _userService.UploadAvatarAsync(userId, file, targetUserId, GetUserRoleFromClaims());

            if (!result.Success)
            {
                return HandleServiceResult(result);
            }

            return Ok(new AvatarUploadResponseDto("Avatar updated successfully.", result.Data!));
        }
    }
}
