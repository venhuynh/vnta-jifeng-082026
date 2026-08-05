using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop.DependencyInjection;

/// <summary>Feature composition boundary for deduction-summary read models, commands and policy adapters.</summary>
public static class KhauTruTongHopServiceCollectionExtensions
{
    public static IServiceCollection AddKhauTruTongHop(this IServiceCollection services)
    {
        services.AddSingleton<IPayrollDeductionSummaryRequestValidator, PayrollDeductionSummaryRequestValidator>();
        services.AddScoped<IPayrollDeductionSummaryTargetRosterPolicy, DatabasePayrollDeductionSummaryTargetRosterPolicy>();
        services.AddScoped<DatabasePayrollDeductionSummaryReadService>();
        services.AddScoped<IPayrollDeductionSummaryReadService>(sp => sp.GetRequiredService<DatabasePayrollDeductionSummaryReadService>());
        services.AddScoped<IPayrollDeductionSummaryExportService>(sp => sp.GetRequiredService<DatabasePayrollDeductionSummaryReadService>());
        services.AddScoped<IPayrollDeductionSummarySyncService, DatabasePayrollDeductionSummarySyncService>();
        services.AddScoped<IPayrollDeductionSummaryRefreshService, DatabasePayrollDeductionSummaryRefreshService>();
        services.AddScoped<IPayrollDeductionSummaryManualAdjustmentService, DatabasePayrollDeductionSummaryManualAdjustmentService>();
        services.AddScoped<IPayrollDeductionSummaryLockService, DatabasePayrollDeductionSummaryLockService>();
        services.AddScoped<IPayrollDeductionDashboardService, DatabasePayrollDeductionDashboardService>();
        return services;
    }
}
