using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpPayrollEmployeeOtherDeductionAllowanceService(NavigationManager navigationManager)
    : IPayrollEmployeeOtherDeductionAllowanceService
{
    private readonly HttpClient httpClient = new() { BaseAddress = new Uri(navigationManager.BaseUri) };

    public async Task PreparePeriodAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/other-deductions/prepare-period",
            new PreparePayrollEmployeeOtherDeductionAllowancePeriodRequest(year, month),
            cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollEmployeeOtherDeductionAllowanceListItemDto>> SearchAsync(PayrollEmployeeOtherDeductionAllowanceFilter filter, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/other-deductions/search", filter, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<PayrollEmployeeOtherDeductionAllowanceListItemDto>>(cancellationToken);
    }

    public async Task<PayrollEmployeeOtherDeductionAllowancePageDto> SearchPageAsync(PayrollEmployeeOtherDeductionAllowanceFilter filter, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/other-deductions/search-page", filter, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<PayrollEmployeeOtherDeductionAllowancePageDto>(cancellationToken);
    }

    public async Task<RefreshPayrollEmployeeOtherDeductionAllowanceResult> RefreshAsync(RefreshPayrollEmployeeOtherDeductionAllowanceRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/other-deductions/refresh", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<RefreshPayrollEmployeeOtherDeductionAllowanceResult>(cancellationToken);
    }

    public async Task<PayrollEmployeeOtherDeductionAllowanceListItemDto> UpdateManualValuesAsync(UpdatePayrollEmployeeOtherDeductionAllowanceManualValuesRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/other-deductions/manual-values", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<PayrollEmployeeOtherDeductionAllowanceListItemDto>(cancellationToken);
    }

    public async Task<PayrollEmployeeOtherDeductionAllowanceListItemDto> SetLockStateAsync(SetPayrollEmployeeOtherDeductionAllowanceLockStateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/other-deductions/lock-state", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<PayrollEmployeeOtherDeductionAllowanceListItemDto>(cancellationToken);
    }

    public async Task<SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/payroll/other-deductions/lock-state/batch", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateResult>(cancellationToken);
    }
}
