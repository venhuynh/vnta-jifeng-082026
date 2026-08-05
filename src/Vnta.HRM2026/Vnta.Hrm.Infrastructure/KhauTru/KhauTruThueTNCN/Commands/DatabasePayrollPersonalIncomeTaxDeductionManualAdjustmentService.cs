using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruThueTNCN.Commands;

public sealed class DatabasePayrollPersonalIncomeTaxDeductionManualAdjustmentService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    PayrollPersonalIncomeTaxDeductionManualValuePolicy manualValuePolicy)
    : IPayrollPersonalIncomeTaxDeductionManualAdjustmentService
{
    public async Task<PayrollPersonalIncomeTaxDeductionListItemDto> UpdateManualValueAsync(UpdatePayrollPersonalIncomeTaxDeductionManualValueRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedAmount = manualValuePolicy.ValidateAndNormalize(request);
        var detail = await dbContext.PayrollDeductionTaxRecords.AsNoTracking().SingleOrDefaultAsync(row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng Thuế TNCN cần điều chỉnh.");
        var summary = await dbContext.PayrollDeductionSummaryRecords.AsNoTracking().SingleOrDefaultAsync(row => row.Id == request.PayrollDeductionSummaryRecordId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng tổng hợp khấu trừ liên quan.");
        if (detail.IsLocked || summary.IsLocked)
            throw new PayrollPersonalIncomeTaxDeductionConflictException("Dòng Thuế TNCN hoặc kỳ tổng hợp đã khóa nên không thể điều chỉnh.");
        if ((detail.UpdatedAtUtc ?? detail.CreatedAtUtc) != request.OriginalUpdatedAtUtc!.Value)
            throw new PayrollPersonalIncomeTaxDeductionConflictException("Dòng Thuế TNCN đã được cập nhật ở phiên khác. Vui lòng tải lại dữ liệu trước khi lưu tiếp.");

        var command = auditScope.Current ?? throw new InvalidOperationException("Thiếu audit scope cho thao tác điều chỉnh Thuế TNCN.");
        var now = DateTime.UtcNow;
        await auditedMutation.ExecuteAsync(command with { ActionIntent = AuditActions.PersonalIncomeTaxDeduction.ManualValueUpdated }, async token =>
        {
            var detailUpdated = await dbContext.PayrollDeductionTaxRecords
                .Where(row => row.PayrollDeductionSummaryRecordId == request.PayrollDeductionSummaryRecordId && !row.IsLocked && (row.UpdatedAtUtc ?? row.CreatedAtUtc) == request.OriginalUpdatedAtUtc.Value)
                .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.DeductionAmount, normalizedAmount).SetProperty(row => row.UpdatedAtUtc, now), token);
            if (detailUpdated != 1)
                throw new PayrollPersonalIncomeTaxDeductionConflictException("Dòng Thuế TNCN đã thay đổi hoặc bị khóa bởi thao tác khác. Vui lòng tải lại dữ liệu.");
            var summaryUpdated = await dbContext.PayrollDeductionSummaryRecords
                .Where(row => row.Id == request.PayrollDeductionSummaryRecordId && !row.IsLocked)
                .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.PersonalIncomeTaxDeductionAmount, normalizedAmount).SetProperty(row => row.UpdatedAtUtc, now), token);
            if (summaryUpdated != 1)
                throw new PayrollPersonalIncomeTaxDeductionConflictException("Dòng tổng hợp khấu trừ đã bị khóa hoặc thay đổi. Vui lòng tải lại dữ liệu.");
            return true;
        }, _ => new AuditOperationEvent(AuditActions.PersonalIncomeTaxDeduction.ManualValueUpdated, AuditEntityTypes.PersonalIncomeTaxDeduction,
            request.PayrollDeductionSummaryRecordId.ToString("D"), Metadata: new Dictionary<string, string> { ["concurrencyTokenProvided"] = bool.TrueString }), cancellationToken);
        dbContext.ChangeTracker.Clear();
        return await GetRequiredAsync(request.PayrollDeductionSummaryRecordId, cancellationToken);
    }

    private async Task<PayrollPersonalIncomeTaxDeductionListItemDto> GetRequiredAsync(Guid summaryRecordId, CancellationToken cancellationToken)
    {
        var row = await (from detail in dbContext.PayrollDeductionTaxRecords.AsNoTracking()
                         join summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking() on detail.PayrollDeductionSummaryRecordId equals summary.Id
                         join employee in dbContext.Employees.AsNoTracking() on summary.EmployeeId equals employee.Id into employeeGroup
                         from employee in employeeGroup.DefaultIfEmpty()
                         join department in dbContext.Departments.AsNoTracking() on employee.DepartmentId equals department.Id into departmentGroup
                         from department in departmentGroup.DefaultIfEmpty()
                         join position in dbContext.Positions.AsNoTracking() on employee.PositionId equals position.Id into positionGroup
                         from position in positionGroup.DefaultIfEmpty()
                         where detail.PayrollDeductionSummaryRecordId == summaryRecordId
                         select new PayrollPersonalIncomeTaxDeductionListItemDto(detail.PayrollDeductionSummaryRecordId, summary.EmployeeId,
                             employee == null ? null : employee.EmployeeCode,
                             employee == null ? null : string.Join(" ", new[] { employee.LastName, employee.FirstName }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim())),
                             department == null ? null : FirstNotEmpty(department.GroupName, department.TeamName, department.DepartmentOrWorkshopName, department.CenterName),
                             position == null ? null : position.Name, summary.PayrollMonth, summary.PayrollYear, detail.DeductionAmount, summary.IsLocked, detail.IsLocked, detail.CreatedAtUtc, detail.UpdatedAtUtc)).SingleOrDefaultAsync(cancellationToken);
        return row ?? throw new InvalidOperationException("Không thể tải lại dòng Thuế TNCN sau khi cập nhật.");
    }

    private static string? FirstNotEmpty(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
