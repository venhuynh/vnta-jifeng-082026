using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

public sealed class DatabasePayrollDeductionSummaryLockService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation,
    IPayrollDeductionSummaryTargetRosterPolicy? targetRosterPolicy = null,
    IPayrollDeductionSummaryRequestValidator? requestValidator = null)
    : PayrollDeductionSummaryCommandServiceBase(dbContext, auditScope, auditedMutation, targetRosterPolicy, requestValidator), IPayrollDeductionSummaryLockService
{
    public async Task<PayrollDeductionSummaryListItemDto> SetLockStateAsync(SetPayrollDeductionSummaryLockStateRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();
        if(request.Id == Guid.Empty) throw new InvalidOperationException("Thiếu định danh dòng tổng kết khấu trừ cần cập nhật.");
        var actor = NormalizeActor(request.Actor);
        var audit = auditScope.Current;
        await MapConcurrencyAsync(() => auditedMutation.ExecuteAsync(CreateLockStateAuditCommand(audit, AuditActions.DeductionSummary.LockStateChanged), async token =>
        {
            var row = await dbContext.PayrollDeductionSummaryRecords.SingleOrDefaultAsync(x => x.Id == request.Id, token)
                ?? throw new InvalidOperationException("Không tìm thấy dòng tổng kết khấu trừ cần cập nhật.");
            var decision = PayrollDeductionSummaryLockStatePolicy.Decide(PayrollDeductionSummaryLockStatePolicy.FromPersistenceFlag(row.IsLocked), PayrollDeductionSummaryLockStatePolicy.FromPersistenceFlag(request.IsLocked));
            if(decision == PayrollDeductionSummaryLockStateChangeDecision.ChangeRequired && PayrollDeductionSummaryConcurrencyPolicy.Evaluate(new(GetRecordVersion(row), request.OriginalUpdatedAtUtc)) != PayrollDeductionSummaryConcurrencyStatus.VersionMatches)
                throw new PayrollDeductionSummaryConcurrencyException("Dòng tổng kết khấu trừ đã được cập nhật bởi phiên khác.");
            var updated = decision == PayrollDeductionSummaryLockStateChangeDecision.ChangeRequired;
            if(updated) { row.IsLocked = request.IsLocked; row.UpdatedAtUtc = GetDatabaseNow(); row.UpdatedBy = actor; }
            return new SetPayrollDeductionSummaryBatchLockStateResult(row.PayrollYear, row.PayrollMonth, 1, updated ? 1 : 0, updated ? 0 : 1);
        }, result => CreateLockStateAuditEvent(AuditActions.DeductionSummary.LockStateChanged, request.IsLocked, "single-row", audit is null ? "system-fallback" : "request", result), cancellationToken));
        return await GetByIdAsync(request.Id, cancellationToken) ?? throw new InvalidOperationException("Không thể tải lại dòng tổng kết khấu trừ vừa cập nhật.");
    }

    public Task<SetPayrollDeductionSummaryBatchLockStateResult> SetLockStateBatchAsync(SetPayrollDeductionSummaryBatchLockStateRequest request, CancellationToken cancellationToken = default) => SetLockStateBatchCoreAsync(request, cancellationToken);

    private async Task<SetPayrollDeductionSummaryBatchLockStateResult> SetLockStateBatchCoreAsync(SetPayrollDeductionSummaryBatchLockStateRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();
        var versioned = request.Items?.Where(x => x.Id != Guid.Empty).GroupBy(x => x.Id).Select(x => x.First()).ToArray();
        var explicitTargets = request.PayrollDeductionSummaryRecordIds is not null || request.Items is not null;
        if(request.PayrollDeductionSummaryRecordIds?.Any(x => x == Guid.Empty) == true) throw new InvalidOperationException("Danh sách dòng được chọn có định danh không hợp lệ.");
        var ids = versioned is not null ? versioned.Select(x => x.Id).ToArray() : request.PayrollDeductionSummaryRecordIds?.Distinct().ToArray();
        if(explicitTargets && ids?.Length == 0) return new(request.PayrollYear, request.PayrollMonth, 0, 0);
        var month = (short)request.PayrollMonth; var year = (short)request.PayrollYear;
        var query = dbContext.PayrollDeductionSummaryRecords.Where(x => x.PayrollYear == year && x.PayrollMonth == month);
        if(ids is { Length: > 0 }) query = query.Where(x => ids.Contains(x.Id));
        var audit = auditScope.Current;
        return await MapConcurrencyAsync(() => auditedMutation.ExecuteAsync(CreateLockStateAuditCommand(audit, AuditActions.DeductionSummary.BatchLockStateChanged), async token =>
        {
            if(dbContext.Database.IsNpgsql()) await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({GetPeriodLockKey(year, month)})", token);
            var rows = await query.ToListAsync(token);
            if(explicitTargets && rows.Count != ids!.Length) throw new InvalidOperationException("Một hoặc nhiều dòng được chọn không tồn tại hoặc không thuộc kỳ lương đang áp dụng.");
            if(versioned is not null && rows.Any(row => PayrollDeductionSummaryConcurrencyPolicy.Evaluate(new(GetRecordVersion(row), versioned.Single(x => x.Id == row.Id).OriginalUpdatedAtUtc)) != PayrollDeductionSummaryConcurrencyStatus.VersionMatches)) throw new PayrollDeductionSummaryConcurrencyException("Có dòng tổng kết khấu trừ đã được cập nhật bởi phiên khác.");
            var targets = rows.Where(row => row.IsLocked != request.IsLocked).ToArray();
            if(targets.Length > 0) { var now = GetDatabaseNow(); var actor = NormalizeActor(request.Actor); foreach(var row in targets) { row.IsLocked = request.IsLocked; row.UpdatedAtUtc = now; row.UpdatedBy = actor; } }
            return new SetPayrollDeductionSummaryBatchLockStateResult(request.PayrollYear, request.PayrollMonth, rows.Count, targets.Length, rows.Count - targets.Length);
        }, result => CreateLockStateAuditEvent(AuditActions.DeductionSummary.BatchLockStateChanged, request.IsLocked, explicitTargets ? "selected-rows" : "whole-period", audit is null ? "system-fallback" : "request", result), cancellationToken));
    }
}
