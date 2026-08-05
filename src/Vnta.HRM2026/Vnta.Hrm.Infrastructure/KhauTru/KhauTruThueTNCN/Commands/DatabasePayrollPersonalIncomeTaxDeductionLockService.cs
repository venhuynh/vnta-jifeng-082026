using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruThueTNCN.Commands;

public sealed class DatabasePayrollPersonalIncomeTaxDeductionLockService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    PayrollPersonalIncomeTaxDeductionPeriodPolicy periodPolicy)
    : IPayrollPersonalIncomeTaxDeductionLockService
{
    public async Task<SetPayrollPersonalIncomeTaxDeductionBatchLockStateResult> SetLockStateBatchAsync(SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        periodPolicy.Validate(request.PayrollYear, request.PayrollMonth);
        var selected = request.Scope == PayrollPersonalIncomeTaxDeductionLockActionScope.SelectedRows;
        if (!selected && request.Scope != PayrollPersonalIncomeTaxDeductionLockActionScope.WholePeriod)
            throw new InvalidOperationException("Phạm vi khóa Thuế TNCN không hợp lệ.");
        var targetIds = request.PayrollDeductionSummaryRecordIds?.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (selected && (targetIds is null || targetIds.Length == 0))
            throw new InvalidOperationException("Phải chọn ít nhất một dòng Thuế TNCN khi khóa theo dòng đã chọn.");
        if (!selected && targetIds is { Length: > 0 })
            throw new InvalidOperationException("Khóa toàn bộ kỳ lương không nhận danh sách dòng cụ thể.");

        var requestAudit = auditScope.Current;
        var command = requestAudit ?? new AuditCommand(Guid.NewGuid(), AuditActions.PersonalIncomeTaxDeduction.BatchLockStateChanged,
            new AuditActor("system", "system", AuditActorKind.System, AuditSource.Worker), Guid.NewGuid().ToString("N"), AuditCaptureMode.OperationOnly,
            Metadata: new Dictionary<string, string> { ["auditScope"] = "system-fallback" });
        return await auditedMutation.ExecuteAsync(command with { ActionIntent = AuditActions.PersonalIncomeTaxDeduction.BatchLockStateChanged }, async token =>
        {
            var targets = await (from detail in dbContext.PayrollDeductionTaxRecords
                                 join summary in dbContext.PayrollDeductionSummaryRecords on detail.PayrollDeductionSummaryRecordId equals summary.Id
                                 where summary.PayrollYear == request.PayrollYear && summary.PayrollMonth == request.PayrollMonth
                                 select new { detail, summary }).ToListAsync(token);
            if (selected) targets = targets.Where(row => targetIds!.Contains(row.detail.PayrollDeductionSummaryRecordId)).ToList();
            if (targets.Any(row => row.summary.IsLocked))
                throw new PayrollPersonalIncomeTaxDeductionConflictException("Kỳ tổng hợp khấu trừ đã khóa nên không thể thay đổi trạng thái khóa Thuế TNCN.");
            var now = DateTime.UtcNow;
            var changed = targets.Select(row => row.detail).Where(row => row.IsLocked != request.IsLocked).ToArray();
            foreach (var row in changed) { row.IsLocked = request.IsLocked; row.UpdatedAtUtc = now; }
            return new SetPayrollPersonalIncomeTaxDeductionBatchLockStateResult(request.PayrollYear, request.PayrollMonth, targets.Count, changed.Length, targets.Count - changed.Length);
        }, result => new AuditOperationEvent(AuditActions.PersonalIncomeTaxDeduction.BatchLockStateChanged, AuditEntityTypes.PersonalIncomeTaxDeduction,
            EntityDisplayName: $"{result.PayrollMonth:00}/{result.PayrollYear}", Metadata: new Dictionary<string, string>
            {
                ["isLocked"] = request.IsLocked.ToString(), ["scope"] = selected ? "selected-rows" : "whole-period", ["targetRowCount"] = result.TargetRowCount.ToString(), ["updatedCount"] = result.UpdatedCount.ToString(), ["unchangedCount"] = result.UnchangedCount.ToString(), ["auditScope"] = requestAudit is null ? "system-fallback" : "request"
            }), cancellationToken);
    }
}
