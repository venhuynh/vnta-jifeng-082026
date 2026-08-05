using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api;

/// <summary>
/// HTTP adapter cho nghiệp vụ tổng hợp phụ cấp.
/// Mỗi phương thức ánh xạ một-một tới endpoint dưới <c>/api/payroll/allowance-summary</c>.
/// </summary>
public sealed class HttpPayrollAllowanceSummaryService(NavigationManager navigationManager)
    : IPayrollAllowanceDashboardReadService,
      IPayrollAllowanceDashboardBreakdownQueryService,
      IPayrollAllowanceDashboardTrendQueryService,
      IPayrollAllowanceDashboardMonthlyComparisonQueryService,
      IPayrollAllowanceDashboardDepartmentComparisonQueryService,
      IPayrollAllowanceSummaryReadService,
      IPayrollAllowanceSummaryExportService,
      IPayrollAllowanceSummaryPreviousMonthSyncService,
      IPayrollAllowanceSummaryRefreshService,
      IPayrollAllowanceSummaryManualAdjustmentService,
      IPayrollAllowanceSummaryLockService
{
    // Dùng base URI của ứng dụng đang chạy để request luôn cùng origin với Blazor Server.
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    #region Truy vấn

    public async Task<PayrollAllowanceSummaryOverviewDto> GetSummaryAsync(
        PayrollAllowanceSummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/allowance-summary/summary",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollAllowanceSummaryOverviewDto>(cancellationToken);
    }

    public async Task<PayrollAllowanceDashboardDto> GetDashboardAsync(
        PayrollAllowanceDashboardFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/allowance-summary/dashboard",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollAllowanceDashboardDto>(cancellationToken);
    }

    public Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceBreakdownDto>> GetAllowanceBreakdownAsync(PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default) =>
        PostDashboardReportAsync<PayrollAllowanceDashboardAllowanceBreakdownDto>("breakdown", filter, cancellationToken);

    public Task<IReadOnlyList<PayrollAllowanceDashboardTrendPointDto>> GetTrendAsync(PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default) =>
        PostDashboardReportAsync<PayrollAllowanceDashboardTrendPointDto>("trend", filter, cancellationToken);

    public Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceComparisonDto>> GetAllowanceMonthlyComparisonAsync(PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default) =>
        PostDashboardReportAsync<PayrollAllowanceDashboardAllowanceComparisonDto>("monthly-comparison", filter, cancellationToken);

    public Task<IReadOnlyList<PayrollAllowanceDashboardDepartmentTreeNodeDto>> GetDepartmentMonthlyComparisonAsync(PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default) =>
        PostDashboardReportAsync<PayrollAllowanceDashboardDepartmentTreeNodeDto>("department-monthly-comparison", filter, cancellationToken);

    private async Task<IReadOnlyList<T>> PostDashboardReportAsync<T>(string report, PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync($"api/payroll/allowance-summary/dashboard/{report}", filter, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<T>>(cancellationToken);
    }

    public async Task<PayrollAllowanceSummaryPageDto> SearchAsync(
        PayrollAllowanceSummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/allowance-summary/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollAllowanceSummaryPageDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollAllowanceSummaryExportRowDto>> ExportAsync(
        PayrollAllowanceSummaryExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/allowance-summary/export",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<PayrollAllowanceSummaryExportRowDto>>(cancellationToken);
    }

    #endregion

    #region Đồng bộ và làm mới

    public async Task<SyncPayrollAllowanceSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncPayrollAllowanceSummaryFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/allowance-summary/sync-previous-month",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SyncPayrollAllowanceSummaryFromPreviousMonthResult>(cancellationToken);
    }

    public async Task<RefreshPayrollAllowanceSummaryResult> RefreshAsync(
        RefreshPayrollAllowanceSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/allowance-summary/refresh",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RefreshPayrollAllowanceSummaryResult>(cancellationToken);
    }

    #endregion

    #region Thay đổi dữ liệu

    public async Task<PayrollAllowanceSummaryListItemDto> SetLockStateAsync(
        SetPayrollAllowanceSummaryLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/allowance-summary/lock-state",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollAllowanceSummaryListItemDto>(cancellationToken);
    }

    public async Task<SetPayrollAllowanceSummaryBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollAllowanceSummaryBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/allowance-summary/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetPayrollAllowanceSummaryBatchLockStateResult>(cancellationToken);
    }

    public async Task<PayrollAllowanceSummaryListItemDto> UpdateManualValuesAsync(
        UpdatePayrollAllowanceSummaryManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/allowance-summary/manual-adjustment",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollAllowanceSummaryListItemDto>(cancellationToken);
    }
    #endregion
}
