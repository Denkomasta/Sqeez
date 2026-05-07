using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    [Route("api/enrollments")]
    public class EnrollmentsController : ApiBaseController
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly ISubjectService _subjectService;

        public EnrollmentsController(IEnrollmentService enrollmentService, ISubjectService subjectService)
        {
            _enrollmentService = enrollmentService;
            _subjectService = subjectService;
        }

        /// <summary>
        /// GET /api/enrollments
        /// Global search for enrollments (useful for Admins or global gradebooks)
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<PagedResponse<EnrollmentDto>>> GetAllEnrollments([FromQuery] EnrollmentFilterDto filter)
        {
            var role = GetUserRoleFromClaims();
            if (role == "Student")
            {
                filter.StudentId = CurrentUserId;
            }
            else if (role == "Teacher")
            {
                if (!filter.SubjectId.HasValue)
                {
                    return Forbid();
                }

                var accessResult = await EnsureTeacherOwnsSubjectAsync(filter.SubjectId.Value);
                if (accessResult != null)
                {
                    return accessResult;
                }
            }

            var result = await _enrollmentService.GetAllEnrollmentsAsync(filter);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/enrollments/452
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<EnrollmentDto>> GetEnrollmentById(long id)
        {
            var result = await _enrollmentService.GetEnrollmentByIdAsync(id);
            if (!result.Success || result.Data == null)
            {
                return HandleServiceResult(result);
            }

            var role = GetUserRoleFromClaims();
            if (role == "Student" && result.Data.StudentId != CurrentUserId)
            {
                return Forbid();
            }

            if (role == "Teacher")
            {
                var accessResult = await EnsureTeacherOwnsSubjectAsync(result.Data.SubjectId);
                if (accessResult != null)
                {
                    return accessResult;
                }
            }

            return HandleServiceResult(result);
        }

        /// <summary>
        /// PATCH /api/enrollments/452
        /// Used by teachers to grade a student (update the Mark)
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
        /// Used by Admins to delete a specific enrollment record directly
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeleteEnrollment(long id)
        {
            var role = GetUserRoleFromClaims();
            if (role != "Admin")
            {
                var response = await _enrollmentService.GetEnrollmentByIdAsync(id);
                if (!response.Success || response.Data == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new
                    {
                        error = "Not found",
                        message = response.ErrorMessage
                    });
                }

                if (!IsIdLoggedUser(response.Data.StudentId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        error = "Forbidden",
                        message = "You do not have permission to delete students's enrollment."
                    });
                }
            }

            var result = await _enrollmentService.DeleteEnrollmentAsync(id);
            return HandleServiceResult(result);
        }

        private async Task<ActionResult?> EnsureTeacherOwnsSubjectAsync(long subjectId)
        {
            var subjectResult = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (!subjectResult.Success || subjectResult.Data == null)
            {
                return HandleServiceResult(subjectResult);
            }

            return subjectResult.Data.TeacherId == CurrentUserId ? null : Forbid();
        }
    }
}
