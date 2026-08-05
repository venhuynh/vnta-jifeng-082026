namespace Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruTongHop;

/// <summary>Cổng đọc dashboard khấu trừ cho giao diện Blazor.</summary>
public sealed class PayrollDeductionDashboardDataProvider(
    IPayrollDeductionDashboardService dashboardService)
{
    public Task<PayrollDeductionDashboardDto> GetDashboardAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken = default) =>
        dashboardService.GetDashboardAsync(
            new PayrollDeductionDashboardFilter(payrollMonth, payrollYear),
            cancellationToken);
}
