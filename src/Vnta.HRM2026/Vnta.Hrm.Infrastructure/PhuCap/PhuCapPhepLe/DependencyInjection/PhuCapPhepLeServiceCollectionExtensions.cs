using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Policies;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Policies;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.DependencyInjection;

/// <summary>Registers isolated read and command capabilities for leave/holiday allowance.</summary>
public static class PhuCapPhepLeServiceCollectionExtensions
{
    public static IServiceCollection AddPhuCapPhepLe(this IServiceCollection services)
    {
        services.AddSingleton<ILeaveHolidayAllowanceRequestValidator, LeaveHolidayAllowanceRequestValidator>();
        services.AddScoped<DatabaseLeaveHolidayAllowanceReadService>();
        services.AddScoped<ILeaveHolidayAllowanceReadService>(sp =>
            sp.GetRequiredService<DatabaseLeaveHolidayAllowanceReadService>());
        services.AddScoped<ILeaveHolidayAllowanceRecalculationSource, DatabaseLeaveHolidayAllowanceRecalculationSource>();
        services.AddScoped<ILeaveHolidayAllowancePeriodPreparationService, DatabaseLeaveHolidayAllowancePeriodPreparationService>();
        services.AddScoped<ILeaveHolidayAllowanceClearManualValuesService, DatabaseLeaveHolidayAllowanceClearManualValuesService>();
        services.AddScoped<ILeaveHolidayAllowancePreviousMonthSyncService, DatabaseLeaveHolidayAllowancePreviousMonthSyncService>();
        services.AddScoped<ILeaveHolidayAllowanceRecalculationService, DatabaseLeaveHolidayAllowanceRecalculationService>();
        services.AddScoped<ILeaveHolidayAllowanceManualAdjustmentService, DatabaseLeaveHolidayAllowanceManualAdjustmentService>();
        services.AddScoped<ILeaveHolidayAllowanceLockService, DatabaseLeaveHolidayAllowanceLockService>();
        return services;
    }
}
