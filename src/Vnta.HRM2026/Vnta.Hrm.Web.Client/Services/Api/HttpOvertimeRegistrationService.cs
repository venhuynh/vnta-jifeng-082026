using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpOvertimeRegistrationService(NavigationManager navigationManager)
    : IOvertimeRegistrationService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<OvertimeRegistrationListItemDto>> SearchAsync(
        OvertimeRegistrationFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/overtime-registrations/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<OvertimeRegistrationListItemDto>>(cancellationToken);
    }

    public async Task<OvertimeRegistrationDraftDto> CreateDraftAsync(
        CreateOvertimeRegistrationDraftRequest request,
        OvertimeRegistrationActorContext actorContext,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/overtime-registrations/draft",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<OvertimeRegistrationDraftDto>(cancellationToken);
    }

    public async Task<OvertimeRegistrationListItemDto> SaveAsync(
        UpsertOvertimeRegistrationRequest request,
        bool submitAfterSave,
        OvertimeRegistrationActorContext actorContext,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/attendance/overtime-registrations?submitAfterSave={submitAfterSave.ToString().ToLowerInvariant()}",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<OvertimeRegistrationListItemDto>(cancellationToken);
    }

    public async Task ChangeStatusAsync(
        ChangeOvertimeRegistrationStatusRequest request,
        OvertimeRegistrationActorContext actorContext,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/overtime-registrations/status",
            request,
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }
}
