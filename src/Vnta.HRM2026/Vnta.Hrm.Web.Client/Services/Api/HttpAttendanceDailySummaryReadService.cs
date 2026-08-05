using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceDailySummaryReadService(NavigationManager navigationManager)
    : IAttendanceDailySummaryReadService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<AttendanceDailySummaryListItemDto>> SearchAsync(
        AttendanceDailySummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/logs/daily-summary/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<AttendanceDailySummaryListItemDto>>(cancellationToken);
    }
}
