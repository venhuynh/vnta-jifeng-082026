using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapPhepLe;

public sealed class HttpLeaveHolidayAllowanceService(
    IHttpClientFactory httpClientFactory,
    NavigationManager navigationManager)
    : ILeaveHolidayAllowanceReadService,
      ILeaveHolidayAllowancePeriodPreparationService,
      ILeaveHolidayAllowanceRecalculationService,
      ILeaveHolidayAllowanceManualAdjustmentService,
      ILeaveHolidayAllowanceLockService
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

    public async Task PreparePeriodAsync(
        int payrollYear,
        int payrollMonth,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"api/payroll/leave-holiday-allowance/prepare-period?year={payrollYear}&month={payrollMonth}",
            content: null,
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveHolidayAllowanceListItemDto>> SearchAsync(
        LeaveHolidayAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/leave-holiday-allowance/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<LeaveHolidayAllowanceListItemDto>>(cancellationToken);
    }

    [Obsolete("Compatibility-only operation; remove after legacy clear consumers are retired.")]
    public async Task<ClearLeaveHolidayAllowanceManualValuesResult> ClearManualValuesAsync(
        ClearLeaveHolidayAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/leave-holiday-allowance/clear-manual-values",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<ClearLeaveHolidayAllowanceManualValuesResult>(cancellationToken);
    }

    [Obsolete("Compatibility-only operation; remove after legacy sync consumers are retired.")]
    public async Task<SyncLeaveHolidayAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncLeaveHolidayAllowanceFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/leave-holiday-allowance/sync-previous-month",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SyncLeaveHolidayAllowanceFromPreviousMonthResult>(cancellationToken);
    }

    public async Task<RecalculateLeaveHolidayAllowanceResult> RecalculateAsync(
        RecalculateLeaveHolidayAllowanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/leave-holiday-allowance/recalculate",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RecalculateLeaveHolidayAllowanceResult>(cancellationToken);
    }

    public async Task<LeaveHolidayAllowanceListItemDto> UpdateManualValuesAsync(
        UpdateLeaveHolidayAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/leave-holiday-allowance/manual-values",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<LeaveHolidayAllowanceListItemDto>(cancellationToken);
    }

    public async Task<LeaveHolidayAllowanceListItemDto> SetLockStateAsync(
        SetLeaveHolidayAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/leave-holiday-allowance/lock-state",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<LeaveHolidayAllowanceListItemDto>(cancellationToken);
    }

    public async Task<SetLeaveHolidayAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetLeaveHolidayAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/leave-holiday-allowance/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetLeaveHolidayAllowanceBatchLockStateResult>(cancellationToken);
    }
}
