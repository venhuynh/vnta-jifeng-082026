using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceWorkdaySummaryReadService(NavigationManager navigationManager)
    : IAttendanceWorkdaySummaryReadService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<AttendanceWorkdaySummaryListItemDto>> SearchAsync(
        AttendanceWorkdaySummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/logs/workday-summary/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<AttendanceWorkdaySummaryListItemDto>>(cancellationToken);
    }
}
