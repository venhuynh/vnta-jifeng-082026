using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

/// <summary>Temporary contract adapter; it owns neither EF access nor command behavior.</summary>
[Obsolete("Inject a deduction-summary capability contract directly.")]
public sealed class PayrollDeductionSummaryCompatibilityFacade(
    IPayrollDeductionSummaryReadService read,
    IPayrollDeductionSummaryExportService export,
    IPayrollDeductionSummarySyncService sync,
    IPayrollDeductionSummaryRefreshService refresh,
    IPayrollDeductionSummaryManualAdjustmentService manual,
    IPayrollDeductionSummaryLockService locks)
    : IPayrollDeductionSummaryService, IPayrollDeductionSummaryCommands
{
    public Task<PayrollDeductionSummaryPageDto> SearchAsync(PayrollDeductionSummaryFilter filter, CancellationToken cancellationToken = default) => read.SearchAsync(filter, cancellationToken);
    public Task<IReadOnlyList<PayrollDeductionSummaryExportItemDto>> ExportPeriodAsync(int payrollMonth, int payrollYear, PayrollDeductionSummaryExportFormat format, CancellationToken cancellationToken = default) => export.ExportPeriodAsync(payrollMonth, payrollYear, format, cancellationToken);
    public Task<SyncPayrollDeductionSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(SyncPayrollDeductionSummaryFromPreviousMonthRequest request, CancellationToken cancellationToken = default) => sync.SyncFromPreviousMonthAsync(request, cancellationToken);
    public Task<RefreshPayrollDeductionSummaryResult> RefreshAsync(RefreshPayrollDeductionSummaryRequest request, CancellationToken cancellationToken = default) => refresh.RefreshAsync(request, cancellationToken);
    public Task<RecalculatePayrollDeductionSummaryPeriodResult> RecalculatePeriodAsync(RecalculatePayrollDeductionSummaryPeriodRequest request, CancellationToken cancellationToken = default) => refresh.RecalculatePeriodAsync(request, cancellationToken);
    public Task<PayrollDeductionSummaryListItemDto> UpdateManualOtherDeductionAsync(UpdatePayrollDeductionSummaryManualOtherDeductionRequest request, CancellationToken cancellationToken = default) => manual.UpdateManualOtherDeductionAsync(request, cancellationToken);
    public Task<PayrollDeductionSummaryListItemDto> SetLockStateAsync(SetPayrollDeductionSummaryLockStateRequest request, CancellationToken cancellationToken = default) => locks.SetLockStateAsync(request, cancellationToken);
    public Task<SetPayrollDeductionSummaryBatchLockStateResult> SetLockStateBatchAsync(SetPayrollDeductionSummaryBatchLockStateRequest request, CancellationToken cancellationToken = default) => locks.SetLockStateBatchAsync(request, cancellationToken);
}
