using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Persistence;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Queries;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.DependencyInjection;

/// <summary>Đăng ký các capability hạ tầng của feature Phụ cấp thâm niên.</summary>
public static class PhuCapThamNienServiceCollectionExtensions
{
    public static IServiceCollection AddPhuCapThamNien(this IServiceCollection services)
    {
        services.AddSingleton<IPayrollEmployeeSeniorityAllowanceCalculator, PayrollEmployeeSeniorityAllowanceCalculator>();
        services.AddSingleton<IPayrollEmployeeSeniorityAllowanceWorkdayCalculator, PayrollEmployeeSeniorityAllowanceWorkdayCalculator>();
        services.AddSingleton<IPayrollEmployeeSeniorityAllowanceTenureCalculator, PayrollEmployeeSeniorityAllowanceTenureCalculator>();
        services.AddScoped<IPayrollEmployeeSeniorityAllowanceWorkdaySource, DatabasePayrollEmployeeSeniorityAllowanceWorkdaySource>();
        services.AddScoped<SeniorityAllowancePeriodWriter>();
        services.AddScoped<IPayrollEmployeeSeniorityAllowanceReadService, DatabasePayrollEmployeeSeniorityAllowanceReadService>();
        services.AddScoped<IPayrollEmployeeSeniorityAllowanceRangeSummaryService, DatabasePayrollEmployeeSeniorityAllowanceRangeSummaryService>();
        services.AddScoped<IPayrollEmployeeSeniorityAllowancePeriodPreparationService, DatabasePayrollEmployeeSeniorityAllowancePeriodPreparationService>();
        services.AddScoped<IPayrollEmployeeSeniorityAllowanceRefreshService, DatabasePayrollEmployeeSeniorityAllowanceRefreshService>();
        services.AddScoped<IPayrollEmployeeSeniorityAllowanceManualAdjustmentService, DatabasePayrollEmployeeSeniorityAllowanceManualAdjustmentService>();
        services.AddScoped<IPayrollEmployeeSeniorityAllowanceLockService, DatabasePayrollEmployeeSeniorityAllowanceLockService>();
        return services;
    }
}
