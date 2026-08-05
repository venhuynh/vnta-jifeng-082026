using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpEmployeeTaxDependentService(NavigationManager navigationManager)
    : IEmployeeTaxDependentService
{
    private readonly HttpClient httpClient = new() { BaseAddress = new Uri(navigationManager.BaseUri) };

    public async Task<IReadOnlyList<EmployeeTaxDependentDto>> GetByEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/payroll/tax-dependents/{employeeId}", cancellationToken);
        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<EmployeeTaxDependentDto>>(cancellationToken);
    }

    public async Task<EmployeeTaxDependentPageDto> SearchAsync(
        EmployeeTaxDependentFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/tax-dependents/search", filter, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<EmployeeTaxDependentPageDto>(cancellationToken);
    }

    public async Task<EmployeeTaxDependentDto> SaveAsync(
        SaveEmployeeTaxDependentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/tax-dependents", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<EmployeeTaxDependentDto>(cancellationToken);
    }
}
