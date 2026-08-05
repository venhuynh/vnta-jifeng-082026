using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

/// <summary>Obsolete in-process adapter retained for legacy callers that construct the former all-in-one type.</summary>
[Obsolete("Inject a focused deduction-summary contract; this adapter has no EF behavior of its own.")]
public sealed class DatabasePayrollDeductionSummaryService : IPayrollDeductionSummaryService
{
    private readonly DatabasePayrollDeductionSummaryReadService read;
    private readonly DatabasePayrollDeductionSummarySyncService sync;
    private readonly DatabasePayrollDeductionSummaryRefreshService refresh;
    private readonly DatabasePayrollDeductionSummaryManualAdjustmentService manual;
    private readonly DatabasePayrollDeductionSummaryLockService locks;

    public DatabasePayrollDeductionSummaryService(ApplicationDbContext dbContext, IAuditScope auditScope,
        IAuditedMutation auditedMutation, IPayrollDeductionSummaryRequestValidator requestValidator,
        IPayrollDeductionSummaryTargetRosterPolicy? targetRosterPolicy = null)
    {
        var roster = targetRosterPolicy ?? new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext);
        read = new(dbContext, auditScope, auditedMutation, requestValidator);
        sync = new(dbContext, auditScope, auditedMutation, roster, requestValidator);
        refresh = new(dbContext, auditScope, auditedMutation, roster, requestValidator);
        manual = new(dbContext, auditScope, auditedMutation, roster, requestValidator);
        locks = new(dbContext, auditScope, auditedMutation, roster, requestValidator);
    }

    public Task<PayrollDeductionSummaryPageDto> SearchAsync(PayrollDeductionSummaryFilter filter, CancellationToken cancellationToken = default) => read.SearchAsync(filter, cancellationToken);
    public Task<IReadOnlyList<PayrollDeductionSummaryExportItemDto>> ExportPeriodAsync(int payrollMonth, int payrollYear, PayrollDeductionSummaryExportFormat format, CancellationToken cancellationToken = default) => read.ExportPeriodAsync(payrollMonth, payrollYear, format, cancellationToken);
    public Task<SyncPayrollDeductionSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(SyncPayrollDeductionSummaryFromPreviousMonthRequest request, CancellationToken cancellationToken = default) => sync.SyncFromPreviousMonthAsync(request, cancellationToken);
    public Task<RefreshPayrollDeductionSummaryResult> RefreshAsync(RefreshPayrollDeductionSummaryRequest request, CancellationToken cancellationToken = default) => refresh.RefreshAsync(request, cancellationToken);
    public Task<RecalculatePayrollDeductionSummaryPeriodResult> RecalculatePeriodAsync(RecalculatePayrollDeductionSummaryPeriodRequest request, CancellationToken cancellationToken = default) => refresh.RecalculatePeriodAsync(request, cancellationToken);
    public Task<PayrollDeductionSummaryListItemDto> UpdateManualOtherDeductionAsync(UpdatePayrollDeductionSummaryManualOtherDeductionRequest request, CancellationToken cancellationToken = default) => manual.UpdateManualOtherDeductionAsync(request, cancellationToken);
    public Task<PayrollDeductionSummaryListItemDto> SetLockStateAsync(SetPayrollDeductionSummaryLockStateRequest request, CancellationToken cancellationToken = default) => locks.SetLockStateAsync(request, cancellationToken);
    public Task<SetPayrollDeductionSummaryBatchLockStateResult> SetLockStateBatchAsync(SetPayrollDeductionSummaryBatchLockStateRequest request, CancellationToken cancellationToken = default) => locks.SetLockStateBatchAsync(request, cancellationToken);
}
