using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT.Commands;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT.Queries;

namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT;

public static class KhauTruBHXHYTServiceCollectionExtensions
{
    public static IServiceCollection AddKhauTruBHXHYT(this IServiceCollection services)
    {
        services.AddScoped<IPayrollInsuranceDeductionReadService, DatabasePayrollInsuranceDeductionReadService>();
        services.AddScoped<IPayrollInsuranceDeductionRefreshService, DatabasePayrollInsuranceDeductionRefreshService>();
        services.AddScoped<IPayrollInsuranceDeductionPreviousMonthSyncService, DatabasePayrollInsuranceDeductionPreviousMonthSyncService>();
        services.AddScoped<IPayrollInsuranceDeductionManualAdjustmentService, DatabasePayrollInsuranceDeductionManualAdjustmentService>();
        services.AddScoped<IPayrollInsuranceDeductionLockService, DatabasePayrollInsuranceDeductionLockService>();
        services.AddScoped<IPayrollInsuranceDeductionLegacyWriteService, DatabasePayrollInsuranceDeductionLegacyWriteService>();
        return services;
    }
}
