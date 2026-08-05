using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem.DependencyInjection;

public static class PhuCapTrachNhiemServiceCollectionExtensions
{
    /// <summary>Registers responsibility-allowance infrastructure by focused application capability.</summary>
    public static IServiceCollection AddPhuCapTrachNhiem(this IServiceCollection services)
    {
        services.AddScoped<IPayrollResponsibilityAllowanceGradeConfigurationReadService, DatabasePayrollResponsibilityAllowanceGradeConfigurationReadService>();
        services.AddScoped<IPayrollResponsibilityAllowanceGradeConfigurationWriteService, DatabasePayrollResponsibilityAllowanceGradeConfigurationWriteService>();
        services.AddScoped<IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService, DatabasePayrollResponsibilityAllowanceEmployeeAssignmentCommandService>();
        services.AddScoped<IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService, DatabasePayrollResponsibilityAllowanceEmployeeAssignmentReadService>();
        services.AddScoped<IPayrollResponsibilityAllowanceEmployeeAssignmentExportService>(sp =>
            (IPayrollResponsibilityAllowanceEmployeeAssignmentExportService)sp.GetRequiredService<IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService>());

        services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcQueryService, DatabasePayrollResponsibilityAllowanceMonthlyAbcReadService>();
        services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcExportService>(sp =>
            (IPayrollResponsibilityAllowanceMonthlyAbcExportService)sp.GetRequiredService<IPayrollResponsibilityAllowanceMonthlyAbcQueryService>());
        services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcRefreshService, DatabasePayrollResponsibilityAllowanceMonthlyAbcRefreshCommandService>();
        services.AddScoped<IPayrollResponsibilityAllowanceRecalculationService>(sp =>
            (IPayrollResponsibilityAllowanceRecalculationService)sp.GetRequiredService<IPayrollResponsibilityAllowanceMonthlyAbcRefreshService>());
        services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcCopyService, DatabasePayrollResponsibilityAllowanceMonthlyAbcCopyCommandService>();
        services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcLockService, DatabasePayrollResponsibilityAllowanceMonthlyAbcLockCommandService>();
        services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService, DatabasePayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentCommandService>();
        services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService, DatabasePayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusCommandService>();
        services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcCommandService, PayrollResponsibilityAllowanceMonthlyAbcCommandCompatibilityAdapter>();

        services.AddScoped<IResponsibilityPositionAssignmentReadService, DatabaseResponsibilityPositionAssignmentReadService>();
        services.AddScoped<IResponsibilityPositionAssignmentExportReadService>(sp =>
            (IResponsibilityPositionAssignmentExportReadService)sp.GetRequiredService<IResponsibilityPositionAssignmentReadService>());
        services.AddScoped<IResponsibilityPositionAssignmentCommandService, DatabaseResponsibilityPositionAssignmentCommandService>();
        services.AddScoped<IResponsibilityPositionAssignmentCopyService>(sp =>
            (IResponsibilityPositionAssignmentCopyService)sp.GetRequiredService<IResponsibilityPositionAssignmentCommandService>());

        return services;
    }
}
