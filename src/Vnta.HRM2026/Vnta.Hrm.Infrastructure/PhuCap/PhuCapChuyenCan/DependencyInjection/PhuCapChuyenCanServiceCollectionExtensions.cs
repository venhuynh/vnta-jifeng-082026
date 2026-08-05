using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.DependencyInjection;

/// <summary>Feature composition boundary for attendance allowance read side, commands and policy adapters.</summary>
public static class PhuCapChuyenCanServiceCollectionExtensions
{
    public static IServiceCollection AddPhuCapChuyenCan(this IServiceCollection services)
    {
        services.AddScoped<DatabaseAttendanceAllowanceWorkdaySource>();
        services.AddScoped<IAttendanceAllowanceWorkdaySource>(serviceProvider =>
            serviceProvider.GetRequiredService<DatabaseAttendanceAllowanceWorkdaySource>());
        services.AddScoped<IAttendanceAllowanceWorkdayInputSource>(serviceProvider =>
            serviceProvider.GetRequiredService<DatabaseAttendanceAllowanceWorkdaySource>());
        services.AddScoped<IAttendanceAllowanceEligibleStatusCodeSource>(serviceProvider =>
            serviceProvider.GetRequiredService<DatabaseAttendanceAllowanceWorkdaySource>());
        services.AddSingleton<AttendanceAllowanceRequestValidator>();
        services.AddSingleton<IAttendanceAllowanceRequestValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<AttendanceAllowanceRequestValidator>());
        services.AddSingleton<IAttendanceAllowancePayrollPeriodValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<AttendanceAllowanceRequestValidator>());
        services.AddSingleton<IAttendanceAllowanceRefreshRequestValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<AttendanceAllowanceRequestValidator>());
        services.AddSingleton<IAttendanceAllowanceManualAdjustmentRequestValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<AttendanceAllowanceRequestValidator>());
        services.AddSingleton<IAttendanceAllowanceLockStateRequestValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<AttendanceAllowanceRequestValidator>());
        services.AddSingleton<IAttendanceAllowanceBatchLockRequestValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<AttendanceAllowanceRequestValidator>());
        services.AddSingleton<IAttendanceAllowanceExportRequestValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<AttendanceAllowanceRequestValidator>());
        services.AddSingleton<AttendanceAllowanceWorkdayMetricPolicy>();
        services.AddSingleton<AttendanceAllowanceCalculationPolicy>();
        services.AddSingleton<AttendanceAllowanceWorkdayAdjustmentPolicy>();
        services.AddScoped<IAttendanceAllowanceReadService, DatabaseAttendanceAllowanceReadService>();
        services.AddScoped<IAttendanceAllowanceExportService, DatabaseAttendanceAllowanceExportService>();
        services.AddScoped<IAttendanceAllowanceRefreshService, DatabaseAttendanceAllowanceRefreshService>();
        services.AddScoped<IAttendanceAllowanceManualAdjustmentService, DatabaseAttendanceAllowanceManualAdjustmentService>();
        services.AddScoped<IAttendanceAllowanceWorkdayAdjustmentService, DatabaseAttendanceAllowanceManualAdjustmentService>();
        services.AddScoped<IAttendanceAllowanceLockService, DatabaseAttendanceAllowanceLockService>();
        return services;
    }
}
