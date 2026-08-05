namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapDashboard;

/// <summary>
/// Cổng đọc dữ liệu dashboard phụ cấp cho giao diện Blazor.
/// </summary>
public sealed class PayrollAllowanceDashboardDataProvider(
    IPayrollAllowanceDashboardReadService dashboardService)
{
    public Task<PayrollAllowanceDashboardDto> GetDashboardAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken = default) =>
        dashboardService.GetDashboardAsync(
            new PayrollAllowanceDashboardFilter(payrollMonth, payrollYear),
            cancellationToken);
}

