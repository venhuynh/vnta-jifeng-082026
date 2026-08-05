using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
namespace Vnta.Hrm.Web.Client.Services.Api.KhauTru.KhauTruTongHop;

public sealed class HttpPayrollDeductionSummaryService(NavigationManager navigationManager)
    : IPayrollDeductionSummaryReadService,
      IPayrollDeductionSummaryExportService,
      IPayrollDeductionSummarySyncService,
      IPayrollDeductionSummaryRefreshService,
      IPayrollDeductionSummaryManualAdjustmentService,
      IPayrollDeductionSummaryLockService,
      IPayrollDeductionDashboardService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<PayrollDeductionDashboardDto> GetDashboardAsync(
        PayrollDeductionDashboardFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/deduction-summary/dashboard",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollDeductionDashboardDto>(cancellationToken);
    }

    public async Task<PayrollDeductionSummaryPageDto> SearchAsync(
        PayrollDeductionSummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/deduction-summary/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollDeductionSummaryPageDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollDeductionSummaryExportItemDto>> ExportPeriodAsync(
        int payrollMonth,
        int payrollYear,
        PayrollDeductionSummaryExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/deduction-summary/export",
            new PayrollDeductionSummaryExportRequest(payrollYear, payrollMonth, format),
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<PayrollDeductionSummaryExportItemDto>>(cancellationToken);
    }

    public async Task<SyncPayrollDeductionSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncPayrollDeductionSummaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/deduction-summary/sync-previous-month",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SyncPayrollDeductionSummaryFromPreviousMonthResult>(cancellationToken);
    }

    public async Task<RefreshPayrollDeductionSummaryResult> RefreshAsync(
        RefreshPayrollDeductionSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/deduction-summary/refresh",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RefreshPayrollDeductionSummaryResult>(cancellationToken);
    }

    public async Task<RecalculatePayrollDeductionSummaryPeriodResult> RecalculatePeriodAsync(
        RecalculatePayrollDeductionSummaryPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/deduction-summary/recalculate",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RecalculatePayrollDeductionSummaryPeriodResult>(cancellationToken);
    }

    public async Task<PayrollDeductionSummaryListItemDto> SetLockStateAsync(
        SetPayrollDeductionSummaryLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/deduction-summary/lock-state",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollDeductionSummaryListItemDto>(cancellationToken);
    }

    public async Task<SetPayrollDeductionSummaryBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollDeductionSummaryBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/deduction-summary/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetPayrollDeductionSummaryBatchLockStateResult>(cancellationToken);
    }

    public async Task<PayrollDeductionSummaryListItemDto> UpdateManualOtherDeductionAsync(
        UpdatePayrollDeductionSummaryManualOtherDeductionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/deduction-summary/manual-other-deduction",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollDeductionSummaryListItemDto>(cancellationToken);
    }
}
