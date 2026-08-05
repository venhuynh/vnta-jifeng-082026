namespace Vnta.Hrm.Application.ChamCong.DashboardBangChamCong;

/// <summary>Điều kiện tải dashboard bảng công theo tháng.</summary>
public sealed record AttendanceTimesheetDashboardFilter(int WorkMonth, int WorkYear);

public interface IAttendanceTimesheetDashboardService
{
    Task<AttendanceTimesheetDashboardDto> GetDashboardAsync(
        AttendanceTimesheetDashboardFilter filter,
        CancellationToken cancellationToken = default);
}

public sealed record AttendanceTimesheetDashboardDto(
    int WorkMonth,
    int WorkYear,
    AttendanceTimesheetDashboardOverviewDto Overview,
    IReadOnlyList<AttendanceTimesheetDashboardDailyTrendPointDto> DailyTrend,
    IReadOnlyList<AttendanceTimesheetDashboardStatusBreakdownDto> StatusBreakdown,
    IReadOnlyList<AttendanceTimesheetDashboardDepartmentDto> Departments,
    IReadOnlyList<AttendanceTimesheetDashboardExceptionDto> Exceptions);

public sealed record AttendanceTimesheetDashboardOverviewDto(
    int EmployeeCount,
    int RecordCount,
    int OvertimeMinutes,
    int LateEarlyMinutes);

public sealed record AttendanceTimesheetDashboardDailyTrendPointDto(
    DateOnly WorkDate,
    int RecordCount,
    int OvertimeMinutes,
    int LateEarlyMinutes);

public sealed record AttendanceTimesheetDashboardStatusBreakdownDto(string Status, int RecordCount);

public sealed record AttendanceTimesheetDashboardDepartmentDto(
    string DepartmentName,
    int EmployeeCount,
    int RecordCount,
    int OvertimeMinutes,
    int LateEarlyMinutes);

public sealed record AttendanceTimesheetDashboardExceptionDto(
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    int LateEarlyMinutes,
    int OvertimeMinutes,
    bool IsMissingPunch);
