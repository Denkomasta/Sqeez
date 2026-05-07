using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Gamification;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Implements badge CRUD, icon storage, rule evaluation, and student badge awards.
    /// </summary>
    public class BadgeService : BaseService<BadgeService>, IBadgeService
    {
        private readonly IFileStorageService _fileStorageService;

        public BadgeService(
            SqeezDbContext context,
            ILogger<BadgeService> logger,
            IFileStorageService fileStorageService) : base(context, logger)
        {
            _fileStorageService = fileStorageService;
        }

        public async Task<ServiceResult<BadgeDto>> CreateBadgeAsync(CreateBadgeDto dto)
        {
            string? iconUrl = null;
            if (dto.IconFile != null)
            {
                var uploadResult = await _fileStorageService.UploadFileAsync(dto.IconFile, "badges", isPublic: true);
                if (!uploadResult.Success)
                {
                    return ServiceResult<BadgeDto>.Failure(uploadResult.ErrorMessage ?? "Not specified", uploadResult.ErrorCode);
                }
                iconUrl = uploadResult.Data;
            }

            var badge = new Badge
            {
                Name = dto.Name,
                Description = dto.Description,
                IconUrl = iconUrl,
                XpBonus = dto.XpBonus,
                Rules = dto.Rules.Select(r => new BadgeRule
                {
                    Metric = r.Metric,
                    Operator = r.Operator,
                    TargetValue = r.TargetValue
                }).ToList()
            };

            _context.Badges.Add(badge);
            await _context.SaveChangesAsync();

            var ruleDtos = badge.Rules.Select(r => new BadgeRuleDto(r.Id, r.Metric, r.Operator, r.TargetValue)).ToList();

            return ServiceResult<BadgeDto>.Ok(new BadgeDto(
                badge.Id, badge.Name, badge.Description, badge.IconUrl, badge.XpBonus, ruleDtos));
        }

        public async Task<ServiceResult<BadgeDto>> UpdateBadgeAsync(long id, UpdateBadgeDto dto)
        {
            var badge = await _context.Badges
                .Include(b => b.Rules)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (badge == null) return ServiceResult<BadgeDto>.Failure("Badge not found.", ServiceError.NotFound);

            if (dto.Name != null) badge.Name = dto.Name;
            if (dto.Description != null) badge.Description = dto.Description;
            if (dto.XpBonus.HasValue) badge.XpBonus = dto.XpBonus.Value;

            if (dto.NewIconFile != null)
            {
                if (!string.IsNullOrEmpty(badge.IconUrl))
                {
                    await _fileStorageService.DeleteFileAsync(badge.IconUrl);
                }

                var uploadResult = await _fileStorageService.UploadFileAsync(dto.NewIconFile, "badges", isPublic: true);
                if (!uploadResult.Success)
                {
                    return ServiceResult<BadgeDto>.Failure(uploadResult.ErrorMessage ?? "Not specified", uploadResult.ErrorCode);
                }

                badge.IconUrl = uploadResult.Data!;
            }

            if (dto.Rules != null)
            {
                var incomingIds = dto.Rules
                    .Where(r => r.Id.HasValue && r.Id > 0)
                    .Select(r => r.Id!.Value)
                    .ToList();

                var rulesToRemove = badge.Rules.Where(r => !incomingIds.Contains(r.Id)).ToList();
                _context.BadgeRules.RemoveRange(rulesToRemove);

                foreach (var ruleDto in dto.Rules)
                {
                    if (!ruleDto.Id.HasValue || ruleDto.Id == 0)
                    {
                        badge.Rules.Add(new BadgeRule
                        {
                            Metric = ruleDto.Metric,
                            Operator = ruleDto.Operator,
                            TargetValue = ruleDto.TargetValue
                        });
                    }
                    else
                    {
                        var existingRule = badge.Rules.FirstOrDefault(r => r.Id == ruleDto.Id);
                        if (existingRule != null)
                        {
                            existingRule.Metric = ruleDto.Metric;
                            existingRule.Operator = ruleDto.Operator;
                            existingRule.TargetValue = ruleDto.TargetValue;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            var ruleDtos = badge.Rules.Select(r => new BadgeRuleDto(r.Id, r.Metric, r.Operator, r.TargetValue)).ToList();

            return ServiceResult<BadgeDto>.Ok(new BadgeDto(
                badge.Id, badge.Name, badge.Description, badge.IconUrl, badge.XpBonus, ruleDtos));
        }

        public async Task<ServiceResult<bool>> DeleteBadgeAsync(long id)
        {
            var badge = await _context.Badges.FindAsync(id);
            if (badge == null) return ServiceResult<bool>.Failure("Badge not found.", ServiceError.NotFound);

            string? iconUrlToDelete = badge.IconUrl;

            _context.Badges.Remove(badge);

            try
            {
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(iconUrlToDelete))
                {
                    await _fileStorageService.DeleteFileAsync(iconUrlToDelete);
                }

                return ServiceResult<bool>.Ok(true);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete Badge {Id} due to database constraints.", id);
                return ServiceResult<bool>.Failure(
                    "Cannot delete this badge because it has already been awarded to one or more students.",
                    ServiceError.Conflict);
            }
        }

        public async Task<ServiceResult<PagedResponse<BadgeDto>>> GetAllBadgesAsync(BadgeFilterDto filter)
        {
            var query = _context.Badges.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(b =>
                    b.Name.ToLower().Contains(searchTerm) ||
                    (b.Description != null && b.Description.ToLower().Contains(searchTerm)));
            }

            if (filter.StudentId.HasValue && filter.isEarned.HasValue)
            {
                if (filter.isEarned == true)
                {
                    query = query.Where(b => b.StudentBadges.Any(sb => sb.StudentId == filter.StudentId.Value));
                }
                else if (filter.isEarned == false)
                {
                    query = query.Where(b => !b.StudentBadges.Any(sb => sb.StudentId == filter.StudentId.Value));
                }
            }

            var totalCount = await query.CountAsync();

            var badges = await query
                .OrderBy(b => b.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new BadgeDto(
                    b.Id,
                    b.Name,
                    b.Description,
                    b.IconUrl,
                    b.XpBonus,
                    b.Rules.Select(r => new BadgeRuleDto(r.Id, r.Metric, r.Operator, r.TargetValue)).ToList()
                ))
                .ToListAsync();

            var pagedResponse = new PagedResponse<BadgeDto>
            {
                Data = badges,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ServiceResult<PagedResponse<BadgeDto>>.Ok(pagedResponse);
        }

        public async Task<ServiceResult<IEnumerable<StudentBadgeDto>>> GetStudentBadgesAsync(long studentId)
        {
            var earnedBadges = await _context.StudentBadges
                .Include(sb => sb.Badge)
                .Where(sb => sb.StudentId == studentId)
                .OrderByDescending(sb => sb.EarnedAt)
                .Select(sb => new StudentBadgeDto(
                    sb.BadgeId, sb.Badge.Name, sb.Badge.Description, sb.Badge.IconUrl, sb.Badge.XpBonus, sb.EarnedAt))
                .ToListAsync();

            return ServiceResult<IEnumerable<StudentBadgeDto>>.Ok(earnedBadges);
        }

        public async Task<ServiceResult<StudentBadgeBasicDto>> AwardBadgeToStudentAsync(long studentId, long badgeId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return ServiceResult<StudentBadgeBasicDto>.Failure("Student not found.", ServiceError.NotFound);

            var badge = await _context.Badges.FindAsync(badgeId);
            if (badge == null) return ServiceResult<StudentBadgeBasicDto>.Failure("Badge not found.", ServiceError.NotFound);

            bool alreadyEarned = await _context.StudentBadges
                .AnyAsync(sb => sb.StudentId == studentId && sb.BadgeId == badgeId);

            if (alreadyEarned)
                return ServiceResult<StudentBadgeBasicDto>.Failure("Student has already earned this badge.", ServiceError.Conflict);

            var studentBadge = new StudentBadge
            {
                StudentId = studentId,
                BadgeId = badgeId,
                EarnedAt = DateTime.UtcNow
            };

            _context.StudentBadges.Add(studentBadge);
            student.CurrentXP += badge.XpBonus;

            await _context.SaveChangesAsync();

            var resultDto = new StudentBadgeBasicDto
            {
                BadgeId = badge.Id,
                Name = badge.Name,
                IconUrl = badge.IconUrl,
                EarnedAt = studentBadge.EarnedAt
            };

            return ServiceResult<StudentBadgeBasicDto>.Ok(resultDto);
        }

        public async Task<ServiceResult<List<StudentBadgeBasicDto>>> EvaluateAndAwardBadgesAsync(long studentId, BadgeEvaluationMetrics metrics)
        {
            var earnedBadgeIds = await _context.StudentBadges
                .Where(sb => sb.StudentId == studentId)
                .Select(sb => sb.BadgeId)
                .ToListAsync();

            var availableBadges = await _context.Badges
                .Include(b => b.Rules)
                .Where(b => !earnedBadgeIds.Contains(b.Id))
                .ToListAsync();

            var newlyAwardedDtos = new List<StudentBadgeBasicDto>();
            var studentBadgesToInsert = new List<StudentBadge>();
            int totalXpBonus = 0;

            foreach (var badge in availableBadges)
            {
                if (!badge.Rules.Any()) continue;

                bool meetsAllRules = true;

                foreach (var rule in badge.Rules)
                {
                    decimal metricValue = rule.Metric switch
                    {
                        BadgeMetric.ScorePercentage => metrics.ScorePercentage,
                        BadgeMetric.TotalScore => metrics.TotalScore,
                        _ => -1
                    };

                    if (metricValue == -1) { meetsAllRules = false; break; }

                    bool ruleMet = rule.Operator switch
                    {
                        BadgeOperator.Equals => metricValue == rule.TargetValue,
                        BadgeOperator.GreaterThan => metricValue > rule.TargetValue,
                        BadgeOperator.GreaterThanOrEqual => metricValue >= rule.TargetValue,
                        BadgeOperator.LessThan => metricValue < rule.TargetValue,
                        BadgeOperator.LessThanOrEqual => metricValue <= rule.TargetValue,
                        _ => false
                    };

                    if (!ruleMet) { meetsAllRules = false; break; }
                }

                if (meetsAllRules)
                {
                    var earnedAt = DateTime.UtcNow;

                    studentBadgesToInsert.Add(new StudentBadge
                    {
                        StudentId = studentId,
                        BadgeId = badge.Id,
                        EarnedAt = earnedAt
                    });

                    totalXpBonus += badge.XpBonus;

                    newlyAwardedDtos.Add(new StudentBadgeBasicDto
                    {
                        BadgeId = badge.Id,
                        Name = badge.Name,
                        IconUrl = badge.IconUrl,
                        EarnedAt = earnedAt
                    });
                }
            }

            if (studentBadgesToInsert.Any())
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student != null)
                {
                    _context.StudentBadges.AddRange(studentBadgesToInsert);

                    student.CurrentXP += totalXpBonus;

                    await _context.SaveChangesAsync();
                }
            }

            return ServiceResult<List<StudentBadgeBasicDto>>.Ok(newlyAwardedDtos);
        }
    }
}
