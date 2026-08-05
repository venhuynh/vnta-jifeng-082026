using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruThueTNCN.Commands;

public sealed class DatabasePayrollPersonalIncomeTaxDeductionRefreshService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    PayrollPersonalIncomeTaxDeductionPeriodPolicy periodPolicy,
    PayrollPersonalIncomeTaxDeductionRefreshPolicy refreshPolicy)
    : IPayrollPersonalIncomeTaxDeductionRefreshService
{
    public async Task<RefreshPayrollPersonalIncomeTaxDeductionResult> RefreshAsync(
        RefreshPayrollPersonalIncomeTaxDeductionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.PayrollDeductionSummaryRecordId == Guid.Empty)
            throw new InvalidOperationException("Thiếu dòng tổng hợp khấu trừ cần làm mới Thuế TNCN.");
        periodPolicy.Validate(request.PayrollYear, request.PayrollMonth);

        var detail = await dbContext.PayrollDeductionTaxRecords.AsNoTracking()
            .SingleOrDefaultAsync(row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng Thuế TNCN cần làm mới.");
        var summary = await dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == request.PayrollDeductionSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng tổng kết khấu trừ liên quan.");

        if (summary.PayrollYear != request.PayrollYear || summary.PayrollMonth != request.PayrollMonth)
            throw new InvalidOperationException("Dòng Thuế TNCN không thuộc kỳ lương cần làm mới.");

        var decision = refreshPolicy.Decide(detail.DeductionAmount, summary.PersonalIncomeTaxDeductionAmount, detail.IsLocked, summary.IsLocked);
        if (decision == PayrollPersonalIncomeTaxDeductionSynchronizationDecision.SkippedLocked)
            return new(request.PayrollYear, request.PayrollMonth, request.PayrollDeductionSummaryRecordId, 0, 0, 1);
        if (decision == PayrollPersonalIncomeTaxDeductionSynchronizationDecision.Unchanged)
            return new(request.PayrollYear, request.PayrollMonth, request.PayrollDeductionSummaryRecordId, 0, 1, 0);

        var command = auditScope.Current ?? throw new InvalidOperationException("Thiếu audit scope cho thao tác làm mới Thuế TNCN.");
        var now = DateTime.UtcNow;
        var actor = string.IsNullOrWhiteSpace(command.Actor.ActorId) ? "system" : command.Actor.ActorId.Trim();
        await auditedMutation.ExecuteAsync(
            command with { ActionIntent = AuditActions.PersonalIncomeTaxDeduction.Refreshed },
            async token =>
            {
                var updatedCount = string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal)
                    ? await UpdateForInMemoryAsync(request, detail.DeductionAmount, now, actor, token)
                    : await dbContext.PayrollDeductionSummaryRecords
                        .Where(row => row.Id == request.PayrollDeductionSummaryRecordId && row.PayrollYear == request.PayrollYear && row.PayrollMonth == request.PayrollMonth && !row.IsLocked)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(row => row.PersonalIncomeTaxDeductionAmount, detail.DeductionAmount)
                            .SetProperty(row => row.UpdatedAtUtc, now)
                            .SetProperty(row => row.UpdatedBy, actor), token);
                if (updatedCount != 1)
                    throw new PayrollPersonalIncomeTaxDeductionConflictException("Dòng tổng kết khấu trừ đã thay đổi hoặc bị khóa bởi thao tác khác. Vui lòng tải lại dữ liệu.");
                return true;
            },
            _ => new AuditOperationEvent(AuditActions.PersonalIncomeTaxDeduction.Refreshed, AuditEntityTypes.PersonalIncomeTaxDeduction,
                request.PayrollDeductionSummaryRecordId.ToString("D"), Metadata: new Dictionary<string, string>
                {
                    ["payrollPeriod"] = $"{request.PayrollMonth:00}/{request.PayrollYear}", ["source"] = "payroll-deduction-tax-record", ["updatedCount"] = "1"
                }), cancellationToken);
        return new(request.PayrollYear, request.PayrollMonth, request.PayrollDeductionSummaryRecordId, 1, 0, 0);
    }

    private async Task<int> UpdateForInMemoryAsync(RefreshPayrollPersonalIncomeTaxDeductionRequest request, decimal deductionAmount, DateTime now, string actor, CancellationToken cancellationToken)
    {
        var summary = await dbContext.PayrollDeductionSummaryRecords.SingleOrDefaultAsync(row => row.Id == request.PayrollDeductionSummaryRecordId && row.PayrollYear == request.PayrollYear && row.PayrollMonth == request.PayrollMonth && !row.IsLocked, cancellationToken);
        if (summary is null) return 0;
        summary.PersonalIncomeTaxDeductionAmount = deductionAmount;
        summary.UpdatedAtUtc = now;
        summary.UpdatedBy = actor;
        return 1;
    }
}
