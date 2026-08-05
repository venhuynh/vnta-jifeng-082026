using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Queries;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Commands;

public sealed class DatabasePayrollEmployeeSeniorityAllowanceManualAdjustmentService(
    ApplicationDbContext dbContext, IAuditScope auditScope, IAuditedMutation auditedMutation)
    : IPayrollEmployeeSeniorityAllowanceManualAdjustmentService
{
    public Task<PayrollEmployeeSeniorityAllowanceListItemDto> UpdateManualValuesAsync(
        UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(request, cancellationToken);

    private async Task<PayrollEmployeeSeniorityAllowanceListItemDto> UpdateAsync(
        UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if(request.PayrollAllowanceSummaryRecordId == Guid.Empty)
            throw new InvalidOperationException("Thiếu dòng tổng hợp phụ cấp để cập nhật thủ công.");
        if(request.OriginalUpdatedAtUtc == default)
            throw new PayrollEmployeeSeniorityAllowanceConflictException("Thiếu phiên bản dữ liệu gốc. Vui lòng tải lại dữ liệu trước khi cập nhật.");
        if(request.AllowanceAmount < 0)
            throw new InvalidOperationException("Phụ cấp thâm niên không được nhỏ hơn 0.");

        var detail = await dbContext.PayrollEmployeeSeniorityAllowances.SingleOrDefaultAsync(
            x => x.PayrollAllowanceSummaryRecordId == request.PayrollAllowanceSummaryRecordId, cancellationToken);
        if(detail is null)
            throw new InvalidOperationException("Không tìm thấy dòng phụ cấp thâm niên cần cập nhật.");
        if(detail.IsLocked)
            throw new InvalidOperationException("Dòng phụ cấp thâm niên đã khóa, không thể cập nhật thủ công.");
        var summaryExists = await dbContext.PayrollAllowanceSummaryRecords.AnyAsync(
            x => x.Id == request.PayrollAllowanceSummaryRecordId, cancellationToken);
        if(!summaryExists)
            throw new InvalidOperationException("Không tìm thấy dòng tổng hợp phụ cấp liên quan để cập nhật.");

        var command = auditScope.Current
            ?? throw new InvalidOperationException("Thiếu audit scope cho cập nhật thủ công phụ cấp thâm niên.");
        var amount = decimal.Round(request.AllowanceAmount, 0, MidpointRounding.AwayFromZero);
        var now = SeniorityAllowanceCommandSupport.GetDatabaseNow();
        var note = SeniorityAllowanceReadProjection.NormalizeOptional(request.Note);
        await auditedMutation.ExecuteAsync(command with { ActionIntent = AuditActions.SeniorityAllowance.ManualValueUpdated }, async token =>
        {
            var detailCount = await dbContext.PayrollEmployeeSeniorityAllowances
                .Where(x => x.PayrollAllowanceSummaryRecordId == request.PayrollAllowanceSummaryRecordId && !x.IsLocked
                    && (x.UpdatedAtUtc ?? x.CreatedAtUtc) == request.OriginalUpdatedAtUtc)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.AllowanceAmount, amount)
                    .SetProperty(x => x.Note, note).SetProperty(x => x.UpdatedAtUtc, now)
                    .SetProperty(x => x.UpdatedBy, SeniorityAllowanceCommandSupport.SystemActor), token);
            if(detailCount != 1)
                throw new PayrollEmployeeSeniorityAllowanceConflictException("Dòng phụ cấp thâm niên đã được thay đổi hoặc khóa bởi thao tác khác. Vui lòng tải lại dữ liệu.");
            var summaryCount = await dbContext.PayrollAllowanceSummaryRecords.Where(x => x.Id == request.PayrollAllowanceSummaryRecordId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SeniorityAllowanceAmount, amount)
                    .SetProperty(x => x.UpdatedAtUtc, now).SetProperty(x => x.UpdatedBy, SeniorityAllowanceCommandSupport.SystemActor), token);
            if(summaryCount != 1)
                throw new InvalidOperationException("Không tìm thấy dòng tổng hợp phụ cấp liên quan để cập nhật.");
            return true;
        }, _ => new AuditOperationEvent(AuditActions.SeniorityAllowance.ManualValueUpdated, AuditEntityTypes.SeniorityAllowance,
            request.PayrollAllowanceSummaryRecordId.ToString("D"), Metadata: new Dictionary<string, string> { ["concurrencyTokenProvided"] = bool.TrueString }), cancellationToken);

        dbContext.ChangeTracker.Clear();
        return await SeniorityAllowanceCommandSupport.ReadSingleAsync(dbContext, request.PayrollAllowanceSummaryRecordId, cancellationToken);
    }
}
