using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Calculates quiz summary and per-question statistics for teachers and admins.
    /// </summary>
    public class QuizStatisticsService : BaseService<QuizStatisticsService>, IQuizStatisticsService
    {
        public QuizStatisticsService(SqeezDbContext context, ILogger<QuizStatisticsService> logger)
            : base(context, logger)
        {
        }

        public async Task<ServiceResult<QuizSummaryStatDto>> GetQuizSummaryStatsAsync(long quizId, long teacherId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return ServiceResult<QuizSummaryStatDto>.Failure("Quiz not found.", ServiceError.NotFound);

            if (quiz.Subject.TeacherId != teacherId)
                return ServiceResult<QuizSummaryStatDto>.Failure("You do not have permission to view statistics for this quiz.", ServiceError.Forbidden);

            var attemptsQuery = _context.QuizAttempts.Where(a => a.QuizId == quizId);
            var totalAttempts = await attemptsQuery.CountAsync();

            if (totalAttempts == 0)
            {
                return ServiceResult<QuizSummaryStatDto>.Ok(new QuizSummaryStatDto { QuizId = quizId });
            }

            var completedData = await attemptsQuery
                .Where(a => a.Status == AttemptStatus.Completed)
                .Select(a => new
                {
                    a.TotalScore,
                    a.StartTime,
                    a.EndTime
                })
                .ToListAsync();

            var summary = new QuizSummaryStatDto
            {
                QuizId = quizId,
                TotalAttempts = totalAttempts,
                CompletedAttempts = completedData.Count,

                AverageScore = completedData.Any() ? completedData.Average(a => a.TotalScore) : 0,
                HighestScore = completedData.Any() ? completedData.Max(a => a.TotalScore) : 0,
                LowestScore = completedData.Any() ? completedData.Min(a => a.TotalScore) : 0,

                AverageCompletionTimeMinutes = completedData
                    .Where(a => a.StartTime.HasValue && a.EndTime.HasValue)
                    .Select(a => (a.EndTime!.Value - a.StartTime!.Value).TotalMinutes)
                    .DefaultIfEmpty(0)
                    .Average()
            };

            return ServiceResult<QuizSummaryStatDto>.Ok(summary);
        }

        public async Task<ServiceResult<IEnumerable<QuestionStatDto>>> GetQuestionStatsAsync(long quizId, long teacherId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return ServiceResult<IEnumerable<QuestionStatDto>>.Failure("Quiz not found.", ServiceError.NotFound);

            if (quiz.Subject.TeacherId != teacherId)
                return ServiceResult<IEnumerable<QuestionStatDto>>.Failure("You do not have permission to view statistics for this quiz.", ServiceError.Forbidden);

            var validStatuses = new[] { AttemptStatus.Completed, AttemptStatus.PendingCorrection };

            var questionStats = await _context.QuizQuestions
                .AsNoTracking()
                .Where(q => q.QuizId == quizId)
                .Select(q => new QuestionStatDto
                {
                    Id = q.Id,
                    QuestionText = q.Title,
                    IsFreeText = q.Options.Any(o => o.IsFreeText),

                    TotalAnswers = _context.QuizQuestionResponses
                        .Count(r => r.QuizQuestionId == q.Id && validStatuses.Contains(r.QuizAttempt.Status)),

                    Options = q.Options.Select(o => new OptionStatDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect,
                        // PickCount is irrelevant for free text options, force to 0 to avoid confusion
                        PickCount = o.IsFreeText ? 0 : o.Responses.Count(r => validStatuses.Contains(r.QuizAttempt.Status))
                    }).ToList(),

                    // Fetch the actual text answers submitted by students
                    SubmittedFreeTextAnswers = _context.QuizQuestionResponses
                        .Where(r => r.QuizQuestionId == q.Id && r.FreeTextAnswer != null && validStatuses.Contains(r.QuizAttempt.Status))
                        .Select(r => r.FreeTextAnswer!)
                        .ToList(),

                    // Average score ignores nulls automatically, which is perfect here
                    AverageScore = _context.QuizQuestionResponses
                        .Where(r => r.QuizQuestionId == q.Id && validStatuses.Contains(r.QuizAttempt.Status))
                        .Average(r => (double?)r.Score) ?? 0,

                    AverageResponseTimeSeconds = (_context.QuizQuestionResponses
                        .Where(r => r.QuizQuestionId == q.Id && validStatuses.Contains(r.QuizAttempt.Status))
                        .Average(r => (double?)r.ResponseTimeMs) ?? 0) / 1000.0
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<QuestionStatDto>>.Ok(questionStats);
        }
    }
}
