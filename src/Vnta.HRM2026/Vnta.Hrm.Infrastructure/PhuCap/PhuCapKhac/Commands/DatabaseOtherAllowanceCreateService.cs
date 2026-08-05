using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;

public sealed class DatabaseOtherAllowanceCreateService(ApplicationDbContext dbContext) : IOtherAllowanceCreateService
{
    public async Task<OtherAllowanceCommandResult> CreateAsync(CreateOtherAllowanceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if(request.PayrollAllowanceSummaryRecordId == Guid.Empty)
            throw new InvalidOperationException("Thiếu bản ghi tổng hợp phụ cấp.");
        var definition = OtherAllowanceDefinitionPolicy.Normalize(new OtherAllowanceDefinitionInput(
            request.AllowanceName,
            OtherAllowancePolicyAdapter.ToAmountType(request.IsFixedAmount),
            request.AllowanceAmount,
            request.Note));
        var auditActor = OtherAllowanceAuditPolicy.ResolveActor(request.RequestedBy);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleOrDefaultAsync(row => row.Id == request.PayrollAllowanceSummaryRecordId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bản ghi tổng hợp phụ cấp.");
        OtherAllowanceEditPolicy.EnsureCanCreate(OtherAllowancePolicyAdapter.ToLockState(summary.IsLocked));

        var now = DateTime.UtcNow;
        var detail = new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(), PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceName = definition.AllowanceName,
            IsFixedAmount = definition.AmountType == OtherAllowanceAmountType.Fixed,
            AllowanceAmount = definition.AllowanceAmount,
            Note = definition.Note, CreatedAtUtc = now,
            CreatedBy = auditActor.Value
        };
        dbContext.PayrollOtherAllowanceRecords.Add(detail);
        await dbContext.SaveChangesAsync(cancellationToken);
        await OtherAllowanceSummarySynchronizer.SyncAsync(dbContext, summary, cancellationToken);
        summary.UpdatedAtUtc = now;
        summary.UpdatedBy = detail.CreatedBy;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await OtherAllowanceQueryProjection.GetRequiredCommandResultAsync(dbContext, detail.Id, cancellationToken);
    }

}
