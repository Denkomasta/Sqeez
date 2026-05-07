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
    /// Implements quiz attempt lifecycle, response submission, scoring, grading, and reward processing.
    /// </summary>
    public class QuizAttemptService : BaseService<QuizAttemptService>, IQuizAttemptService
    {
        private readonly IBadgeService _badgeService;

        public QuizAttemptService(SqeezDbContext context, ILogger<QuizAttemptService> logger, IBadgeService badgeService)
            : base(context, logger)
        {
            _badgeService = badgeService;
        }

        private async Task<long?> GetNextQuestionId(long quizId, long quizQuestionId)
        {
            return await _context.QuizQuestions
                .Where(q => q.QuizId == quizId && q.Id > quizQuestionId)
                .OrderBy(q => q.Id)
                .Select(q => (long?)q.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<ServiceResult<QuizAttemptDto>> StartAttemptAsync(long studentId, StartQuizAttemptDto dto)
        {
            // Verify the student is actually enrolled in the subject this quiz belongs to
            var enrollment = await _context.Enrollments
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId && e.StudentId == studentId);

            if (enrollment == null)
                return ServiceResult<QuizAttemptDto>.Failure("You are not enrolled in this subject.", ServiceError.Forbidden);

            // Verify the quiz exists and belongs to this subject
            var quiz = await _context.Quizzes.FindAsync(dto.QuizId);
            if (quiz == null || quiz.SubjectId != enrollment.SubjectId)
                return ServiceResult<QuizAttemptDto>.Failure("Quiz not found.", ServiceError.NotFound);

            var now = DateTime.UtcNow;
            // If there's no publish date, it's a draft. If the date is in the future, it's scheduled.
            if (!quiz.PublishDate.HasValue || now < quiz.PublishDate.Value)
                return ServiceResult<QuizAttemptDto>.Failure("This quiz is not published yet.", ServiceError.ValidationFailed);

            // If there is a closing date and we are past it, lock it down.
            if (quiz.ClosingDate.HasValue && now > quiz.ClosingDate.Value)
                return ServiceResult<QuizAttemptDto>.Failure("This quiz has already closed.", ServiceError.ValidationFailed);

            // Max Retries Business Rule
            if (quiz.MaxRetries > 0)
            {
                var previousAttempts = await _context.QuizAttempts
                    .CountAsync(a => a.QuizId == dto.QuizId && a.EnrollmentId == dto.EnrollmentId);

                if (previousAttempts >= quiz.MaxRetries)
                    return ServiceResult<QuizAttemptDto>.Failure($"You have reached the maximum of {quiz.MaxRetries} retries for this quiz.", ServiceError.Conflict);
            }

            // Create the new Attempt
            var attempt = new QuizAttempt
            {
                QuizId = dto.QuizId,
                EnrollmentId = dto.EnrollmentId,
                StartTime = DateTime.UtcNow,
                Status = AttemptStatus.Created,
                TotalScore = 0
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            long? nextQuestionId = await GetNextQuestionId(attempt.QuizId, 0);  // first question, biggest nonexisting id.

            return ServiceResult<QuizAttemptDto>.Ok(new QuizAttemptDto(
                attempt.Id, attempt.QuizId, attempt.EnrollmentId, attempt.StartTime,
                attempt.EndTime, attempt.Status, attempt.TotalScore, attempt.Mark, nextQuestionId));
        }

        public async Task<ServiceResult<QuestionAnsweredDto>> SubmitAnswerAsync(long attemptId, long studentId, SubmitQuestionResponseDto dto)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.Enrollment)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null) return ServiceResult<QuestionAnsweredDto>.Failure("Attempt not found.", ServiceError.NotFound);
            if (attempt.Enrollment.StudentId != studentId) return ServiceResult<QuestionAnsweredDto>.Failure("Access denied.", ServiceError.Forbidden);

            if (attempt.Status == AttemptStatus.Created) attempt.Status = AttemptStatus.Started;
            if (attempt.Status != AttemptStatus.Started) return ServiceResult<QuestionAnsweredDto>.Failure("This quiz attempt is no longer in progress.", ServiceError.Conflict);

            var question = await _context.QuizQuestions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == dto.QuizQuestionId && q.QuizId == attempt.QuizId);

            if (question == null) return ServiceResult<QuestionAnsweredDto>.Failure("Question not found on this quiz.", ServiceError.NotFound);

            var response = await _context.QuizQuestionResponses
                .Include(r => r.Options)
                .FirstOrDefaultAsync(r => r.QuizAttemptId == attemptId && r.QuizQuestionId == dto.QuizQuestionId);

            if (response != null)
            {
                return ServiceResult<QuestionAnsweredDto>.Failure("You have already submitted an answer for this question and cannot change it.", ServiceError.Conflict);
            }

            var selectedOptions = new List<QuizOption>();
            if (dto.SelectedOptionIds != null && dto.SelectedOptionIds.Any())
            {
                var selectedOptionIds = dto.SelectedOptionIds.Distinct().ToList();
                selectedOptions = question.Options.Where(o => selectedOptionIds.Contains(o.Id)).ToList();
                if (selectedOptions.Count != selectedOptionIds.Count)
                {
                    return ServiceResult<QuestionAnsweredDto>.Failure(
                        "One or more selected options do not belong to this question.",
                        ServiceError.ValidationFailed);
                }
            }

            response = new QuizQuestionResponse
            {
                QuizAttemptId = attemptId,
                QuizQuestionId = dto.QuizQuestionId,
                ResponseTimeMs = dto.ResponseTimeMs,
                FreeTextAnswer = dto.FreeTextAnswer
            };
            _context.QuizQuestionResponses.Add(response);

            if (selectedOptions.Any())
            {
                foreach (var opt in selectedOptions)
                {
                    response.Options.Add(opt);
                }
            }

            bool isFreeTextQuestion = question.Options.Any(o => o.IsFreeText);

            if (isFreeTextQuestion)
            {
                response.Score = null;
            }
            else
            {
                response.Score = 0;

                if (response.Options.Any())
                {
                    var correctOptionIds = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
                    var selectedIds = response.Options.Select(o => o.Id).ToList();
                    bool isCorrect = false;

                    if (question.IsStrictMultipleChoice)
                    {
                        isCorrect = correctOptionIds.Count == selectedIds.Count
                                           && !correctOptionIds.Except(selectedIds).Any();
                    }
                    else
                    {
                        isCorrect = selectedIds.Count == 1 && correctOptionIds.Contains(selectedIds.First());
                    }

                    if (isCorrect)
                    {
                        response.Score = question.Difficulty;
                    }
                    else
                    {
                        response.Score = -question.PenaltyPoints;
                    }
                }
            }

            await _context.SaveChangesAsync();

            var actualCorrectOptionIds = question.Options.Where(o => o.IsCorrect && !o.IsFreeText).Select(o => o.Id).ToList();
            var actualCorrectFreeText = question.Options.FirstOrDefault(o => o.IsCorrect && o.IsFreeText)?.Text;

            long? nextQuestionId = await GetNextQuestionId(attempt.QuizId, dto.QuizQuestionId);

            return ServiceResult<QuestionAnsweredDto>.Ok(new QuestionAnsweredDto(
                response.Id, response.QuizQuestionId, response.ResponseTimeMs,
                response.FreeTextAnswer, response.IsLiked, response.Score,
                response.Options.Select(o => o.Id).ToList(),
                actualCorrectOptionIds, actualCorrectFreeText,
                nextQuestionId
            ));
        }

        public async Task<ServiceResult<long?>> GetNextPendingQuestionIdAsync(long attemptId, long studentId)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.Enrollment)
                .Include(a => a.Responses)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null) return ServiceResult<long?>.Failure("Attempt not found.", ServiceError.NotFound);
            if (attempt.Enrollment.StudentId != studentId) return ServiceResult<long?>.Failure("Access denied.", ServiceError.Forbidden);

            if (attempt.Status != AttemptStatus.Started && attempt.Status != AttemptStatus.Created)
                return ServiceResult<long?>.Failure("This attempt is no longer in progress.", ServiceError.Conflict);

            long lastAnsweredQuestionId = attempt.Responses.Any()
                ? attempt.Responses.Max(r => r.QuizQuestionId)
                : 0;

            long? nextQuestionId = await GetNextQuestionId(attempt.QuizId, lastAnsweredQuestionId);

            return ServiceResult<long?>.Ok(nextQuestionId);
        }

        private async Task<ServiceResult<List<StudentBadgeBasicDto>>> ProcessCompletedAttemptRewardsAsync(QuizAttempt attempt, long studentId)
        {
            int previousHighScore = await _context.QuizAttempts
                .Where(a => a.QuizId == attempt.QuizId &&
                            a.Enrollment.StudentId == studentId &&
                            a.Status == AttemptStatus.Completed &&
                            a.Id != attempt.Id)
                .Select(a => (int?)a.TotalScore)
                .MaxAsync() ?? 0;

            int xpToAward = Math.Max(0, attempt.TotalScore - previousHighScore);

            if (xpToAward > 0)
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student != null)
                {
                    student.CurrentXP += xpToAward;
                }
            }

            await _context.SaveChangesAsync();

            int maxPossibleScore = await _context.QuizQuestions
                .Where(q => q.QuizId == attempt.QuizId)
                .SumAsync(q => q.Difficulty);

            decimal scorePercentage = maxPossibleScore > 0 ? ((decimal)attempt.TotalScore / maxPossibleScore) * 100 : 0;

            int totalAttempts = await _context.QuizAttempts
                .CountAsync(a => a.QuizId == attempt.QuizId &&
                                 a.Enrollment.StudentId == studentId &&
                                 a.Status == AttemptStatus.Completed);

            int perfectAnswersCount = attempt.Responses.Count(r => r.Score > 0);

            var metrics = new BadgeEvaluationMetrics(
                ScorePercentage: scorePercentage,
                TotalScore: attempt.TotalScore,
                PerfectAnswersCount: perfectAnswersCount,
                TotalAttempts: totalAttempts
            );

            var newBadges = await _badgeService.EvaluateAndAwardBadgesAsync(studentId, metrics);

            if (!newBadges.Success)
            {
                return ServiceResult<List<StudentBadgeBasicDto>>.Failure(newBadges.ErrorMessage ?? "Badge evaluation failed", ServiceError.InternalError);
            }

            var studentBadges = newBadges.Data?.Select(b => new StudentBadgeBasicDto
            {
                BadgeId = b.BadgeId,
                Name = b.Name,
                IconUrl = b.IconUrl,
                EarnedAt = b.EarnedAt
            }).ToList() ?? new List<StudentBadgeBasicDto>();

            return ServiceResult<List<StudentBadgeBasicDto>>.Ok(studentBadges);
        }

        public async Task<ServiceResult<QuizAttemptDto>> CompleteAttemptAsync(long attemptId, long studentId)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.Enrollment)
                .Include(a => a.Responses)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
                return ServiceResult<QuizAttemptDto>.Failure("Attempt not found.", ServiceError.NotFound);

            if (attempt.Enrollment.StudentId != studentId)
                return ServiceResult<QuizAttemptDto>.Failure("Access denied.", ServiceError.Forbidden);

            if (attempt.Status != AttemptStatus.Started)
                return ServiceResult<QuizAttemptDto>.Failure("This attempt is already completed.", ServiceError.Conflict);

            bool requiresManualGrading = attempt.Responses.Any(r => r.Score == null);

            using var transaction = await _context.Database.BeginTransactionAsync();

            attempt.Status = requiresManualGrading ? AttemptStatus.PendingCorrection : AttemptStatus.Completed;
            attempt.EndTime = DateTime.UtcNow;
            attempt.TotalScore = attempt.Responses.Sum(r => r.Score ?? 0);

            await _context.SaveChangesAsync();

            if (requiresManualGrading)
            {
                // Commit the transaction since we successfully set it to PendingCorrection
                await transaction.CommitAsync();

                return ServiceResult<QuizAttemptDto>.Ok(new QuizAttemptDto(
                    attempt.Id, attempt.QuizId, attempt.EnrollmentId, attempt.StartTime,
                    attempt.EndTime, attempt.Status, attempt.TotalScore, attempt.Mark,
                    EarnedBadges: null));
            }

            var rewardResult = await ProcessCompletedAttemptRewardsAsync(attempt, studentId);

            if (!rewardResult.Success)
            {
                // If badge evaluation or XP fails, roll the whole thing back
                await transaction.RollbackAsync();

                return ServiceResult<QuizAttemptDto>.Failure(
                    rewardResult.ErrorMessage ?? "Failed to process attempt rewards.",
                    ServiceError.InternalError);
            }

            await transaction.CommitAsync();

            return ServiceResult<QuizAttemptDto>.Ok(new QuizAttemptDto(
                attempt.Id, attempt.QuizId, attempt.EnrollmentId, attempt.StartTime,
                attempt.EndTime, attempt.Status, attempt.TotalScore, attempt.Mark,
                EarnedBadges: rewardResult.Data));
        }

        public async Task<ServiceResult<QuizAttemptDetailDto>> GetAttemptDetailsAsync(long attemptId, long currentUserId, string currentUserRole)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.Enrollment)
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Subject)
                .Include(a => a.Responses)
                    .ThenInclude(r => r.Options)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null) return ServiceResult<QuizAttemptDetailDto>.Failure("Attempt not found.", ServiceError.NotFound);

            bool canView = currentUserRole switch
            {
                "Admin" => true,
                "Teacher" => attempt.Quiz.Subject.TeacherId == currentUserId,
                _ => attempt.Enrollment.StudentId == currentUserId
            };

            if (!canView)
                return ServiceResult<QuizAttemptDetailDto>.Failure("You do not have permission to view this attempt.", ServiceError.Forbidden);

            var responseDtos = attempt.Responses.Select(r => new QuestionResponseDto(
                r.Id, r.QuizQuestionId, r.ResponseTimeMs, r.FreeTextAnswer, r.IsLiked, r.Score,
                r.Options.Select(o => o.Id).ToList()
            )).ToList();

            return ServiceResult<QuizAttemptDetailDto>.Ok(new QuizAttemptDetailDto(
                attempt.Id, attempt.QuizId, attempt.EnrollmentId, attempt.StartTime,
                attempt.EndTime, attempt.Status, attempt.TotalScore, attempt.Mark, responseDtos));
        }

        public async Task<ServiceResult<PagedResponse<QuizAttemptDto>>> GetAttemptsForQuizAsync(long quizId, long userId, int pageNumber = 1, int pageSize = 20)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, ValidationConstants.MaxPageSize);

            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return ServiceResult<PagedResponse<QuizAttemptDto>>.Failure("Quiz not found.", ServiceError.NotFound);

            var isQuizOwner = quiz.Subject.TeacherId == userId;

            var query = _context.QuizAttempts
                .Where(a => a.QuizId == quizId);

            if (!isQuizOwner)
            {
                query = query.Where(a => a.Enrollment.StudentId == userId);
            }

            query = query.OrderByDescending(a => a.StartTime);

            int totalCount = await query.CountAsync();

            var attempts = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new QuizAttemptDto(
                    a.Id,
                    a.QuizId,
                    a.EnrollmentId,
                    a.StartTime,
                    a.EndTime,
                    a.Status,
                    a.TotalScore,
                    a.Mark,
                    null,
                    null,
                    isQuizOwner ? (a.Enrollment.Student.FirstName + " " + a.Enrollment.Student.LastName) : null,
                    isQuizOwner ? a.Enrollment.StudentId : null
                ))
                .ToListAsync();

            return ServiceResult<PagedResponse<QuizAttemptDto>>.Ok(new PagedResponse<QuizAttemptDto>
            {
                Data = attempts,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<QuestionResponseDto>> GradeFreeTextResponseAsync(long responseId, long teacherId, GradeQuestionResponseDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var response = await _context.QuizQuestionResponses
                .Include(r => r.Options)
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Quiz)
                        .ThenInclude(q => q.Subject)
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Responses)
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Enrollment)
                .FirstOrDefaultAsync(r => r.Id == responseId);

            if (response == null) return ServiceResult<QuestionResponseDto>.Failure("Response not found.", ServiceError.NotFound);

            if (response.QuizAttempt.Quiz.Subject.TeacherId != teacherId)
                return ServiceResult<QuestionResponseDto>.Failure("You can only grade responses for your own subjects.", ServiceError.Forbidden);

            response.Score = dto.Score;
            response.IsLiked = dto.IsLiked;

            var attempt = response.QuizAttempt;
            attempt.TotalScore = attempt.Responses.Sum(r => r.Score ?? 0);

            // Check if there are any remaining ungraded questions
            bool isFullyGraded = !attempt.Responses.Any(r => r.Score == null);

            if (isFullyGraded && attempt.Status == AttemptStatus.PendingCorrection)
            {
                attempt.Status = AttemptStatus.Completed;

                // Save the completed status FIRST before evaluating rewards
                await _context.SaveChangesAsync();

                // Call our extracted reward logic
                var rewardResult = await ProcessCompletedAttemptRewardsAsync(attempt, attempt.Enrollment.StudentId);

                if (!rewardResult.Success)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<QuestionResponseDto>.Failure(rewardResult?.ErrorMessage ?? "Internal error", ServiceError.InternalError);
                }
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            return ServiceResult<QuestionResponseDto>.Ok(new QuestionResponseDto(
                response.Id, response.QuizQuestionId, response.ResponseTimeMs,
                response.FreeTextAnswer, response.IsLiked, response.Score,
                response.Options.Select(o => o.Id).ToList()));
        }

        public async Task<ServiceResult<bool>> DeleteAttemptAsync(long attemptId, long teacherId)
        {
            var attempt = await _context.QuizAttempts
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Subject)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
                return ServiceResult<bool>.Failure("Attempt not found.", ServiceError.NotFound);

            if (attempt.Quiz.Subject.TeacherId != teacherId)
                return ServiceResult<bool>.Failure("You do not have permission to delete attempts for this subject.", ServiceError.Forbidden);

            _context.QuizAttempts.Remove(attempt);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteAllAttemptsForQuizAsync(long quizId, long teacherId, bool isAdmin = false)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return ServiceResult<bool>.Failure("Quiz not found.", ServiceError.NotFound);

            if (!isAdmin && quiz.Subject.TeacherId != teacherId)
                return ServiceResult<bool>.Failure("You do not have permission to delete attempts for this quiz.", ServiceError.Forbidden);

            var attempts = await _context.QuizAttempts
                .Where(a => a.QuizId == quizId)
                .ToListAsync();

            if (!attempts.Any())
                return ServiceResult<bool>.Ok(true);

            _context.QuizAttempts.RemoveRange(attempts);

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }
    }
}
