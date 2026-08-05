using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

public sealed class DatabasePayrollDeductionSummaryManualAdjustmentService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    IPayrollDeductionSummaryTargetRosterPolicy? targetRosterPolicy = null,
    IPayrollDeductionSummaryRequestValidator? requestValidator = null)
    : PayrollDeductionSummaryCommandServiceBase(dbContext, auditScope, auditedMutation, targetRosterPolicy, requestValidator),
        IPayrollDeductionSummaryManualAdjustmentService
{
    public async Task<PayrollDeductionSummaryListItemDto> UpdateManualOtherDeductionAsync(
        UpdatePayrollDeductionSummaryManualOtherDeductionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestValidator.Validate(request).ThrowIfInvalid();
        if(request.Id == Guid.Empty) throw new InvalidOperationException("Thiếu định danh dòng tổng kết khấu trừ cần điều chỉnh.");
        PayrollDeductionSummaryManualOtherDeductionPolicy.Validate(new(request.OtherDeductionAmount));
        if(request.OriginalUpdatedAtUtc == default) throw new InvalidOperationException("Thiếu phiên bản dữ liệu để điều chỉnh khoản khấu trừ khác.");
        var actor = NormalizeActor(request.Actor);
        var now = GetDatabaseNow();
        await MapConcurrencyAsync(() => auditedMutation.ExecuteAsync(CreateManualOtherDeductionAuditCommand(auditScope.Current), async token =>
        {
            var summary = await dbContext.PayrollDeductionSummaryRecords.SingleOrDefaultAsync(row => row.Id == request.Id, token)
                ?? throw new InvalidOperationException("Không tìm thấy dòng tổng kết khấu trừ cần điều chỉnh.");
            if(summary.IsLocked) throw new InvalidOperationException("Dòng tổng kết khấu trừ đã khóa, không thể điều chỉnh.");
            if(PayrollDeductionSummaryConcurrencyPolicy.Evaluate(new(GetRecordVersion(summary), request.OriginalUpdatedAtUtc)) != PayrollDeductionSummaryConcurrencyStatus.VersionMatches)
                throw new PayrollDeductionSummaryConcurrencyException("Dòng tổng kết khấu trừ đã được thay đổi bởi thao tác khác. Vui lòng tải lại dữ liệu.");
            var other = await dbContext.PayrollDeductionOtherRecords.SingleOrDefaultAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id, token);
            if(other?.IsLocked == true) throw new InvalidOperationException("Khoản khấu trừ khác đã khóa, không thể điều chỉnh.");
            if(other is null) { other = new PayrollDeductionOtherRecordRow { PayrollDeductionSummaryRecordId = summary.Id, CreatedAtUtc = now }; dbContext.PayrollDeductionOtherRecords.Add(other); }
            other.DeductionAmount = request.OtherDeductionAmount;
            other.UpdatedAtUtc = now;
            summary.OtherDeductionAmount = request.OtherDeductionAmount;
            summary.Note = NormalizeOptional(request.Note);
            summary.UpdatedAtUtc = now;
            summary.UpdatedBy = actor;
            return summary.Id;
        }, id => new AuditOperationEvent(AuditActions.DeductionSummary.ManualOtherDeductionUpdated, AuditEntityTypes.DeductionSummary, id.ToString("D"), Metadata: new Dictionary<string, string> { ["updatedFields"] = "OtherDeductionAmount,Note" }), cancellationToken));
        dbContext.ChangeTracker.Clear();
        return await GetByIdAsync(request.Id, cancellationToken) ?? throw new InvalidOperationException("Không thể tải lại dòng tổng kết khấu trừ vừa điều chỉnh.");
    }
}
