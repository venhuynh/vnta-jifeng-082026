using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceDailySummaryService(NavigationManager navigationManager)
    : IAttendanceDailySummaryService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<RebuildAttendanceDailySummaryResult> RebuildAsync(
        RebuildAttendanceDailySummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/logs/daily-summary/rebuild",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RebuildAttendanceDailySummaryResult>(cancellationToken);
    }
}
