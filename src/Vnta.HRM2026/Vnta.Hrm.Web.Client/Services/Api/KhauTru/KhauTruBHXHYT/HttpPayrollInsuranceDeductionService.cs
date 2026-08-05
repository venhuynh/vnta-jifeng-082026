using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api.KhauTru.KhauTruBHXHYT;

public sealed class HttpPayrollInsuranceDeductionService(NavigationManager navigationManager)
    : IPayrollInsuranceDeductionReadService,
      IPayrollInsuranceDeductionRefreshService,
      IPayrollInsuranceDeductionPreviousMonthSyncService,
      IPayrollInsuranceDeductionManualAdjustmentService,
      IPayrollInsuranceDeductionLockService,
      IPayrollInsuranceDeductionLegacyWriteService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<PayrollInsuranceDeductionPageDto> SearchAsync(
        PayrollInsuranceDeductionFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/social-health-insurance-deductions/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollInsuranceDeductionPageDto>(cancellationToken);
    }

    public async Task<SyncPayrollInsuranceDeductionFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncPayrollInsuranceDeductionFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/social-health-insurance-deductions/sync-previous-month",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SyncPayrollInsuranceDeductionFromPreviousMonthResult>(cancellationToken);
    }

    public async Task<RefreshPayrollInsuranceDeductionResult> RefreshAsync(
        RefreshPayrollInsuranceDeductionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/social-health-insurance-deductions/refresh",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RefreshPayrollInsuranceDeductionResult>(cancellationToken);
    }

    public async Task<PayrollInsuranceDeductionListItemDto> UpdateManualValuesAsync(
        UpdatePayrollInsuranceDeductionManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/social-health-insurance-deductions/manual-values",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollInsuranceDeductionListItemDto>(cancellationToken);
    }

    public async Task<PayrollInsuranceDeductionListItemDto> SetLockStateAsync(
        SetPayrollInsuranceDeductionLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/social-health-insurance-deductions/lock-state",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollInsuranceDeductionListItemDto>(cancellationToken);
    }

    public async Task<SetPayrollInsuranceDeductionBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollInsuranceDeductionBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/social-health-insurance-deductions/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetPayrollInsuranceDeductionBatchLockStateResult>(cancellationToken);
    }

    public async Task<string?> ValidateAsync(
        UpsertPayrollInsuranceDeductionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/social-health-insurance-deductions/validate",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<string?>(cancellationToken);
    }

    public async Task<PayrollInsuranceDeductionListItemDto> SaveAsync(
        UpsertPayrollInsuranceDeductionRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/payroll/social-health-insurance-deductions?isNew={isNew}",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollInsuranceDeductionListItemDto>(cancellationToken);
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/social-health-insurance-deductions/delete",
            ids,
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }
}
