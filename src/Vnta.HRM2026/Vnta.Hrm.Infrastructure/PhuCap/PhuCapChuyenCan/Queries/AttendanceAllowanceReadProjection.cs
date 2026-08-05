using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Queries;

/// <summary>Single read-only projection shared by the query side and command result reloads.</summary>
internal static class AttendanceAllowanceReadProjection
{
    public static IQueryable<AttendanceAllowanceJoinedRow> BuildQuery(
        ApplicationDbContext dbContext,
        AttendanceAllowanceResultFilter filter,
        bool requirePeriod)
    {
        EnsureSupportedAllowanceKind(filter.AllowanceKind);
        if(requirePeriod) ValidateRequiredPeriod(filter.PayrollYear, filter.PayrollMonth);

        var normalizedSearch = NormalizeOptional(filter.SearchText);
        IQueryable<AttendanceAllowanceJoinedRow> query =
            from detail in dbContext.PayrollAttendanceAllowanceRecords.AsNoTracking()
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
            select new AttendanceAllowanceJoinedRow
            {
                Detail = detail, Summary = summary, Employee = employee, Department = department, Position = position
            };

        if(filter.PayrollMonth.HasValue)
        {
            var month = requirePeriod ? (short)filter.PayrollMonth.Value : (short)Math.Clamp(filter.PayrollMonth.Value, 1, 12);
            query = query.Where(x => x.Summary.PayrollMonth == month);
        }
        if(filter.PayrollYear.HasValue)
        {
            var year = requirePeriod
                ? (short)filter.PayrollYear.Value
                : (short)Math.Clamp(
                    filter.PayrollYear.Value,
                    AttendanceAllowancePayrollPeriodPolicy.MinimumSupportedYear,
                    AttendanceAllowancePayrollPeriodPolicy.MaximumSupportedYear);
            query = query.Where(x => x.Summary.PayrollYear == year);
        }
        if(filter.AttendanceClass is { } attendanceClass)
            query = query.Where(x => x.Detail.AttendanceClass == attendanceClass.ToStorageValue());
        if(!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(x =>
                (x.Employee != null && x.Employee.EmployeeCode != null && EF.Functions.ILike(x.Employee.EmployeeCode, pattern))
                || (x.Employee != null && x.Employee.FirstName != null && EF.Functions.ILike(x.Employee.FirstName, pattern))
                || (x.Employee != null && x.Employee.LastName != null && EF.Functions.ILike(x.Employee.LastName, pattern))
                || (x.Department != null && x.Department.DepartmentOrWorkshopName != null && EF.Functions.ILike(x.Department.DepartmentOrWorkshopName, pattern))
                || (x.Department != null && x.Department.TeamName != null && EF.Functions.ILike(x.Department.TeamName, pattern))
                || (x.Department != null && x.Department.GroupName != null && EF.Functions.ILike(x.Department.GroupName, pattern))
                || (x.Position != null && x.Position.Name != null && EF.Functions.ILike(x.Position.Name, pattern))
                || (x.Detail.Note != null && EF.Functions.ILike(x.Detail.Note, pattern)));
        }
        return ApplyLockState(query, filter.LockState);
    }

    public static IQueryable<AttendanceAllowanceJoinedRow> ApplyLockState(
        IQueryable<AttendanceAllowanceJoinedRow> query, AttendanceAllowanceLockState state) => state switch
    {
        AttendanceAllowanceLockState.Open => query.Where(x => !x.Detail.IsLocked && !x.Summary.IsLocked),
        AttendanceAllowanceLockState.Locked => query.Where(x => x.Detail.IsLocked || x.Summary.IsLocked),
        _ => query
    };

    public static IOrderedQueryable<AttendanceAllowanceJoinedRow> ApplyStableOrder(IQueryable<AttendanceAllowanceJoinedRow> query) =>
        query.OrderByDescending(x => x.Summary.PayrollYear).ThenByDescending(x => x.Summary.PayrollMonth)
            .ThenBy(x => x.Employee == null ? string.Empty : x.Employee.EmployeeCode)
            .ThenByDescending(x => x.Detail.CreatedAtUtc).ThenByDescending(x => x.Detail.PayrollAllowanceSummaryRecordId);

