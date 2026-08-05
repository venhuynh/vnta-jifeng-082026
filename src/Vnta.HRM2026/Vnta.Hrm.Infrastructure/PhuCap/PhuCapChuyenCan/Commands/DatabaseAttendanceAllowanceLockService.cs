using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Commands;

/// <summary>Owns row and period lock transitions; selected-row requests retain their optimistic-concurrency version.</summary>
public sealed class DatabaseAttendanceAllowanceLockService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    IAttendanceAllowanceLockStateRequestValidator lockStateRequestValidator,
    IAttendanceAllowanceBatchLockRequestValidator batchLockRequestValidator) : IAttendanceAllowanceLockService
{
    public Task<SetAttendanceAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetAttendanceAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default)
    {
        batchLockRequestValidator.Validate(request).ThrowIfInvalid();
        return auditedMutation.ExecuteAsync(AttendanceAllowanceCommandSupport.CreateOperationAuditCommand(auditScope, AuditActions.AttendanceAllowance.SetLockStateBatch), token => SetBatchCoreAsync(request, token), AttendanceAllowanceCommandSupport.LockAudit, cancellationToken);
    }

    public async Task<AttendanceAllowanceResultListItemDto> SetLockStateAsync(SetAttendanceAllowanceLockStateRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lockStateRequestValidator.Validate(request).ThrowIfInvalid();
        return await auditedMutation.ExecuteAsync(AttendanceAllowanceCommandSupport.CreateOperationAuditCommand(auditScope, AuditActions.AttendanceAllowance.SetLockState), token => SetCoreAsync(request, token), AttendanceAllowanceCommandSupport.LockAudit, cancellationToken);
    }

    private async Task<AttendanceAllowanceResultListItemDto> SetCoreAsync(SetAttendanceAllowanceLockStateRequest request, CancellationToken token)
    {
        var current = await (from d in dbContext.PayrollAttendanceAllowanceRecords.AsNoTracking() join s in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking() on d.PayrollAllowanceSummaryRecordId equals s.Id where d.PayrollAllowanceSummaryRecordId == request.Id select new { d.IsLocked, d.UpdatedAtUtc, SummaryIsLocked = s.IsLocked }).SingleOrDefaultAsync(token)
            ?? throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.NotFound, "Không tìm thấy dòng phụ cấp chuyên cần để khóa hoặc mở khóa.");
        if(current.SummaryIsLocked) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Locked, "Kỳ lương phụ cấp chuyên cần đã khóa, không thể thay đổi trạng thái dòng.");
        if(current.IsLocked == request.IsLocked) return await AttendanceAllowanceCommandSupport.GetByIdAsync(dbContext, request.Id, token) ?? throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.NotFound, "Không tìm thấy dòng phụ cấp chuyên cần sau khi kiểm tra trạng thái khóa.");
        if(current.UpdatedAtUtc != request.OriginalUpdatedAtUtc) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "Dòng phụ cấp chuyên cần đã được cập nhật bởi phiên khác. Hãy tải lại dữ liệu trước khi khóa hoặc mở khóa.");
        var now = AttendanceAllowanceCommandSupport.ToDatabaseTimestamp(DateTime.UtcNow); var actor = AttendanceAllowanceCommandSupport.CurrentActorId(auditScope);
        var changed = await dbContext.PayrollAttendanceAllowanceRecords.Where(d => d.PayrollAllowanceSummaryRecordId == request.Id && d.UpdatedAtUtc == request.OriginalUpdatedAtUtc && d.IsLocked != request.IsLocked).Where(d => dbContext.PayrollAllowanceSummaryRecords.Any(s => s.Id == d.PayrollAllowanceSummaryRecordId && !s.IsLocked)).ExecuteUpdateAsync(s => s.SetProperty(d => d.IsLocked, request.IsLocked).SetProperty(d => d.UpdatedAtUtc, now).SetProperty(d => d.UpdatedBy, actor), token);
        if(changed != 1) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "Dòng hoặc kỳ lương phụ cấp chuyên cần đã được cập nhật hoặc khóa bởi phiên khác. Hãy tải lại dữ liệu trước khi thao tác tiếp.");
        return await AttendanceAllowanceCommandSupport.GetByIdAsync(dbContext, request.Id, token) ?? throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.NotFound, "Không thể tải lại dòng phụ cấp chuyên cần sau khi thay đổi trạng thái khóa.");
    }

    private async Task<SetAttendanceAllowanceBatchLockStateResult> SetBatchCoreAsync(SetAttendanceAllowanceBatchLockStateRequest request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(request.Scope == AttendanceAllowanceBatchLockScope.SelectedRows)
        {
            var items = request.Items!
                .Where(item => item.Id != Guid.Empty)
                .GroupBy(item => item.Id)
                .Select(group => group.First())
                .ToArray();

            return await SetVersionedAsync(request, items, (short)request.PayrollYear, (short)request.PayrollMonth, token);
        }

        return await SetWholePeriodAsync(request, token);
    }

    private async Task<SetAttendanceAllowanceBatchLockStateResult> SetWholePeriodAsync(
        SetAttendanceAllowanceBatchLockStateRequest request,
        CancellationToken token)
    {
        var year = (short)request.PayrollYear;
        var month = (short)request.PayrollMonth;
        var query = dbContext.PayrollAttendanceAllowanceRecords.Where(d => dbContext.PayrollAllowanceSummaryRecords.Any(s => s.Id == d.PayrollAllowanceSummaryRecordId && s.PayrollMonth == month && s.PayrollYear == year));
        var targeted = await query.CountAsync(token);
        var skipped = await query.CountAsync(d => dbContext.PayrollAllowanceSummaryRecords.Any(s => s.Id == d.PayrollAllowanceSummaryRecordId && s.IsLocked), token);
        var unchanged = await query.CountAsync(d => d.IsLocked == request.IsLocked && dbContext.PayrollAllowanceSummaryRecords.Any(s => s.Id == d.PayrollAllowanceSummaryRecordId && !s.IsLocked), token);
        var now = AttendanceAllowanceCommandSupport.ToDatabaseTimestamp(DateTime.UtcNow); var actor = AttendanceAllowanceCommandSupport.CurrentActorId(auditScope);
        var updated = await query.Where(d => d.IsLocked != request.IsLocked).Where(d => dbContext.PayrollAllowanceSummaryRecords.Any(s => s.Id == d.PayrollAllowanceSummaryRecordId && !s.IsLocked)).ExecuteUpdateAsync(s => s.SetProperty(d => d.IsLocked, request.IsLocked).SetProperty(d => d.UpdatedAtUtc, now).SetProperty(d => d.UpdatedBy, actor), token);
        return new(request.PayrollYear, request.PayrollMonth, targeted, updated, unchanged, skipped, request.IsLocked, IsWholePeriod: true);
    }

    private async Task<SetAttendanceAllowanceBatchLockStateResult> SetVersionedAsync(SetAttendanceAllowanceBatchLockStateRequest request, IReadOnlyList<AttendanceAllowanceLockItem> items, short year, short month, CancellationToken token)
    {
        if(items.Count == 0) return new(request.PayrollYear, request.PayrollMonth, 0, 0, IsLocked: request.IsLocked, IsWholePeriod: false);
        var byId = items.ToDictionary(x => x.Id); var ids = byId.Keys.ToArray();
        var targets = await (from d in dbContext.PayrollAttendanceAllowanceRecords.AsNoTracking() join s in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking() on d.PayrollAllowanceSummaryRecordId equals s.Id where ids.Contains(d.PayrollAllowanceSummaryRecordId) && s.PayrollYear == year && s.PayrollMonth == month select new LockTarget(d.PayrollAllowanceSummaryRecordId, d.IsLocked, d.UpdatedAtUtc, s.IsLocked)).ToListAsync(token);
        if(targets.Count != items.Count) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.NotFound, "Có dòng phụ cấp chuyên cần không còn thuộc kỳ đang thao tác. Hãy tải lại dữ liệu trước khi khóa hoặc mở khóa.");
        if(targets.Any(x => x.UpdatedAtUtc != byId[x.Id].OriginalUpdatedAtUtc)) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "Có dòng phụ cấp chuyên cần đã được cập nhật bởi phiên khác. Hãy tải lại dữ liệu trước khi khóa hoặc mở khóa.");
        var eligible = targets.Where(x => !x.SummaryIsLocked && x.IsLocked != request.IsLocked).ToArray(); var skipped = targets.Count(x => x.SummaryIsLocked); var unchanged = targets.Count(x => !x.SummaryIsLocked && x.IsLocked == request.IsLocked);
        var now = AttendanceAllowanceCommandSupport.ToDatabaseTimestamp(DateTime.UtcNow); var actor = AttendanceAllowanceCommandSupport.CurrentActorId(auditScope);
        foreach(var target in eligible)
        {
            var changed = await dbContext.PayrollAttendanceAllowanceRecords.Where(d => d.PayrollAllowanceSummaryRecordId == target.Id && d.UpdatedAtUtc == byId[target.Id].OriginalUpdatedAtUtc && d.IsLocked != request.IsLocked).Where(d => dbContext.PayrollAllowanceSummaryRecords.Any(s => s.Id == d.PayrollAllowanceSummaryRecordId && s.PayrollYear == year && s.PayrollMonth == month && !s.IsLocked)).ExecuteUpdateAsync(s => s.SetProperty(d => d.IsLocked, request.IsLocked).SetProperty(d => d.UpdatedAtUtc, now).SetProperty(d => d.UpdatedBy, actor), token);
            if(changed != 1) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "Có dòng phụ cấp chuyên cần đã được cập nhật hoặc khóa bởi phiên khác. Hãy tải lại dữ liệu trước khi thao tác tiếp.");
        }
        return new(request.PayrollYear, request.PayrollMonth, targets.Count, eligible.Length, unchanged, skipped, request.IsLocked, false);
    }

    private sealed record LockTarget(Guid Id, bool IsLocked, DateTime? UpdatedAtUtc, bool SummaryIsLocked);
}
