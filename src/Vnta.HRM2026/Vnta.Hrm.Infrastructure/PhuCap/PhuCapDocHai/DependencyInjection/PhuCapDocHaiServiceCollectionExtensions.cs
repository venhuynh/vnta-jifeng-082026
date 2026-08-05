using Microsoft.Extensions.DependencyInjection;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

/// <summary>Registers hazard allowance policies and feature contracts in one place.</summary>
public static class PhuCapDocHaiServiceCollectionExtensions
{
    public static IServiceCollection AddPhuCapDocHai(this IServiceCollection services)
    {
        services.AddSingleton<IHazardAllowanceRequestValidator, HazardAllowanceRequestValidator>();
        services.AddScoped<IHazardAllowanceCalculationPolicy, HazardAllowanceCalculationPolicy>();
        services.AddScoped<IHazardAllowanceWorkdayMetricsCalculator, HazardAllowanceWorkdayMetricsCalculator>();
        services.AddScoped<IHazardAllowanceManualAdjustmentPolicy, HazardAllowanceManualAdjustmentPolicy>();
        services.AddScoped<HazardAllowanceLockStatePolicy>();
        services.AddScoped<HazardAllowanceReadProjection>();
        services.AddScoped<DatabaseHazardAllowanceReadService>();
        services.AddScoped<DatabaseHazardAllowanceExportService>();
        services.AddScoped<DatabaseHazardAllowanceRefreshService>();
        services.AddScoped<DatabaseHazardAllowanceManualAdjustmentService>();
        services.AddScoped<DatabaseHazardAllowanceEntitlementService>();
        services.AddScoped<DatabaseHazardAllowanceLockService>();
        services.AddScoped<DatabaseHazardAllowanceExportJobService>();
        services.AddScoped<IHazardAllowanceReadService>(sp => sp.GetRequiredService<DatabaseHazardAllowanceReadService>());
        services.AddScoped<IHazardAllowanceExportService>(sp => sp.GetRequiredService<DatabaseHazardAllowanceExportService>());
        services.AddScoped<IHazardAllowanceRefreshService>(sp => sp.GetRequiredService<DatabaseHazardAllowanceRefreshService>());
        services.AddScoped<IHazardAllowanceManualAdjustmentService>(sp => sp.GetRequiredService<DatabaseHazardAllowanceManualAdjustmentService>());
        services.AddScoped<IHazardAllowanceEntitlementService>(sp => sp.GetRequiredService<DatabaseHazardAllowanceEntitlementService>());
        services.AddScoped<IHazardAllowanceLockService>(sp => sp.GetRequiredService<DatabaseHazardAllowanceLockService>());
        services.AddScoped<IHazardAllowanceExportJobService>(sp => sp.GetRequiredService<DatabaseHazardAllowanceExportJobService>());
        services.AddScoped<IHazardAllowanceExportJobProcessor>(sp => sp.GetRequiredService<DatabaseHazardAllowanceExportJobService>());
        return services;
    }
}
