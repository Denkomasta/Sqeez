using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Implements quiz question management and role-aware detailed question retrieval.
    /// </summary>
    public class QuizQuestionService : BaseService<QuizQuestionService>, IQuizQuestionService
    {
        private readonly IMediaAssetService _mediaAssetService;

        public QuizQuestionService(SqeezDbContext context, ILogger<QuizQuestionService> logger, IMediaAssetService mas) : base(context, logger)
        {
            _mediaAssetService = mas;
        }

        public async Task<ServiceResult<PagedResponse<QuizQuestionDto>>> GetAllQuizQuestionsAsync(QuizQuestionFilterDto filter, long currentUserId, bool isAdmin)
        {
            var query = _context.QuizQuestions.AsNoTracking();

            if (!isAdmin)
            {
                query = query.Where(q => q.Quiz.Subject.TeacherId == currentUserId);
            }

            if (filter.QuizId.HasValue)
            {
                query = query.Where(q => q.QuizId == filter.QuizId.Value);
            }

            if (filter.Difficulty.HasValue)
            {
                query = query.Where(q => q.Difficulty == filter.Difficulty.Value);
            }

            if (filter.MediaAssetId.HasValue)
            {
                query = query.Where(q => q.MediaAssetId == filter.MediaAssetId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.Trim().ToLower();
                query = query.Where(q => q.Title != null && q.Title.ToLower().Contains(search));
            }

            int totalCount = await query.CountAsync();

            var questions = await query
                .OrderBy(q => q.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(q => new QuizQuestionDto(
                    q.Id,
                    q.Title ?? string.Empty,
                    q.Difficulty,
                    q.HasPenalty,
                    q.TimeLimit,
                    q.IsStrictMultipleChoice,
                    q.QuizId,
                    q.MediaAssetId,
                    q.Options.Count,
                    q.PenaltyPoints
                ))
                .ToListAsync();

            return ServiceResult<PagedResponse<QuizQuestionDto>>.Ok(new PagedResponse<QuizQuestionDto>
            {
                Data = questions,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            });
        }

        public async Task<ServiceResult<QuizQuestionDto>> GetQuizQuestionByIdAsync(long id, long currentUserId)
        {
            var question = await _context.QuizQuestions
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Subject)
                .Where(q => q.Id == id)
                .FirstOrDefaultAsync();

            if (question == null)
                return ServiceResult<QuizQuestionDto>.Failure("Quiz question not found.", ServiceError.NotFound);

            if (question.Quiz.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<QuizQuestionDto>.Failure("You do not have permission to view this question.", ServiceError.Forbidden);
            }

            var dto = new QuizQuestionDto(
                    question.Id,
                    question.Title ?? string.Empty,
                    question.Difficulty,
                    question.HasPenalty,
                    question.TimeLimit,
                    question.IsStrictMultipleChoice,
                    question.QuizId,
                    question.MediaAssetId,
                    _context.QuizOptions.Count(o => o.QuizQuestionId == question.Id),
                    question.PenaltyPoints
                );

            return ServiceResult<QuizQuestionDto>.Ok(dto);
        }

        public async Task<ServiceResult<QuizQuestionDto>> CreateQuizQuestionAsync(CreateQuizQuestionDto dto, long currentUserId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.Id == dto.QuizId);

            if (quiz == null)
                return ServiceResult<QuizQuestionDto>.Failure("The specified Quiz does not exist.", ServiceError.NotFound);

            if (quiz.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<QuizQuestionDto>.Failure("You do not have permission to modify questions in this quiz.", ServiceError.Forbidden);
            }

            // Prevent adding questions if the subject is closed
            if (quiz.Subject.HasEnded)
            {
                return ServiceResult<QuizQuestionDto>.Failure(
                    "Cannot add questions to a quiz that belongs to a closed subject.",
                    ServiceError.Forbidden);
            }

            if (quiz.QuizAttempts.Any())
            {
                return ServiceResult<QuizQuestionDto>.Failure(
                    "Cannot add new questions to this quiz because students have already started or completed it.",
                    ServiceError.Conflict);
            }

            var question = new QuizQuestion
            {
                Title = dto.Title,
                Difficulty = dto.Difficulty,
                HasPenalty = dto.HasPenalty,
                TimeLimit = dto.TimeLimit,
                QuizId = dto.QuizId,
                MediaAssetId = dto.MediaAssetId
            };

            _context.QuizQuestions.Add(question);
            await _context.SaveChangesAsync();

            return ServiceResult<QuizQuestionDto>.Ok(new QuizQuestionDto(
                question.Id,
                question.Title,
                question.Difficulty,
                question.HasPenalty,
                question.TimeLimit,
                question.IsStrictMultipleChoice,
                question.QuizId,
                question.MediaAssetId,
                0,  // 0 Options on initial creation
                question.PenaltyPoints));
        }

        public async Task<ServiceResult<QuizQuestionDto>> PatchQuizQuestionAsync(long id, PatchQuizQuestionDto dto, long currentUserId)
        {
            var question = await _context.QuizQuestions
                .Include(q => q.Options)
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Subject)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return ServiceResult<QuizQuestionDto>.Failure("Quiz question not found.", ServiceError.NotFound);

            if (question.Quiz.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<QuizQuestionDto>.Failure("You do not have permission to modify questions in this quiz.", ServiceError.Forbidden);
            }

            // Prevent modifying the question if the subject has officially ended
            if (question.Quiz.Subject.HasEnded)
            {
                return ServiceResult<QuizQuestionDto>.Failure(
                    "Cannot modify a question that belongs to a subject that has already ended.",
                    ServiceError.Forbidden);
            }

            bool hasAttempts = await _context.QuizAttempts.AnyAsync(a => a.QuizId == question.QuizId);
            if (hasAttempts)
            {
                return ServiceResult<QuizQuestionDto>.Failure(
                    "Cannot modify this question because students have already started or completed this quiz.",
                    ServiceError.Conflict);
            }

            if (dto.Title != null) question.Title = dto.Title;
            if (dto.Difficulty.HasValue) question.Difficulty = dto.Difficulty.Value;
            if (dto.HasPenalty.HasValue) question.HasPenalty = dto.HasPenalty.Value;
            if (dto.TimeLimit.HasValue) question.TimeLimit = dto.TimeLimit.Value;
            if (dto.IsStrictMultipleChoice.HasValue) question.IsStrictMultipleChoice = dto.IsStrictMultipleChoice.Value;

            long? oldMediaAssetId = null;

            if (dto.MediaAssetId.HasValue)
            {
                if (dto.MediaAssetId.Value == 0)
                {
                    if (question.MediaAssetId != null)
                    {
                        oldMediaAssetId = question.MediaAssetId;
                        question.MediaAssetId = null;
                    }
                }
                else if (dto.MediaAssetId.Value != question.MediaAssetId)
                {
                    var mediaExists = await _context.MediaAssets.AnyAsync(m => m.Id == dto.MediaAssetId.Value);
                    if (!mediaExists)
                        return ServiceResult<QuizQuestionDto>.Failure("The specified Media Asset does not exist.", ServiceError.NotFound);

                    oldMediaAssetId = question.MediaAssetId;
                    question.MediaAssetId = dto.MediaAssetId.Value;
                }
            }

            await _context.SaveChangesAsync();

            if (oldMediaAssetId.HasValue)
            {
                await _mediaAssetService.DeleteMediaAssetAndFileAsync(oldMediaAssetId.Value);
            }

            return ServiceResult<QuizQuestionDto>.Ok(new QuizQuestionDto(
                question.Id,
                question.Title ?? string.Empty,
                question.Difficulty,
                question.HasPenalty,
                question.TimeLimit,
                question.IsStrictMultipleChoice,
                question.QuizId,
                question.MediaAssetId,
                question.Options.Count,
                question.PenaltyPoints));
        }

        public async Task<ServiceResult<bool>> DeleteQuizQuestionAsync(long id, long currentUserId, bool isAdmin)
        {
            var question = await _context.QuizQuestions
                .Include(q => q.Options)
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Subject)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return ServiceResult<bool>.Failure("Quiz question not found.", ServiceError.NotFound);

            if (!isAdmin && question.Quiz.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<bool>.Failure("You do not have permission to delete this question.", ServiceError.Forbidden);
            }

            // Prevent deleting the question if the subject has officially ended
            if (question.Quiz.Subject.HasEnded)
            {
                return ServiceResult<bool>.Failure(
                    "Cannot delete a question that belongs to a subject that has already ended.",
                    ServiceError.Forbidden);
            }

            bool hasAttempts = await _context.QuizAttempts.AnyAsync(a => a.QuizId == question.QuizId);
            if (hasAttempts)
            {
                return ServiceResult<bool>.Failure(
                    "Cannot delete this question because students have already started or completed this quiz.",
                    ServiceError.Conflict);
            }

            var mediaAssetIdsToDelete = new List<long>();

            if (question.MediaAssetId.HasValue)
                mediaAssetIdsToDelete.Add(question.MediaAssetId.Value);

            foreach (var option in question.Options)
            {
                if (option.MediaAssetId.HasValue)
                    mediaAssetIdsToDelete.Add(option.MediaAssetId.Value);
            }

            _context.QuizQuestions.Remove(question);

            try
            {
                await _context.SaveChangesAsync();

                foreach (var assetId in mediaAssetIdsToDelete)
                {
                    await _mediaAssetService.DeleteMediaAssetAndFileAsync(assetId);
                }

                return ServiceResult<bool>.Ok(true);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to delete QuizQuestion {Id} due to database constraints.", id);
                return ServiceResult<bool>.Failure("Cannot delete this question because students have already submitted responses to it.", ServiceError.Conflict);
            }
        }

        public async Task<ServiceResult<DetailedQuizQuestionDto>> GetDetailedQuizQuestionByIdAsync(long id, long quizId, long currentUserId, string role)
        {
            var question = await _context.QuizQuestions
                .Include(q => q.Options)
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Subject)
                        .ThenInclude(subject => subject.Enrollments)
                .FirstOrDefaultAsync(q => q.Id == id && q.QuizId == quizId);

            if (question == null)
                return ServiceResult<DetailedQuizQuestionDto>.Failure("Quiz question not found.", ServiceError.NotFound);

            bool isTeacherOfSubject = question.Quiz.Subject.TeacherId == currentUserId;
            bool isEnrolledStudent = question.Quiz.Subject.Enrollments.Any(e => e.StudentId == currentUserId && e.ArchivedAt == null);
            bool isAdmin = role == "Admin";

            if (isTeacherOfSubject)
            {
                // Access granted.
            }
            else if (isEnrolledStudent)
            {
                // Access conditionally granted. Enforce the active attempt rule.
                bool hasActiveAttempt = await _context.QuizAttempts.AnyAsync(a =>
                    a.QuizId == quizId &&
                    a.Enrollment.StudentId == currentUserId &&
                    (a.Status == AttemptStatus.Created || a.Status == AttemptStatus.Started));

                if (!hasActiveAttempt)
                {
                    return ServiceResult<DetailedQuizQuestionDto>.Failure(
                        "You cannot view quiz questions unless you have an active attempt in progress. Please start the quiz first.",
                        ServiceError.Forbidden);
                }
            }
            else
            {
                return ServiceResult<DetailedQuizQuestionDto>.Failure("You do not have permission to view this detailed question.", ServiceError.Forbidden);
            }

            // Map and Return safely
            var dto = new DetailedQuizQuestionDto(
                    question.Id,
                    question.Title ?? string.Empty,
                    question.Difficulty,
                    question.HasPenalty,
                    question.PenaltyPoints,
                    question.TimeLimit,
                    question.IsStrictMultipleChoice,
                    question.QuizId,
                    question.MediaAssetId,
                    question.Options.Select(o => new StudentQuizOptionDto(
                        o.Id,
                        o.IsFreeText ? null : o.Text,
                        o.IsFreeText,
                        o.QuizQuestionId,
                        o.MediaAssetId
                    )).ToList()
                );

            return ServiceResult<DetailedQuizQuestionDto>.Ok(dto);
        }
    }
}
