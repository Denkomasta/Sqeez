using System.ComponentModel.DataAnnotations;

namespace Sqeez.Api.Validation
{
    /// <summary>
    /// Validates that a date-time input is either null or explicitly normalized to UTC.
    /// </summary>
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
        /// Validates nullable and non-null DateTime values.
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
