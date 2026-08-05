namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;

/// <summary>
/// External-data boundary for the attendance roster that determines who belongs to a deduction
/// summary snapshot in a payroll period.
/// </summary>
public interface IPayrollDeductionSummaryTargetRosterPolicy
{
    Task<PayrollDeductionSummaryTargetRoster> GetTargetRosterAsync(
        PayrollDeductionSummaryTargetRosterRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record PayrollDeductionSummaryTargetRosterRequest(
    short TargetPayrollYear,
    short TargetPayrollMonth);

public sealed record PayrollDeductionSummaryTargetRoster(
    IReadOnlyList<Guid> EmployeeIds);
