namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;

/// <summary>Read capability used by the allowance-summary list and overview.</summary>
public interface IPayrollAllowanceSummaryReadService
{
    Task<PayrollAllowanceSummaryOverviewDto> GetSummaryAsync(PayrollAllowanceSummaryFilter filter, CancellationToken cancellationToken = default);
    Task<PayrollAllowanceSummaryPageDto> SearchAsync(PayrollAllowanceSummaryFilter filter, CancellationToken cancellationToken = default);
}

public interface IPayrollAllowanceSummaryExportService
{
    Task<IReadOnlyList<PayrollAllowanceSummaryExportRowDto>> ExportAsync(PayrollAllowanceSummaryExportRequest request, CancellationToken cancellationToken = default);
}

public interface IPayrollAllowanceSummaryPreviousMonthSyncService
{
    Task<SyncPayrollAllowanceSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(SyncPayrollAllowanceSummaryFromPreviousMonthRequest request, CancellationToken cancellationToken = default);
}

public interface IPayrollAllowanceSummaryRefreshService
{
    Task<RefreshPayrollAllowanceSummaryResult> RefreshAsync(RefreshPayrollAllowanceSummaryRequest request, CancellationToken cancellationToken = default);
}

public interface IPayrollAllowanceSummaryDeletionService
{
    Task DeleteAsync(DeletePayrollAllowanceSummariesRequest request, CancellationToken cancellationToken = default);
}

public interface IPayrollAllowanceSummaryManualAdjustmentService
{
    Task<PayrollAllowanceSummaryListItemDto> UpdateManualValuesAsync(UpdatePayrollAllowanceSummaryManualValuesRequest request, CancellationToken cancellationToken = default);
}

public interface IPayrollAllowanceSummaryLockService
{
    Task<PayrollAllowanceSummaryListItemDto> SetLockStateAsync(SetPayrollAllowanceSummaryLockStateRequest request, CancellationToken cancellationToken = default);
    Task<SetPayrollAllowanceSummaryBatchLockStateResult> SetLockStateBatchAsync(SetPayrollAllowanceSummaryBatchLockStateRequest request, CancellationToken cancellationToken = default);
}
