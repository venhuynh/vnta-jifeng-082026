using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceWorkdaySummaryService(NavigationManager navigationManager)
    : IAttendanceWorkdaySummaryService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<RebuildAttendanceWorkdaySummaryResult> RebuildAsync(
        RebuildAttendanceWorkdaySummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/logs/workday-summary/rebuild",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RebuildAttendanceWorkdaySummaryResult>(cancellationToken);
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/logs/workday-summary/delete",
            ids,
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }

    public async Task<AttendanceWorkdaySummaryListItemDto> UpdateAsync(
        UpdateAttendanceWorkdaySummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/logs/workday-summary/update",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceWorkdaySummaryListItemDto>(cancellationToken);
    }

    public async Task SetLockStateAsync(
        SetAttendanceWorkdaySummaryLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/logs/workday-summary/lock-state",
            request,
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }
}
