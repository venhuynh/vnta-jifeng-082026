using Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;

namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;

/// <summary>Validates meal-allowance requests at the application boundary.</summary>
public interface IMealAllowanceRequestValidator
{
    MealAllowanceValidationResult ValidatePeriod(int payrollMonth, int payrollYear);
    MealAllowanceValidationResult Validate(RefreshMealAllowanceRequest request);
    MealAllowanceValidationResult Validate(UpdateMealAllowanceManualValuesRequest request);
    MealAllowanceValidationResult Validate(SetMealAllowanceLockStateBatchRequest request);
}

/// <summary>Transport-neutral validation result shared by HTTP and database adapters.</summary>
public sealed record MealAllowanceValidationResult(string? ErrorMessage)
{
    public bool IsValid => string.IsNullOrWhiteSpace(ErrorMessage);

    public void ThrowIfInvalid()
    {
        if(!IsValid)
            throw new InvalidOperationException(ErrorMessage);
    }
}
