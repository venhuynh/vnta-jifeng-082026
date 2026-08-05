using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceDeviceService(NavigationManager navigationManager)
    : IAttendanceDeviceService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<AttendanceDeviceDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/attendance/devices", cancellationToken);
        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<AttendanceDeviceDto>>(cancellationToken);
    }

    public async Task<string?> ValidateAsync(
        UpsertAttendanceDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/attendance/devices/validate", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<string?>(cancellationToken);
    }

    public async Task<AttendanceDeviceDto> SaveAsync(
        UpsertAttendanceDeviceRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/attendance/devices?isNew={isNew}", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<AttendanceDeviceDto>(cancellationToken);
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/attendance/devices/delete", ids, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
    }
}
