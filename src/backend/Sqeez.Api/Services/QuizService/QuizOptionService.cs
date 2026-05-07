using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Constants;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Implements quiz option management and ownership checks.
    /// </summary>
    public class QuizOptionService : BaseService<QuizOptionService>, IQuizOptionService
    {
        private readonly IMediaAssetService _mediaAssetService;
        public QuizOptionService(SqeezDbContext context, ILogger<QuizOptionService> logger, IMediaAssetService mas) : base(context, logger)
        {
            _mediaAssetService = mas;
        }

        public async Task<ServiceResult<PagedResponse<QuizOptionDto>>> GetAllQuizOptionsAsync(QuizOptionFilterDto filter, long currentUserId, bool isAdmin)
        {
            var query = _context.QuizOptions.AsNoTracking();

            // Only return the teacher's own options
            if (!isAdmin)
            {
                query = query.Where(o => o.QuizQuestion.Quiz.Subject.TeacherId == currentUserId);
            }

            if (filter.QuizQuestionId.HasValue)
            {
                query = query.Where(o => o.QuizQuestionId == filter.QuizQuestionId.Value);
            }

            if (filter.IsFreeText.HasValue)
            {
                query = query.Where(o => o.IsFreeText == filter.IsFreeText.Value);
            }

            if (filter.IsCorrect.HasValue)
            {
                query = query.Where(o => o.IsCorrect == filter.IsCorrect.Value);
            }

            if (filter.MediaAssetId.HasValue)
            {
                query = query.Where(o => o.MediaAssetId == filter.MediaAssetId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.Trim().ToLower();
                query = query.Where(o => o.Text != null && o.Text.ToLower().Contains(search));
            }

            int totalCount = await query.CountAsync();

            var options = await query
                .OrderBy(o => o.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(o => new QuizOptionDto(
                    o.Id,
                    o.Text,
                    o.IsFreeText,
                    o.IsCorrect,
                    o.QuizQuestionId,
                    o.MediaAssetId,
                    o.Responses.Count
                ))
                .ToListAsync();

            return ServiceResult<PagedResponse<QuizOptionDto>>.Ok(new PagedResponse<QuizOptionDto>
            {
                Data = options,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            });
        }

        public async Task<ServiceResult<QuizOptionDto>> GetQuizOptionByIdAsync(long id, long currentUserId)
        {
            var option = await _context.QuizOptions
                .Include(o => o.Responses)
                .Include(o => o.QuizQuestion)
                    .ThenInclude(q => q.Quiz)
                        .ThenInclude(quiz => quiz.Subject)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (option == null)
                return ServiceResult<QuizOptionDto>.Failure("Quiz option not found.", ServiceError.NotFound);

            if (option.QuizQuestion.Quiz.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<QuizOptionDto>.Failure("You do not have permission to view options for this subject.", ServiceError.Forbidden);
            }

            return ServiceResult<QuizOptionDto>.Ok(new QuizOptionDto(
                option.Id, option.Text, option.IsFreeText, option.IsCorrect,
                option.QuizQuestionId, option.MediaAssetId, option.Responses.Count));
        }

        public async Task<ServiceResult<QuizOptionDto>> CreateQuizOptionAsync(CreateQuizOptionDto dto, long currentUserId)
        {
            var question = await _context.QuizQuestions
                .Include(q => q.Options)
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Subject)
                .FirstOrDefaultAsync(q => q.Id == dto.QuizQuestionID);

            if (question == null)
                return ServiceResult<QuizOptionDto>.Failure("The specified Quiz Question does not exist.", ServiceError.NotFound);

            if (question.Quiz.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<QuizOptionDto>.Failure("You do not have permission to modify options in this quiz.", ServiceError.Forbidden);
            }

            // Prevent adding options if the subject has officially ended
            if (question.Quiz.Subject.HasEnded)
            {
                return ServiceResult<QuizOptionDto>.Failure(
                    "Cannot add options to a question that belongs to a subject that has already ended.",
                    ServiceError.Forbidden);
            }

            bool hasAttempts = await _context.QuizAttempts.AnyAsync(a => a.QuizId == question.QuizId);
            if (hasAttempts)
            {
                return ServiceResult<QuizOptionDto>.Failure(
                    "Cannot add new options because students have already started or completed this quiz.",
                    ServiceError.Conflict);
            }

            if (question.Options.Count >= QuizConstants.MaxOptionsPerQuestion)
                return ServiceResult<QuizOptionDto>.Failure(
                    $"A quiz question can have a maximum of {QuizConstants.MaxOptionsPerQuestion} options.",
                    ServiceError.ValidationFailed);

            if (dto.MediaAssetId.HasValue)
            {
                var mediaExists = await _context.MediaAssets.AnyAsync(m => m.Id == dto.MediaAssetId.Value);
                if (!mediaExists)
                    return ServiceResult<QuizOptionDto>.Failure("The specified Media Asset does not exist.", ServiceError.NotFound);
            }

            var option = new QuizOption
            {
                Text = dto.Text,
                IsFreeText = dto.IsFreeText,
                IsCorrect = dto.IsCorrect,
                QuizQuestionId = dto.QuizQuestionID,
                MediaAssetId = dto.MediaAssetId
            };

            _context.QuizOptions.Add(option);
            await _context.SaveChangesAsync();

            return ServiceResult<QuizOptionDto>.Ok(new QuizOptionDto(
                option.Id,
                option.Text,
                option.IsFreeText,
                option.IsCorrect,
                option.QuizQuestionId,
                option.MediaAssetId,
                0)); // 0 responses upon creation
        }

        public async Task<ServiceResult<QuizOptionDto>> PatchQuizOptionAsync(long id, PatchQuizOptionDto dto, long currentUserId)
        {
            var option = await _context.QuizOptions
                .Include(o => o.Responses)
                .Include(o => o.QuizQuestion)
                    .ThenInclude(q => q.Quiz)
                        .ThenInclude(quiz => quiz.Subject)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (option == null)
                return ServiceResult<QuizOptionDto>.Failure("Quiz option not found.", ServiceError.NotFound);

            if (option.QuizQuestion.Quiz.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<QuizOptionDto>.Failure("You do not have permission to modify options in this quiz.", ServiceError.Forbidden);
            }

            // Prevent modifying the option if the subject has officially ended
            if (option.QuizQuestion.Quiz.Subject.HasEnded)
            {
                return ServiceResult<QuizOptionDto>.Failure(
                    "Cannot modify an option that belongs to a subject that has already ended.",
                    ServiceError.Forbidden);
            }

            bool hasAttempts = await _context.QuizAttempts.AnyAsync(a => a.QuizId == option.QuizQuestion.QuizId);
            if (hasAttempts)
            {
                return ServiceResult<QuizOptionDto>.Failure(
                    "Cannot modify this option because students have already started or completed this quiz.",
                    ServiceError.Conflict);
            }

            if (dto.Text != null) option.Text = dto.Text;
            if (dto.IsFreeText.HasValue) option.IsFreeText = dto.IsFreeText.Value;
            if (dto.IsCorrect.HasValue) option.IsCorrect = dto.IsCorrect.Value;

            long? oldMediaAssetId = null;

            if (dto.MediaAssetId.HasValue)
            {
                if (dto.MediaAssetId.Value == 0)
                {
                    if (option.MediaAssetId != null)
                    {
                        oldMediaAssetId = option.MediaAssetId;
                        option.MediaAssetId = null;
                    }
                }
                else if (dto.MediaAssetId.Value != option.MediaAssetId)
                {
                    var mediaExists = await _context.MediaAssets.AnyAsync(m => m.Id == dto.MediaAssetId.Value);
                    if (!mediaExists)
                        return ServiceResult<QuizOptionDto>.Failure("The specified Media Asset does not exist.", ServiceError.NotFound);

                    oldMediaAssetId = option.MediaAssetId;
                    option.MediaAssetId = dto.MediaAssetId.Value;
                }
            }

            await _context.SaveChangesAsync();

            if (oldMediaAssetId.HasValue)
            {
                await _mediaAssetService.DeleteMediaAssetAndFileAsync(oldMediaAssetId.Value);
            }

            return ServiceResult<QuizOptionDto>.Ok(new QuizOptionDto(
                option.Id,
                option.Text,
                option.IsFreeText,
                option.IsCorrect,
                option.QuizQuestionId,
                option.MediaAssetId,
                option.Responses.Count));
        }

        public async Task<ServiceResult<bool>> DeleteQuizOptionAsync(long id, long currentUserId, bool isAdmin)
        {
            var option = await _context.QuizOptions
                 .Include(o => o.QuizQuestion)
                    .ThenInclude(q => q.Quiz)
                        .ThenInclude(quiz => quiz.Subject)
                 .FirstOrDefaultAsync(o => o.Id == id);

            if (option == null)
                return ServiceResult<bool>.Failure("Quiz option not found.", ServiceError.NotFound);

            if (!isAdmin && option.QuizQuestion.Quiz.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<bool>.Failure("You do not have permission to delete this option.", ServiceError.Forbidden);
            }

            // Prevent deleting the option if the subject has officially ended
            if (option.QuizQuestion.Quiz.Subject.HasEnded)
            {
                return ServiceResult<bool>.Failure(
                    "Cannot delete an option that belongs to a subject that has already ended.",
                    ServiceError.Forbidden);
            }

            bool hasAttempts = await _context.QuizAttempts.AnyAsync(a => a.QuizId == option.QuizQuestion.QuizId);
            if (hasAttempts)
            {
                return ServiceResult<bool>.Failure(
                    "Cannot delete this option because students have already started or completed this quiz.",
                    ServiceError.Conflict);
            }

            long? oldMediaAssetId = option.MediaAssetId;

            _context.QuizOptions.Remove(option);

            try
            {
                await _context.SaveChangesAsync();

                if (oldMediaAssetId.HasValue)
                {
                    await _mediaAssetService.DeleteMediaAssetAndFileAsync(oldMediaAssetId.Value);
                }

                return ServiceResult<bool>.Ok(true);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete QuizOption {Id} due to database constraints.", id);
                return ServiceResult<bool>.Failure(
                    "Cannot delete this option because it has already been selected in a student's quiz attempt.",
                    ServiceError.Conflict);
            }
        }
    }
}
