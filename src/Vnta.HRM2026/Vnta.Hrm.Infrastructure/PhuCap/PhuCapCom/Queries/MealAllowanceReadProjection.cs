using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Queries;

/// <summary>Shared, read-only EF projection used by the list, summary, export and command result flows.</summary>
internal static class MealAllowanceReadProjection
{
    public static IQueryable<MealAllowanceJoinedRow> BuildFilteredQuery(
        ApplicationDbContext dbContext,
        MealAllowanceFilter filter)
    {
        var normalizedSearchTerm = NormalizeOptional(filter.SearchText);

        IQueryable<MealAllowanceJoinedRow> query =
            from result in dbContext.PayrollMealAllowanceRecords.AsNoTracking()
            join summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                on result.PayrollAllowanceSummaryRecordId equals summary.Id
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new MealAllowanceJoinedRow
            {
                Result = result,
                Summary = summary,
                Employee = employee,
                Department = department,
                Position = position
            };

        if(filter.PayrollMonth.HasValue)
        {
            var month = (short)Math.Clamp(filter.PayrollMonth.Value, 1, 12);
            query = query.Where(x => x.Summary.PayrollMonth == month);
        }

        if(filter.PayrollYear.HasValue)
        {
            var year = (short)Math.Clamp(filter.PayrollYear.Value, 2000, 2100);
            query = query.Where(x => x.Summary.PayrollYear == year);
        }

        if(!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            var searchPattern = $"%{normalizedSearchTerm}%";
            query = query.Where(x =>
                (x.Employee != null && x.Employee.EmployeeCode != null && EF.Functions.ILike(x.Employee.EmployeeCode, searchPattern))
                || (x.Employee != null && x.Employee.FirstName != null && EF.Functions.ILike(x.Employee.FirstName, searchPattern))
                || (x.Employee != null && x.Employee.LastName != null && EF.Functions.ILike(x.Employee.LastName, searchPattern))
                || (x.Department != null && x.Department.DepartmentOrWorkshopName != null && EF.Functions.ILike(x.Department.DepartmentOrWorkshopName, searchPattern))
                || (x.Position != null && x.Position.Name != null && EF.Functions.ILike(x.Position.Name, searchPattern)));
        }

        return ApplySummaryBucketFilter(query, filter.SummaryBucketKey);
    }

    public static MealAllowanceListItemDto MapToDto(MealAllowanceJoinedRow row) =>
        new(
            row.Result.PayrollAllowanceSummaryRecordId,
            row.Summary.EmployeeId,
            row.Employee?.EmployeeCode,
            row.Employee is null ? null : BuildEmployeeName(row.Employee),
            row.Department?.DepartmentOrWorkshopName,
            row.Position?.Name,
            row.Summary.PayrollMonth,
            row.Summary.PayrollYear,
            row.Result.QualifiedMealDays,
            row.Result.Overtime1900Days,
            row.Result.MealAllowancePerQualifiedDay,
            row.Result.MealAllowanceAmount,
            string.IsNullOrWhiteSpace(row.Result.RuleCode) ? MealAllowancePolicy.QualifiedMealRuleCode : row.Result.RuleCode,
            row.Result.RuleVersion,
            row.Result.Note,
            row.Result.IsLocked,
            row.Result.CalculatedAtUtc,
            row.Result.CreatedAtUtc,
            row.Result.UpdatedAtUtc);

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<MealAllowanceJoinedRow> ApplySummaryBucketFilter(
        IQueryable<MealAllowanceJoinedRow> query,
        string? summaryBucketKey)
    {
        var normalizedSummaryBucketKey = NormalizeOptional(summaryBucketKey)?.ToLowerInvariant();
        return normalizedSummaryBucketKey switch
        {
            "qualified" => query.Where(x => !x.Result.IsLocked && x.Result.RuleCode == MealAllowancePolicy.QualifiedMealRuleCode),
            "manual" => query.Where(x => !x.Result.IsLocked && x.Result.RuleCode == MealAllowancePolicy.ManualAdjustmentRuleCode),
            "locked" => query.Where(x => x.Result.IsLocked),
            "with-allowance" => query.Where(x => x.Result.Overtime1900Days > 0),
            "without-allowance" => query.Where(x => x.Result.Overtime1900Days == 0),
            "other" => query.Where(x =>
                !x.Result.IsLocked
                && x.Result.RuleCode != MealAllowancePolicy.QualifiedMealRuleCode
                && x.Result.RuleCode != MealAllowancePolicy.ManualAdjustmentRuleCode),
            _ => query
        };
    }

    private static string BuildEmployeeName(AttendanceGatewayEmployeeRow employee)
    {
        var fullName = string.Join(
            " ",
            new[] { employee.LastName, employee.FirstName }
                .Where(static part => !string.IsNullOrWhiteSpace(part))
                .Select(static part => part!.Trim()));

        return string.IsNullOrWhiteSpace(fullName) ? employee.EmployeeCode : fullName;
    }
}

internal sealed class MealAllowanceJoinedRow
{
    public PayrollMealAllowanceRecordRow Result { get; init; } = default!;
    public PayrollAllowanceSummaryRecordRow Summary { get; init; } = default!;
    public AttendanceGatewayEmployeeRow? Employee { get; init; }
    public AttendanceDepartmentRow? Department { get; init; }
    public AttendanceGatewayPositionRow? Position { get; init; }
}
