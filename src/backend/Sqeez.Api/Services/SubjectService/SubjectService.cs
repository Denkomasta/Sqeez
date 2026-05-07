using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Extensions;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Import;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services.SubjectService
{
    /// <summary>
    /// Implements subject search, management, schedule validation, and assignment cleanup.
    /// </summary>
    public class SubjectService : BaseService<SubjectService>, ISubjectService
    {
        public SubjectService(SqeezDbContext context, ILogger<SubjectService> logger)
            : base(context, logger) { }

        private static SubjectDto MapSubjectToDto(Subject subject)
        {
            if (subject == null) return null!;

            return new SubjectDto(
                Id: subject.Id,
                Name: subject.Name,
                Code: subject.Code,
                Description: subject.Description,
                StartDate: subject.StartDate,
                EndDate: subject.EndDate,
                TeacherId: subject.TeacherId,
                TeacherName: subject.Teacher != null
                    ? $"{subject.Teacher.FirstName} {subject.Teacher.LastName}"
                    : null,
                SchoolClassId: subject.SchoolClassId,
                SchoolClassName: subject.SchoolClass?.Name,
                EnrollmentCount: subject.Enrollments?.Count ?? 0,
                QuizCount: subject.Quizzes?.Count ?? 0
            );
        }

        public async Task<ServiceResult<PagedResponse<SubjectDto>>> GetAllSubjectsAsync(SubjectFilterDto filter)
        {
            _logger.LogInformation("Fetching subjects with filters.");
            try
            {
                var query = _context.Subjects.AsNoTracking();

                if (filter.IsActive.HasValue)
                {
                    if (filter.IsActive.Value)
                        query = query.WhereIsActive();
                    else
                        query = query.WhereIsInactive();
                }

                if (filter.TeacherId.HasValue)
                    query = query.Where(s => s.TeacherId == filter.TeacherId.Value);

                if (filter.StudentId.HasValue)
                {
                    query = query.Where(s =>
                        s.TeacherId != filter.StudentId.Value &&
                        !s.Enrollments.Any(e => e.StudentId == filter.StudentId.Value && e.ArchivedAt == null)
                    );
                }

                if (filter.SchoolClassId.HasValue)
                    query = query.Where(s => s.SchoolClassId == filter.SchoolClassId.Value);

                if (filter.StartingAfter.HasValue)
                {
                    query = query.Where(s => s.StartDate >= filter.StartingAfter.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Code))
                {
                    var codeSearch = filter.Code.ToLower();
                    query = query.Where(s => s.Code.ToLower().Contains(codeSearch));
                }

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var search = filter.SearchTerm.ToLower();
                    query = query.Where(s => s.Name.ToLower().Contains(search) ||
                                             s.Code.ToLower().Contains(search));
                }

                query = filter.IsDescending
                    ? query.OrderByDescending(s => s.Name)
                    : query.OrderBy(s => s.Name);

                int totalCount = await query.CountAsync();

                var subjects = await query
                    .Include(s => s.Teacher)
                    .Include(s => s.SchoolClass)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(s => new SubjectDto(
                        s.Id,
                        s.Name,
                        s.Code,
                        s.Description,
                        s.StartDate,
                        s.EndDate,
                        s.TeacherId,
                        s.Teacher != null ? s.Teacher.Username : null,
                        s.SchoolClassId,
                        s.SchoolClass != null ? s.SchoolClass.Name : null,
                        s.Enrollments.Count,
                        s.Quizzes.Count
                    ))
                    .ToListAsync();

                return ServiceResult<PagedResponse<SubjectDto>>.Ok(new PagedResponse<SubjectDto>
                {
                    Data = subjects,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subjects");
                return ServiceResult<PagedResponse<SubjectDto>>.Failure("Internal error.", ServiceError.InternalError);
            }
        }

        public async Task<ServiceResult<SubjectDto>> GetSubjectByIdAsync(long id)
        {
            var subject = await _context.Subjects
                .Where(s => s.Id == id)
                .Select(s => new SubjectDto(
                    s.Id,
                    s.Name,
                    s.Code,
                    s.Description,
                    s.StartDate,
                    s.EndDate,
                    s.TeacherId,
                    s.Teacher != null ? s.Teacher.Username : null,
                    s.SchoolClassId,
                    s.SchoolClass != null ? s.SchoolClass.Name : null,
                    s.Enrollments.Count,
                    s.Quizzes.Count
                ))
                .FirstOrDefaultAsync();

            if (subject == null)
                return ServiceResult<SubjectDto>.Failure("Subject not found.", ServiceError.NotFound);

            return ServiceResult<SubjectDto>.Ok(subject);
        }


        public async Task<ServiceResult<SubjectDto>> CreateSubjectAsync(CreateSubjectDto dto)
        {
            string? teacherName = null;
            string? className = null;

            if (dto.TeacherId.HasValue)
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == dto.TeacherId.Value);
                if (teacher == null)
                {
                    return ServiceResult<SubjectDto>.Failure("Provided Teacher ID does not exist or belongs to a non-teacher user.", ServiceError.ValidationFailed);
                }
                teacherName = teacher.Username;
            }

            if (dto.SchoolClassId.HasValue)
            {
                var schoolClass = await _context.SchoolClasses.FirstOrDefaultAsync(c => c.Id == dto.SchoolClassId.Value);
                if (schoolClass == null)
                {
                    return ServiceResult<SubjectDto>.Failure("Provided School Class ID does not exist.", ServiceError.ValidationFailed);
                }
                className = schoolClass.Name;
            }

            var subject = new Subject
            {
                Name = dto.Name,
                Code = dto.Code,
                Description = dto.Description,
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                EndDate = dto.EndDate,
                TeacherId = dto.TeacherId,
                SchoolClassId = dto.SchoolClassId
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return ServiceResult<SubjectDto>.Ok(new SubjectDto(
                subject.Id,
                subject.Name,
                subject.Code,
                subject.Description,
                subject.StartDate,
                subject.EndDate,
                subject.TeacherId,
                teacherName ?? "",
                subject.SchoolClassId,
                className ?? "",
                0,
                0));
        }

        public async Task<ServiceResult<BulkOperationResult<SubjectDto>>> CreateSubjectsBulkAsync(IEnumerable<Subject> subjects)
        {
            var bulkResult = new BulkOperationResult<SubjectDto>();
            var subjectList = subjects.ToList();
            if (!subjectList.Any()) return ServiceResult<BulkOperationResult<SubjectDto>>.Ok(bulkResult);

            var incomingCodes = subjectList.Where(s => !string.IsNullOrWhiteSpace(s.Code)).Select(s => s.Code.ToLower()).ToList();
            var existingCodes = await _context.Subjects
                .Where(s => incomingCodes.Contains(s.Code.ToLower()))
                .Select(s => s.Code.ToLower())
                .ToHashSetAsync();

            var validSubjectsToInsert = new List<Subject>();

            foreach (var subject in subjectList)
            {
                if (existingCodes.Contains(subject.Code.ToLower()))
                {
                    bulkResult.SkippedMessages.Add($"Subject '{subject.Name}' skipped: Code '{subject.Code}' already exists.");
                    continue;
                }

                validSubjectsToInsert.Add(subject);
                existingCodes.Add(subject.Code.ToLower());
            }

            if (validSubjectsToInsert.Any())
            {
                await _context.Subjects.AddRangeAsync(validSubjectsToInsert);
                await _context.SaveChangesAsync();

                bulkResult.Created = validSubjectsToInsert.Select(MapSubjectToDto).ToList();
            }

            return ServiceResult<BulkOperationResult<SubjectDto>>.Ok(bulkResult);
        }

        public async Task<ServiceResult<SubjectDto>> PatchSubjectAsync(long id, PatchSubjectDto dto)
        {
            _logger.LogInformation("Attempting to patch subject with ID: {Id}", id);

            var subject = await _context.Subjects
                .Include(s => s.Teacher)
                .Include(s => s.SchoolClass)
                .Include(s => s.Enrollments)
                .Include(s => s.Quizzes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
            {
                return ServiceResult<SubjectDto>.Failure("Subject not found.", ServiceError.NotFound);
            }

            if (!string.IsNullOrWhiteSpace(dto.Name)) subject.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.Code)) subject.Code = dto.Code;

            if (dto.Description != null)
            {
                subject.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description;
            }

            if (dto.StartDate.HasValue) subject.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) subject.EndDate = dto.EndDate.Value;

            if (dto.TeacherId.HasValue)
            {
                if (dto.TeacherId.Value == 0) // 0 means "Remove Teacher"
                {
                    subject.TeacherId = null;
                    subject.Teacher = null;
                }
                else if (dto.TeacherId.Value != subject.TeacherId)
                {
                    // Prevent assigning a teacher who is already enrolled as a student
                    bool isAlreadyEnrolled = subject.Enrollments.Any(e => e.StudentId == dto.TeacherId.Value);

                    if (isAlreadyEnrolled)
                    {
                        return ServiceResult<SubjectDto>.Failure(
                            "This user is currently enrolled in the subject as a student and cannot be assigned as its teacher.",
                            ServiceError.Forbidden);
                    }

                    var teacherExists = await _context.Teachers.AnyAsync(t => t.Id == dto.TeacherId.Value);
                    if (!teacherExists)
                        return ServiceResult<SubjectDto>.Failure("Provided Teacher ID is invalid.", ServiceError.ValidationFailed);

                    subject.TeacherId = dto.TeacherId.Value;
                }
            }

            if (dto.SchoolClassId.HasValue)
            {
                if (dto.SchoolClassId.Value == 0) // 0 means "Remove School Class"
                {
                    subject.SchoolClassId = null;
                    subject.SchoolClass = null;
                }
                else if (dto.SchoolClassId.Value != subject.SchoolClassId)
                {
                    var classExists = await _context.SchoolClasses.AnyAsync(c => c.Id == dto.SchoolClassId.Value);
                    if (!classExists)
                        return ServiceResult<SubjectDto>.Failure("Provided School Class ID is invalid.", ServiceError.ValidationFailed);

                    subject.SchoolClassId = dto.SchoolClassId.Value;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully patched subject {Id}", id);

                var updatedDto = new SubjectDto(
                    subject.Id,
                    subject.Name,
                    subject.Code,
                    subject.Description,
                    subject.StartDate,
                    subject.EndDate,
                    subject.TeacherId,
                    subject.Teacher?.Username,
                    subject.SchoolClassId,
                    subject.SchoolClass?.Name,
                    subject.Enrollments.Count,
                    subject.Quizzes.Count
                );

                return ServiceResult<SubjectDto>.Ok(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching subject {Id}", id);
                return ServiceResult<SubjectDto>.Failure("Internal error occurred while patching subject.", ServiceError.InternalError);
            }
        }

        public async Task<ServiceResult<bool>> DeleteSubjectAsync(long id)
        {
            _logger.LogInformation("Attempting to delete subject with ID: {Id}", id);

            var subject = await _context.Subjects
                .Include(s => s.Enrollments)
                .Include(s => s.Quizzes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
            {
                return ServiceResult<bool>.Failure("Subject not found.", ServiceError.NotFound);
            }

            try
            {
                // HARD DELETE: If the subject is completely empty, it's safe to actually delete it.
                if (subject.Enrollments.Count == 0 && subject.Quizzes.Count == 0)
                {
                    _context.Subjects.Remove(subject);
                    _logger.LogInformation("Hard deleted empty subject {Id}", id);
                }
                // SOFT DELETE: If there is historical data, we just archive it.
                else
                {
                    subject.EndDate = DateTime.UtcNow;
                    _logger.LogInformation("Soft deleted (archived) subject {Id} to preserve records.", id);
                }

                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subject {Id}", id);
                return ServiceResult<bool>.Failure("Internal error occurred while deleting subject.", ServiceError.InternalError);
            }
        }
    }
}
