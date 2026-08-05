using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceStatusCodeService(NavigationManager navigationManager)
    : IAttendanceStatusCodeService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<AttendanceStatusCodeListItemDto>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return await HrmReadRetryPolicy.ExecuteAsync(async token =>
        {
            var response = await httpClient.GetAsync("api/attendance/status-codes", token);
            return await response.ReadRequiredFromJsonAsync<IReadOnlyList<AttendanceStatusCodeListItemDto>>(token);
        }, cancellationToken);
    }

    public async Task<AttendanceStatusCodeListItemDto> UpdateFlagsAsync(
        UpdateAttendanceStatusCodeFlagsRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/attendance/status-codes/{request.Id}",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceStatusCodeListItemDto>(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/attendance/status-codes/{id}", cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
    }
}
