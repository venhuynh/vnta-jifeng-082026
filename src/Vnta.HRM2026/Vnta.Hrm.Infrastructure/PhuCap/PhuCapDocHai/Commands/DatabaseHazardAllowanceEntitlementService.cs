using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

/// <summary>Persists user-selected entitlement changes with optimistic concurrency for every selected row.</summary>
public sealed class DatabaseHazardAllowanceEntitlementService(
    ApplicationDbContext dbContext,
    IHazardAllowanceRequestValidator requestValidator)
    : IHazardAllowanceEntitlementService
{
    public async Task<SetHazardAllowanceEntitlementBatchResult> SetEntitlementBatchAsync(
        SetHazardAllowanceEntitlementBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();
        if(dbContext.ChangeTracker.HasChanges()) dbContext.ChangeTracker.Clear();

        var targetsById = request.Targets
            .GroupBy(target => target.PayrollAllowanceSummaryRecordId)
            .ToDictionary(group => group.Key, group => group.Single());
        var targetIds = targetsById.Keys.ToArray();
        var rows = await (
            from summary in dbContext.PayrollAllowanceSummaryRecords
            join detail in dbContext.PayrollHazardAllowanceRecords on summary.Id equals detail.PayrollAllowanceSummaryRecordId
            where targetIds.Contains(summary.Id)
            select new TargetRow(summary, detail))
            .ToListAsync(cancellationToken);

        if(rows.Count != targetIds.Length)
            throw new InvalidOperationException("Một hoặc nhiều dòng phụ cấp độc hại không còn tồn tại.");
        if(rows.Any(row => row.Summary.IsLocked || row.Detail.IsLocked))
            throw new InvalidOperationException("Có dòng phụ cấp độc hại đã khóa, không thể cập nhật trạng thái hưởng.");
        if(rows.Any(row =>
            (row.Detail.UpdatedAtUtc ?? row.Detail.CreatedAtUtc) != targetsById[row.Summary.Id].OriginalDetailUpdatedAtUtc
            || (row.Summary.UpdatedAtUtc ?? row.Summary.CreatedAtUtc) != targetsById[row.Summary.Id].OriginalSummaryUpdatedAtUtc))
        {
            throw new HazardAllowanceConflictException(
                "Dữ liệu phụ cấp độc hại vừa thay đổi. Vui lòng tải lại trước khi cập nhật trạng thái hưởng.");
        }

        var now = HazardAllowancePersistence.ToDatabaseTimestamp(DateTime.UtcNow);
        var actor = HazardAllowancePersistence.NormalizeOptional(request.RequestedBy) ?? "system";
        var updated = 0;
        foreach(var row in rows)
        {
            if(row.Detail.IsEligibleForAllowance == request.IsEligibleForAllowance)
            {
                continue;
            }

            row.Detail.IsEligibleForAllowance = request.IsEligibleForAllowance;
            if(!request.IsEligibleForAllowance)
            {
                row.Detail.HazardAllowanceAmount = 0m;
                row.Detail.ExclusionReason = "Ngoại lệ do người dùng chọn.";
                row.Summary.HazardAllowanceAmount = 0m;
                row.Summary.UpdatedAtUtc = now;
                row.Summary.UpdatedBy = actor;
            }

            row.Detail.UpdatedAtUtc = now;
            row.Detail.UpdatedBy = actor;
            updated++;
        }

        if(updated > 0)
        {
            await dbContext.SaveChangesWithConcurrencyGuardAsync(cancellationToken);
        }

        return new SetHazardAllowanceEntitlementBatchResult(rows.Count, updated);
    }

    private sealed record TargetRow(
        PayrollAllowanceSummaryRecordRow Summary,
        PayrollHazardAllowanceRecordRow Detail);
}
