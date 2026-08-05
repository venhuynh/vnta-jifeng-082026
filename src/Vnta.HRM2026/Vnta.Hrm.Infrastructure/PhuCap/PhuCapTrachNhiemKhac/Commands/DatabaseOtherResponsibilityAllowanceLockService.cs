using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac;

/// <summary>Owns the lock mutation for the detail snapshot and its linked payroll summary.</summary>
public sealed class DatabaseOtherResponsibilityAllowanceLockService(ApplicationDbContext dbContext)
    : IOtherResponsibilityAllowanceLockService
{
    public async Task<SetOtherResponsibilityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetOtherResponsibilityAllowanceBatchLockStateRequest request,
        string? requestedBy = null,
        CancellationToken cancellationToken = default)
    {
        OtherResponsibilityAllowancePeriodPolicy.Validate(request.PayrollYear, request.PayrollMonth);
        var selectedIds = request.PayrollAllowanceSummaryRecordIds?.Distinct().ToArray();
        if(selectedIds is { Length: 0 })
        {
            throw new InvalidOperationException("Phải chọn ít nhất một dòng hoặc dùng phạm vi toàn kỳ lương.");
        }

        var query =
            from detail in dbContext.PayrollAllowanceOtherResponsibilityRecords
            join summary in dbContext.PayrollAllowanceSummaryRecords
                on detail.PayrollAllowanceSummaryRecordId equals summary.Id
            where summary.PayrollYear == request.PayrollYear && summary.PayrollMonth == request.PayrollMonth
            select new { Detail = detail, Summary = summary };
        if(selectedIds is not null)
        {
            query = query.Where(row => selectedIds.Contains(row.Detail.PayrollAllowanceSummaryRecordId));
        }

        var targetRows = (await query.ToListAsync(cancellationToken))
            .Select(row => new LockTarget(row.Detail, row.Summary))
            .ToArray();
        if(selectedIds is not null && targetRows.Length != selectedIds.Length)
        {
            throw new InvalidOperationException("Một hoặc nhiều dòng được chọn không còn thuộc kỳ lương hiện tại.");
        }

        ValidateConcurrencyTokens(targetRows, request.ConcurrencyTokens, selectedIds is not null);

        var rowsToUpdate = targetRows.Where(row => row.Detail.IsLocked != request.IsLocked || row.Summary.IsLocked != request.IsLocked).ToArray();
        if(rowsToUpdate.Length > 0)
        {
            var now = OtherResponsibilityAllowancePersistenceSupport.GetDatabaseNow();
            var actor = OtherResponsibilityAllowancePersistenceSupport.NormalizeActor(requestedBy);
            foreach(var row in rowsToUpdate)
            {
                row.Detail.IsLocked = request.IsLocked;
                row.Detail.UpdatedAtUtc = now;
                row.Detail.UpdatedBy = actor;
                row.Summary.IsLocked = request.IsLocked;
                row.Summary.UpdatedAtUtc = now;
                row.Summary.UpdatedBy = actor;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SetOtherResponsibilityAllowanceBatchLockStateResult(
            request.PayrollYear,
            request.PayrollMonth,
            targetRows.Length,
            rowsToUpdate.Length);
    }

    private static void ValidateConcurrencyTokens(
        IReadOnlyCollection<LockTarget> rows,
        IReadOnlyList<OtherResponsibilityAllowanceLockStateConcurrencyToken>? tokens,
        bool selectionWasSpecified)
    {
        if(!selectionWasSpecified) return;
        if(tokens is null || tokens.Count != rows.Count)
        {
            throw new OtherResponsibilityAllowanceConcurrencyException("Dữ liệu đã thay đổi. Hãy tải lại danh sách trước khi thao tác.");
        }

        var tokenById = tokens
            .GroupBy(token => token.PayrollAllowanceSummaryRecordId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        if(tokenById.Count != rows.Count
            || tokenById.Values.Any(group => group.Length != 1)
            || rows.Any(row => !tokenById.TryGetValue(row.Detail.PayrollAllowanceSummaryRecordId, out var token)
                               || token[0].OriginalUpdatedAtUtc != (row.Summary.UpdatedAtUtc ?? row.Detail.UpdatedAtUtc)))
        {
            throw new OtherResponsibilityAllowanceConcurrencyException("Dữ liệu đã thay đổi. Hãy tải lại danh sách trước khi thao tác.");
        }
    }

    private sealed record LockTarget(
        PayrollAllowanceOtherResponsibilityRecordRow Detail,
        PayrollAllowanceSummaryRecordRow Summary);
}
