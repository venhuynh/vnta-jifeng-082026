using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api.TinhLuong;

public sealed class HttpBasicSalaryService(NavigationManager navigationManager)
    : IBasicSalaryService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<BasicSalaryListItemDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/payroll/basic-salaries", cancellationToken);
        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<BasicSalaryListItemDto>>(cancellationToken);
    }

    public async Task<BasicSalaryListItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/payroll/basic-salaries/{id}", cancellationToken);
        return response.StatusCode == System.Net.HttpStatusCode.NotFound
            ? null
            : await response.ReadRequiredFromJsonAsync<BasicSalaryListItemDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<BasicSalaryListItemDto>> SearchAsync(
        BasicSalaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/basic-salaries/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<BasicSalaryListItemDto>>(cancellationToken);
    }

    public async Task<string?> ValidateAsync(
        UpsertBasicSalaryRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/basic-salaries/validate", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<string?>(cancellationToken);
    }

    public async Task<SyncBasicSalaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncBasicSalaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/basic-salaries/sync-previous-month",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SyncBasicSalaryFromPreviousMonthResult>(cancellationToken);
    }

    public async Task<BasicSalaryListItemDto> SaveAsync(
        UpsertBasicSalaryRecordRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/payroll/basic-salaries?isNew={isNew}", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<BasicSalaryListItemDto>(cancellationToken);
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/basic-salaries/delete", ids, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
    }
}
