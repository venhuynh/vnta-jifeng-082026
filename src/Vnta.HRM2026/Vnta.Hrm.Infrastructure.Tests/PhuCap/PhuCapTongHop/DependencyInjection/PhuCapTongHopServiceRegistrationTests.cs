using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.DependencyInjection;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Queries;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapDashboard.Queries;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTongHop.DependencyInjection;

public sealed class PhuCapTongHopServiceRegistrationTests
{
    [Fact]
    public void AddPhuCapTongHop_maps_read_and_each_command_capability_to_its_own_use_case_service()
    {
        var services = new ServiceCollection();

        services.AddPhuCapTongHop();

        Assert.Equal(typeof(DatabasePayrollAllowanceSummaryReadService), ImplementationFor<IPayrollAllowanceSummaryReadService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceSummaryExportService), ImplementationFor<IPayrollAllowanceSummaryExportService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceDashboardQueryService), ImplementationFor<IPayrollAllowanceDashboardReadService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceDashboardQueryService), ImplementationFor<IPayrollAllowanceDashboardBreakdownQueryService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceDashboardQueryService), ImplementationFor<IPayrollAllowanceDashboardTrendQueryService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceDashboardQueryService), ImplementationFor<IPayrollAllowanceDashboardMonthlyComparisonQueryService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceDashboardQueryService), ImplementationFor<IPayrollAllowanceDashboardDepartmentComparisonQueryService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceSummaryRefreshService), ImplementationFor<IPayrollAllowanceSummaryRefreshService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceSummaryPreviousMonthSyncService), ImplementationFor<IPayrollAllowanceSummaryPreviousMonthSyncService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceSummaryDeletionService), ImplementationFor<IPayrollAllowanceSummaryDeletionService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceSummaryManualAdjustmentService), ImplementationFor<IPayrollAllowanceSummaryManualAdjustmentService>(services));
        Assert.Equal(typeof(DatabasePayrollAllowanceSummaryLockService), ImplementationFor<IPayrollAllowanceSummaryLockService>(services));
    }

    private static Type? ImplementationFor<TService>(IServiceCollection services) =>
        services.Single(descriptor => descriptor.ServiceType == typeof(TService)).ImplementationType;
}
