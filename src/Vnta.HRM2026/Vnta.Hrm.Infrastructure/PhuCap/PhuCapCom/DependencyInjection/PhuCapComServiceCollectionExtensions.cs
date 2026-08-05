using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Policies;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Policies;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.DependencyInjection;

/// <summary>Registers the isolated read side, command use cases and policy adapter for meal allowance.</summary>
public static class PhuCapComServiceCollectionExtensions
{
    public static IServiceCollection AddPhuCapCom(this IServiceCollection services)
    {
        services.AddSingleton<IMealAllowanceRequestValidator, MealAllowanceRequestValidator>();
        services.AddScoped<IMealAllowanceWorkdaySource, DatabaseMealAllowanceWorkdaySource>();
        services.AddScoped<IMealAllowanceRefreshCalculator, MealAllowanceRefreshCalculator>();
        services.AddScoped<DatabaseMealAllowanceReadService>();
        services.AddScoped<IMealAllowanceReadService>(sp => sp.GetRequiredService<DatabaseMealAllowanceReadService>());
        services.AddScoped<IMealAllowanceExportService>(sp => sp.GetRequiredService<DatabaseMealAllowanceReadService>());
        services.AddScoped<IMealAllowanceRefreshService, DatabaseMealAllowanceRefreshService>();
        services.AddScoped<IMealAllowanceLockService, DatabaseMealAllowanceLockService>();
        services.AddScoped<IMealAllowanceManualAdjustmentService, DatabaseMealAllowanceManualAdjustmentService>();
        return services;
    }
}
