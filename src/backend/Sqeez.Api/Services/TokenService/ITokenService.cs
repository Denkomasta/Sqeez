using Sqeez.Api.DTOs;
using Sqeez.Api.Models.Users;

namespace Sqeez.Api.Services.TokenService
{
    /// <summary>
    /// Defines JWT access token and opaque refresh token creation operations.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Creates a signed JWT access token for the provided user.
        /// </summary>
        /// <param name="user">User whose id, username, and role are written as token claims.</param>
        /// <returns>The serialized access token, or internal error when token creation fails.</returns>
        ServiceResult<string> CreateToken(Student user);

        /// <summary>
        /// Generates a cryptographically random refresh token.
        /// </summary>
        /// <returns>A base64-encoded refresh token suitable for storing in a user session.</returns>
        string GenerateRefreshToken();
    }
}
