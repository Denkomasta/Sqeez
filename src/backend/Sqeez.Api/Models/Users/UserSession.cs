namespace Sqeez.Api.Models.Users
{
    /// <summary>
    /// Refresh-token session for a user account.
    /// </summary>
    public class UserSession
    {
        /// <summary>
        /// Primary identifier of the session row.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Identifier of the user that owns the refresh token.
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// User that owns the refresh token.
        /// </summary>
        public Student User { get; set; } = null!;

        /// <summary>
        /// Stored refresh token value used for session rotation and revocation.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// UTC timestamp when the session was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp after which the refresh token is no longer valid.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Indicates whether the session has been explicitly revoked.
        /// </summary>
        public bool IsRevoked { get; set; }
    }
}
