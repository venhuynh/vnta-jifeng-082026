using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruPhiCongDoan;

public static class KhauTruPhiCongDoanServiceCollectionExtensions
{
    public static IServiceCollection AddKhauTruPhiCongDoan(this IServiceCollection services)
    {
        services.AddScoped<DatabasePayrollUnionFeeDeductionReadService>();
        services.AddScoped<IPayrollUnionFeeDeductionReadService>(sp =>
            sp.GetRequiredService<DatabasePayrollUnionFeeDeductionReadService>());

        services.AddScoped<DatabasePayrollUnionFeeDeductionCommandService>();
        services.AddScoped<IPayrollUnionFeeDeductionPeriodPreparationService>(sp =>
            sp.GetRequiredService<DatabasePayrollUnionFeeDeductionCommandService>());
        services.AddScoped<IPayrollUnionFeeDeductionRefreshService>(sp =>
            sp.GetRequiredService<DatabasePayrollUnionFeeDeductionCommandService>());
        services.AddScoped<IPayrollUnionFeeDeductionManualAdjustmentService>(sp =>
            sp.GetRequiredService<DatabasePayrollUnionFeeDeductionCommandService>());
        services.AddScoped<IPayrollUnionFeeDeductionLockService>(sp =>
            sp.GetRequiredService<DatabasePayrollUnionFeeDeductionCommandService>());
        return services;
    }
}
