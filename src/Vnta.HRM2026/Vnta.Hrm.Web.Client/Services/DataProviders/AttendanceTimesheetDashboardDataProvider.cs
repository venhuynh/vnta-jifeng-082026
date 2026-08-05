using Vnta.Hrm.Application.ChamCong.DashboardBangChamCong;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

/// <summary>Adapter dữ liệu Dashboard bảng công cho giao diện Blazor.</summary>
public sealed class AttendanceTimesheetDashboardDataProvider(
    IAttendanceTimesheetDashboardService dashboardService)
{
    public Task<AttendanceTimesheetDashboardDto> GetDashboardAsync(
        int workMonth,
        int workYear,
        CancellationToken cancellationToken = default) =>
        dashboardService.GetDashboardAsync(
            new AttendanceTimesheetDashboardFilter(workMonth, workYear),
            cancellationToken);
}
