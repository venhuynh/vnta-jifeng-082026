namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Pure business rule for creating a hazard allowance snapshot.</summary>
public interface IHazardAllowanceCalculationPolicy
{
    HazardAllowanceCalculationResult Calculate(HazardAllowanceCalculationInput input);
}

public sealed record HazardAllowanceCalculationInput(
    string? DepartmentPath,
    decimal QualifiedWorkdayCount,
    decimal LateEarlyDeductionDays,
    bool? IsEligibleForAllowance = null,
    string? PositionName = null);

public sealed record HazardAllowanceCalculationResult(
    decimal QualifiedWorkdayCount,
    decimal LateEarlyDeductionDays,
    decimal PayableWorkdayCount,
    decimal HazardAllowancePerDay,
    decimal HazardAllowanceAmount,
    bool IsEligibleDepartment,
    string? ExclusionReason,
    bool IsEligibleForAllowance = true);
