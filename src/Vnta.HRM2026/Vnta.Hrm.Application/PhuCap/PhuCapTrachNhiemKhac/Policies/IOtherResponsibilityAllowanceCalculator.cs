namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Policies;

/// <summary>Pure calculation policy for the prorated responsibility allowance.</summary>
public interface IOtherResponsibilityAllowanceCalculator
{
    OtherResponsibilityAllowanceCalculationResult Calculate(OtherResponsibilityAllowanceCalculationInput input);
}

/// <summary>All monetary and workday inputs required to calculate one allowance snapshot.</summary>
public sealed record OtherResponsibilityAllowanceCalculationInput(
    decimal StandardResponsibilityAllowanceAmount,
    decimal StandardWorkdayCount,
    decimal AllowanceCalculationWorkdayCount);

/// <summary>The server-authoritative actual allowance amount for one allowance snapshot.</summary>
public sealed record OtherResponsibilityAllowanceCalculationResult(decimal ActualResponsibilityAllowanceAmount);
