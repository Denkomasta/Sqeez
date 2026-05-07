using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.AuthService
{
    /// <summary>
    /// Defines account, session, verification, password-reset, and role-management operations.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user when public registration is enabled.
        /// </summary>
        /// <param name="dto">Registration data containing identity, credentials, and remember-me preference.</param>
        /// <returns>
        /// A successful result when the user is created. Returns conflict for duplicate email or username,
        /// or forbidden when public registration is disabled.
        /// </returns>
        Task<ServiceResult<bool>> RegisterAsync(RegisterDTO dto);

        /// <summary>
        /// Verifies an email verification token and starts an authenticated session for the verified user.
        /// </summary>
        /// <param name="token">The email verification token stored on the user.</param>
        /// <param name="rememberMe">Whether the created refresh-token session should use the longer lifetime.</param>
        /// <returns>
        /// Authentication tokens when the token is valid. Returns not found for an unknown token or unauthorized
        /// for an expired token.
        /// </returns>
        Task<ServiceResult<AuthResponseDto>> VerifyEmailAsync(string token, bool rememberMe);

        /// <summary>
        /// Sends another verification email for an unverified account without revealing whether the account exists.
        /// </summary>
        /// <param name="dto">Email address and remember-me preference to encode in the verification link.</param>
        /// <returns>
        /// A successful result for missing accounts, already verified accounts, and queued emails. Returns too many
        /// requests only when an unverified account is still inside the resend cooldown window.
        /// </returns>
        Task<ServiceResult<bool>> ResendVerificationEmailAsync(ResendVerificationDto dto);

        /// <summary>
        /// Starts the password-reset flow for an email address without revealing whether the account exists.
        /// </summary>
        /// <param name="email">The account email address.</param>
        /// <returns>
        /// A successful result in all user-visible cases. Existing users receive a password-reset token unless
        /// a recent token is still inside the resend throttle window.
        /// </returns>
        Task<ServiceResult<bool>> ForgotPasswordAsync(string email);

        /// <summary>
        /// Replaces a user's password using a valid password-reset token.
        /// </summary>
        /// <param name="dto">Reset token and new password.</param>
        /// <returns>
        /// A successful result when the password is updated. Returns bad request for an unknown token or
        /// unauthorized for an expired token.
        /// </returns>
        Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto dto);

        /// <summary>
        /// Authenticates a verified user and creates a refresh-token session.
        /// </summary>
        /// <param name="dto">Login credentials and remember-me preference.</param>
        /// <returns>
        /// Access and refresh tokens when credentials are valid. Returns not found or unauthorized for invalid
        /// credentials, and unauthorized when email verification is required but incomplete.
        /// </returns>
        Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDTO dto);

        /// <summary>
        /// Rotates a valid refresh-token session into a new access token and refresh token.
        /// </summary>
        /// <param name="dto">The refresh token to rotate.</param>
        /// <returns>
        /// New authentication tokens when the session is active. Returns unauthorized for missing, revoked,
        /// or expired sessions.
        /// </returns>
        Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto);

        /// <summary>
        /// Revokes one refresh-token session or all active sessions for a user.
        /// </summary>
        /// <param name="userId">The user to log out.</param>
        /// <param name="refreshToken">Optional specific refresh token to revoke; when omitted, all active sessions are revoked.</param>
        /// <returns>A successful result when sessions are revoked, or not found when the user does not exist.</returns>
        Task<ServiceResult<bool>> LogoutAsync(long userId, string? refreshToken = null);

        /// <summary>
        /// Loads the current authenticated user's lightweight profile.
        /// </summary>
        /// <param name="userId">The authenticated user's id.</param>
        /// <param name="role">The authenticated role claim used to choose the concrete user set.</param>
        /// <returns>The current user DTO, or not found when the user cannot be resolved.</returns>
        Task<ServiceResult<UserDTO>> GetCurrentUserAsync(long userId, string? role);

        /// <summary>
        /// Updates a user's role and role-specific metadata when performed by an authorized admin.
        /// </summary>
        /// <param name="adminId">The admin performing the role update.</param>
        /// <param name="dto">Target user id, new role, and optional role metadata.</param>
        /// <returns>
        /// A successful result when the role is unchanged or updated. Returns unauthorized for non-admin callers,
        /// forbidden for protected super-user/admin operations, conflict for active teacher dependencies, or not found
        /// for a missing target user.
        /// </returns>
        Task<ServiceResult<bool>> UpdateUserRoleAsync(long adminId, UpdateRoleDTO dto);
    }
}
