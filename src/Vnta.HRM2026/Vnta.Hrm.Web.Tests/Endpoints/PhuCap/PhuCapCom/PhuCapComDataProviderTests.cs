using Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Queries;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapCom;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapCom;

public sealed class PhuCapComDataProviderTests
{
    [Fact]
    public async Task Search_page_maps_server_calculated_values_to_the_ui_record_without_recalculating_them()
    {
        var dto = CreateRow(amount: 54_000.015m, overtimeDays: 1);
        var provider = new PhuCapComDataProvider(
            new StubReadService(new MealAllowancePageDto([dto], 1)),
            new StubExportService([dto]),
            null!,
            null!,
            null!);

        var result = await provider.SearchPageAsync(new MealAllowanceFilter(7, 2026, null));

        var record = Assert.Single(result.Rows);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(dto.Id, record.Id);
        Assert.Equal(dto.QualifiedMealDays, record.QualifiedMealDays);
        Assert.Equal(dto.Overtime1900Days, record.Overtime1900Days);
        Assert.Equal(54_000.02m, record.MealAllowanceAmount);
        Assert.Equal("NV-01 - Test Employee", record.EmployeeDisplay);
    }

    [Fact]
    public async Task Export_forwards_the_selected_payroll_period_to_the_feature_export_capability()
    {
        var exportService = new StubExportService([CreateRow(18_000m, 1)]);
        var provider = new PhuCapComDataProvider(
            new StubReadService(new MealAllowancePageDto([], 0)), exportService, null!, null!, null!);

        var records = await provider.ExportPeriodAsync(7, 2026);

        Assert.Equal((7, 2026), exportService.RequestedPeriod);
        Assert.Single(records);
    }

    private static MealAllowanceListItemDto CreateRow(decimal amount, int overtimeDays) => new(
        Guid.NewGuid(), Guid.NewGuid(), "NV-01", "Test Employee", "Production", "Worker", 7, 2026,
        3, overtimeDays, 18_000m, amount, "manual-adjustment", "test", "manual", false,
        DateTime.UnixEpoch, DateTime.UnixEpoch, DateTime.UnixEpoch);

    private sealed class StubReadService(MealAllowancePageDto page) : IMealAllowanceReadService
    {
        public Task<IReadOnlyList<MealAllowanceListItemDto>> SearchAsync(MealAllowanceFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(page.Rows);
        public Task<MealAllowancePageDto> SearchPageAsync(MealAllowanceFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(page);
        public Task<MealAllowanceSummaryDto> GetSummaryAsync(MealAllowanceFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(new MealAllowanceSummaryDto(0, 0, 0, 0, 0, 0, 0, 0m));
    }

    private sealed class StubExportService(IReadOnlyList<MealAllowanceListItemDto> rows) : IMealAllowanceExportService
    {
        public (int Month, int Year)? RequestedPeriod { get; private set; }
        public Task<IReadOnlyList<MealAllowanceListItemDto>> ExportPeriodAsync(int payrollMonth, int payrollYear, CancellationToken cancellationToken = default)
        {
            RequestedPeriod = (payrollMonth, payrollYear);
            return Task.FromResult(rows);
        }
    }
}
