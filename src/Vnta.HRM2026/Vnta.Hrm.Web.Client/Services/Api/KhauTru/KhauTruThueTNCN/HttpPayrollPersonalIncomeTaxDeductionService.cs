using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

namespace Vnta.Hrm.Web.Client.Services.Api.KhauTru.KhauTruThueTNCN;

public sealed class HttpPayrollPersonalIncomeTaxDeductionService(NavigationManager navigationManager)
    : IPayrollPersonalIncomeTaxDeductionReadService,
      IPayrollPersonalIncomeTaxDeductionRefreshService,
      IPayrollPersonalIncomeTaxDeductionManualAdjustmentService,
      IPayrollPersonalIncomeTaxDeductionLockService
{
    private readonly HttpClient httpClient = new() { BaseAddress = new Uri(navigationManager.BaseUri) };

    public async Task<PayrollPersonalIncomeTaxDeductionPageDto> SearchAsync(
        PayrollPersonalIncomeTaxDeductionFilter filter,
        CancellationToken cancellationToken = default) =>
        await (await httpClient.PostAsJsonAsync("api/payroll/personal-income-tax-deductions/search", filter, cancellationToken))
            .ReadRequiredFromJsonAsync<PayrollPersonalIncomeTaxDeductionPageDto>(cancellationToken);

    public async Task<RefreshPayrollPersonalIncomeTaxDeductionResult> RefreshAsync(
        RefreshPayrollPersonalIncomeTaxDeductionRequest request,
        CancellationToken cancellationToken = default) =>
        await (await httpClient.PostAsJsonAsync("api/payroll/personal-income-tax-deductions/refresh", request, cancellationToken))
            .ReadRequiredFromJsonAsync<RefreshPayrollPersonalIncomeTaxDeductionResult>(cancellationToken);

    public async Task<PayrollPersonalIncomeTaxDeductionListItemDto> UpdateManualValueAsync(
        UpdatePayrollPersonalIncomeTaxDeductionManualValueRequest request,
        CancellationToken cancellationToken = default) =>
        await (await httpClient.PostAsJsonAsync("api/payroll/personal-income-tax-deductions/manual-value", request, cancellationToken))
            .ReadRequiredFromJsonAsync<PayrollPersonalIncomeTaxDeductionListItemDto>(cancellationToken);

    public async Task<SetPayrollPersonalIncomeTaxDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        await (await httpClient.PostAsJsonAsync("api/payroll/personal-income-tax-deductions/lock-state/batch", request, cancellationToken))
            .ReadRequiredFromJsonAsync<SetPayrollPersonalIncomeTaxDeductionBatchLockStateResult>(cancellationToken);
}
