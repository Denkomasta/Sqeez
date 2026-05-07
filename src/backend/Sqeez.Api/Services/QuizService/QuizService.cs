using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Extensions;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Implements quiz search, retrieval, creation, patch, delete, and schedule validation.
    /// </summary>
    public class QuizService : BaseService<QuizService>, IQuizService
    {
        public QuizService(SqeezDbContext context, ILogger<QuizService> logger) : base(context, logger) { }

        public async Task<ServiceResult<PagedResponse<QuizDto>>> GetAllQuizzesAsync(QuizFilterDto filter)
        {
            var query = _context.Quizzes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var search = filter.SearchTerm.Trim().ToLower();
                query = query.Where(q => q.Title.ToLower().Contains(search) ||
                                         q.Description.ToLower().Contains(search));
            }

            if (filter.TeacherId.HasValue)
            {
                query = query.Where(q => q.Subject.TeacherId == filter.TeacherId.Value);
            }

            if (filter.SubjectId.HasValue)
            {
                query = query.Where(q => q.SubjectId == filter.SubjectId.Value);
            }

            if (filter.StudentId.HasValue)
            {
                query = query.Where(q => _context.Enrollments.Any(e =>
                    e.StudentId == filter.StudentId.Value &&
                    e.SubjectId == q.SubjectId &&
                    e.ArchivedAt == null));
            }

            if (filter.IsActive.HasValue)
            {
                if (filter.IsActive.Value)
                {
                    // Active: Published in the past, and (No closing date OR closing date is in the future)
                    query = query.WhereIsActive();
                }
                else
                {
                    // Inactive: Not published, published in future, or closing date has passed
                    query = query.WhereIsInactive();
                }
            }

            if (filter.PublishDate.HasValue) query = query.Where(q => q.PublishDate >= filter.PublishDate.Value);
            if (filter.ClosingDate.HasValue) query = query.Where(q => q.ClosingDate <= filter.ClosingDate.Value);

            int totalCount = await query.CountAsync();

            var quizzes = await query
                .OrderByDescending(q => q.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(q => new QuizDto(
                    q.Id,
                    q.Title,
                    q.Description,
                    q.MaxRetries,
                    q.CreatedAt,
                    q.PublishDate,
                    q.ClosingDate,
                    q.SubjectId,
                    q.QuizQuestions.Count,
                    q.QuizAttempts.Count(qa =>
                        !filter.StudentId.HasValue ||
                        qa.Enrollment.StudentId == filter.StudentId.Value)
                ))
                .ToListAsync();

            return ServiceResult<PagedResponse<QuizDto>>.Ok(new PagedResponse<QuizDto>
            {
                Data = quizzes,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            });
        }

        public async Task<ServiceResult<QuizDto>> GetQuizByIdAsync(long id, GetQuizDto dto)
        {
            var quiz = await _context.Quizzes
                .Where(q => q.Id == id)
                .Select(q => new QuizDto(
                    q.Id,
                    q.Title,
                    q.Description,
                    q.MaxRetries,
                    q.CreatedAt,
                    q.PublishDate,
                    q.ClosingDate,
                    q.SubjectId,
                    q.QuizQuestions.Count,
                    q.QuizAttempts.Count(qa =>
                        !dto.studentId.HasValue ||
                        qa.Enrollment.StudentId == dto.studentId.Value)
                ))
                .FirstOrDefaultAsync();

            if (quiz == null) return ServiceResult<QuizDto>.Failure("Quiz not found.", ServiceError.NotFound);

            return ServiceResult<QuizDto>.Ok(quiz);
        }

        public async Task<ServiceResult<QuizDto>> CreateQuizAsync(CreateQuizDto dto, long currentUserId)
        {
            var subject = await _context.Subjects.FindAsync(dto.SubjectId);

            if (subject == null)
                return ServiceResult<QuizDto>.Failure("Subject not found.", ServiceError.NotFound);

            if (subject.TeacherId != currentUserId)
            {
                return ServiceResult<QuizDto>.Failure("You do not have permission to add quizzes to this subject.", ServiceError.Forbidden);
            }

            if (subject.HasEnded)
                return ServiceResult<QuizDto>.Failure(
                    "Cannot create quizzes for a subject that is closed.",
                    ServiceError.Forbidden);

            if (dto.ClosingDate.HasValue && subject.EndDate.HasValue && dto.ClosingDate.Value > subject.EndDate.Value)
            {
                return ServiceResult<QuizDto>.Failure(
                    $"The quiz closing date cannot be later than the subject's end date ({subject.EndDate.Value:yyyy-MM-dd}).",
                    ServiceError.ValidationFailed);
            }

            var quiz = new Quiz
            {
                Title = dto.Title,
                Description = dto.Description,
                MaxRetries = dto.MaxRetries,
                SubjectId = dto.SubjectId,
                PublishDate = dto.PublishDate,
                ClosingDate = dto.ClosingDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return ServiceResult<QuizDto>.Ok(new QuizDto(
                quiz.Id, quiz.Title, quiz.Description, quiz.MaxRetries, quiz.CreatedAt,
                quiz.PublishDate, quiz.ClosingDate, quiz.SubjectId, 0, 0));
        }

        public async Task<ServiceResult<QuizDto>> PatchQuizAsync(long id, PatchQuizDto dto, long currentUserId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.QuizQuestions)
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return ServiceResult<QuizDto>.Failure("Quiz not found.", ServiceError.NotFound);

            if (quiz.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<QuizDto>.Failure("You do not have permission to modify this quiz.", ServiceError.Forbidden);
            }

            // Determine the target subject
            var targetSubject = quiz.Subject;
            if (dto.SubjectId.HasValue && dto.SubjectId.Value != quiz.SubjectId)
            {
                targetSubject = await _context.Subjects.FindAsync(dto.SubjectId.Value);
                if (targetSubject == null) return ServiceResult<QuizDto>.Failure("Target subject not found.", ServiceError.NotFound);

                // Prevent moving the quiz into a closed subject
                if (targetSubject.HasEnded)
                {
                    return ServiceResult<QuizDto>.Failure("Cannot move a quiz into a closed subject.", ServiceError.Forbidden);
                }
            }

            var intendedClosingDate = dto.ClosingDate.HasValue ? dto.ClosingDate.Value : quiz.ClosingDate;

            // Validate the intended closing date against the target subject's end date
            if (intendedClosingDate.HasValue && targetSubject.EndDate.HasValue && intendedClosingDate.Value > targetSubject.EndDate.Value)
            {
                return ServiceResult<QuizDto>.Failure(
                    $"The quiz closing date cannot be later than the subject's end date ({targetSubject.EndDate.Value:yyyy-MM-dd}).",
                    ServiceError.ValidationFailed);
            }

            if (!string.IsNullOrWhiteSpace(dto.Title)) quiz.Title = dto.Title;
            if (dto.Description != null) quiz.Description = dto.Description;
            if (dto.MaxRetries.HasValue) quiz.MaxRetries = dto.MaxRetries.Value;
            if (dto.PublishDate.HasValue) quiz.PublishDate = dto.PublishDate.Value;
            if (dto.ClosingDate.HasValue) quiz.ClosingDate = dto.ClosingDate.Value;
            if (dto.SubjectId.HasValue) quiz.SubjectId = dto.SubjectId.Value;

            await _context.SaveChangesAsync();

            return ServiceResult<QuizDto>.Ok(new QuizDto(
                quiz.Id, quiz.Title, quiz.Description, quiz.MaxRetries, quiz.CreatedAt,
                quiz.PublishDate, quiz.ClosingDate, quiz.SubjectId, quiz.QuizQuestions.Count, quiz.QuizAttempts.Count));
        }

        public async Task<ServiceResult<bool>> DeleteQuizAsync(long id, long currentUserId, bool isAdmin)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return ServiceResult<bool>.Failure("Quiz not found.", ServiceError.NotFound);

            if (!isAdmin && quiz.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<bool>.Failure("You do not have permission to delete this quiz.", ServiceError.Forbidden);
            }

            // NEW: Prevent deleting the quiz if the subject has officially ended
            if (quiz.Subject.HasEnded)
            {
                return ServiceResult<bool>.Failure(
                    "Cannot delete a quiz that belongs to a subject that has already ended.",
                    ServiceError.Forbidden);
            }

            if (quiz.QuizAttempts.Any())
            {
                // Soft Delete: If students have already taken it, we just close it so history is preserved.
                quiz.ClosingDate = DateTime.UtcNow;
                _logger.LogInformation("Soft deleted Quiz {Id} by setting ClosingDate to now.", id);
            }
            else
            {
                // Hard Delete: Safe to wipe completely (cascade rules will delete questions and options)
                _context.Quizzes.Remove(quiz);
                _logger.LogInformation("Hard deleted Quiz {Id}.", id);
            }

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
    }
}
