namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Validates hazard-allowance input at application boundaries, independent of transport and persistence.</summary>
public interface IHazardAllowanceRequestValidator
{
    HazardAllowanceValidationResult ValidatePeriod(int payrollMonth, int payrollYear);
    HazardAllowanceValidationResult Validate(HazardAllowanceFilter filter);
    HazardAllowanceValidationResult Validate(RefreshHazardAllowanceRequest request);
    HazardAllowanceValidationResult Validate(UpdateHazardAllowanceManualValuesRequest request);
    HazardAllowanceValidationResult Validate(SetHazardAllowanceEntitlementBatchRequest request);
    HazardAllowanceValidationResult Validate(SetHazardAllowanceLockStateRequest request);
    HazardAllowanceValidationResult Validate(SetHazardAllowanceBatchLockStateRequest request);
    HazardAllowanceValidationResult Validate(CreateHazardAllowanceExportJobRequest request);
}

/// <summary>Transport-neutral validation result shared by HTTP handlers and application workflows.</summary>
public sealed record HazardAllowanceValidationResult(string? ErrorMessage)
{
    public bool IsValid => string.IsNullOrWhiteSpace(ErrorMessage);

    public void ThrowIfInvalid()
    {
        if(!IsValid)
            throw new InvalidOperationException(ErrorMessage);
    }
}
