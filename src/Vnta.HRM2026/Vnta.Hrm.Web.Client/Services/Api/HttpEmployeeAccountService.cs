using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpEmployeeAccountService(NavigationManager navigationManager)
    : IEmployeeAccountService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<EmployeeAccountListItemDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/admin/employee-accounts", cancellationToken);
        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<EmployeeAccountListItemDto>>(cancellationToken);
    }

    public async Task<EmployeeAccountListItemDto> OpenAsync(
        OpenEmployeeAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/admin/employee-accounts/open",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<EmployeeAccountListItemDto>(cancellationToken);
    }

    public async Task<EmployeeAccountListItemDto> ApproveAsync(
        ReviewEmployeeAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/admin/employee-accounts/approve",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<EmployeeAccountListItemDto>(cancellationToken);
    }

    public async Task<EmployeeAccountListItemDto> RejectAsync(
        ReviewEmployeeAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/admin/employee-accounts/reject",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<EmployeeAccountListItemDto>(cancellationToken);
    }

    public async Task<EmployeeAccountListItemDto> ResetPasswordAsync(
        ResetEmployeeAccountPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/admin/employee-accounts/reset-password",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<EmployeeAccountListItemDto>(cancellationToken);
    }

    public async Task<EmployeeAccountListItemDto> ActivateAsync(
        EmployeeAccountStateChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/admin/employee-accounts/activate",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<EmployeeAccountListItemDto>(cancellationToken);
    }

    public async Task<EmployeeAccountListItemDto> DeactivateAsync(
        EmployeeAccountStateChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/admin/employee-accounts/deactivate",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<EmployeeAccountListItemDto>(cancellationToken);
    }
}
