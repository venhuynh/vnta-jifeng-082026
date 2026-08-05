using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Commands;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Queries;

/// <summary>Single read projection shared by query handlers and command responses.</summary>
internal static class SeniorityAllowanceReadProjection
{
    internal const string AllDepartments = "Tất cả";
    internal const int MaxTake = 5000;

    public static IQueryable<SeniorityAllowanceJoinedRow> BuildFilteredQuery(
        ApplicationDbContext dbContext,
        PayrollEmployeeSeniorityAllowanceFilter filter)
    {
        SeniorityAllowanceCommandSupport.ValidatePeriod(filter.PayrollYear, filter.PayrollMonth);
        var searchText = NormalizeOptional(filter.SearchText);
        var departmentName = NormalizeOptional(filter.DepartmentName);

        IQueryable<SeniorityAllowanceJoinedRow> query =
            from detail in dbContext.PayrollEmployeeSeniorityAllowances.AsNoTracking()
            join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                on detail.PayrollAllowanceSummaryRecordId equals summary.Id
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employees
            from employee in employees.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positions
            from position in positions.DefaultIfEmpty()
            where summary.PayrollYear == filter.PayrollYear && summary.PayrollMonth == filter.PayrollMonth
            select new SeniorityAllowanceJoinedRow
            {
                Detail = detail,
                Summary = summary,
                EmployeeCode = employee == null ? null : employee.EmployeeCode,
                EmployeeName = employee == null ? null : ((employee.LastName ?? string.Empty) + " " + (employee.FirstName ?? string.Empty)).Trim(),
                DepartmentName = department == null ? null : department.DepartmentOrWorkshopName ?? department.TeamName ?? department.GroupName ?? department.CenterName,
                PositionName = position == null ? null : position.Name
            };

        if(!string.IsNullOrWhiteSpace(departmentName) && !string.Equals(departmentName, AllDepartments, StringComparison.CurrentCultureIgnoreCase))
            query = query.Where(x => x.DepartmentName == departmentName);
        if(filter.IsLocked.HasValue)
            query = query.Where(x => x.Detail.IsLocked == filter.IsLocked.Value);
        if(!string.IsNullOrWhiteSpace(searchText))
        {
            var pattern = $"%{searchText}%";
            query = query.Where(x =>
                (x.EmployeeCode != null && EF.Functions.ILike(x.EmployeeCode, pattern))
                || (x.EmployeeName != null && EF.Functions.ILike(x.EmployeeName, pattern))
                || (x.DepartmentName != null && EF.Functions.ILike(x.DepartmentName, pattern))
                || (x.PositionName != null && EF.Functions.ILike(x.PositionName, pattern))
                || (x.Detail.Note != null && EF.Functions.ILike(x.Detail.Note, pattern)));
        }

        return filter.SeniorityRangeKey switch
        {
            "under-1" => query.Where(x => x.Detail.CompletedSeniorityYears < 1),
            "1-3" => query.Where(x => x.Detail.CompletedSeniorityYears >= 1 && x.Detail.CompletedSeniorityYears < 3),
            "3-6" => query.Where(x => x.Detail.CompletedSeniorityYears >= 3 && x.Detail.CompletedSeniorityYears < 6),
            "6-10" => query.Where(x => x.Detail.CompletedSeniorityYears >= 6 && x.Detail.CompletedSeniorityYears < 10),
            "10-13" => query.Where(x => x.Detail.CompletedSeniorityYears >= 10 && x.Detail.CompletedSeniorityYears < 13),
            "13-plus" => query.Where(x => x.Detail.CompletedSeniorityYears >= 13),
            _ => query
        };
    }

    public static PayrollEmployeeSeniorityAllowanceListItemDto Map(SeniorityAllowanceJoinedRow row) => new(
        row.Detail.PayrollAllowanceSummaryRecordId, row.Detail.PayrollAllowanceSummaryRecordId, row.Summary.EmployeeId,
        row.EmployeeCode, row.EmployeeName, row.DepartmentName, row.PositionName, row.Summary.PayrollMonth, row.Summary.PayrollYear,
        row.Detail.EmploymentStartDate, row.Detail.CompletedSeniorityYears, row.Detail.CompletedSeniorityMonths,
        row.Detail.AdministrativeWorkDays, row.Detail.LateEarlyLeaveWorkDays, row.Detail.SalaryWorkDays,
        row.Detail.AppliedRuleKey, row.Detail.AllowanceAmount, row.Detail.Note, row.Detail.IsLocked,
        row.Detail.RefreshedAtUtc, row.Detail.RefreshedBy, row.Detail.UpdatedAtUtc ?? row.Detail.CreatedAtUtc,
        row.Summary.IsLocked);

    public static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal sealed class SeniorityAllowanceJoinedRow
{
    public PayrollEmployeeSeniorityAllowanceRow Detail { get; init; } = default!;
    public PayrollAllowanceSummaryRecordRow Summary { get; init; } = default!;
    public string? EmployeeCode { get; init; }
    public string? EmployeeName { get; init; }
    public string? DepartmentName { get; init; }
    public string? PositionName { get; init; }
}
