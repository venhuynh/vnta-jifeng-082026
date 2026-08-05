using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Commands;

internal static class AttendanceAllowanceCommandSupport
{
    internal const string SystemActor = "system";
    public static DateTime ToDatabaseTimestamp(DateTime value) => PostgreSqlTimestamp.ToTimestampWithoutTimeZone(value);
    public static string CurrentActorId(IAuditScope scope) => scope.Current?.Actor.ActorId ?? SystemActor;
    public static AuditCommand CreateOperationAuditCommand(IAuditScope scope, string action) { var c = scope.Current; return new(c?.OperationId ?? Guid.NewGuid(), action, c?.Actor ?? new AuditActor(SystemActor, SystemActor, AuditActorKind.System, AuditSource.Worker), c?.CorrelationId ?? Guid.NewGuid().ToString("N"), AuditCaptureMode.OperationOnly, Metadata: c?.Metadata); }
    public static Task<AttendanceAllowanceResultListItemDto?> GetByIdAsync(ApplicationDbContext dbContext, Guid id, CancellationToken token) => AttendanceAllowanceReadProjection.GetByIdAsync(dbContext, id, token);
    public static async Task ReloadClaimedRowsAsync(ApplicationDbContext dbContext, PayrollAttendanceAllowanceRecordRow detail, PayrollAllowanceSummaryRecordRow summary, CancellationToken token) { await dbContext.Entry(detail).ReloadAsync(token); await dbContext.Entry(summary).ReloadAsync(token); }
    public static AttendanceAllowanceKpViolationState ToKpViolationState(bool value) => value ? AttendanceAllowanceKpViolationState.Present : AttendanceAllowanceKpViolationState.NotPresent;
    public static AuditOperationEvent RefreshAudit(RefreshAttendanceAllowanceResult r) => new(AuditActions.AttendanceAllowance.Refresh, r.PayrollAllowanceSummaryRecordId.HasValue ? "AttendanceAllowanceRow" : "AttendanceAllowancePeriod", r.PayrollAllowanceSummaryRecordId?.ToString("N") ?? $"{r.PayrollYear:D4}-{r.PayrollMonth:D2}", Outcome: r.UpdatedCount == 0 ? AuditOperationOutcome.NoChanges : AuditOperationOutcome.Succeeded, Metadata: new Dictionary<string, string> { ["scope"] = r.PayrollAllowanceSummaryRecordId.HasValue ? "row" : "period", ["targetSummaryId"] = r.PayrollAllowanceSummaryRecordId?.ToString("N") ?? string.Empty, ["matched"] = r.MatchedRowCount.ToString(), ["updated"] = r.UpdatedCount.ToString(), ["skippedLocked"] = r.SkippedLockedCount.ToString() });
    public static AuditOperationEvent LockAudit(SetAttendanceAllowanceBatchLockStateResult r) => new(AuditActions.AttendanceAllowance.SetLockStateBatch, "AttendanceAllowancePeriod", $"{r.PayrollYear:D4}-{r.PayrollMonth:D2}", Outcome: r.UpdatedCount == 0 ? AuditOperationOutcome.NoChanges : AuditOperationOutcome.Succeeded, Metadata: new Dictionary<string, string> { ["targeted"] = r.TargetRowCount.ToString(), ["updated"] = r.UpdatedCount.ToString(), ["unchanged"] = r.UnchangedCount.ToString(), ["skippedSummaryLocked"] = r.SkippedSummaryLockedCount.ToString(), ["targetLockState"] = r.IsLocked.ToString(), ["scope"] = r.IsWholePeriod ? "whole-period" : "selected-rows" });
    public static AuditOperationEvent LockAudit(AttendanceAllowanceResultListItemDto r) => new(AuditActions.AttendanceAllowance.SetLockState, "AttendanceAllowanceRecord", r.Id.ToString("N"), Outcome: AuditOperationOutcome.Succeeded, Metadata: new Dictionary<string, string> { ["payrollYear"] = r.PayrollYear.ToString(), ["payrollMonth"] = r.PayrollMonth.ToString(), ["isLocked"] = r.IsLocked.ToString() });
}
