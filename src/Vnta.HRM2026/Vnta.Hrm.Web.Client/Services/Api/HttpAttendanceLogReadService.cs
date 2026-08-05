using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceLogReadService(NavigationManager navigationManager)
    : IAttendanceLogReadService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<AttendanceLogListItemDto>> GetRecentAsync(
        int take = 500,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"api/attendance/logs/recent?take={take}",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<AttendanceLogListItemDto>>(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceLogListItemDto>> SearchAsync(
        AttendanceLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/logs/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<AttendanceLogListItemDto>>(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceLogListItemDto>> GetByDateRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        int take = 2000,
        CancellationToken cancellationToken = default)
    {
        var from = fromDate.ToString("yyyy-MM-dd");
        var to = toDate.ToString("yyyy-MM-dd");
        var response = await httpClient.GetAsync(
            $"api/attendance/logs/by-date-range?fromDate={from}&toDate={to}&take={take}",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<AttendanceLogListItemDto>>(cancellationToken);
    }
}
