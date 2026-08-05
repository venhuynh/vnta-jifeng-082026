using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Queries;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapTongHop;

public sealed class PayrollAllowanceSummaryDataProviderTests
{
    [Fact]
    public async Task Search_maps_the_api_row_to_the_grid_model_and_preserves_page_total()
    {
        var source = CreateRow();
        var readService = new StubReadService(new PayrollAllowanceSummaryPageDto([source], 17));
        var provider = CreateProvider(readService, new StubExportService([]));

        var page = await provider.SearchAsync(new PayrollAllowanceSummaryFilter(7, 2026, "NV001", true, 50, 25));

        var row = Assert.Single(page.Rows);
        Assert.Equal(17, page.TotalCount);
        Assert.Equal(source.Id, row.Id);
        Assert.Equal("NV001 - Nguyễn Văn A", row.EmployeeDisplay);
        Assert.Equal("Đã khóa", row.LockStatusText);
        Assert.Equal(36m, row.TotalAllowanceAmount);
        Assert.Equal(50, readService.LastFilter!.Skip);
        Assert.Equal(25, readService.LastFilter.Take);
    }

    [Fact]
    public async Task Export_uses_only_the_selected_period_and_maps_the_allowlisted_export_record()
    {
        var exportService = new StubExportService([new PayrollAllowanceSummaryExportRowDto(
            "NV001", "Nguyễn Văn A", "Nhân sự", "Chuyên viên", 7, 2026,
            1m, 2m, 3m, 4m, 5m, 6m, 7m, 8m, 36m, true, "đã kiểm tra")]);
        var provider = CreateProvider(new StubReadService(new PayrollAllowanceSummaryPageDto([], 0)), exportService);

        var rows = await provider.LoadAllForPeriodExportAsync(7, 2026, PayrollAllowanceSummaryExportFormat.Pdf);

        var row = Assert.Single(rows);
        Assert.Equal(new PayrollAllowanceSummaryExportRequest(2026, 7, PayrollAllowanceSummaryExportFormat.Pdf), exportService.Request);
        Assert.Equal(36m, row.TotalAllowanceAmount);
        Assert.True(row.IsLocked);
        Assert.Equal("07/2026", row.PayrollPeriodDisplay);
    }

    private static PayrollAllowanceSummaryDataProvider CreateProvider(
        IPayrollAllowanceSummaryReadService readService,
        IPayrollAllowanceSummaryExportService exportService) =>
        new(readService, exportService, null!, null!, null!, null!);

    private static PayrollAllowanceSummaryListItemDto CreateRow() => new(
        Guid.NewGuid(), Guid.NewGuid(), " NV001 ", " Nguyễn Văn A ", " Nhân sự ", " Chuyên viên ",
        7, 2026, 1m, 2m, 3m, 4m, 5m, 6m, 7m, 8m, true, "đã kiểm tra",
        new DateTime(2026, 7, 1), "tester", new DateTime(2026, 7, 2), "admin");

    private sealed class StubReadService(PayrollAllowanceSummaryPageDto page) : IPayrollAllowanceSummaryReadService
    {
        public PayrollAllowanceSummaryFilter? LastFilter { get; private set; }
        public Task<PayrollAllowanceSummaryOverviewDto> GetSummaryAsync(PayrollAllowanceSummaryFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PayrollAllowanceSummaryOverviewDto(0, 0, 0, 0m));

        public Task<PayrollAllowanceSummaryPageDto> SearchAsync(PayrollAllowanceSummaryFilter filter, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            return Task.FromResult(page);
        }
    }

    private sealed class StubExportService(IReadOnlyList<PayrollAllowanceSummaryExportRowDto> rows) : IPayrollAllowanceSummaryExportService
    {
        public PayrollAllowanceSummaryExportRequest? Request { get; private set; }

        public Task<IReadOnlyList<PayrollAllowanceSummaryExportRowDto>> ExportAsync(PayrollAllowanceSummaryExportRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(rows);
        }
    }
}
