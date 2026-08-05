using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Vnta.Hrm.Web.Client.Validation;

/// <summary>
/// Validates decimal ranges using invariant-culture bounds, independently of the UI culture.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class InvariantDecimalRangeAttribute : ValidationAttribute
{
    private readonly decimal minimum;
    private readonly decimal maximum;

    public InvariantDecimalRangeAttribute(string minimum, string maximum)
    {
        this.minimum = decimal.Parse(minimum, NumberStyles.Number, CultureInfo.InvariantCulture);
        this.maximum = decimal.Parse(maximum, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if(value is null)
        {
            return ValidationResult.Success;
        }

        if(value is decimal number && number >= minimum && number <= maximum)
        {
            return ValidationResult.Success;
        }

        var errorMessage = ErrorMessage
            ?? $"{validationContext.DisplayName} must be between {minimum} and {maximum}.";

        return new ValidationResult(
            errorMessage,
            validationContext.MemberName is null ? null : [validationContext.MemberName]);
    }
}
