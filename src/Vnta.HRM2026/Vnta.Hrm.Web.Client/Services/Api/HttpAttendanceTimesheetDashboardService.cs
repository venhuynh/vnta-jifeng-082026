using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.ChamCong.DashboardBangChamCong;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceTimesheetDashboardService(NavigationManager navigationManager)
    : IAttendanceTimesheetDashboardService
{
    private readonly HttpClient httpClient = new() { BaseAddress = new Uri(navigationManager.BaseUri) };

    public async Task<AttendanceTimesheetDashboardDto> GetDashboardAsync(
        AttendanceTimesheetDashboardFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/timesheet-dashboard",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceTimesheetDashboardDto>(cancellationToken);
    }
}
