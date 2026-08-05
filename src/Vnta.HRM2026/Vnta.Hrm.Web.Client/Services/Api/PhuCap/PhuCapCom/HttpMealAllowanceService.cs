using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapCom;

public sealed class HttpMealAllowanceService(NavigationManager navigationManager)
    : IMealAllowanceReadService,
      IMealAllowanceExportService,
      IMealAllowanceRefreshService,
      IMealAllowanceLockService,
      IMealAllowanceManualAdjustmentService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<MealAllowanceListItemDto>> SearchAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/meal-allowance/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<MealAllowanceListItemDto>>(cancellationToken);
    }

    public async Task<MealAllowancePageDto> SearchPageAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/meal-allowance/search-page",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<MealAllowancePageDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<MealAllowanceListItemDto>> ExportPeriodAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"api/payroll/meal-allowance/export-period/{payrollYear}/{payrollMonth}",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<MealAllowanceListItemDto>>(cancellationToken);
    }

    public async Task<MealAllowanceSummaryDto> GetSummaryAsync(
        MealAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/meal-allowance/summary",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<MealAllowanceSummaryDto>(cancellationToken);
    }

    public async Task<RefreshMealAllowanceResult> RefreshAsync(
        RefreshMealAllowanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/meal-allowance/refresh",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RefreshMealAllowanceResult>(cancellationToken);
    }

    public async Task<MealAllowanceListItemDto> UpdateManualValuesAsync(
        UpdateMealAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/meal-allowance/manual-values",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<MealAllowanceListItemDto>(cancellationToken);
    }

    public async Task<SetMealAllowanceLockStateBatchResult> SetLockStateBatchAsync(
        SetMealAllowanceLockStateBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/meal-allowance/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetMealAllowanceLockStateBatchResult>(cancellationToken);
    }

}
