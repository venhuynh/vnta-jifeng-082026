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
        services.AddScoped<IAttendanceAllowanceWorkdaySource, DatabaseAttendanceAllowanceWorkdaySource>();
        services.AddSingleton<IAttendanceAllowanceRequestValidator, AttendanceAllowanceRequestValidator>();
        services.AddSingleton<AttendanceAllowanceWorkdayMetricPolicy>();
        services.AddSingleton<AttendanceAllowanceCalculationPolicy>();
        services.AddScoped<IAttendanceAllowanceReadService, DatabaseAttendanceAllowanceReadService>();
        services.AddScoped<IAttendanceAllowanceExportService, DatabaseAttendanceAllowanceExportService>();
        services.AddScoped<IAttendanceAllowanceRefreshService, DatabaseAttendanceAllowanceRefreshService>();
        services.AddScoped<IAttendanceAllowanceManualAdjustmentService, DatabaseAttendanceAllowanceManualAdjustmentService>();
        services.AddScoped<IAttendanceAllowanceLockService, DatabaseAttendanceAllowanceLockService>();
        return services;
    }
}
