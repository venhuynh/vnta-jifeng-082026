using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapThamNien;

/// <summary>
/// UI-facing boundary for the seniority-allowance coordinator. Implementations own
/// authorization, auditing and transport; components only depend on this contract.
/// </summary>
public interface IPayrollEmployeeSeniorityAllowanceDataProvider
{
    Task PreparePeriodAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<PhuCapThamNienPage> SearchPageAsync(PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>> LoadRangeSummariesAsync(PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhuCapThamNienRecord>> LoadAllForPeriodExportAsync(int payrollYear, int payrollMonth, CancellationToken cancellationToken = default);
    Task<RefreshPayrollEmployeeSeniorityAllowanceResult> RefreshAsync(RefreshPayrollEmployeeSeniorityAllowanceRequest request, CancellationToken cancellationToken = default);
    Task<PhuCapThamNienRecord> SetLockStateAsync(Guid payrollAllowanceSummaryRecordId, bool isLocked, DateTime originalUpdatedAtUtc, CancellationToken cancellationToken = default);
    Task<SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default);
    Task<PhuCapThamNienRecord> UpdateManualValuesAsync(Guid payrollAllowanceSummaryRecordId, decimal allowanceAmount, string? note, DateTime originalUpdatedAtUtc, CancellationToken cancellationToken = default);
}
