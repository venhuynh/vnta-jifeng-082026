using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

public sealed class DatabasePayrollDeductionSummarySyncService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    IPayrollDeductionSummaryTargetRosterPolicy? targetRosterPolicy = null,
    IPayrollDeductionSummaryRequestValidator? requestValidator = null)
    : PayrollDeductionSummaryCommandServiceBase(dbContext, auditScope, auditedMutation, targetRosterPolicy, requestValidator),
        IPayrollDeductionSummarySyncService
{
    /// <summary>Synchronizes the target snapshot from the preceding payroll period.</summary>
    public Task<SyncPayrollDeductionSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncPayrollDeductionSummaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default) =>
        MapConcurrencyAsync(() => ExecuteSyncFromPreviousMonthAsync(request, cancellationToken));
}
