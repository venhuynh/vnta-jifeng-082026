using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac;

public sealed class DatabaseOtherResponsibilityAllowanceReadService(ApplicationDbContext dbContext)
    : IOtherResponsibilityAllowanceReadService
{
    public async Task<IReadOnlyList<OtherResponsibilityAllowanceListItemDto>> SearchAsync(
        OtherResponsibilityAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        OtherResponsibilityAllowancePeriodPolicy.Validate(filter.PayrollYear, filter.PayrollMonth);
        var normalizedSearchText = NormalizeOptional(filter.SearchText);
        var take = Math.Clamp(filter.Take, 1, OtherResponsibilityAllowancePeriodPolicy.MaxSearchResultLimit);

        var query =
            from detail in dbContext.PayrollAllowanceOtherResponsibilityRecords.AsNoTracking()
            join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                on detail.PayrollAllowanceSummaryRecordId equals summary.Id
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            where summary.PayrollYear == filter.PayrollYear && summary.PayrollMonth == filter.PayrollMonth
            select new { Detail = detail, Summary = summary, Employee = employee, Department = department, Position = position };

        if(filter.IsLocked.HasValue)
        {
            query = query.Where(row => row.Summary.IsLocked == filter.IsLocked.Value);
        }

        if(!string.IsNullOrWhiteSpace(normalizedSearchText))
        {
            var searchPattern = $"%{normalizedSearchText}%";
            query = query.Where(row =>
                (row.Employee != null
                    && ((row.Employee.EmployeeCode != null && EF.Functions.ILike(row.Employee.EmployeeCode, searchPattern))
                        || (row.Employee.LastName != null && EF.Functions.ILike(row.Employee.LastName, searchPattern))
                        || (row.Employee.FirstName != null && EF.Functions.ILike(row.Employee.FirstName, searchPattern))
                        || EF.Functions.ILike(((row.Employee.LastName ?? string.Empty) + " " + (row.Employee.FirstName ?? string.Empty)).Trim(), searchPattern)))
                || (row.Department != null
                    && ((row.Department.DepartmentOrWorkshopName != null && EF.Functions.ILike(row.Department.DepartmentOrWorkshopName, searchPattern))
                        || (row.Department.TeamName != null && EF.Functions.ILike(row.Department.TeamName, searchPattern))
                        || (row.Department.GroupName != null && EF.Functions.ILike(row.Department.GroupName, searchPattern))
                        || (row.Department.CenterName != null && EF.Functions.ILike(row.Department.CenterName, searchPattern))))
                || (row.Position != null && row.Position.Name != null && EF.Functions.ILike(row.Position.Name, searchPattern))
                || (row.Detail.Note != null && EF.Functions.ILike(row.Detail.Note, searchPattern)));
        }

        return await query
            .OrderBy(row => row.Employee == null ? string.Empty : row.Employee.EmployeeCode ?? string.Empty)
            .ThenBy(row => row.Employee == null ? string.Empty : row.Employee.LastName ?? string.Empty)
            .ThenBy(row => row.Employee == null ? string.Empty : row.Employee.FirstName ?? string.Empty)
            .ThenBy(row => row.Detail.PayrollAllowanceSummaryRecordId)
            .Take(take)
            .Select(row => new OtherResponsibilityAllowanceListItemDto(
                row.Detail.PayrollAllowanceSummaryRecordId,
                row.Detail.PayrollAllowanceSummaryRecordId,
                row.Summary.EmployeeId,
                row.Employee == null ? null : row.Employee.EmployeeCode,
                row.Employee == null ? null : BuildEmployeeName(row.Employee.LastName, row.Employee.FirstName),
                row.Department == null ? null : (row.Department.DepartmentOrWorkshopName ?? row.Department.TeamName ?? row.Department.GroupName ?? row.Department.CenterName),
                row.Position == null ? null : row.Position.Name,
                row.Summary.PayrollMonth,
                row.Summary.PayrollYear,
                row.Detail.AllowanceWorkdayCount,
                row.Detail.StandardResponsibilityAllowanceAmount,
                row.Detail.ActualResponsibilityAllowanceAmount,
                row.Detail.Note,
                row.Summary.IsLocked,
                row.Detail.RefreshedAtUtc,
                row.Detail.RefreshedBy,
                row.Detail.CreatedAtUtc,
                row.Detail.CreatedBy,
                row.Summary.UpdatedAtUtc ?? row.Detail.UpdatedAtUtc,
                row.Summary.UpdatedBy ?? row.Detail.UpdatedBy))
            .ToListAsync(cancellationToken);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildEmployeeName(string? lastName, string? firstName) => string.Join(
        " ",
        new[] { lastName, firstName }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
}
