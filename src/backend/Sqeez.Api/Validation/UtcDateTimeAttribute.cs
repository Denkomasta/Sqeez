using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.Validation
{
    /// <summary>
    /// Validates that a date-time input is either null or explicitly normalized to UTC.
    /// </summary>
    /// <remarks>
    /// JSON clients should send ISO 8601 values with a trailing <c>Z</c>, for example <c>2026-05-20T10:00:00Z</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class UtcDateTimeAttribute : ValidationAttribute
    {
        /// <summary>
        /// Creates the attribute with the shared UTC validation error message.
        /// </summary>
        public UtcDateTimeAttribute()
        {
            ErrorMessage = UtcDateTimeValidator.ErrorMessageFormat;
        }

        /// <summary>
        /// Validates nullable and non-null <see cref="DateTime"/> values.
        /// </summary>
        public override bool IsValid(object? value)
        {
            return value switch
            {
                null => true,
                DateTime dateTime => UtcDateTimeValidator.IsUtc(dateTime),
                _ => false
            };
        }
    }
}
