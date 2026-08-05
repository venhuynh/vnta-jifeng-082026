using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpEmployeeApiService(NavigationManager navigationManager)
    : IEmployeeApiService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<EmployeeListItemDto>> SearchAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/employees/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<EmployeeListItemDto>>(cancellationToken);
    }

    public async Task<EmployeeSummaryDto> GetSummaryAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/employees/summary",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<EmployeeSummaryDto>(cancellationToken);
    }

    public async Task<EmployeeListItemDto> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/employees",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<EmployeeListItemDto>(cancellationToken);
    }

    public async Task<EmployeeListItemDto> UpdateAsync(
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/attendance/employees/{request.Id}",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<EmployeeListItemDto>(cancellationToken);
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/employees/delete",
            ids,
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }

    public async Task<EmployeeRefreshResult> RefreshFromDeviceUserProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/employees/refresh",
            new { },
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<EmployeeRefreshResult>(cancellationToken);
    }
}
