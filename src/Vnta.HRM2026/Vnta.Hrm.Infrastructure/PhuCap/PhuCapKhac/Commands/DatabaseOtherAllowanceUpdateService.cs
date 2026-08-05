using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;

public sealed class DatabaseOtherAllowanceUpdateService(ApplicationDbContext dbContext) : IOtherAllowanceUpdateService
{
    public async Task<OtherAllowanceCommandResult> UpdateAsync(UpdateOtherAllowanceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if(request.Id == Guid.Empty)
            throw new InvalidOperationException("Thiếu dòng phụ cấp khác.");
        var definition = OtherAllowanceDefinitionPolicy.Normalize(new OtherAllowanceDefinitionInput(
            request.AllowanceName,
            OtherAllowancePolicyAdapter.ToAmountType(request.IsFixedAmount),
            request.AllowanceAmount,
            request.Note));
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

        row.AllowanceName = definition.AllowanceName;
        row.IsFixedAmount = definition.AmountType == OtherAllowanceAmountType.Fixed;
        row.AllowanceAmount = definition.AllowanceAmount;
        row.Note = definition.Note;
        var now = DateTime.UtcNow;
        row.UpdatedAtUtc = now;
        row.UpdatedBy = auditActor.Value;
        await dbContext.SaveChangesAsync(cancellationToken);
        await OtherAllowanceSummarySynchronizer.SyncAsync(dbContext, summary, cancellationToken);
        summary.UpdatedAtUtc = now;
        summary.UpdatedBy = row.UpdatedBy;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await OtherAllowanceQueryProjection.GetRequiredCommandResultAsync(dbContext, row.Id, cancellationToken);
    }

}
