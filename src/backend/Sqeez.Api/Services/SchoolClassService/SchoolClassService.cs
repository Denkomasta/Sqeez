using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Import;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Implements school class management and student/teacher assignment validation.
    /// </summary>
    public class SchoolClassService : BaseService<SchoolClassService>, ISchoolClassService
    {
        public SchoolClassService(SqeezDbContext context, ILogger<SchoolClassService> logger) : base(context, logger)
        {
        }

        private static SchoolClassDto MapClassToDto(SchoolClass schoolClass)
        {
            if (schoolClass == null) return null!;

            return new SchoolClassDto(
                Id: schoolClass.Id,
                Name: schoolClass.Name,
                AcademicYear: schoolClass.AcademicYear,
                Section: schoolClass.Section,
                TeacherId: schoolClass.Teacher?.Id,
                TeacherName: schoolClass.Teacher != null
                    ? $"{schoolClass.Teacher.FirstName} {schoolClass.Teacher.LastName}"
                    : null,
                StudentCount: schoolClass.Students?.Count ?? 0,
                SubjectCount: schoolClass.Subjects?.Count ?? 0
            );
        }

        public async Task<ServiceResult<PagedResponse<SchoolClassDto>>> GetAllClassesAsync(SchoolClassFilterDto filter)
        {
            _logger.LogInformation("Fetching school classes with filters.");

            try
            {
                var query = _context.SchoolClasses.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.Trim().ToLower();
                    query = query.Where(c =>
                        c.Name.ToLower().Contains(searchTerm) ||
                        c.Section.ToLower().Contains(searchTerm));
                }

                if (!string.IsNullOrWhiteSpace(filter.AcademicYear))
                {
                    query = query.Where(c => c.AcademicYear == filter.AcademicYear.Trim());
                }

                if (filter.TeacherId.HasValue)
                {
                    query = query.Where(c => c.Teacher != null && c.Teacher.Id == filter.TeacherId.Value);
                }

                int totalCount = await query.CountAsync();

                var classes = await query
                    .OrderBy(c => c.Name)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(c => new SchoolClassDto(
                        c.Id,
                        c.Name,
                        c.AcademicYear,
                        c.Section,
                        c.Teacher != null ? c.Teacher.Id : null,
                        c.Teacher != null ? c.Teacher.Username : null,
                        c.Students.Count,
                        c.Subjects.Count
                    ))
                    .ToListAsync();

                var pagedResponse = new PagedResponse<SchoolClassDto>
                {
                    Data = classes,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };

                return ServiceResult<PagedResponse<SchoolClassDto>>.Ok(pagedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching filtered school classes");
                return ServiceResult<PagedResponse<SchoolClassDto>>.Failure("Internal error occurred.", ServiceError.InternalError);
            }
        }

        public async Task<ServiceResult<SchoolClassDetailDto>> GetClassByIdAsync(long id)
        {
            _logger.LogInformation("Fetching school class details for ID: {ClassId}", id);

            try
            {
                var classDetail = await _context.SchoolClasses
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new SchoolClassDetailDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        AcademicYear = c.AcademicYear,
                        Section = c.Section,

                        Teacher = c.Teacher != null ? new TeacherBasicDto
                        {
                            Id = c.Teacher.Id,
                            FirstName = c.Teacher.FirstName,
                            LastName = c.Teacher.LastName,
                            Email = c.Teacher.Email,
                            AvatarUrl = c.Teacher.AvatarUrl,
                        } : null,

                        Students = c.Students.Select(s => new ClassmateDto
                        {
                            Id = s.Id,
                            FirstName = s.FirstName,
                            LastName = s.LastName,
                            Email = s.Email,
                            AvatarUrl = s.AvatarUrl
                        }).ToList(),

                        Subjects = c.Subjects.Select(s => new SubjectBasicDto
                        {
                            Id = s.Id,
                            Name = s.Name,
                            Code = s.Code
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (classDetail == null)
                {
                    _logger.LogWarning("School class with ID {ClassId} was not found.", id);
                    return ServiceResult<SchoolClassDetailDto>.Failure(
                        "Class not found.",
                        ServiceError.NotFound);
                }

                return ServiceResult<SchoolClassDetailDto>.Ok(classDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching school class details for ID: {ClassId}", id);
                return ServiceResult<SchoolClassDetailDto>.Failure(
                    "Internal error occurred.",
                    ServiceError.InternalError);
            }
        }

        public async Task<ServiceResult<SchoolClassDto>> CreateClassAsync(CreateSchoolClassDto dto)
        {
            _logger.LogInformation("Attempting to create a new school class.");

            Teacher? teacher = null;

            if (dto.TeacherId.HasValue)
            {
                teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == dto.TeacherId.Value);
                if (teacher == null)
                {
                    _logger.LogWarning("Failed to create school class: invalid TeacherId {TeacherId}", dto.TeacherId);
                    return ServiceResult<SchoolClassDto>.Failure("Provided Teacher ID does not exist or belongs to a non-teacher user.", ServiceError.ValidationFailed);
                }
            }

            var schoolClass = new SchoolClass
            {
                Name = dto.Name,
                AcademicYear = dto.AcademicYear,
                Section = dto.Section,
                Teacher = teacher
            };

            try
            {
                _context.SchoolClasses.Add(schoolClass);
                await _context.SaveChangesAsync();

                var createdDto = new SchoolClassDto(
                    schoolClass.Id,
                    schoolClass.Name,
                    schoolClass.AcademicYear,
                    schoolClass.Section,
                    schoolClass.Teacher?.Id,
                    schoolClass.Teacher?.Username,
                    0,
                    0
                );

                _logger.LogInformation("Successfully created school class {Id}", schoolClass.Id);
                return ServiceResult<SchoolClassDto>.Ok(createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating school class.");
                return ServiceResult<SchoolClassDto>.Failure("Internal error occurred while creating class.", ServiceError.InternalError);
            }
        }

        public async Task<ServiceResult<BulkOperationResult<SchoolClassDto>>> EnsureClassesExistAsync(IEnumerable<string> classNames)
        {
            var bulkResult = new BulkOperationResult<SchoolClassDto>();

            var cleanedNames = classNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct().ToList();
            if (!cleanedNames.Any()) return ServiceResult<BulkOperationResult<SchoolClassDto>>.Ok(bulkResult);

            var lowerNames = cleanedNames.Select(n => n.ToLower()).ToList();

            var existingClasses = await _context.SchoolClasses
                .Where(c => lowerNames.Contains(c.Name.ToLower()))
                .ToListAsync();

            bulkResult.Existing = existingClasses.Select(MapClassToDto).ToList();

            var existingNamesLower = existingClasses.Select(c => c.Name.ToLower()).ToHashSet();

            var currentDate = DateTime.UtcNow;
            string defaultAcademicYear = $"{(currentDate.Month >= 8 ? currentDate.Year : currentDate.Year - 1)}/{(currentDate.Month >= 8 ? currentDate.Year + 1 : currentDate.Year)}";

            var classesToCreate = cleanedNames
                .Where(n => !existingNamesLower.Contains(n.ToLower()))
                .Select(n => new SchoolClass { Name = n, AcademicYear = defaultAcademicYear, Section = "Imported" })
                .ToList();

            if (classesToCreate.Any())
            {
                await _context.SchoolClasses.AddRangeAsync(classesToCreate);
                await _context.SaveChangesAsync();

                bulkResult.Created = classesToCreate.Select(MapClassToDto).ToList();
            }

            return ServiceResult<BulkOperationResult<SchoolClassDto>>.Ok(bulkResult);
        }

        public async Task<ServiceResult<SchoolClassDto>> PatchClassAsync(long id, PatchSchoolClassDto dto)
        {
            _logger.LogInformation("Attempting to patch school class with ID: {Id}", id);

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.Students)
                .Include(c => c.Teacher)
                .Include(c => c.Subjects)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null)
            {
                return ServiceResult<SchoolClassDto>.Failure("Class not found.", ServiceError.NotFound);
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
                schoolClass.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.AcademicYear))
                schoolClass.AcademicYear = dto.AcademicYear;

            if (!string.IsNullOrWhiteSpace(dto.Section))
                schoolClass.Section = dto.Section;

            if (dto.TeacherId.HasValue)
            {
                // TeacherId = 0 means "Remove the teacher"
                if (dto.TeacherId.Value == 0)
                {
                    schoolClass.Teacher = null;
                }
                else if (dto.TeacherId.Value != schoolClass.Teacher?.Id)
                {
                    var newTeacher = await _context.Teachers.FindAsync(dto.TeacherId.Value);
                    if (newTeacher == null)
                    {
                        return ServiceResult<SchoolClassDto>.Failure("Provided Teacher ID is invalid.", ServiceError.ValidationFailed);
                    }

                    if (newTeacher.SchoolClassId == schoolClass.Id ||
                        schoolClass.Students.Any(student => student.Id == newTeacher.Id))
                    {
                        return ServiceResult<SchoolClassDto>.Failure(
                            "This teacher is already assigned to this class as a student and cannot manage the same class.",
                            ServiceError.ValidationFailed);
                    }

                    schoolClass.Teacher = newTeacher;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully patched school class {Id}", id);

                var updatedDto = new SchoolClassDto(
                    schoolClass.Id,
                    schoolClass.Name,
                    schoolClass.AcademicYear,
                    schoolClass.Section,
                    schoolClass.Teacher?.Id,
                    schoolClass.Teacher?.Username,
                    schoolClass.Students.Count,
                    schoolClass.Subjects.Count
                );

                return ServiceResult<SchoolClassDto>.Ok(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching school class {Id}", id);
                return ServiceResult<SchoolClassDto>.Failure("Internal error occurred while patching class.", ServiceError.InternalError);
            }
        }

        public async Task<ServiceResult<bool>> DeleteClassAsync(long id)
        {
            _logger.LogInformation("Attempting to delete school class with ID: {Id}", id);

            var schoolClass = await _context.SchoolClasses.FindAsync(id);
            if (schoolClass == null)
            {
                _logger.LogWarning("Deletion failed: School class with ID {Id} not found.", id);
                return ServiceResult<bool>.Failure("Class not found.", ServiceError.NotFound);
            }

            try
            {
                _context.SchoolClasses.Remove(schoolClass);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted school class {Id}", id);
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting school class {Id}", id);
                return ServiceResult<bool>.Failure("Internal error occurred while deleting class.", ServiceError.InternalError);
            }
        }

        public async Task<ServiceResult<bool>> AssignStudentsToClassAsync(long classId, AssignStudentsDto dto)
        {
            _logger.LogInformation("Assigning {Count} students to class {ClassId}", dto.StudentIds.Count, classId);

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
            {
                return ServiceResult<bool>.Failure("School class not found.", ServiceError.NotFound);
            }

            if (!dto.StudentIds.Any())
            {
                return ServiceResult<bool>.Ok(true);
            }

            try
            {
                if (schoolClass.Teacher != null && dto.StudentIds.Contains(schoolClass.Teacher.Id))
                {
                    return ServiceResult<bool>.Failure(
                        "The class teacher cannot also be assigned as a student of the same class.",
                        ServiceError.ValidationFailed);
                }

                var students = await _context.Students
                    .Where(s => dto.StudentIds.Contains(s.Id))
                    .ToListAsync();

                if (students.Count != dto.StudentIds.Count)
                {
                    return ServiceResult<bool>.Failure(
                    "One or more provided IDs do not exist.",
                    ServiceError.ValidationFailed);
                }

                foreach (var student in students)
                {
                    student.SchoolClassId = classId;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully assigned students to class {ClassId}", classId);
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning students to class {ClassId}", classId);
                return ServiceResult<bool>.Failure("Internal error occurred.", ServiceError.InternalError);
            }
        }

        public async Task<ServiceResult<bool>> RemoveStudentsFromClassAsync(long classId, RemoveStudentsDto dto)
        {
            _logger.LogInformation("Attempting to remove {Count} students from class {ClassId}", dto.StudentIds.Count, classId);

            var classExists = await _context.SchoolClasses.AnyAsync(c => c.Id == classId);
            if (!classExists)
            {
                return ServiceResult<bool>.Failure("School class not found.", ServiceError.NotFound);
            }

            if (!dto.StudentIds.Any())
            {
                return ServiceResult<bool>.Ok(true);
            }

            try
            {
                var studentsToRemove = await _context.Students
                    .Where(s => dto.StudentIds.Contains(s.Id) && s.SchoolClassId == classId)
                    .ToListAsync();

                foreach (var student in studentsToRemove)
                {
                    student.SchoolClassId = null;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully removed {RemovedCount} students from class {ClassId}", studentsToRemove.Count, classId);
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing students from class {ClassId}", classId);
                return ServiceResult<bool>.Failure("Internal error occurred while removing students.", ServiceError.InternalError);
            }
        }
    }
}
