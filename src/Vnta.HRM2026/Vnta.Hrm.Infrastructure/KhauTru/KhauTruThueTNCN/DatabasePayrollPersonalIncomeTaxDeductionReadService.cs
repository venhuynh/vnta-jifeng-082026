using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruThueTNCN;

public sealed class DatabasePayrollPersonalIncomeTaxDeductionReadService(ApplicationDbContext dbContext)
    : IPayrollPersonalIncomeTaxDeductionReadService
{
    private const int MinimumSupportedYear = 2000;
    private const int MaximumSupportedYear = 2100;
    private const int MaximumPageSize = 2000;

    public async Task<PayrollPersonalIncomeTaxDeductionPageDto> SearchAsync(
        PayrollPersonalIncomeTaxDeductionFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePeriod(filter.PayrollMonth, filter.PayrollYear);

        var searchText = NormalizeOptional(filter.SearchText);
        var query =
            from detail in dbContext.PayrollDeductionTaxRecords.AsNoTracking()
            join summary in dbContext.PayrollDeductionSummaryRecords.AsNoTracking()
                on detail.PayrollDeductionSummaryRecordId equals summary.Id
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new { detail, summary, employee, department, position };

        if (filter.PayrollMonth.HasValue)
        {
            query = query.Where(item => item.summary.PayrollMonth == filter.PayrollMonth.Value);
        }

        if (filter.PayrollYear.HasValue)
        {
            query = query.Where(item => item.summary.PayrollYear == filter.PayrollYear.Value);
        }

        if (searchText is not null)
        {
            var searchPattern = $"%{searchText}%";
            query = query.Where(item =>
                (item.employee != null && item.employee.EmployeeCode != null && EF.Functions.ILike(item.employee.EmployeeCode, searchPattern))
                || (item.employee != null && item.employee.FirstName != null && EF.Functions.ILike(item.employee.FirstName, searchPattern))
                || (item.employee != null && item.employee.LastName != null && EF.Functions.ILike(item.employee.LastName, searchPattern))
                || (item.department != null && item.department.DepartmentOrWorkshopName != null && EF.Functions.ILike(item.department.DepartmentOrWorkshopName, searchPattern))
                || (item.department != null && item.department.TeamName != null && EF.Functions.ILike(item.department.TeamName, searchPattern))
                || (item.department != null && item.department.GroupName != null && EF.Functions.ILike(item.department.GroupName, searchPattern))
                || (item.position != null && item.position.Name != null && EF.Functions.ILike(item.position.Name, searchPattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(item => item.summary.PayrollYear)
            .ThenByDescending(item => item.summary.PayrollMonth)
            .ThenBy(item => item.employee == null ? string.Empty : item.employee.EmployeeCode)
            .ThenBy(item => item.employee == null ? string.Empty : item.employee.LastName)
            .ThenBy(item => item.employee == null ? string.Empty : item.employee.FirstName)
            .ThenBy(item => item.summary.Id)
            .Skip(Math.Max(filter.Skip, 0))
            .Take(Math.Clamp(filter.Take, 1, MaximumPageSize))
            .Select(item => new PayrollPersonalIncomeTaxDeductionRow(
                item.detail.PayrollDeductionSummaryRecordId,
                item.summary.EmployeeId,
                item.employee == null ? null : item.employee.EmployeeCode,
                item.employee == null ? null : item.employee.LastName,
                item.employee == null ? null : item.employee.FirstName,
                item.department == null ? null : item.department.GroupName,
                item.department == null ? null : item.department.TeamName,
                item.department == null ? null : item.department.DepartmentOrWorkshopName,
                item.department == null ? null : item.department.CenterName,
                item.position == null ? null : item.position.Name,
                item.summary.PayrollMonth,
                item.summary.PayrollYear,
                item.detail.DeductionAmount,
                item.summary.IsLocked,
                item.detail.IsLocked,
                item.detail.CreatedAtUtc,
                item.detail.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PayrollPersonalIncomeTaxDeductionPageDto(rows.Select(MapToDto).ToArray(), totalCount);
    }

    private static PayrollPersonalIncomeTaxDeductionListItemDto MapToDto(PayrollPersonalIncomeTaxDeductionRow source) =>
        new(
            source.PayrollDeductionSummaryRecordId,
            source.EmployeeId,
            source.EmployeeCode,
            BuildEmployeeName(source.EmployeeLastName, source.EmployeeFirstName),
            FirstNotEmpty(source.DepartmentGroupName, source.DepartmentTeamName, source.DepartmentName, source.DepartmentCenterName),
            source.PositionName,
            source.PayrollMonth,
            source.PayrollYear,
            source.DeductionAmount,
            source.IsSummaryLocked,
            source.IsLocked,
            source.CreatedAtUtc,
            source.UpdatedAtUtc);

    private static void ValidatePeriod(int? payrollMonth, int? payrollYear)
    {
        if (payrollMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng kỳ lương phải nằm trong khoảng từ 1 đến 12.");
        }

        if (payrollYear is < MinimumSupportedYear or > MaximumSupportedYear)
        {
            throw new InvalidOperationException($"Năm kỳ lương phải nằm trong khoảng từ {MinimumSupportedYear} đến {MaximumSupportedYear}.");
        }
    }

    private static string? BuildEmployeeName(string? lastName, string? firstName)
    {
        var name = string.Join(" ", new[] { lastName, firstName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim()));
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static string? FirstNotEmpty(params string?[] values) =>
        values.Select(NormalizeOptional).FirstOrDefault(value => value is not null);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record PayrollPersonalIncomeTaxDeductionRow(
        Guid PayrollDeductionSummaryRecordId,
        Guid EmployeeId,
        string? EmployeeCode,
        string? EmployeeLastName,
        string? EmployeeFirstName,
        string? DepartmentGroupName,
        string? DepartmentTeamName,
        string? DepartmentName,
        string? DepartmentCenterName,
        string? PositionName,
        short PayrollMonth,
        short PayrollYear,
        decimal DeductionAmount,
        bool IsSummaryLocked,
        bool IsLocked,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}
