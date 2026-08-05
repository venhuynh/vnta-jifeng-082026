using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT.Commands;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT.Queries;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruBHXHYT;

public sealed class KhauTruBHXHYTServiceCollectionExtensionsTests
{
    [Fact]
    public void Feature_extension_maps_each_capability_to_its_own_use_case_service()
    {
        var services = new ServiceCollection();

        services.AddKhauTruBHXHYT();

        AssertImplementation<IPayrollInsuranceDeductionReadService, DatabasePayrollInsuranceDeductionReadService>(services);
        AssertImplementation<IPayrollInsuranceDeductionRefreshService, DatabasePayrollInsuranceDeductionRefreshService>(services);
        AssertImplementation<IPayrollInsuranceDeductionPreviousMonthSyncService, DatabasePayrollInsuranceDeductionPreviousMonthSyncService>(services);
        AssertImplementation<IPayrollInsuranceDeductionManualAdjustmentService, DatabasePayrollInsuranceDeductionManualAdjustmentService>(services);
        AssertImplementation<IPayrollInsuranceDeductionLockService, DatabasePayrollInsuranceDeductionLockService>(services);
        AssertImplementation<IPayrollInsuranceDeductionLegacyWriteService, DatabasePayrollInsuranceDeductionLegacyWriteService>(services);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(PayrollInsuranceDeductionPersistence));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(DatabasePayrollInsuranceDeductionService));
    }

    private static void AssertImplementation<TContract, TImplementation>(IServiceCollection services)
        where TContract : class
        where TImplementation : class
    {
        var registration = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TContract));
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
        Assert.Equal(typeof(TImplementation), registration.ImplementationType);
    }
}
