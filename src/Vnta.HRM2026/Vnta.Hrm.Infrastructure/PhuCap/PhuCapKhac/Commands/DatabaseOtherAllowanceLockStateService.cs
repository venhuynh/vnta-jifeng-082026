using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;

public sealed class DatabaseOtherAllowanceLockStateService(ApplicationDbContext dbContext) : IOtherAllowanceLockService
{
    public async Task SetLockStateAsync(SetOtherAllowanceLockStateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var auditActor = OtherAllowanceAuditPolicy.ResolveActor(request.RequestedBy);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var row = await dbContext.PayrollOtherAllowanceRecords.SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy dòng phụ cấp khác.");
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(item => item.Id == row.PayrollAllowanceSummaryRecordId, cancellationToken);
        OtherAllowanceEditPolicy.EnsureCanChangeLockState(
            OtherAllowancePolicyAdapter.ToLockState(summary.IsLocked),
            new OtherAllowanceVersionInput(row.UpdatedAtUtc ?? row.CreatedAtUtc, request.OriginalUpdatedAtUtc));
        row.IsLocked = request.IsLocked;
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.UpdatedBy = auditActor.Value;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<SetOtherAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetOtherAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if(request.PayrollMonth is < 1 or > 12 || request.PayrollYear < 1)
            throw new InvalidOperationException("Kỳ lương không hợp lệ.");

        var versionedItems = request.Items?
            .Where(item => item.Id != Guid.Empty)
            .GroupBy(item => item.Id)
            .Select(group => group.First())
            .ToArray();
        var hasVersionedItems = request.Items is not null;
        var requestedIds = hasVersionedItems
            ? versionedItems!.Select(item => item.Id).ToArray()
            : request.Ids?.Where(id => id != Guid.Empty).Distinct().ToArray();
        var isWholePeriod = !hasVersionedItems && request.Ids is null;
        if(!isWholePeriod && (requestedIds is null || requestedIds.Length == 0))
            throw new InvalidOperationException("Cần chọn ít nhất một dòng phụ cấp khác.");

        var auditActor = OtherAllowanceAuditPolicy.ResolveActor(request.RequestedBy);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var rowsQuery =
            from row in dbContext.PayrollOtherAllowanceRecords
            join summary in dbContext.PayrollAllowanceSummaryRecords
                on row.PayrollAllowanceSummaryRecordId equals summary.Id
            where summary.PayrollMonth == request.PayrollMonth
                  && summary.PayrollYear == request.PayrollYear
            select new { Row = row, SummaryIsLocked = summary.IsLocked };
        if(requestedIds is not null)
            rowsQuery = rowsQuery.Where(item => requestedIds.Contains(item.Row.Id));

        var rows = await rowsQuery.ToListAsync(cancellationToken);
        if(requestedIds is not null && rows.Count != requestedIds.Length)
            throw new InvalidOperationException("Một hoặc nhiều dòng được chọn không thuộc kỳ lương hiện tại.");

        if(hasVersionedItems)
        {
            var versionsById = versionedItems!.ToDictionary(item => item.Id);
            var hasStaleRow = rows.Any(item =>
                (item.Row.UpdatedAtUtc ?? item.Row.CreatedAtUtc) != versionsById[item.Row.Id].OriginalUpdatedAtUtc);
            if(hasStaleRow)
                throw new OtherAllowanceConflictException("Có dòng phụ cấp khác đã được cập nhật bởi thao tác khác. Vui lòng tải lại trước khi khóa hoặc mở khóa.");
        }

        var skippedSummaryLockedCount = rows.Count(item => item.SummaryIsLocked);
        var unchangedCount = rows.Count(item => !item.SummaryIsLocked && item.Row.IsLocked == request.IsLocked);
        var targetRows = rows
            .Where(item => !item.SummaryIsLocked && item.Row.IsLocked != request.IsLocked)
            .Select(item => item.Row)
            .ToArray();
        foreach(var row in targetRows)
        {
            row.IsLocked = request.IsLocked;
            row.UpdatedAtUtc = DateTime.UtcNow;
            row.UpdatedBy = auditActor.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SetOtherAllowanceBatchLockStateResult(
            rows.Count,
            targetRows.Length,
            unchangedCount,
            skippedSummaryLockedCount,
            request.IsLocked,
            isWholePeriod);
    }

}
