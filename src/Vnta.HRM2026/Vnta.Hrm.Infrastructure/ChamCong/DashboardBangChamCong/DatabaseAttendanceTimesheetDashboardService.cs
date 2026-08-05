using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.ChamCong.DashboardBangChamCong;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.ChamCong.DashboardBangChamCong;

/// <summary>Truy vấn tổng hợp server-side cho Dashboard bảng công.</summary>
public sealed class DatabaseAttendanceTimesheetDashboardService(ApplicationDbContext dbContext)
    : IAttendanceTimesheetDashboardService
{
    public async Task<AttendanceTimesheetDashboardDto> GetDashboardAsync(
        AttendanceTimesheetDashboardFilter filter,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(filter);
        var fromDate = new DateOnly(filter.WorkYear, filter.WorkMonth, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);
        var summaries = dbContext.AttendanceWorkdaySummaries
            .AsNoTracking()
            .Where(summary => summary.WorkDate >= fromDate && summary.WorkDate <= toDate);

        var overview = await summaries
            .GroupBy(_ => 1)
            .Select(group => new AttendanceTimesheetDashboardOverviewDto(
                group.Select(row => row.EmployeeId).Distinct().Count(),
                group.Count(),
                group.Sum(row => Math.Max(0, row.OvertimeMinutes)),
                group.Sum(row => Math.Max(0, row.LateMinutes) + Math.Max(0, row.EarlyLeaveMinutes))))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new AttendanceTimesheetDashboardOverviewDto(0, 0, 0, 0);

        var dailyTrend = await summaries
            .GroupBy(row => row.WorkDate)
            .Select(group => new AttendanceTimesheetDashboardDailyTrendPointDto(
                group.Key,
                group.Count(),
                group.Sum(row => Math.Max(0, row.OvertimeMinutes)),
                group.Sum(row => Math.Max(0, row.LateMinutes) + Math.Max(0, row.EarlyLeaveMinutes))))
            .OrderBy(point => point.WorkDate)
            .ToListAsync(cancellationToken);

        var statusBreakdown = await (
                from summary in summaries
                join statusCode in dbContext.AttendanceStatusCodes.AsNoTracking()
                    on summary.CodeKetQuaTinhCongId equals statusCode.Id into statusCodeGroup
                from statusCode in statusCodeGroup.DefaultIfEmpty()
                group summary by (statusCode == null || string.IsNullOrWhiteSpace(statusCode.Code)
                    ? "Chưa có mã"
                    : statusCode.Code) into statusGroup
                select new AttendanceTimesheetDashboardStatusBreakdownDto(statusGroup.Key, statusGroup.Count()))
            .OrderByDescending(item => item.RecordCount)
            .ThenBy(item => item.Status)
            .ToListAsync(cancellationToken);

        var departmentQuery =
            from summary in summaries
            join employee in dbContext.Employees.AsNoTracking() on summary.EmployeeId equals employee.Id into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            group summary by (department == null
                ? "Chưa phân phòng ban"
                : department.DepartmentOrWorkshopName
                    ?? department.TeamName
                    ?? department.GroupName
                    ?? department.CenterName
                    ?? "Chưa phân phòng ban") into departmentSummaryGroup
            select new AttendanceTimesheetDashboardDepartmentDto(
                departmentSummaryGroup.Key,
                departmentSummaryGroup.Select(row => row.EmployeeId).Distinct().Count(),
                departmentSummaryGroup.Count(),
                departmentSummaryGroup.Sum(row => Math.Max(0, row.OvertimeMinutes)),
                departmentSummaryGroup.Sum(row => Math.Max(0, row.LateMinutes) + Math.Max(0, row.EarlyLeaveMinutes)));

        var departments = await departmentQuery
            .OrderByDescending(item => item.LateEarlyMinutes)
            .ThenBy(item => item.DepartmentName)
            .ToListAsync(cancellationToken);

        var exceptions = await (
                from summary in summaries
                join employee in dbContext.Employees.AsNoTracking() on summary.EmployeeId equals employee.Id into employeeGroup
                from employee in employeeGroup.DefaultIfEmpty()
                join department in dbContext.Departments.AsNoTracking()
                    on employee.DepartmentId equals department.Id into departmentGroup
                from department in departmentGroup.DefaultIfEmpty()
                let isMissingPunch = summary.DayType == "regular"
                    && (string.IsNullOrWhiteSpace(summary.CheckInAt) || string.IsNullOrWhiteSpace(summary.CheckOutAt))
                where summary.LateMinutes > 0 || summary.EarlyLeaveMinutes > 0 || isMissingPunch
                select new AttendanceTimesheetDashboardExceptionDto(
                    employee == null || string.IsNullOrWhiteSpace(employee.EmployeeCode) ? "--" : employee.EmployeeCode,
                    employee == null ? "--" : ((employee.LastName ?? "") + " " + (employee.FirstName ?? "")).Trim(),
                    department == null
                        ? "Chưa phân phòng ban"
                        : department.DepartmentOrWorkshopName
                            ?? department.TeamName
                            ?? department.GroupName
                            ?? department.CenterName
                            ?? "Chưa phân phòng ban",
                    Math.Max(0, summary.LateMinutes) + Math.Max(0, summary.EarlyLeaveMinutes),
                    Math.Max(0, summary.OvertimeMinutes),
                    isMissingPunch))
            .OrderByDescending(item => item.LateEarlyMinutes)
            .ThenByDescending(item => item.IsMissingPunch)
            .ThenBy(item => item.EmployeeCode)
            .Take(20)
            .ToListAsync(cancellationToken);

        return new AttendanceTimesheetDashboardDto(
            filter.WorkMonth,
            filter.WorkYear,
            overview,
            dailyTrend,
            statusBreakdown,
            departments,
            exceptions);
    }

    private static void ValidatePeriod(AttendanceTimesheetDashboardFilter filter)
    {
        if(filter.WorkYear is < 2000 or > 2100)
        {
            throw new InvalidOperationException("Năm dữ liệu phải nằm trong khoảng 2000 đến 2100.");
        }

        if(filter.WorkMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Tháng dữ liệu phải nằm trong khoảng 1 đến 12.");
        }
    }
}
