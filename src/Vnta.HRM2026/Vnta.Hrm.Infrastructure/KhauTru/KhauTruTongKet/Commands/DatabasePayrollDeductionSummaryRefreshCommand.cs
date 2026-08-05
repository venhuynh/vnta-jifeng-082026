using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

public sealed class DatabasePayrollDeductionSummaryRefreshService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    IPayrollDeductionSummaryTargetRosterPolicy? targetRosterPolicy = null,
    IPayrollDeductionSummaryRequestValidator? requestValidator = null)
    : PayrollDeductionSummaryCommandServiceBase(dbContext, auditScope, auditedMutation, targetRosterPolicy, requestValidator),
        IPayrollDeductionSummaryRefreshService
{
    /// <summary>Refreshes one deduction-summary row while preserving its transaction and audit scope.</summary>
    public Task<RefreshPayrollDeductionSummaryResult> RefreshAsync(
        RefreshPayrollDeductionSummaryRequest request,
        CancellationToken cancellationToken = default) =>
        MapConcurrencyAsync(() => ExecuteRefreshAsync(request, cancellationToken));

    /// <summary>Recalculates the selected payroll period while retaining its advisory-lock behavior.</summary>
    public Task<RecalculatePayrollDeductionSummaryPeriodResult> RecalculatePeriodAsync(
        RecalculatePayrollDeductionSummaryPeriodRequest request,
        CancellationToken cancellationToken = default) =>
        MapConcurrencyAsync(() => ExecuteRecalculatePeriodAsync(request, cancellationToken));
}
