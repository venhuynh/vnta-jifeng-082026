using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

public sealed class DatabaseHazardAllowanceLockService(
    ApplicationDbContext dbContext,
    HazardAllowanceLockStatePolicy lockStatePolicy,
    IHazardAllowanceRequestValidator requestValidator)
    : IHazardAllowanceLockService
{
    public async Task SetLockStateAsync(SetHazardAllowanceLockStateRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();
        if (dbContext.ChangeTracker.HasChanges()) dbContext.ChangeTracker.Clear();
        var ids = request.PayrollAllowanceSummaryRecordIds.Distinct().ToArray();

        var rows = await (
            from summary in dbContext.PayrollAllowanceSummaryRecords
            join detail in dbContext.PayrollHazardAllowanceRecords on summary.Id equals detail.PayrollAllowanceSummaryRecordId
            where ids.Contains(summary.Id)
            select new LockTarget(summary, detail)).ToListAsync(cancellationToken);
        if (rows.Count != ids.Length)
            throw new InvalidOperationException("Danh sách khóa có dòng không thuộc phụ cấp độc hại hoặc không còn tồn tại.");

        if (rows.Any(row => row.Summary.IsLocked))
            throw new InvalidOperationException("Kỳ lương phụ cấp độc hại đã khóa, không thể thay đổi trạng thái dòng.");

        if (Apply(rows, request.IsLocked, HazardAllowancePersistence.ToDatabaseTimestamp(DateTime.UtcNow),
                HazardAllowancePersistence.NormalizeOptional(request.RequestedBy) ?? "system") > 0)
            await dbContext.SaveChangesWithConcurrencyGuardAsync(cancellationToken);
    }

    public async Task<SetHazardAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetHazardAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();
        if (dbContext.ChangeTracker.HasChanges()) dbContext.ChangeTracker.Clear();
        var hasExplicitTargets = request.PayrollAllowanceSummaryRecordIds is not null;
        var ids = request.PayrollAllowanceSummaryRecordIds?.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (hasExplicitTargets && (ids is null || ids.Length == 0))
            return new SetHazardAllowanceBatchLockStateResult(request.PayrollYear, request.PayrollMonth, 0, 0);

        var month = (short)request.PayrollMonth;
        var year = (short)request.PayrollYear;
        var query =
            from summary in dbContext.PayrollAllowanceSummaryRecords
            join detail in dbContext.PayrollHazardAllowanceRecords on summary.Id equals detail.PayrollAllowanceSummaryRecordId
            where summary.PayrollMonth == month && summary.PayrollYear == year
            select new { Summary = summary, Detail = detail };
        if (hasExplicitTargets) query = query.Where(row => ids!.Contains(row.Summary.Id));

        var rows = await query
            .Select(row => new LockTarget(row.Summary, row.Detail))
            .ToListAsync(cancellationToken);
        var updated = Apply(rows, request.IsLocked, HazardAllowancePersistence.ToDatabaseTimestamp(DateTime.UtcNow),
            HazardAllowancePersistence.NormalizeOptional(request.RequestedBy) ?? "system");
        if (updated > 0) await dbContext.SaveChangesWithConcurrencyGuardAsync(cancellationToken);
        return new SetHazardAllowanceBatchLockStateResult(request.PayrollYear, request.PayrollMonth, rows.Count, updated);
    }

    private int Apply(IEnumerable<LockTarget> rows, bool isLocked, DateTime now, string actor)
    {
        var updated = 0;
        var requested = isLocked ? HazardAllowanceRowLockState.Locked : HazardAllowanceRowLockState.Open;
        foreach (var row in rows)
        {
            if (row.Summary.IsLocked) continue;
            var current = row.Detail.IsLocked ? HazardAllowanceRowLockState.Locked : HazardAllowanceRowLockState.Open;
            if (!lockStatePolicy.ShouldUpdate(current, requested)) continue;
            row.Detail.IsLocked = isLocked;
            row.Detail.UpdatedAtUtc = now;
            row.Detail.UpdatedBy = actor;
            updated++;
        }
        return updated;
    }

    private sealed record LockTarget(
        PayrollAllowanceSummaryRecordRow Summary,
        PayrollHazardAllowanceRecordRow Detail);
}
