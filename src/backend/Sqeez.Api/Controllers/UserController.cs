using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ApiBaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<PagedResponse<StudentDto>>> GetAllUsers([FromQuery] UserFilterDto filter)
        {
            var result = await _userService.GetAllUsersAsync(filter);

            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<StudentDto>> GetUserById(long id)
        {
            var result = await _userService.GetUserByIdAsync(id);

            if (!result.Success) return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpGet("{id}/details")]
        [Authorize]
        public async Task<ActionResult<DetailedUserDto>> GetDetailedUserById(long id)
        {
            var result = await _userService.GetDetailedUserByIdAsync(id);

            if (!result.Success)
                return NotFound(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<StudentDto>> CreateUser([FromBody] CreateStudentDto dto)
        {
            var result = await _userService.CreateUserAsync(dto);

            if (!result.Success) return BadRequest(result.ErrorMessage);

            return CreatedAtAction(nameof(GetUserById), new { id = result.Data!.Id }, result.Data);
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<ActionResult<StudentDto>> PatchUser(long id, [FromBody] PatchStudentDto dto)
        {
            var role = GetUserRoleFromClaims();
            if (role != "Admin" && !IsIdLoggedUser(id))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    error = "Forbidden",
                    message = "You do not have permission to modify another student's profile."
                });
            }

            if (role != "Admin" && dto.SchoolClassId.HasValue)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    error = "Forbidden",
                    message = "You do not have permission to change school class assignment."
                });
            }

            if (role != "Admin" &&
                dto is PatchTeacherDto teacherDto &&
                (teacherDto.Department != null || teacherDto.ManagedClassId.HasValue))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    error = "Forbidden",
                    message = "You do not have permission to change teacher assignments."
                });
            }

            var result = await _userService.PatchUserAsync(id, dto);

            if (!result.Success) return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> ArchiveUser(long id)
        {
            var role = GetUserRoleFromClaims();
            if (role != "Admin" && !IsIdLoggedUser(id))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    error = "Forbidden",
                    message = "You do not have permission to modify another student's profile."
                });
            }

            var result = await _userService.ArchiveUserAsync(id);

            if (!result.Success) return NotFound(result.ErrorMessage);

            return NoContent();
        }

        [Authorize]
        [HttpPost("me/avatar")]
        public async Task<ActionResult<AvatarUploadResponseDto>> UploadAvatar(IFormFile file)
        {
            var userIdClaim = GetUserIdFromClaims();
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            long userId = long.Parse(userIdClaim);

            var result = await _userService.UploadAvatarAsync(userId, file);

            if (!result.Success)
            {
                return HandleServiceResult(result);
            }

            return Ok(new AvatarUploadResponseDto("Avatar updated successfully.", result.Data!));
        }
    }
}
