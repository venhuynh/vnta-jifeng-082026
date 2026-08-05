using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;

public static class OtherAllowanceServiceCollectionExtensions
{
    public static IServiceCollection AddOtherAllowance(this IServiceCollection services)
    {
        services.AddScoped<DatabaseOtherAllowanceQueryService>();
        services.AddScoped<IOtherAllowanceReadService>(sp => sp.GetRequiredService<DatabaseOtherAllowanceQueryService>());
        services.AddScoped<DatabaseOtherAllowanceCreateService>();
        services.AddScoped<IOtherAllowanceCreateService>(sp => sp.GetRequiredService<DatabaseOtherAllowanceCreateService>());
        services.AddScoped<DatabaseOtherAllowancePreviousMonthSyncService>();
        services.AddScoped<IOtherAllowancePreviousMonthSyncService>(sp => sp.GetRequiredService<DatabaseOtherAllowancePreviousMonthSyncService>());
        services.AddScoped<DatabaseOtherAllowanceUpdateService>();
        services.AddScoped<IOtherAllowanceUpdateService>(sp => sp.GetRequiredService<DatabaseOtherAllowanceUpdateService>());
        services.AddScoped<DatabaseOtherAllowanceLockStateService>();
        services.AddScoped<IOtherAllowanceLockService>(sp => sp.GetRequiredService<DatabaseOtherAllowanceLockStateService>());
        services.AddScoped<DatabaseOtherAllowanceDeleteService>();
        services.AddScoped<IOtherAllowanceDeleteService>(sp => sp.GetRequiredService<DatabaseOtherAllowanceDeleteService>());
        return services;
    }
}
