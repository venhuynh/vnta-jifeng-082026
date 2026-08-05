using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemGanNhanVien;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Web.Client.Utils;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class PayrollResponsibilityAllowanceServiceRegistrationTests
{
    [Fact]
    public void AddAppServices_RegistersEmployeeAssignmentProvider()
    {
        var services = new ServiceCollection();

        services.AddAppServices();

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(PhuCapTrachNhiemGanNhanVienDataProvider));
    }

    [Fact]
    public void AddAppServices_RegistersDedicatedEmployeeAssignmentViewProvider()
    {
        var services = new ServiceCollection();

        services.AddAppServices();

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(PhuCapTrachNhiemGanNhanVienXemDataProvider));
    }

    [Fact]
    public void AddAppServices_RegistersAllowanceSummaryScreenContract()
    {
        var services = new ServiceCollection();

        services.AddAppServices();

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IPayrollAllowanceSummaryDataProvider)
                && descriptor.ImplementationFactory is not null);
    }
}
