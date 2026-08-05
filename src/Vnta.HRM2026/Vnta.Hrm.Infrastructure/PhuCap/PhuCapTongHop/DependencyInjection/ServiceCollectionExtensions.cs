using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Queries;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapDashboard.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.DependencyInjection;

/// <summary>Feature composition for allowance-summary read and command use cases.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPhuCapTongHop(this IServiceCollection services)
    {
        services.AddScoped<PayrollAllowanceSummaryPersistence>();
        services.AddScoped<IPayrollAllowanceSummaryReadService, DatabasePayrollAllowanceSummaryReadService>();
        services.AddScoped<IPayrollAllowanceSummaryExportService, DatabasePayrollAllowanceSummaryExportService>();
        services.AddScoped<IPayrollAllowanceDashboardReadService, DatabasePayrollAllowanceDashboardQueryService>();
        services.AddScoped<IPayrollAllowanceDashboardBreakdownQueryService, DatabasePayrollAllowanceDashboardQueryService>();
        services.AddScoped<IPayrollAllowanceDashboardTrendQueryService, DatabasePayrollAllowanceDashboardQueryService>();
        services.AddScoped<IPayrollAllowanceDashboardMonthlyComparisonQueryService, DatabasePayrollAllowanceDashboardQueryService>();
        services.AddScoped<IPayrollAllowanceDashboardDepartmentComparisonQueryService, DatabasePayrollAllowanceDashboardQueryService>();
        services.AddScoped<IPayrollAllowanceSummaryPreviousMonthSyncService, DatabasePayrollAllowanceSummaryPreviousMonthSyncService>();
        services.AddScoped<IPayrollAllowanceSummaryRefreshService, DatabasePayrollAllowanceSummaryRefreshService>();
        services.AddScoped<IPayrollAllowanceSummaryDeletionService, DatabasePayrollAllowanceSummaryDeletionService>();
        services.AddScoped<IPayrollAllowanceSummaryManualAdjustmentService, DatabasePayrollAllowanceSummaryManualAdjustmentService>();
        services.AddScoped<IPayrollAllowanceSummaryLockService, DatabasePayrollAllowanceSummaryLockService>();
        return services;
    }
}
