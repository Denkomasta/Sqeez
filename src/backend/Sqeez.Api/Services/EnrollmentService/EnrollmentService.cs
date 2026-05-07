using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Implements subject enrollment search, enrollment mutation, grading, and cleanup.
    /// </summary>
    public class EnrollmentService : BaseService<EnrollmentService>, IEnrollmentService
    {
        public EnrollmentService(SqeezDbContext context, ILogger<EnrollmentService> logger)
            : base(context, logger) { }

        public async Task<ServiceResult<PagedResponse<EnrollmentDto>>> GetAllEnrollmentsAsync(EnrollmentFilterDto filter)
        {
            _logger.LogInformation("Fetching enrollments with filters.");
            var query = _context.Enrollments.AsNoTracking();

            if (filter.StudentId.HasValue) query = query.Where(e => e.StudentId == filter.StudentId.Value);
            if (filter.SubjectId.HasValue) query = query.Where(e => e.SubjectId == filter.SubjectId.Value);
            if (filter.Mark.HasValue) query = query.Where(e => e.Mark == filter.Mark.Value);

            if (filter.IsActive.HasValue)
            {
                if (filter.IsActive.Value) query = query.Where(e => e.ArchivedAt == null);
                else query = query.Where(e => e.ArchivedAt != null);
            }

            query = filter.IsDescending
                ? query.OrderByDescending(e => e.EnrolledAt)
                : query.OrderBy(e => e.EnrolledAt);

            int totalCount = await query.CountAsync();

            var data = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(e => new EnrollmentDto(
                    e.Id,
                    e.Mark,
                    e.EnrolledAt,
                    e.ArchivedAt,
                    e.StudentId,
                    e.Student.Username,
                    e.SubjectId,
                    e.Subject.Name,
                    e.Subject.Code,
                    e.QuizAttempts.Count
                ))
                .ToListAsync();

            return ServiceResult<PagedResponse<EnrollmentDto>>.Ok(new PagedResponse<EnrollmentDto>
            {
                Data = data,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            });
        }

        public async Task<ServiceResult<EnrollmentDto>> GetEnrollmentByIdAsync(long id)
        {
            var e = await _context.Enrollments
                .Include(e => e.QuizAttempts)
                .Include(e => e.Student)
                .Include(e => e.Subject)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (e == null) return ServiceResult<EnrollmentDto>.Failure("Enrollment not found.", ServiceError.NotFound);

            var dto = new EnrollmentDto(e.Id, e.Mark, e.EnrolledAt, e.ArchivedAt, e.StudentId, e.Student.Username, e.SubjectId, e.Subject.Name, e.Subject.Code, e.QuizAttempts.Count);
            return ServiceResult<EnrollmentDto>.Ok(dto);
        }

        public async Task<ServiceResult<EnrollmentDto>> PatchEnrollmentAsync(long id, PatchEnrollmentDto dto, long currentUserId)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.QuizAttempts)
                .Include(e => e.Student)
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
                return ServiceResult<EnrollmentDto>.Failure("Enrollment not found.", ServiceError.NotFound);

            if (enrollment.Subject.TeacherId != currentUserId)
            {
                return ServiceResult<EnrollmentDto>.Failure("You do not have permission to grade this student. Only the subject's teacher can set the mark.", ServiceError.Forbidden);
            }

            if (dto.RemoveMark == true)
            {
                enrollment.Mark = null;
            }

            if (dto.Mark.HasValue)
            {
                if (dto.Mark.Value < 1 || dto.Mark.Value > 5)
                    return ServiceResult<EnrollmentDto>.Failure("Mark must be between 1 and 5.", ServiceError.ValidationFailed);

                enrollment.Mark = dto.Mark.Value;
            }

            await _context.SaveChangesAsync();

            var resultDto = new EnrollmentDto(enrollment.Id, enrollment.Mark, enrollment.EnrolledAt, enrollment.ArchivedAt, enrollment.StudentId, enrollment.Student.Username, enrollment.SubjectId, enrollment.Subject.Name, enrollment.Subject.Code, enrollment.QuizAttempts.Count);
            return ServiceResult<EnrollmentDto>.Ok(resultDto);
        }

        private void RemoveOrArchiveEnrollment(Enrollment enrollment, DateTime archiveTime)
        {
            // Ensure QuizAttempts was included in the query
            if (enrollment.QuizAttempts == null || enrollment.QuizAttempts.Count == 0)
            {
                _context.Enrollments.Remove(enrollment); // Hard delete: No history, safe to wipe
            }
            else
            {
                enrollment.ArchivedAt = archiveTime; // Soft delete: Preserve quiz history
            }
        }

        public async Task<ServiceResult<bool>> DeleteEnrollmentAsync(long id)
        {
            var enrollment = await _context.Enrollments.Include(e => e.QuizAttempts).FirstOrDefaultAsync(e => e.Id == id);
            if (enrollment == null) return ServiceResult<bool>.Failure("Enrollment not found.", ServiceError.NotFound);

            RemoveOrArchiveEnrollment(enrollment, DateTime.UtcNow);

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<BulkEnrollmentResultDto>> EnrollStudentsInSubjectAsync(long subjectId, AssignStudentsDto dto)
        {
            _logger.LogInformation("Bulk enrolling {Count} students into subject {SubjectId}", dto.StudentIds.Count, subjectId);

            if (!dto.StudentIds.Any()) return ServiceResult<BulkEnrollmentResultDto>.Ok(new BulkEnrollmentResultDto());

            var subject = await _context.Subjects.FindAsync(subjectId);

            if (subject == null)
                return ServiceResult<BulkEnrollmentResultDto>.Failure("Subject not found.", ServiceError.NotFound);

            if (subject.HasEnded)
                return ServiceResult<BulkEnrollmentResultDto>.Failure(
                    "Cannot enroll students because this subject is closed.",
                    ServiceError.Forbidden);

            if (dto.StudentIds.Contains(subject.TeacherId ?? 0))
            {
                return ServiceResult<BulkEnrollmentResultDto>.Failure(
                    "A teacher cannot be enrolled as a student in their own subject.",
                    ServiceError.ValidationFailed);
            }

            var existingStudentIds = await _context.Enrollments
                .Where(e => e.SubjectId == subjectId && dto.StudentIds.Contains(e.StudentId))
                .Select(e => e.StudentId)
                .ToListAsync();

            var newStudentIds = dto.StudentIds.Except(existingStudentIds).ToList();

            if (!newStudentIds.Any()) return ServiceResult<BulkEnrollmentResultDto>.Ok(new BulkEnrollmentResultDto
            {
                AlreadyEnrolledIds = existingStudentIds
            });

            var validStudentIds = await _context.Students
                .Where(s => newStudentIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            if (validStudentIds.Count != newStudentIds.Count)
            {
                return ServiceResult<BulkEnrollmentResultDto>.Failure(
                    "One or more provided IDs do not exist.",
                    ServiceError.ValidationFailed);
            }

            var newEnrollments = validStudentIds.Select(studentId => new Enrollment
            {
                StudentId = studentId,
                SubjectId = subjectId,
                EnrolledAt = DateTime.UtcNow
            });

            _context.Enrollments.AddRange(newEnrollments);

            try
            {
                await _context.SaveChangesAsync();
                return ServiceResult<BulkEnrollmentResultDto>.Ok(new BulkEnrollmentResultDto
                {
                    NewlyEnrolledIds = validStudentIds,
                    AlreadyEnrolledIds = existingStudentIds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk enrolling students in subject {SubjectId}", subjectId);
                return ServiceResult<BulkEnrollmentResultDto>.Failure("Internal database error.", ServiceError.InternalError);
            }
        }

        public async Task<ServiceResult<bool>> UnenrollStudentsFromSubjectAsync(long subjectId, RemoveStudentsDto dto)
        {
            _logger.LogInformation("Bulk unenrolling {Count} students from subject {SubjectId}", dto.StudentIds.Count, subjectId);

            if (!dto.StudentIds.Any()) return ServiceResult<bool>.Ok(true);

            var enrollmentsToDeactivate = await _context.Enrollments
                .Include(e => e.QuizAttempts)
                .Where(e => e.SubjectId == subjectId && dto.StudentIds.Contains(e.StudentId) && e.ArchivedAt == null)
                .ToListAsync();

            if (!enrollmentsToDeactivate.Any()) return ServiceResult<bool>.Ok(true);

            var archiveTime = DateTime.UtcNow;
            foreach (var enrollment in enrollmentsToDeactivate)
            {
                RemoveOrArchiveEnrollment(enrollment, archiveTime);
            }

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
    }
}