    public static AttendanceAllowanceResultListItemDto MapToDto(AttendanceAllowanceJoinedRow row) =>
        new(row.Detail.PayrollAllowanceSummaryRecordId, PayrollAllowanceKind.Attendance, row.Summary.EmployeeId,
            row.Employee?.EmployeeCode, row.Employee is null ? null : BuildEmployeeName(row.Employee),
            row.Department is null ? null : BuildDepartmentName(row.Department), row.Position?.Name,
            row.Summary.PayrollMonth, row.Summary.PayrollYear, row.Detail.StandardAllowanceAmount,
            row.Detail.StandardWorkdayCount, row.Detail.ActualWorkdayCount, row.Detail.AttendanceRate,
            row.Detail.AllowanceAmount, row.Detail.IsLocked || row.Summary.IsLocked, row.Detail.CreatedAtUtc,
            row.Detail.UpdatedAtUtc, row.Detail.AppliedRuleKey, AttendanceAllowancePolicyStorageValues.FromStorageValue(row.Detail.AttendanceClass), row.Detail.CtlWorkdayCount,
            row.Detail.LateEarlyMinutes, row.Detail.Kqcc, row.Detail.HasKpViolation,
            row.Detail.AdministrativeWorkdayCount, row.Detail.LateEarlyDeductionDays);

    public static async Task<AttendanceAllowanceResultListItemDto?> GetByIdAsync(
        ApplicationDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        await BuildQuery(dbContext, new AttendanceAllowanceResultFilter(PayrollAllowanceKind.Attendance, null, null, null), false)
            .Where(x => x.Detail.PayrollAllowanceSummaryRecordId == id)
            .Select(x => MapToDto(x)).SingleOrDefaultAsync(cancellationToken);

    public static void ValidateRequiredPeriod(int? year, int? month)
    {
        if(year is null || month is null) throw new InvalidOperationException("Phải chọn đầy đủ tháng và năm kỳ lương.");
        ValidateRequiredPeriod(year.Value, month.Value);
    }
    public static void ValidateRequiredPeriod(int year, int month)
    {
        AttendanceAllowancePayrollPeriodPolicy.EnsureSupported(month, year);
    }
    public static void EnsureSupportedAllowanceKind(PayrollAllowanceKind kind)
    {
        if(kind is 0 or PayrollAllowanceKind.Attendance) return;
        throw new InvalidOperationException("Loại phụ cấp không được hỗ trợ trong màn hình phụ cấp chuyên cần.");
    }
    public static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static string? SanitizeExportText(string? value)
    {
        var normalized = NormalizeOptional(value);
        return string.IsNullOrEmpty(normalized) ? normalized : normalized[0] is '=' or '+' or '-' or '@' ? $"'{normalized}" : normalized;
    }
    private static string BuildEmployeeName(AttendanceGatewayEmployeeRow employee) => string.Join(" ", new[] { employee.LastName, employee.FirstName }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
    private static string BuildDepartmentName(AttendanceDepartmentRow department) => NormalizeOptional(department.GroupName) ?? NormalizeOptional(department.TeamName) ?? NormalizeOptional(department.DepartmentOrWorkshopName) ?? NormalizeOptional(department.CenterName) ?? string.Empty;
}

internal sealed class AttendanceAllowanceJoinedRow
{
    public PayrollAttendanceAllowanceRecordRow Detail { get; init; } = default!;
    public PayrollAllowanceSummaryRecordRow Summary { get; init; } = default!;
    public AttendanceGatewayEmployeeRow? Employee { get; init; }
    public AttendanceDepartmentRow? Department { get; init; }
    public AttendanceGatewayPositionRow? Position { get; init; }
}
