using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Provides enrollment search, grade updates, and enrollment removal with role-based visibility rules.
    /// </summary>
    [Route("api/enrollments")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public class EnrollmentsController : ApiBaseController
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentsController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        /// <summary>
        /// GET /api/enrollments
        /// Searches enrollments. Students are limited to their own enrollments; teachers must filter by an owned subject.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<PagedResponse<EnrollmentDto>>> GetAllEnrollments([FromQuery] EnrollmentFilterDto filter)
        {
            var result = await _enrollmentService.GetAllEnrollmentsAsync(filter, CurrentUserId, GetUserRoleFromClaims());
            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/enrollments/452
        /// Gets a single enrollment when the requester is the student, the subject teacher, or an admin.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<EnrollmentDto>> GetEnrollmentById(long id)
        {
            var result = await _enrollmentService.GetEnrollmentByIdAsync(id, CurrentUserId, GetUserRoleFromClaims());
            return HandleServiceResult(result);
        }

        /// <summary>
        /// PATCH /api/enrollments/452
        /// Used by admins or subject teachers to grade a student by updating the mark.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPatch("{id}")]
        public async Task<ActionResult<EnrollmentDto>> PatchEnrollment(long id, [FromBody] PatchEnrollmentDto dto)
        {
            var result = await _enrollmentService.PatchEnrollmentAsync(id, dto, CurrentUserId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// DELETE /api/enrollments/452
        /// Deletes a specific enrollment. Admins can delete any enrollment; students can delete only their own.
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeleteEnrollment(long id)
        {
            var result = await _enrollmentService.DeleteEnrollmentAsync(id, CurrentUserId, GetUserRoleFromClaims());
            return HandleServiceResult(result);
        }
    }
}
