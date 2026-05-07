using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Manages badges, badge rules, and badge awards.
    /// </summary>
    [Authorize]
    [Route("api/badges")]
    public class BadgesController : ApiBaseController
    {
        private readonly IBadgeService _badgeService;

        public BadgesController(IBadgeService badgeService)
        {
            _badgeService = badgeService;
        }

        /// <summary>
        /// Creates a badge with optional icon upload and rule definitions. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<BadgeDto>> CreateBadge([FromForm] CreateBadgeDto dto)
        {
            var result = await _badgeService.CreateBadgeAsync(dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Updates a badge, optional icon, and rule definitions. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<BadgeDto>> PatchBadge(long id, [FromForm] UpdateBadgeDto dto)
        {
            var result = await _badgeService.UpdateBadgeAsync(id, dto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Deletes a badge and its icon when the badge is not currently awarded. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeleteBadge(long id)
        {
            var result = await _badgeService.DeleteBadgeAsync(id);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Awards a badge to a student. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{badgeId}/award/{studentId}")]
        public async Task<ActionResult<bool>> AwardBadge(long badgeId, long studentId)
        {
            var result = await _badgeService.AwardBadgeToStudentAsync(studentId, badgeId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Gets a paged list of badge definitions.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<BadgeDto>>> GetAllBadges([FromQuery] BadgeFilterDto filter)
        {
            var result = await _badgeService.GetAllBadgesAsync(filter);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Gets badges awarded to a student.
        /// </summary>
        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<StudentBadgeDto>>> GetStudentBadges(long studentId)
        {
            var result = await _badgeService.GetStudentBadgesAsync(studentId);
            return HandleServiceResult(result);
        }
    }
}
