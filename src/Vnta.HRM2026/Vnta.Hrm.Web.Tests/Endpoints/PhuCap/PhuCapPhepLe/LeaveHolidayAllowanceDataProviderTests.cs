using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;
using Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Web.Client.Utils;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapPhepLe;

public sealed class LeaveHolidayAllowanceDataProviderTests
{
    [Fact]
    public void Client_di_registers_the_provider_and_all_current_capability_contracts()
    {
        var services = new ServiceCollection();

        services.AddAppServices();
        services.AddBrowserApiServices();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(LeaveHolidayAllowanceDataProvider));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ILeaveHolidayAllowanceDataProvider));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(HttpLeaveHolidayAllowanceService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ILeaveHolidayAllowanceReadService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ILeaveHolidayAllowancePeriodPreparationService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ILeaveHolidayAllowanceRecalculationService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ILeaveHolidayAllowanceManualAdjustmentService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ILeaveHolidayAllowanceLockService));
    }

    [Fact]
    public async Task Search_maps_the_complete_server_snapshot_to_the_ui_record()
    {
        var source = new LeaveHolidayAllowanceListItemDto(
            Guid.NewGuid(), Guid.NewGuid(), "NV001", "Nguyen Van A", "Payroll", "Specialist",
            7, 2026, 100_000.25m, 1.5m, 2.25m, 375_000.94m, "manual holiday",
            true, DateTime.UnixEpoch, "creator", DateTime.UnixEpoch.AddMinutes(1), "editor", DateTime.UnixEpoch.AddMinutes(2));
        var readService = new CapturingReadService(source);
        var provider = new LeaveHolidayAllowanceDataProvider(
            readService, null!, null!, null!, null!, null!, null!, null!);
        var filter = new LeaveHolidayAllowanceFilter(7, 2026, "NV001", Take: 20);

        var records = await provider.SearchAsync(filter);

        var record = Assert.Single(records);
        Assert.Equal(filter, readService.Filter);
        Assert.Equal(source.PayrollAllowanceSummaryRecordId, record.Id);
        Assert.Equal(source.EmployeeId, record.EmployeeId);
        Assert.Equal(source.EmployeeCode, record.EmployeeCode);
        Assert.Equal(source.EmployeeName, record.EmployeeName);
        Assert.Equal(source.DepartmentName, record.DepartmentName);
        Assert.Equal(source.PositionName, record.PositionName);
        Assert.Equal(source.PayrollMonth, record.PayrollMonth);
        Assert.Equal(source.PayrollYear, record.PayrollYear);
        Assert.Equal(source.DailyWageAmount, record.DailyWageAmount);
        Assert.Equal(source.LeaveDayCount, record.LeaveDayCount);
        Assert.Equal(source.HolidayDayCount, record.HolidayDayCount);
        Assert.Equal(source.LeaveHolidayAllowanceAmount, record.LeaveHolidayAllowanceAmount);
        Assert.Equal(source.Note, record.Note);
        Assert.Equal(source.IsLocked, record.IsLocked);
        Assert.Equal(source.CreatedAtUtc, record.CreatedAtUtc);
        Assert.Equal(source.CreatedBy, record.CreatedBy);
        Assert.Equal(source.UpdatedAtUtc, record.UpdatedAtUtc);
        Assert.Equal(source.UpdatedBy, record.UpdatedBy);
        Assert.Equal(source.DetailUpdatedAtUtc, record.DetailUpdatedAtUtc);
    }

    private sealed class CapturingReadService(LeaveHolidayAllowanceListItemDto row) : ILeaveHolidayAllowanceReadService
    {
        public LeaveHolidayAllowanceFilter? Filter { get; private set; }

        public Task<IReadOnlyList<LeaveHolidayAllowanceListItemDto>> SearchAsync(
            LeaveHolidayAllowanceFilter filter,
            CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return Task.FromResult<IReadOnlyList<LeaveHolidayAllowanceListItemDto>>([row]);
        }
    }
}
