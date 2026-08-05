using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;

namespace Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapKhac;

/// <summary>HTTP transport for the other-allowance API. Business rules remain on the server.</summary>
public sealed class HttpOtherAllowanceService(
    IHttpClientFactory httpClientFactory,
    NavigationManager navigationManager) :
    IOtherAllowanceReadService,
    IOtherAllowanceCreateService,
    IOtherAllowanceUpdateService,
    IOtherAllowanceLockService
{
    private readonly HttpClient httpClient = CreateHttpClient(httpClientFactory, navigationManager);

    private static HttpClient CreateHttpClient(
        IHttpClientFactory httpClientFactory,
        NavigationManager navigationManager)
    {
        var client = httpClientFactory.CreateClient("VntaHrmAuthenticatedApi");
        client.BaseAddress = new Uri(navigationManager.BaseUri);
        return client;
    }

    public async Task<OtherAllowancePageDto> SearchPageAsync(OtherAllowanceFilter filter, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/other-allowances/search", filter, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<OtherAllowancePageDto>(cancellationToken);
    }

    public async Task<OtherAllowanceCommandResult> CreateAsync(CreateOtherAllowanceRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/other-allowances", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<OtherAllowanceCommandResult>(cancellationToken);
    }

    public async Task<OtherAllowanceCommandResult> UpdateAsync(UpdateOtherAllowanceRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/payroll/other-allowances", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<OtherAllowanceCommandResult>(cancellationToken);
    }

    public async Task SetLockStateAsync(SetOtherAllowanceLockStateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/other-allowances/lock-state", request, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
    }

    public async Task<SetOtherAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetOtherAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/other-allowances/lock-state/batch",
            request,
            cancellationToken);
        return await response.ReadRequiredFromJsonAsync<SetOtherAllowanceBatchLockStateResult>(cancellationToken);
    }

}
