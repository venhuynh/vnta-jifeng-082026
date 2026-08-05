using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAdmsDeviceCommandService(NavigationManager navigationManager)
    : IAdmsDeviceCommandService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<AdmsDeviceCommandLookupOptionsDto> GetLookupOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/adms/device-commands/lookup-options", cancellationToken);
        return await response.ReadRequiredFromJsonAsync<AdmsDeviceCommandLookupOptionsDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<AdmsDeviceCommandSummaryDto>> SearchAsync(
        AdmsDeviceCommandFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/adms/device-commands/search", filter, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<AdmsDeviceCommandSummaryDto>>(cancellationToken);
    }

    public async Task<AdmsDeviceCommandDetailDto?> GetDetailAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/adms/device-commands/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await response.ReadRequiredFromJsonAsync<AdmsDeviceCommandDetailDto?>(cancellationToken);
    }

    public async Task<AdmsDeviceInfoResponseDto?> GetLatestInfoResponseAsync(
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"api/adms/device-commands/latest-info-response?serialNumber={Uri.EscapeDataString(serialNumber)}",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await response.ReadRequiredFromJsonAsync<AdmsDeviceInfoResponseDto?>(cancellationToken);
    }

    public async Task<AdmsDeviceCommandDetailDto> CreateAsync(
        UpsertAdmsDeviceCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/adms/device-commands", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<AdmsDeviceCommandDetailDto>(cancellationToken);
    }

    public async Task<AdmsDeviceCommandDetailDto> UpdateAsync(
        int id,
        UpsertAdmsDeviceCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/adms/device-commands/{id}", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<AdmsDeviceCommandDetailDto>(cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/adms/device-commands/{id}", cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync("api/adms/device-commands/all", cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
    }
}
