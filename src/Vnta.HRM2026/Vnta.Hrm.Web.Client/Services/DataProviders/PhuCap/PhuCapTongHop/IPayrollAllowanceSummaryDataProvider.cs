using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTongHop;

/// <summary>
/// Screen-facing contract for the allowance-summary workflow.
/// It keeps Blazor components and neighbouring features independent from the
/// concrete adapter that translates application contracts to view models.
/// </summary>
public interface IPayrollAllowanceSummaryDataProvider
{
    Task<PayrollAllowanceSummaryOverviewDto> GetSummaryAsync(
        PayrollAllowanceSummaryFilter filter,
        CancellationToken cancellationToken = default);

    Task<PayrollAllowanceSummaryLoadResult> SearchAsync(
        PayrollAllowanceSummaryFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PayrollAllowanceSummaryExportRecord>> LoadAllForPeriodExportAsync(
        int payrollMonth,
        int payrollYear,
        PayrollAllowanceSummaryExportFormat format,
        CancellationToken cancellationToken = default);

    Task<SyncPayrollAllowanceSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        CancellationToken cancellationToken = default);

    Task<RefreshPayrollAllowanceSummaryResult> RefreshAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        Guid? payrollAllowanceSummaryRecordId = null,
        CancellationToken cancellationToken = default);

    Task<PayrollAllowanceSummaryRecord> SetLockStateAsync(
        Guid id,
        bool isLocked,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<SetPayrollAllowanceSummaryBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollAllowanceSummaryBatchLockStateRequest request,
        CancellationToken cancellationToken = default);

    Task<PayrollAllowanceSummaryRecord> UpdateManualValuesAsync(
        UpdatePayrollAllowanceSummaryManualValuesRequest request,
        CancellationToken cancellationToken = default);
}
