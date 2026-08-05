using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem.DependencyInjection;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTrachNhiem;

public sealed class PhuCapTrachNhiemDependencyInjectionTests
{
    [Fact]
    public void AddPhuCapTrachNhiem_registers_focused_read_and_command_capabilities()
    {
        var services = new ServiceCollection();

        services.AddPhuCapTrachNhiem();

        Assert.Contains(services, x => x.ServiceType == typeof(IPayrollResponsibilityAllowanceGradeConfigurationReadService));
        Assert.Contains(services, x => x.ServiceType == typeof(IPayrollResponsibilityAllowanceGradeConfigurationWriteService));
        Assert.Contains(services, x => x.ServiceType == typeof(IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService));
        Assert.Contains(services, x => x.ServiceType == typeof(IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService));
        Assert.Contains(services, x => x.ServiceType == typeof(IPayrollResponsibilityAllowanceMonthlyAbcQueryService));
        Assert.Contains(services, x => x.ServiceType == typeof(IPayrollResponsibilityAllowanceMonthlyAbcRefreshService));
        Assert.Contains(services, x => x.ServiceType == typeof(IPayrollResponsibilityAllowanceMonthlyAbcLockService));
        Assert.Contains(services, x => x.ServiceType == typeof(IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService));
        Assert.Contains(services, x => x.ServiceType == typeof(IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService));
    }
}
