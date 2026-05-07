namespace Sqeez.Api.Validation
{
    /// <summary>
    /// Shared UTC date-time validation helpers used by data annotations and service-level checks.
    /// </summary>
    public static class UtcDateTimeValidator
    {
        /// <summary>
        /// Standard validation error for date-time inputs that must be normalized to UTC.
        /// </summary>
        public const string ErrorMessageFormat = "The {0} field must be a UTC date-time value. Use an ISO 8601 value ending with 'Z'.";

        /// <summary>
        /// Returns whether a non-null date-time value is explicitly marked as UTC.
        /// </summary>
        public static bool IsUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc;
        }

        /// <summary>
        /// Returns whether a nullable date-time value is null or explicitly marked as UTC.
        /// </summary>
        public static bool IsUtc(DateTime? value)
        {
            return !value.HasValue || IsUtc(value.Value);
        }

        /// <summary>
        /// Formats the standard UTC validation error for a field name.
        /// </summary>
        public static string GetErrorMessage(string fieldName)
        {
            return string.Format(ErrorMessageFormat, fieldName);
        }
    }
}
