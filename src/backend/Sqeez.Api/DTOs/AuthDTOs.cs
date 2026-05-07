using Sqeez.Api.Constants;
using Sqeez.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.DTOs
{
    /// <summary>
    /// Login request containing credentials and session persistence preference.
    /// </summary>
    public record LoginDTO
    {
        public LoginDTO() { }

        public LoginDTO(string Email, string Password, bool RememberMe = false)
        {
            this.Email = Email;
            this.Password = Password;
            this.RememberMe = RememberMe;
        }

        [StringLength(ValidationConstants.EmailMaxLength)]
        [RegularExpression(ValidationConstants.EmailRegex, ErrorMessage = "Invalid email format.")]
        public string Email { get; init; } = string.Empty;

        [StringLength(ValidationConstants.PasswordMaxLength, MinimumLength = 8)]
        public string Password { get; init; } = string.Empty;

        public bool RememberMe { get; init; } = false;
    }

    /// <summary>
    /// Lightweight authenticated user profile returned by auth endpoints.
    /// </summary>
    public record UserDTO(
        long Id,
        string Username,
        string Email,
        string CurrentXP,
        UserRole Role,
        string? AvatarUrl
    );

    /// <summary>
    /// Public registration request for a new unverified account.
    /// </summary>
    public record RegisterDTO
    {
        public RegisterDTO() { }

        public RegisterDTO(string FirstName, string LastName, string Username, string Email, string Password, bool RememberMe = false)
        {
            this.FirstName = FirstName;
            this.LastName = LastName;
            this.Username = Username;
            this.Email = Email;
            this.Password = Password;
            this.RememberMe = RememberMe;
        }

        [RegularExpression(ValidationConstants.PersonNameRegex, ErrorMessage = "First name can only contain letters, spaces, and dashes.")]
        [StringLength(ValidationConstants.NameMaxLength)]
        public string FirstName { get; init; } = string.Empty;

        [RegularExpression(ValidationConstants.PersonNameRegex, ErrorMessage = "Last name can only contain letters, spaces, and dashes.")]
        [StringLength(ValidationConstants.NameMaxLength)]
        public string LastName { get; init; } = string.Empty;

        [RegularExpression(ValidationConstants.UsernameRegex, ErrorMessage = "Username can only contain letters, numbers, dashes, and underscores.")]
        [StringLength(ValidationConstants.UsernameMaxLength, MinimumLength = ValidationConstants.UsernameMinLength, ErrorMessage = "Username must be between 3 and 20 characters.")]
        public string Username { get; init; } = string.Empty;

        [RegularExpression(ValidationConstants.EmailRegex, ErrorMessage = "Invalid email format.")]
        [StringLength(ValidationConstants.EmailMaxLength)]
        public string Email { get; init; } = string.Empty;

        [RegularExpression(ValidationConstants.PasswordComplexityRegex, ErrorMessage = "Password must be at least 8 characters long and contain an uppercase letter, a lowercase letter, a number, and a special character.")]
        [StringLength(ValidationConstants.PasswordMaxLength, MinimumLength = 8)]
        public string Password { get; init; } = string.Empty;

        public bool RememberMe { get; init; } = false;
    }

    /// <summary>
    /// Administrative request for changing a user's role and role-specific metadata.
    /// </summary>
    public record UpdateRoleDTO
    {
        public UpdateRoleDTO() { }

        public UpdateRoleDTO(long Id, UserRole Role, string? Department = null, string? PhoneNumber = null)
        {
            this.Id = Id;
            this.Role = Role;
            this.Department = Department;
            this.PhoneNumber = PhoneNumber;
        }

        [Required]
        public long Id { get; init; }

        [Required]
        public UserRole Role { get; init; }

        [RegularExpression(ValidationConstants.DepartmentRegex, ErrorMessage = "Department contains invalid characters. No HTML tags allowed.")]
        [StringLength(ValidationConstants.DepartmentMaxLength)]
        public string? Department { get; init; }

        [RegularExpression(ValidationConstants.FlexiblePhoneRegex, ErrorMessage = "Phone number must be between 7 and 15 characters and contain only valid phone symbols.")]
        [StringLength(ValidationConstants.PhoneNumberMaxLength)]
        public string? PhoneNumber { get; init; }
    }

    /// <summary>
    /// Pair of access and refresh tokens produced by authentication flows.
    /// </summary>
    public record AuthResponseDto(
        string AccessToken,
        string RefreshToken
    );

    /// <summary>
    /// Refresh-token rotation request.
    /// </summary>
    public record RefreshTokenDto
    {
        public RefreshTokenDto() { }

        public RefreshTokenDto(string RefreshToken)
        {
            this.RefreshToken = RefreshToken;
        }

        [StringLength(ValidationConstants.TokenMaxLength)]
        public string RefreshToken { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request for resending account verification email.
    /// </summary>
    public record ResendVerificationDto
    {
        public ResendVerificationDto() { }

        public ResendVerificationDto(string Email, bool RememberMe = false)
        {
            this.Email = Email;
            this.RememberMe = RememberMe;
        }

        [RegularExpression(ValidationConstants.EmailRegex, ErrorMessage = "Invalid email format.")]
        [StringLength(ValidationConstants.EmailMaxLength)]
        public string Email { get; init; } = string.Empty;

        public bool RememberMe { get; init; } = false;
    }

    /// <summary>
    /// Request for starting the password-reset email flow.
    /// </summary>
    public record ForgotPasswordDto
    {
        public ForgotPasswordDto() { }

        public ForgotPasswordDto(string Email)
        {
            this.Email = Email;
        }

        [RegularExpression(ValidationConstants.EmailRegex, ErrorMessage = "Invalid email format.")]
        [StringLength(ValidationConstants.EmailMaxLength)]
        public string Email { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request for replacing a password using a reset token.
    /// </summary>
    public record ResetPasswordDto
    {
        public ResetPasswordDto() { }

        public ResetPasswordDto(string Token, string NewPassword)
        {
            this.Token = Token;
            this.NewPassword = NewPassword;
        }

        [StringLength(ValidationConstants.TokenMaxLength)]
        public string Token { get; init; } = string.Empty;

        [RegularExpression(ValidationConstants.PasswordComplexityRegex, ErrorMessage = "Password must be at least 8 characters long and contain an uppercase letter, a lowercase letter, a number, and a special character.")]
        [StringLength(ValidationConstants.PasswordMaxLength, MinimumLength = 8)]
        public string NewPassword { get; init; } = string.Empty;
    }
}
