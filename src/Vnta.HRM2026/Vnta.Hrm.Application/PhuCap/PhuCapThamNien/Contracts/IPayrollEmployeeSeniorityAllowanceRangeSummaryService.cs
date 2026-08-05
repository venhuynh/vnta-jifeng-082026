namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>Reads the lightweight seniority-range summaries used by filters.</summary>
public interface IPayrollEmployeeSeniorityAllowanceRangeSummaryService
{
    Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>> GetRangeSummariesAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default);
}
