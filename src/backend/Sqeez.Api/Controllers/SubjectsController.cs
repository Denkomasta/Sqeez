using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    [Authorize]
    [Route("api/subjects")]
    public class SubjectsController : ApiBaseController
    {
        private readonly ISubjectService _subjectService;
        private readonly IEnrollmentService _enrollmentService;
        private readonly IQuizService _quizService;

        public SubjectsController(
            ISubjectService subjectService,
            IEnrollmentService enrollmentService,
             IQuizService quizService
            )
        {
            _subjectService = subjectService;
            _enrollmentService = enrollmentService;
            _quizService = quizService;
        }

        /// <summary>
        /// GET /api/subjects
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<SubjectDto>>> GetAllSubjects([FromQuery] SubjectFilterDto filter)
        {
            var result = await _subjectService.GetAllSubjectsAsync(filter);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/subjects/5
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SubjectDto>> GetSubjectById(long id)
        {
            var result = await _subjectService.GetSubjectByIdAsync(id);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// POST /api/subjects
        /// </summary>
        [Authorize(Roles = "Admin")] // Only staff can create subjects
        [HttpPost]
        public async Task<ActionResult<SubjectDto>> CreateSubject([FromBody] CreateSubjectDto dto)
        {
            var result = await _subjectService.CreateSubjectAsync(dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// PATCH /api/subjects/5
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}")]
        public async Task<ActionResult<SubjectDto>> PatchSubject(long id, [FromBody] PatchSubjectDto dto)
        {
            var result = await _subjectService.PatchSubjectAsync(id, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// DELETE /api/subjects/5
        /// Performs a Smart Delete (Hard delete if empty, Soft delete if active)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeleteSubject(long id)
        {
            var result = await _subjectService.DeleteSubjectAsync(id);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/subjects/5/enrollments
        /// </summary>
        [HttpGet("{subjectId}/enrollments")]
        public async Task<ActionResult<PagedResponse<EnrollmentDto>>> GetEnrollmentsForSubject(long subjectId, [FromQuery] EnrollmentFilterDto filter)
        {
            var role = GetUserRoleFromClaims();
            if (role == "Teacher")
            {
                var accessResult = await EnsureTeacherOwnsSubjectAsync(subjectId);
                if (accessResult != null)
                {
                    return accessResult;
                }
            }
            else if (role == "Student")
            {
                filter.StudentId = CurrentUserId;
            }

            // Force the filter to only look at this specific subject
            filter.SubjectId = subjectId;
            var result = await _enrollmentService.GetAllEnrollmentsAsync(filter);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// POST /api/subjects/5/enrollments
        /// </summary>
        [Authorize]
        [HttpPost("{subjectId}/enrollments")]
        public async Task<ActionResult<BulkEnrollmentResultDto>> EnrollStudents(long subjectId, [FromBody] AssignStudentsDto dto)
        {
            var role = GetUserRoleFromClaims();
            var claimedId = GetUserIdFromClaims();

            if (role == "Admin" || (long.TryParse(claimedId, out long userId) && dto.StudentIds.Count == 1 && userId == dto.StudentIds[0]))
            {
                var result = await _enrollmentService.EnrollStudentsInSubjectAsync(subjectId, dto);
                return HandleServiceResult(result);
            }

            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "Forbidden",
                message = "You do not have permission to modify another student's profile."
            });
        }

        /// <summary>
        /// DELETE /api/subjects/5/enrollments
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpDelete("{subjectId}/enrollments")]
        public async Task<ActionResult<bool>> UnenrollStudents(long subjectId, [FromBody] RemoveStudentsDto dto)
        {
            if (!IsCurrentUserAdmin)
            {
                var accessResult = await EnsureTeacherOwnsSubjectAsync(subjectId);
                if (accessResult != null)
                {
                    return accessResult;
                }
            }

            var result = await _enrollmentService.UnenrollStudentsFromSubjectAsync(subjectId, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// GET /api/subjects/5/quizzes
        /// </summary>
        [HttpGet("{subjectId}/quizzes")]
        public async Task<ActionResult<PagedResponse<QuizDto>>> GetQuizzesForSubject(long subjectId, [FromQuery] QuizFilterDto filter)
        {
            filter.SubjectId = subjectId; // Force the filter to this subject
            var result = await _quizService.GetAllQuizzesAsync(filter);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// POST /api/subjects/5/quizzes
        /// Creates a new quiz attached to this subject.
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost("{subjectId}/quizzes")]
        public async Task<ActionResult<QuizDto>> AddQuizToSubject(long subjectId, [FromBody] CreateQuizDto dto)
        {
            // Force the subjectId from the route into the DTO
            var safeDto = dto with { SubjectId = subjectId };
            var result = await _quizService.CreateQuizAsync(safeDto, CurrentUserId);
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
