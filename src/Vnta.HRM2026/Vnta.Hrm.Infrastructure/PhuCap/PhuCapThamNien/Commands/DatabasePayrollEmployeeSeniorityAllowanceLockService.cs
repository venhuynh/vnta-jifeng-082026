using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Commands;

public sealed class DatabasePayrollEmployeeSeniorityAllowanceLockService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation)
    : IPayrollEmployeeSeniorityAllowanceLockService
{
    public Task<PayrollEmployeeSeniorityAllowanceListItemDto> SetLockStateAsync(
        SetPayrollEmployeeSeniorityAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        SetSingleAsync(request, cancellationToken);

    public Task<SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        SetBatchAsync(request, cancellationToken);

    private async Task<PayrollEmployeeSeniorityAllowanceListItemDto> SetSingleAsync(
        SetPayrollEmployeeSeniorityAllowanceLockStateRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if(request.PayrollAllowanceSummaryRecordId == Guid.Empty)
            throw new InvalidOperationException("Thiếu dòng tổng hợp phụ cấp để khóa hoặc mở khóa.");
        if(request.OriginalUpdatedAtUtc == default)
            throw new PayrollEmployeeSeniorityAllowanceConflictException("Thiếu phiên bản dữ liệu gốc. Vui lòng tải lại dữ liệu trước khi thay đổi trạng thái khóa.");

        var target = await (
            from detail in dbContext.PayrollEmployeeSeniorityAllowances
            join summary in dbContext.PayrollAllowanceSummaryRecords
                on detail.PayrollAllowanceSummaryRecordId equals summary.Id
            where detail.PayrollAllowanceSummaryRecordId == request.PayrollAllowanceSummaryRecordId
            select new { summary.IsLocked })
            .SingleOrDefaultAsync(cancellationToken);
        if(target is null)
            throw new InvalidOperationException("Không tìm thấy dòng phụ cấp thâm niên cần cập nhật trạng thái khóa.");
        if(target.IsLocked)
            throw new InvalidOperationException("Dòng tổng hợp phụ cấp đã khóa nên không thể thay đổi trạng thái khóa phụ cấp thâm niên.");

        var command = auditScope.Current ?? throw new InvalidOperationException("Thiếu audit scope cho cập nhật trạng thái khóa phụ cấp thâm niên.");
        var now = SeniorityAllowanceCommandSupport.GetDatabaseNow();
        await auditedMutation.ExecuteAsync(command with { ActionIntent = AuditActions.SeniorityAllowance.LockStateChanged }, async token =>
        {
            var count = await dbContext.PayrollEmployeeSeniorityAllowances
                .Where(x => x.PayrollAllowanceSummaryRecordId == request.PayrollAllowanceSummaryRecordId
                    && (x.UpdatedAtUtc ?? x.CreatedAtUtc) == request.OriginalUpdatedAtUtc
                    && dbContext.PayrollAllowanceSummaryRecords.Any(summary =>
                        summary.Id == x.PayrollAllowanceSummaryRecordId && !summary.IsLocked))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsLocked, request.IsLocked)
                    .SetProperty(x => x.UpdatedAtUtc, now).SetProperty(x => x.UpdatedBy, SeniorityAllowanceCommandSupport.SystemActor), token);
            if(count != 1)
                throw new PayrollEmployeeSeniorityAllowanceConflictException("Dòng phụ cấp thâm niên đã được thay đổi bởi thao tác khác. Vui lòng tải lại dữ liệu.");
            return true;
        }, _ => new AuditOperationEvent(AuditActions.SeniorityAllowance.LockStateChanged, AuditEntityTypes.SeniorityAllowance,
            request.PayrollAllowanceSummaryRecordId.ToString("D"), Metadata: new Dictionary<string, string> { ["isLocked"] = request.IsLocked.ToString(), ["concurrencyTokenProvided"] = bool.TrueString }), cancellationToken);
        dbContext.ChangeTracker.Clear();
        return await SeniorityAllowanceCommandSupport.ReadSingleAsync(dbContext, request.PayrollAllowanceSummaryRecordId, cancellationToken);
    }

    private async Task<SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult> SetBatchAsync(
        SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SeniorityAllowanceCommandSupport.ValidatePeriod(request.PayrollYear, request.PayrollMonth);
        var explicitTargets = request.PayrollAllowanceSummaryRecordIds is not null;
        var ids = request.PayrollAllowanceSummaryRecordIds?.Where(x => x != Guid.Empty).Distinct().ToArray();
        if(explicitTargets && (ids is null || ids.Length == 0))
            return new SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult(request.PayrollYear, request.PayrollMonth, 0, 0);

        var requestCommand = auditScope.Current;
        var command = requestCommand ?? new AuditCommand(Guid.NewGuid(), AuditActions.SeniorityAllowance.BatchLockStateChanged,
            new AuditActor(SeniorityAllowanceCommandSupport.SystemActor, SeniorityAllowanceCommandSupport.SystemActor, AuditActorKind.System, AuditSource.Worker),
            Guid.NewGuid().ToString("N"), AuditCaptureMode.OperationOnly, Metadata: new Dictionary<string, string> { ["auditScope"] = "system-fallback" });
        return await auditedMutation.ExecuteAsync(command with { ActionIntent = AuditActions.SeniorityAllowance.BatchLockStateChanged }, async token =>
        {
            var targetQuery = dbContext.PayrollEmployeeSeniorityAllowances.Where(detail => dbContext.PayrollAllowanceSummaryRecords.Any(summary =>
                summary.Id == detail.PayrollAllowanceSummaryRecordId
                && summary.PayrollYear == request.PayrollYear
                && summary.PayrollMonth == request.PayrollMonth));
            if(explicitTargets)
                targetQuery = targetQuery.Where(x => ids!.Contains(x.PayrollAllowanceSummaryRecordId));

            var targets = await targetQuery.CountAsync(token);
            var skippedRows = await (
                from detail in targetQuery
                join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                    on detail.PayrollAllowanceSummaryRecordId equals summary.Id
                join employee in dbContext.Employees.AsNoTracking()
                    on summary.EmployeeId equals employee.Id into employees
                from employee in employees.DefaultIfEmpty()
                where summary.IsLocked
                select new PayrollEmployeeSeniorityAllowanceLockStateSkippedRow(
                    detail.PayrollAllowanceSummaryRecordId,
                    employee == null ? null : employee.EmployeeCode,
                    employee == null ? null : ((employee.LastName ?? string.Empty) + " " + (employee.FirstName ?? string.Empty)).Trim(),
                    "Dòng Phụ cấp tổng hợp đã khóa."))
                .OrderBy(x => x.EmployeeCode ?? string.Empty)
                .ThenBy(x => x.EmployeeName ?? string.Empty)
                .ToListAsync(token);
            var skippedSummaryLocked = skippedRows.Count;
            var eligibleQuery = targetQuery.Where(detail => dbContext.PayrollAllowanceSummaryRecords.Any(summary =>
                summary.Id == detail.PayrollAllowanceSummaryRecordId && !summary.IsLocked));
            var unchanged = await eligibleQuery.CountAsync(x => x.IsLocked == request.IsLocked, token);
            var updated = await eligibleQuery.Where(x => x.IsLocked != request.IsLocked).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsLocked, request.IsLocked).SetProperty(x => x.UpdatedAtUtc, SeniorityAllowanceCommandSupport.GetDatabaseNow())
                .SetProperty(x => x.UpdatedBy, SeniorityAllowanceCommandSupport.SystemActor), token);
            return new SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult(
                request.PayrollYear,
                request.PayrollMonth,
                targets,
                updated,
                unchanged,
                skippedSummaryLocked,
                request.IsLocked,
                !explicitTargets,
                skippedRows);
        }, result => new AuditOperationEvent(AuditActions.SeniorityAllowance.BatchLockStateChanged, AuditEntityTypes.SeniorityAllowance,
            EntityDisplayName: $"{result.PayrollMonth:00}/{result.PayrollYear}", Metadata: new Dictionary<string, string>
            {
                ["isLocked"] = request.IsLocked.ToString(), ["targetRowCount"] = result.TargetRowCount.ToString(),
                ["updatedCount"] = result.UpdatedCount.ToString(), ["unchangedCount"] = result.UnchangedCount.ToString(),
                ["skippedSummaryLockedCount"] = result.SkippedSummaryLockedCount.ToString(),
                ["auditScope"] = requestCommand is null ? "system-fallback" : "request"
            }), cancellationToken);
    }
}
