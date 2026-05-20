namespace Sqeez.Api.Models.Users
{
    /// <summary>
    /// User account with administrative permissions.
    /// </summary>
    public class Admin : Teacher
    {
        /// <summary>
        /// Optional administrative contact phone number.
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
