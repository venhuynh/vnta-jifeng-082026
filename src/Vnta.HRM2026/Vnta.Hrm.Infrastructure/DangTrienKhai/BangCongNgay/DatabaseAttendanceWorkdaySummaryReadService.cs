using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;

public sealed class DatabaseAttendanceWorkdaySummaryReadService(ApplicationDbContext dbContext)
    : IAttendanceWorkdaySummaryReadService
{
    private const int MaxSearchResultLimit = 5000;
    private const int MaxMonthlySearchResultLimit = 100000;
    private const int MaximumMonthlyRangeDays = 31;

    public async Task<IReadOnlyList<AttendanceWorkdaySummaryListItemDto>> SearchAsync(
        AttendanceWorkdaySummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedSearchTerm = NormalizeOptional(filter.SearchText);
        var (fromDate, toDate) = NormalizeDateRange(filter.FromDate, filter.ToDate);
        var normalizedTake = Math.Clamp(
            filter.Take,
            1,
            IsMonthlyRange(fromDate, toDate)
                ? MaxMonthlySearchResultLimit
                : MaxSearchResultLimit);

        var query =
            from summary in dbContext.AttendanceWorkdaySummaries.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            join shift in dbContext.Shifts.AsNoTracking()
                on summary.ShiftId equals shift.Id into shiftGroup
            from shift in shiftGroup.DefaultIfEmpty()
            join statusCode in dbContext.AttendanceStatusCodes.AsNoTracking()
                on summary.CodeKetQuaTinhCongId equals statusCode.Id into statusCodeGroup
            from statusCode in statusCodeGroup.DefaultIfEmpty()
            select new { summary, employee, department, position, shift, statusCode };

        if(fromDate.HasValue)
        {
            query = query.Where(x => x.summary.WorkDate >= fromDate.Value);
        }

        if(toDate.HasValue)
        {
            query = query.Where(x => x.summary.WorkDate <= toDate.Value);
        }

        if(!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            var searchPattern = $"%{normalizedSearchTerm}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.summary.DayType, searchPattern)
                || (x.statusCode != null && EF.Functions.ILike(x.statusCode.Code, searchPattern))
                || (x.statusCode != null && EF.Functions.ILike(x.statusCode.Name, searchPattern))
                || (x.summary.Note != null && EF.Functions.ILike(x.summary.Note, searchPattern))
                || (x.summary.CheckInAt != null && EF.Functions.ILike(x.summary.CheckInAt, searchPattern))
                || (x.summary.CheckOutAt != null && EF.Functions.ILike(x.summary.CheckOutAt, searchPattern))
                || (x.employee != null && x.employee.EmployeeCode != null && EF.Functions.ILike(x.employee.EmployeeCode, searchPattern))
                || (x.employee != null && x.employee.FirstName != null && EF.Functions.ILike(x.employee.FirstName, searchPattern))
                || (x.employee != null && x.employee.LastName != null && EF.Functions.ILike(x.employee.LastName, searchPattern))
                || (x.department != null && x.department.DepartmentOrWorkshopName != null && EF.Functions.ILike(x.department.DepartmentOrWorkshopName, searchPattern))
                || (x.position != null && x.position.Name != null && EF.Functions.ILike(x.position.Name, searchPattern))
                || (x.shift != null && x.shift.Code != null && EF.Functions.ILike(x.shift.Code, searchPattern))
                || (x.shift != null && x.shift.ShortName != null && EF.Functions.ILike(x.shift.ShortName, searchPattern))
                || (x.shift != null && x.shift.Name != null && EF.Functions.ILike(x.shift.Name, searchPattern)));
        }

        return await query
            .OrderByDescending(x => x.summary.WorkDate)
            .ThenBy(x => x.employee == null ? string.Empty : x.employee.EmployeeCode)
            .ThenByDescending(x => x.summary.CreatedAtUtc)
            .ThenByDescending(x => x.summary.Id)
            .Take(normalizedTake)
            .Select(x => new AttendanceWorkdaySummaryListItemDto(
                x.summary.Id,
                x.summary.EmployeeId,
                x.employee == null ? null : x.employee.EmployeeCode,
                x.employee == null ? null : BuildEmployeeName(x.employee),
                x.department == null ? null : BuildDepartmentName(x.department),
                x.position == null ? null : x.position.Name,
                x.summary.WorkDate,
                x.summary.DayType,
                x.summary.ShiftId,
                x.shift == null ? null : x.shift.Code,
                x.shift == null ? null : x.shift.ShortName,
                x.shift == null ? null : x.shift.Name,
                x.shift == null ? null : x.shift.ColorHex,
                x.summary.ScheduledStartAt,
                x.summary.ScheduledEndAt,
                x.summary.CheckInAt,
                x.summary.CheckOutAt,
                x.summary.LateMinutes,
                x.summary.EarlyLeaveMinutes,
                x.statusCode == null ? string.Empty : x.statusCode.Code,
                x.summary.IsLocked,
                x.summary.OvertimeMinutes,
                x.summary.OvertimeMinutes15,
                x.summary.OvertimeMinutes20,
                x.summary.OvertimeMinutes30,
                x.summary.CheckInForOT15,
                x.summary.IsRegisterForOT,
                x.summary.RequireDocument,
                x.summary.Note,
                x.summary.ComputedAtUtc,
                x.summary.CreatedAtUtc,
                x.summary.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static string BuildEmployeeName(AttendanceGatewayEmployeeRow employee)
    {
        var parts = new[] { employee.LastName, employee.FirstName }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part.Trim());

        return string.Join(" ", parts);
    }

    private static string BuildDepartmentName(AttendanceDepartmentRow department)
    {
        return NormalizeOptional(department.DepartmentOrWorkshopName)
            ?? NormalizeOptional(department.TeamName)
            ?? NormalizeOptional(department.GroupName)
            ?? NormalizeOptional(department.CenterName)
            ?? string.Empty;
    }

    private static (DateOnly? FromDate, DateOnly? ToDate) NormalizeDateRange(
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        if(fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
        {
            return (toDate.Value, fromDate.Value);
        }

        return (fromDate, toDate);
    }

    private static bool IsMonthlyRange(DateOnly? fromDate, DateOnly? toDate)
    {
        if (!fromDate.HasValue || !toDate.HasValue)
        {
            return false;
        }

        var totalDays = toDate.Value.DayNumber - fromDate.Value.DayNumber + 1;
        return totalDays is > 0 and <= MaximumMonthlyRangeDays;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
