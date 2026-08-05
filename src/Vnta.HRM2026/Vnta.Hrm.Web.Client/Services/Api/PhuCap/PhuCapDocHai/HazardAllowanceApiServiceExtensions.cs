using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Services;

internal static class HazardAllowanceApiServiceExtensions
{
    internal static IServiceCollection AddHazardAllowanceApi(this IServiceCollection services)
    {
        services.AddScoped<HttpHazardAllowanceService>();
        services.AddScoped<IHazardAllowanceReadService>(sp => sp.GetRequiredService<HttpHazardAllowanceService>());
        services.AddScoped<IHazardAllowanceExportService>(sp => sp.GetRequiredService<HttpHazardAllowanceService>());
        services.AddScoped<IHazardAllowanceExportJobService>(sp => sp.GetRequiredService<HttpHazardAllowanceService>());
        services.AddScoped<IHazardAllowanceRefreshService>(sp => sp.GetRequiredService<HttpHazardAllowanceService>());
        services.AddScoped<IHazardAllowanceManualAdjustmentService>(sp => sp.GetRequiredService<HttpHazardAllowanceService>());
        services.AddScoped<IHazardAllowanceEntitlementService>(sp => sp.GetRequiredService<HttpHazardAllowanceService>());
        services.AddScoped<IHazardAllowanceLockService>(sp => sp.GetRequiredService<HttpHazardAllowanceService>());
        return services;
    }
}
