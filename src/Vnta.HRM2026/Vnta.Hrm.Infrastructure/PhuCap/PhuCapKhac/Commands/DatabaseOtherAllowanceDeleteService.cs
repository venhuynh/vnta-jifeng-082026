using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;

public sealed class DatabaseOtherAllowanceDeleteService(ApplicationDbContext dbContext) : IOtherAllowanceDeleteService
{
    public async Task DeleteAsync(DeleteOtherAllowanceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var auditActor = OtherAllowanceAuditPolicy.ResolveActor(request.RequestedBy);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var row = await dbContext.PayrollOtherAllowanceRecords.SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy dòng phụ cấp khác.");
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(item => item.Id == row.PayrollAllowanceSummaryRecordId, cancellationToken);
        OtherAllowanceEditPolicy.EnsureCanEdit(new OtherAllowanceEditabilityInput(
            OtherAllowancePolicyAdapter.ToLockState(row.IsLocked),
            OtherAllowancePolicyAdapter.ToLockState(summary.IsLocked),
            row.UpdatedAtUtc ?? row.CreatedAtUtc,
            request.OriginalUpdatedAtUtc));
        dbContext.PayrollOtherAllowanceRecords.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        await OtherAllowanceSummarySynchronizer.SyncAsync(dbContext, summary, cancellationToken);
        summary.UpdatedAtUtc = DateTime.UtcNow;
        summary.UpdatedBy = auditActor.Value;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

}
