using Vnta.Hrm.Application.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapThamNien;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapThamNien;

public sealed class PayrollEmployeeSeniorityAllowanceDataProviderTests
{
    [Fact]
    public async Task Search_page_maps_server_record_and_preserves_filtered_summary()
    {
        var source = CreateRow(employeeCode: " NV001 ", employeeName: " Nguyen Van A ");
        var readService = new StubReadService(
            new PayrollEmployeeSeniorityAllowancePageDto([source], 17, 2_550_000m));
        var provider = CreateProvider(readService);

        var page = await provider.SearchPageAsync(new PayrollEmployeeSeniorityAllowanceFilter(
            7, 2026, DepartmentName: "Nhân sự", Take: 25, Skip: 50));

        var row = Assert.Single(page.Rows);
        Assert.Equal(17, page.TotalCount);
        Assert.Equal(2_550_000m, page.TotalAllowanceAmount);
        Assert.Equal(source.PayrollAllowanceSummaryRecordId, row.PayrollAllowanceSummaryRecordId);
        Assert.Equal(source.EmploymentStartDate!.Value.ToDateTime(TimeOnly.MinValue), row.EmploymentStartDate);
        Assert.Equal("NV001 - Nguyen Van A", row.EmployeeDisplay);
        Assert.Equal("Đã khóa", row.LockStatusText);
        Assert.Equal("13-plus", row.AppliedRuleKey);
        Assert.Equal(50, readService.LastPageFilter!.Skip);
        Assert.Equal(25, readService.LastPageFilter.Take);
    }

    [Fact]
    public async Task Load_range_summaries_removes_selected_range_and_page_limits()
    {
        var readService = new StubReadService(new PayrollEmployeeSeniorityAllowancePageDto([], 0, 0m));
        var rangeService = new StubRangeSummaryService();
        var provider = CreateProvider(readService, rangeService);

        await provider.LoadRangeSummariesAsync(new PayrollEmployeeSeniorityAllowanceFilter(
            7, 2026, SeniorityRangeKey: "3-6", Take: 50, Skip: 100));

        Assert.NotNull(rangeService.LastFilter);
        Assert.Null(rangeService.LastFilter!.SeniorityRangeKey);
        Assert.Equal(1, rangeService.LastFilter.Take);
        Assert.Equal(0, rangeService.LastFilter.Skip);
    }

    [Fact]
    public async Task Export_loads_every_server_page_without_using_grid_filter()
    {
        var first = CreateRow(employeeCode: "NV001");
        var second = CreateRow(employeeCode: "NV002");
        var readService = new StubReadService(
            new PayrollEmployeeSeniorityAllowancePageDto([first], 2, 350_000m),
            new PayrollEmployeeSeniorityAllowancePageDto([second], 2, 350_000m));
        var provider = CreateProvider(readService);

        var rows = await provider.LoadAllForPeriodExportAsync(2026, 7);

        Assert.Equal([first.PayrollAllowanceSummaryRecordId, second.PayrollAllowanceSummaryRecordId],
            rows.Select(row => row.PayrollAllowanceSummaryRecordId));
        Assert.Equal(2, readService.PageFilters.Count);
        Assert.All(readService.PageFilters, filter => Assert.Equal(5_000, filter.Take));
        Assert.Equal([0, 1], readService.PageFilters.Select(filter => filter.Skip));
        Assert.All(readService.PageFilters, filter =>
        {
            Assert.Equal(7, filter.PayrollMonth);
            Assert.Equal(2026, filter.PayrollYear);
            Assert.Null(filter.DepartmentName);
            Assert.Null(filter.SearchText);
        });
    }

    private static PayrollEmployeeSeniorityAllowanceDataProvider CreateProvider(
        StubReadService readService,
        IPayrollEmployeeSeniorityAllowanceRangeSummaryService? rangeService = null) => new(
        readService,
        rangeService ?? new StubRangeSummaryService(),
        null!, null!, null!, null!, null!, null!);

    private static PayrollEmployeeSeniorityAllowanceListItemDto CreateRow(string employeeCode, string employeeName = "Nguyen Van A") => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), employeeCode, employeeName, "Nhân sự", "Chuyên viên",
        7, 2026, new DateOnly(2013, 7, 1), 13, 0, 22m, .125m, 21.875m, "13-plus", 350_000m,
        "manual", true, new DateTime(2026, 7, 30, 1, 2, 3, DateTimeKind.Utc), "admin",
        new DateTime(2026, 7, 30, 1, 2, 4, DateTimeKind.Utc));

    private sealed class StubReadService(params PayrollEmployeeSeniorityAllowancePageDto[] pages)
        : IPayrollEmployeeSeniorityAllowanceReadService
    {
        private int pageIndex;
        public List<PayrollEmployeeSeniorityAllowanceFilter> PageFilters { get; } = [];
        public PayrollEmployeeSeniorityAllowanceFilter? LastPageFilter => PageFilters.LastOrDefault();

        public Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceListItemDto>> SearchAsync(
            PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PayrollEmployeeSeniorityAllowanceListItemDto>>([]);

        public Task<PayrollEmployeeSeniorityAllowancePageDto> SearchPageAsync(
            PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken = default)
        {
            PageFilters.Add(filter);
            return Task.FromResult(pages[Math.Min(pageIndex++, pages.Length - 1)]);
        }
    }

    private sealed class StubRangeSummaryService : IPayrollEmployeeSeniorityAllowanceRangeSummaryService
    {
        public PayrollEmployeeSeniorityAllowanceFilter? LastFilter { get; private set; }

        public Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>> GetRangeSummariesAsync(
            PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            return Task.FromResult<IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>>([]);
        }
    }
}
