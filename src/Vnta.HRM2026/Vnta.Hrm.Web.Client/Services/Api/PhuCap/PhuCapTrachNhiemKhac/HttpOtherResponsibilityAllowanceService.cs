using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapTrachNhiemKhac;

public sealed class HttpOtherResponsibilityAllowanceService(
    IHttpClientFactory httpClientFactory,
    NavigationManager navigationManager)
    : IOtherResponsibilityAllowanceReadService,
      IOtherResponsibilityAllowancePeriodPreparationService,
      IOtherResponsibilityAllowanceRecalculationService,
      IOtherResponsibilityAllowanceLockService
{
    private readonly HttpClient httpClient = CreateHttpClient(httpClientFactory, navigationManager);

    public async Task PreparePeriodAsync(
        int year,
        int month,
        string? requestedBy,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"api/payroll/other-responsibility-allowance/prepare-period?year={year}&month={month}",
            content: null,
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }

    private static HttpClient CreateHttpClient(
        IHttpClientFactory httpClientFactory,
        NavigationManager navigationManager)
    {
        var client = httpClientFactory.CreateClient("VntaHrmAuthenticatedApi");
        client.BaseAddress = new Uri(navigationManager.BaseUri);
        return client;
    }

    public async Task<IReadOnlyList<OtherResponsibilityAllowanceListItemDto>> SearchAsync(
        OtherResponsibilityAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/other-responsibility-allowance/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<OtherResponsibilityAllowanceListItemDto>>(cancellationToken);
    }

    public async Task<RecalculateOtherResponsibilityAllowanceResult> RecalculateAsync(
        RecalculateOtherResponsibilityAllowanceRequest request,
        string? requestedBy = null,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/other-responsibility-allowance/recalculate",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RecalculateOtherResponsibilityAllowanceResult>(cancellationToken);
    }

    public async Task<SetOtherResponsibilityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetOtherResponsibilityAllowanceBatchLockStateRequest request,
        string? requestedBy = null,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/other-responsibility-allowance/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetOtherResponsibilityAllowanceBatchLockStateResult>(cancellationToken);
    }
}
