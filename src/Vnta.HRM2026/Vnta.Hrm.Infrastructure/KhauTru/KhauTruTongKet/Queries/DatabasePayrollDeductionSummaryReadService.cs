using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

/// <summary>Read model only: every entity source is no-tracking and projected directly to DTOs.</summary>
public sealed class DatabasePayrollDeductionSummaryReadService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation,
    IPayrollDeductionSummaryRequestValidator? requestValidator = null)
    : IPayrollDeductionSummaryReadService, IPayrollDeductionSummaryExportService
{
    private const int MaxSearchResultLimit = 5000;
    public async Task<PayrollDeductionSummaryPageDto> SearchAsync(
        PayrollDeductionSummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequestValidator.Validate(filter).ThrowIfInvalid();
        var normalizedSearchText = PayrollDeductionSummaryCommandServiceBase.NormalizeOptional(filter.SearchText);
        var query =
            from summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
            join insurance in dbContext.PayrollDeductionInsuranceRecords.AsNoTracking() on summary.Id equals insurance.PayrollDeductionSummaryRecordId into insuranceGroup
            from insurance in insuranceGroup.DefaultIfEmpty()
            join tax in dbContext.PayrollDeductionTaxRecords.AsNoTracking() on summary.Id equals tax.PayrollDeductionSummaryRecordId into taxGroup
            from tax in taxGroup.DefaultIfEmpty()
            join unionFee in dbContext.PayrollDeductionUnionFeeRecords.AsNoTracking() on summary.Id equals unionFee.PayrollDeductionSummaryRecordId into unionFeeGroup
            from unionFee in unionFeeGroup.DefaultIfEmpty()
            join advance in dbContext.PayrollDeductionAdvanceRecords.AsNoTracking() on summary.Id equals advance.PayrollDeductionSummaryRecordId into advanceGroup
            from advance in advanceGroup.DefaultIfEmpty()
            join other in dbContext.PayrollDeductionOtherRecords.AsNoTracking() on summary.Id equals other.PayrollDeductionSummaryRecordId into otherGroup
            from other in otherGroup.DefaultIfEmpty()
            join employee in dbContext.Employees.AsNoTracking() on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking() on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking() on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            where summary.PayrollYear > PayrollDeductionSummaryPeriodPolicy.MinimumSupportedYear
                || (summary.PayrollYear == PayrollDeductionSummaryPeriodPolicy.MinimumSupportedYear
                    && summary.PayrollMonth >= PayrollDeductionSummaryPeriodPolicy.MinimumSupportedMonth)
            select new { summary, insurance, tax, unionFee, advance, other, employee, department, position };

        if(filter.PayrollMonth.HasValue) query = query.Where(x => x.summary.PayrollMonth == (short)filter.PayrollMonth.Value);
        if(filter.PayrollYear.HasValue) query = query.Where(x => x.summary.PayrollYear == (short)filter.PayrollYear.Value);
        if(!string.IsNullOrWhiteSpace(normalizedSearchText))
        {
            var searchPattern = $"%{normalizedSearchText}%";
            query = query.Where(x =>
                (x.employee != null && EF.Functions.ILike(x.employee.EmployeeCode, searchPattern))
                || (x.employee != null && EF.Functions.ILike(x.employee.FirstName, searchPattern))
                || (x.employee != null && EF.Functions.ILike(x.employee.LastName, searchPattern))
                || (x.department != null && EF.Functions.ILike(x.department.DepartmentOrWorkshopName, searchPattern))
                || (x.department != null && x.department.TeamName != null && EF.Functions.ILike(x.department.TeamName, searchPattern))
                || (x.department != null && x.department.GroupName != null && EF.Functions.ILike(x.department.GroupName, searchPattern))
                || (x.position != null && EF.Functions.ILike(x.position.Name, searchPattern))
                || (x.summary.Note != null && EF.Functions.ILike(x.summary.Note, searchPattern)));
        }

        var lockStatusCounts = await query.GroupBy(_ => 1).Select(group => new PayrollDeductionSummaryLockStatusCountsDto(
            group.Count(), group.Sum(row => row.summary.IsLocked ? 0 : 1), group.Sum(row => row.summary.IsLocked ? 1 : 0)))
            .SingleOrDefaultAsync(cancellationToken) ?? PayrollDeductionSummaryLockStatusCountsDto.Empty;
        if(filter.IsLocked.HasValue) query = query.Where(x => x.summary.IsLocked == filter.IsLocked.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var totals = await query.GroupBy(_ => 1).Select(group => new PayrollDeductionSummaryAggregateDto(
            group.Sum(row => row.summary.SocialInsuranceDeductionAmount), group.Sum(row => row.summary.PersonalIncomeTaxDeductionAmount),
            group.Sum(row => row.summary.UnionFeeDeductionAmount), group.Sum(row => row.summary.AdvanceDeductionAmount),
            group.Sum(row => row.summary.OtherDeductionAmount), group.Sum(row => row.summary.SocialInsuranceDeductionAmount)
                + group.Sum(row => row.summary.PersonalIncomeTaxDeductionAmount) + group.Sum(row => row.summary.UnionFeeDeductionAmount)
                + group.Sum(row => row.summary.AdvanceDeductionAmount) + group.Sum(row => row.summary.OtherDeductionAmount)))
            .SingleOrDefaultAsync(cancellationToken) ?? PayrollDeductionSummaryAggregateDto.Empty;
        var skip = Math.Max(0, filter.Skip);
        if(totalCount == 0 || skip >= totalCount) return new PayrollDeductionSummaryPageDto([], totalCount, totals, lockStatusCounts);

        var rows = await query.OrderByDescending(x => x.summary.PayrollYear).ThenByDescending(x => x.summary.PayrollMonth)
            .ThenBy(x => x.employee == null ? string.Empty : x.employee.EmployeeCode)
            .ThenBy(x => x.employee == null ? string.Empty : x.employee.LastName)
            .ThenBy(x => x.employee == null ? string.Empty : x.employee.FirstName).ThenBy(x => x.summary.Id)
            .Skip(skip).Take(Math.Clamp(filter.Take, 1, MaxSearchResultLimit))
            .Select(x => PayrollDeductionSummaryCommandServiceBase.MapToDto(x.summary, x.insurance, x.tax, x.unionFee, x.advance, x.other, x.employee, x.department, x.position))
            .ToListAsync(cancellationToken);
        return new PayrollDeductionSummaryPageDto(rows, totalCount, totals, lockStatusCounts);
    }

    public Task<IReadOnlyList<PayrollDeductionSummaryExportItemDto>> ExportPeriodAsync(
        int payrollMonth, int payrollYear, PayrollDeductionSummaryExportFormat format, CancellationToken cancellationToken = default)
    {
        RequestValidator.Validate(new PayrollDeductionSummaryExportRequest(payrollYear, payrollMonth, format)).ThrowIfInvalid();
        if(!Enum.IsDefined(format)) throw new InvalidOperationException("Định dạng xuất tổng kết khấu trừ không hợp lệ.");
        return auditedMutation.ExecuteAsync<IReadOnlyList<PayrollDeductionSummaryExportItemDto>>(
            CreateExportAuditCommand(auditScope.Current),
            async token =>
            {
                var rows = await SearchAsync(new PayrollDeductionSummaryFilter(payrollMonth, payrollYear, null, Take: MaxSearchResultLimit), token);
                if(rows.TotalCount > MaxSearchResultLimit)
                    throw new InvalidOperationException($"Kỳ {payrollMonth:00}/{payrollYear} có quá {MaxSearchResultLimit:N0} dòng, chưa thể xuất tệp trực tiếp.");
                return rows.Rows.Select(MapToExportDto).ToArray();
            },
            rows => new AuditOperationEvent(AuditActions.DeductionSummary.Exported, AuditEntityTypes.DeductionSummary,
                Metadata: new Dictionary<string, string> { ["format"] = format.ToString(), ["period"] = $"{payrollMonth:00}/{payrollYear}", ["rowCount"] = rows.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) }),
            cancellationToken);
    }

    private static PayrollDeductionSummaryExportItemDto MapToExportDto(PayrollDeductionSummaryListItemDto row)
    {
        var employeeDisplay = string.Join(" - ", new[] { PayrollDeductionSummaryCommandServiceBase.NormalizeOptional(row.EmployeeCode), PayrollDeductionSummaryCommandServiceBase.NormalizeOptional(row.EmployeeName) }.Where(value => value is not null));
        var total = row.SocialInsuranceDeductionAmount + row.PersonalIncomeTaxDeductionAmount + row.UnionFeeDeductionAmount + row.AdvanceDeductionAmount + row.OtherDeductionAmount;
        return new PayrollDeductionSummaryExportItemDto(employeeDisplay.Length == 0 ? "Chưa có nhân viên" : employeeDisplay,
            PayrollDeductionSummaryCommandServiceBase.NormalizeOptional(row.DepartmentName) ?? "Chưa có phòng ban",
            PayrollDeductionSummaryCommandServiceBase.NormalizeOptional(row.PositionName) ?? "Chưa có chức vụ",
            $"{row.PayrollMonth:00}/{row.PayrollYear}", row.SocialInsuranceDeductionAmount,
            row.PersonalIncomeTaxDeductionAmount, row.UnionFeeDeductionAmount, row.AdvanceDeductionAmount,
            row.OtherDeductionAmount, total, row.IsLocked ? "Đã khóa" : "Đang mở");
    }

    private IPayrollDeductionSummaryRequestValidator RequestValidator =>
        requestValidator ?? new PayrollDeductionSummaryRequestValidator();

    private static AuditCommand CreateExportAuditCommand(AuditCommand? current) => new(
        current?.OperationId ?? Guid.NewGuid(), AuditActions.DeductionSummary.Exported,
        current?.Actor ?? new AuditActor("system", "system", AuditActorKind.System, AuditSource.Worker),
        current?.CorrelationId ?? Guid.NewGuid().ToString("N"), AuditCaptureMode.OperationOnly,
        Metadata: new Dictionary<string, string> { ["auditScope"] = current is null ? "system-fallback" : "request" });
}
