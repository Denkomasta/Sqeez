using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Manages school classes, class detail views, and student assignments.
    /// </summary>
    [Route("api/classes")]
    public class SchoolClassesController : ApiBaseController
    {
        private readonly ISchoolClassService _schoolClassService;

        public SchoolClassesController(ISchoolClassService schoolClassService)
        {
            _schoolClassService = schoolClassService;
        }

        /// <summary>
        /// Gets a paged list of school classes.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<PagedResponse<SchoolClassDto>>> GetAll([FromQuery] SchoolClassFilterDto filter)
        {
            var result = await _schoolClassService.GetAllClassesAsync(filter);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Gets class details including assigned teacher and students.
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<SchoolClassDetailDto>> GetById(long id)
        {
            var result = await _schoolClassService.GetClassByIdAsync(id);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Creates a school class. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<SchoolClassDto>> Create([FromBody] CreateSchoolClassDto dto)
        {
            var result = await _schoolClassService.CreateClassAsync(dto);

            if (result.Success && result.Data != null)
            {
                // 201 code
                return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result.Data);
            }

            return HandleServiceResult(result);
        }

        /// <summary>
        /// Updates class metadata and managed teacher assignment. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}")]
        public async Task<ActionResult<SchoolClassDto>> Patch(long id, [FromBody] PatchSchoolClassDto dto)
        {
            var result = await _schoolClassService.PatchClassAsync(id, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Deletes a school class when it can be safely removed. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> Delete(long id)
        {
            var result = await _schoolClassService.DeleteClassAsync(id);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Assigns students to a class while preventing teacher/student assignment conflicts. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/students")]
        public async Task<ActionResult<bool>> AssignStudents(long id, [FromBody] AssignStudentsDto dto)
        {
            var result = await _schoolClassService.AssignStudentsToClassAsync(id, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Removes students from a class. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/students/remove")]
        public async Task<ActionResult<bool>> RemoveStudents(long id, [FromBody] RemoveStudentsDto dto)
        {
            var result = await _schoolClassService.RemoveStudentsFromClassAsync(id, dto);
            return HandleServiceResult(result);
        }
    }
}
