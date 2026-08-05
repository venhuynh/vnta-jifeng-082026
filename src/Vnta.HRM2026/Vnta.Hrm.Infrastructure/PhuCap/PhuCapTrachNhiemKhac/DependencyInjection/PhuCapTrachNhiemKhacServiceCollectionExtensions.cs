using Microsoft.Extensions.DependencyInjection;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac.DependencyInjection;

/// <summary>Feature composition boundary for other responsibility allowance capabilities.</summary>
public static class PhuCapTrachNhiemKhacServiceCollectionExtensions
{
    public static IServiceCollection AddPhuCapTrachNhiemKhac(this IServiceCollection services)
    {
        services.AddSingleton<IOtherResponsibilityAllowanceCalculator, OtherResponsibilityAllowanceCalculator>();
        services.AddSingleton<IOtherResponsibilityAllowanceWorkdayCalculator, OtherResponsibilityAllowanceWorkdayCalculator>();
        services.AddScoped<IOtherResponsibilityAllowancePeriodPreparationService, DatabaseOtherResponsibilityAllowancePeriodPreparationService>();
        services.AddScoped<IOtherResponsibilityAllowanceReadService, DatabaseOtherResponsibilityAllowanceReadService>();
        services.AddScoped<IOtherResponsibilityAllowanceRecalculationService, DatabaseOtherResponsibilityAllowanceRecalculationService>();
        services.AddScoped<IOtherResponsibilityAllowanceLockService, DatabaseOtherResponsibilityAllowanceLockService>();
        return services;
    }
}
