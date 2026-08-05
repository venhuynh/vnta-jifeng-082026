using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpPayrollUnionFeeDeductionCommandService(NavigationManager navigationManager)
    : IPayrollUnionFeeDeductionPeriodPreparationService,
      IPayrollUnionFeeDeductionRefreshService,
      IPayrollUnionFeeDeductionManualAdjustmentService,
      IPayrollUnionFeeDeductionLockService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<PreparePayrollUnionFeeDeductionPeriodResult> PreparePeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"api/payroll/union-fee-deductions/prepare-period?year={year}&month={month}",
            content: null,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PreparePayrollUnionFeeDeductionPeriodResult>(cancellationToken);
    }

    public async Task<RefreshPayrollUnionFeeDeductionResult> RefreshAsync(
        RefreshPayrollUnionFeeDeductionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/union-fee-deductions/refresh",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RefreshPayrollUnionFeeDeductionResult>(cancellationToken);
    }

    public async Task<PayrollUnionFeeDeductionListItemDto> SetLockStateAsync(
        SetPayrollUnionFeeDeductionLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/union-fee-deductions/lock-state",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollUnionFeeDeductionListItemDto>(cancellationToken);
    }

    public async Task<PayrollUnionFeeDeductionListItemDto> UpdateManualValueAsync(
        UpdatePayrollUnionFeeDeductionManualValueRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/union-fee-deductions/manual-value",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollUnionFeeDeductionListItemDto>(cancellationToken);
    }

    public async Task<SetPayrollUnionFeeDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollUnionFeeDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/union-fee-deductions/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetPayrollUnionFeeDeductionBatchLockStateResult>(cancellationToken);
    }
}
