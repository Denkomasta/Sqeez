using Sqeez.Api.Constants;
using Sqeez.Api.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Base user DTO returned for student-shaped profiles and polymorphic user lists.
    /// </summary>
    [JsonDerivedType(typeof(StudentDto), typeDiscriminator: "student")]
    [JsonDerivedType(typeof(TeacherDto), typeDiscriminator: "teacher")]
    [JsonDerivedType(typeof(AdminDto), typeDiscriminator: "admin")]
    public record StudentDto
    {
        public long Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public int CurrentXP { get; init; }
        public UserRole Role { get; init; }
        public string? AvatarUrl { get; init; }
        public DateTime LastSeen { get; init; }
        public long? SchoolClassId { get; init; }
    }

    /// <summary>
    /// Teacher profile DTO with teaching department and managed class assignment.
    /// </summary>
    public record TeacherDto : StudentDto
    {
        public string? Department { get; init; }
        public long? ManagedClassId { get; init; }
    }

    /// <summary>
    /// Admin profile DTO with administrative contact details.
    /// </summary>
    public record AdminDto : TeacherDto
    {
        public string PhoneNumber { get; init; } = string.Empty;
    }

    /// <summary>
    /// Polymorphic user creation DTO; the role discriminator selects student, teacher, or admin fields.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "role")]
    [JsonDerivedType(typeof(CreateStudentDto), typeDiscriminator: "student")]
    [JsonDerivedType(typeof(CreateTeacherDto), typeDiscriminator: "teacher")]
    [JsonDerivedType(typeof(CreateAdminDto), typeDiscriminator: "admin")]
    public record CreateStudentDto
    {
        [StringLength(ValidationConstants.NameMaxLength)]
        public string FirstName { get; init; } = string.Empty;

        [StringLength(ValidationConstants.NameMaxLength)]
        public string LastName { get; init; } = string.Empty;

        [StringLength(ValidationConstants.UsernameMaxLength, MinimumLength = ValidationConstants.UsernameMinLength)]
        [RegularExpression(ValidationConstants.UsernameRegex)]
        public string Username { get; init; } = string.Empty;

        [StringLength(ValidationConstants.EmailMaxLength)]
        [RegularExpression(ValidationConstants.EmailRegex)]
        public string Email { get; init; } = string.Empty;

        [StringLength(ValidationConstants.PasswordMaxLength, MinimumLength = 8)]
        public string Password { get; init; } = string.Empty;
        public long? SchoolClassId { get; init; }
    }

    /// <summary>
    /// Teacher creation DTO with optional department and managed class assignment.
    /// </summary>
    public record CreateTeacherDto : CreateStudentDto
    {
        [StringLength(ValidationConstants.DepartmentMaxLength)]
        [RegularExpression(ValidationConstants.DepartmentRegex)]
        public string? Department { get; init; }
        public long? ManagedClassId { get; init; }
    }

    /// <summary>
    /// Admin creation DTO with optional phone number.
    /// </summary>
    public record CreateAdminDto : CreateTeacherDto
    {
        [StringLength(ValidationConstants.PhoneNumberMaxLength)]
        [RegularExpression(ValidationConstants.InternationalPhoneRegex,
            ErrorMessage = "Phone number must start with 00, followed by a country code and your number.")]
        public string? PhoneNumber { get; init; }
    }

    /// <summary>
    /// Polymorphic user patch DTO; the role discriminator selects which role-specific fields may be supplied.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "role")]
    [JsonDerivedType(typeof(PatchStudentDto), typeDiscriminator: "student")]
    [JsonDerivedType(typeof(PatchTeacherDto), typeDiscriminator: "teacher")]
    [JsonDerivedType(typeof(PatchAdminDto), typeDiscriminator: "admin")]
    public record PatchStudentDto
    {
        [StringLength(ValidationConstants.UsernameMaxLength, MinimumLength = ValidationConstants.UsernameMinLength)]
        [RegularExpression(ValidationConstants.UsernameRegex)]
        public string? Username { get; init; }

        public long? SchoolClassId { get; init; }
        [StringLength(ValidationConstants.UrlMaxLength)]
        public string? AvatarUrl { get; init; }
    }

    /// <summary>
    /// Teacher patch DTO for department and managed class assignment updates.
    /// </summary>
    public record PatchTeacherDto : PatchStudentDto
    {
        [StringLength(ValidationConstants.DepartmentMaxLength)]
        [RegularExpression(ValidationConstants.DepartmentRegex)]
        public string? Department { get; init; }
        public long? ManagedClassId { get; init; }
    }

    /// <summary>
    /// Admin patch DTO for phone-number updates.
    /// </summary>
    public record PatchAdminDto : PatchTeacherDto
    {
        [StringLength(ValidationConstants.PhoneNumberMaxLength)]
        [RegularExpression(ValidationConstants.InternationalPhoneRegex,
            ErrorMessage = "Phone number must start with 00, followed by a country code and your number.")]
        public string? PhoneNumber { get; init; }
    }

    /// <summary>
    /// Sort fields supported by user search.
    /// </summary>
    public enum UserSortField
    {
        Username,
        XP,
        LastSeen
    }

    /// <summary>
    /// User search, role, assignment, and sorting filters.
    /// </summary>
    public class UserFilterDto : PagedFilterDto
    {
        [StringLength(ValidationConstants.SearchTermMaxLength)]
        public string? SearchTerm { get; init; }
        public bool? IsOnline { get; init; }
        public long? SchoolClassId { get; init; }
        public long? SubjectId { get; init; }
        public bool? IsArchived { get; init; }

        public UserRole? Role { get; init; }
        public bool StrictRoleOnly { get; init; } = false;

        [StringLength(ValidationConstants.DepartmentMaxLength)]
        public string? Department { get; init; }
        [StringLength(ValidationConstants.PhoneNumberMaxLength)]
        public string? PhoneNumber { get; init; }
        public UserSortField? SortBy { get; init; }
        public bool IsDescending { get; init; } = false;
        public bool? HasAssignedClass { get; init; }
    }

    /// <summary>
    /// Response returned after a successful avatar upload.
    /// </summary>
    public record AvatarUploadResponseDto(string Message, string AvatarUrl);

    /// <summary>
    /// Compact student profile used inside class detail responses.
    /// </summary>
    public record ClassmateDto
    {
        public long Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public int CurrentXp { get; init; }
        public string? AvatarUrl { get; init; }
    }

    /// <summary>
    /// Compact teacher profile used inside class detail responses.
    /// </summary>
    public record TeacherBasicDto
    {
        public long Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? AvatarUrl { get; init; }
    }

    /// <summary>
    /// Expanded user profile including class, badges, and enrollment history.
    /// </summary>
    public record DetailedUserDto : AdminDto
    {
        public SchoolClassBasicDto? SchoolClassDetails { get; init; }
        public List<StudentBadgeBasicDto> Badges { get; init; } = new();
        public List<EnrollmentBasicDto> Enrollments { get; init; } = new();
    }
}
