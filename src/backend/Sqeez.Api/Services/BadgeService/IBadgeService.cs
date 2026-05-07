using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines badge management, rule evaluation, and badge award operations.
    /// </summary>
    public interface IBadgeService
    {
        /// <summary>
        /// Creates a badge, optionally uploads its icon, and stores all configured badge rules.
        /// </summary>
        /// <param name="dto">Badge metadata, optional icon file, XP bonus, and rule definitions.</param>
        /// <returns>The created badge DTO, or the file-storage failure when icon upload fails.</returns>
        Task<ServiceResult<BadgeDto>> CreateBadgeAsync(CreateBadgeDto dto);

        /// <summary>
        /// Updates badge metadata, replaces the icon when provided, and synchronizes the rule collection.
        /// </summary>
        /// <param name="id">The badge id.</param>
        /// <param name="dto">Patch values, optional replacement icon, and optional full rule set.</param>
        /// <returns>
        /// The updated badge DTO. Returns not found when the badge does not exist or propagates file-storage
        /// failures when a replacement icon cannot be uploaded.
        /// </returns>
        Task<ServiceResult<BadgeDto>> UpdateBadgeAsync(long id, UpdateBadgeDto dto);

        /// <summary>
        /// Deletes a badge and removes its icon file when deletion succeeds.
        /// </summary>
        /// <param name="id">The badge id.</param>
        /// <returns>
        /// A successful result when deleted. Returns not found for a missing badge or conflict when database
        /// constraints prevent deletion because the badge has already been awarded.
        /// </returns>
        Task<ServiceResult<bool>> DeleteBadgeAsync(long id);

        /// <summary>
        /// Gets badges with paging, text search, and optional earned/unearned filtering for a student.
        /// </summary>
        /// <param name="filter">Paging, search, student id, and earned-state filter values.</param>
        /// <returns>A paged list of badges and their rules.</returns>
        Task<ServiceResult<PagedResponse<BadgeDto>>> GetAllBadgesAsync(BadgeFilterDto filter);

        /// <summary>
        /// Gets badges earned by a student, ordered from newest to oldest.
        /// </summary>
        /// <param name="studentId">The student id.</param>
        /// <returns>The student's earned badge DTOs.</returns>
        Task<ServiceResult<IEnumerable<StudentBadgeDto>>> GetStudentBadgesAsync(long studentId);

        /// <summary>
        /// Awards a badge to a student and adds the badge XP bonus to the student's XP.
        /// </summary>
        /// <param name="studentId">The student id.</param>
        /// <param name="badgeId">The badge id.</param>
        /// <returns>
        /// Basic awarded-badge data. Returns not found for missing student or badge, and conflict when the
        /// student has already earned the badge.
        /// </returns>
        Task<ServiceResult<StudentBadgeBasicDto>> AwardBadgeToStudentAsync(long studentId, long badgeId);

        /// <summary>
        /// Evaluates all unearned rule-based badges for a student and awards every badge whose rules match.
        /// </summary>
        /// <param name="studentId">The student id.</param>
        /// <param name="metrics">Quiz or activity metrics used by badge rules.</param>
        /// <returns>
        /// The badges awarded during this evaluation. If no rules match, the list is empty. Matching badges add
        /// their XP bonuses to the student's XP.
        /// </returns>
        Task<ServiceResult<List<StudentBadgeBasicDto>>> EvaluateAndAwardBadgesAsync(long studentId, BadgeEvaluationMetrics metrics);
    }
}
