using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruThueTNCN.Commands;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruThueTNCN.DependencyInjection;

public static class KhauTruThueTNCNServiceCollectionExtensions
{
    public static IServiceCollection AddKhauTruThueTNCN(this IServiceCollection services)
    {
        services.AddSingleton<PayrollPersonalIncomeTaxDeductionPeriodPolicy>();
        services.AddSingleton<PayrollPersonalIncomeTaxDeductionRefreshPolicy>();
        services.AddSingleton<PayrollPersonalIncomeTaxDeductionManualValuePolicy>();
        services.AddScoped<IPayrollPersonalIncomeTaxDeductionReadService, DatabasePayrollPersonalIncomeTaxDeductionReadService>();
        services.AddScoped<IPayrollPersonalIncomeTaxDeductionRefreshService, DatabasePayrollPersonalIncomeTaxDeductionRefreshService>();
        services.AddScoped<IPayrollPersonalIncomeTaxDeductionManualAdjustmentService, DatabasePayrollPersonalIncomeTaxDeductionManualAdjustmentService>();
        services.AddScoped<IPayrollPersonalIncomeTaxDeductionLockService, DatabasePayrollPersonalIncomeTaxDeductionLockService>();
        return services;
    }
}
