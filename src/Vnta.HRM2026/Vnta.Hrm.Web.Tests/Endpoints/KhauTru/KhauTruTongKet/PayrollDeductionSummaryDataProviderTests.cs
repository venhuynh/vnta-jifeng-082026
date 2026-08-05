using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.KhauTru.KhauTruTongKet;

public sealed class PayrollDeductionSummaryDataProviderTests
{
    [Fact]
    public async Task Search_maps_rows_totals_and_lock_status_counts_for_the_summary_grid()
    {
        var source = CreateRow();
        var provider = CreateProvider(new PayrollDeductionSummaryPageDto(
            [source], 8, new PayrollDeductionSummaryAggregateDto(1m, 2m, 3m, 4m, 5m, 15m),
            new PayrollDeductionSummaryLockStatusCountsDto(8, 5, 3)));

        var result = await provider.SearchAsync(new PayrollDeductionSummaryFilter(7, 2026, null));

        var row = Assert.Single(result.Rows);
        Assert.Equal(source.Id, row.Id);
        Assert.Equal(source.EmployeeName, row.EmployeeName);
        Assert.Equal(source.OtherDeductionAmount, row.OtherDeductionAmount);
        Assert.Equal(source.UpdatedAtUtc, row.UpdatedAtUtc);
        Assert.Equal(15m, result.Totals.TotalDeductionAmount);
        Assert.Equal(8, result.TotalCount);
        Assert.Equal(5, result.LockStatusCounts.Open);
        Assert.Equal(3, result.LockStatusCounts.Locked);
    }

    [Fact]
    public async Task Excel_export_sanitizes_formula_like_text_without_changing_amounts()
    {
        var export = new PayrollDeductionSummaryExportItemDto(
            "=NV001", "+Payroll", "-Lead", "@07/2026", 1m, 2m, 3m, 4m, 5m, 15m, "=Locked");
        var provider = CreateProvider(exportRows: [export]);

        var row = Assert.Single(await provider.LoadAllForPeriodExportAsync(
            2026, 7, PayrollDeductionSummaryExportFormat.Excel));

        Assert.Equal("'=NV001", row.EmployeeDisplay);
        Assert.Equal("'+Payroll", row.DepartmentDisplay);
        Assert.Equal("'-Lead", row.PositionDisplay);
        Assert.Equal("'@07/2026", row.PayrollPeriodDisplay);
        Assert.Equal("'=Locked", row.LockStatusText);
        Assert.Equal(15m, row.TotalDeductionAmount);
    }

    private static PayrollDeductionSummaryDataProvider CreateProvider(
        PayrollDeductionSummaryPageDto? page = null,
        IReadOnlyList<PayrollDeductionSummaryExportItemDto>? exportRows = null) => new(
            new StubReadService(page ?? new PayrollDeductionSummaryPageDto([], 0, PayrollDeductionSummaryAggregateDto.Empty, PayrollDeductionSummaryLockStatusCountsDto.Empty)),
            new StubExportService(exportRows ?? []), new UnsupportedSyncService(), new UnsupportedRefreshService(),
            new UnsupportedManualAdjustmentService(), new UnsupportedLockService());

    private static PayrollDeductionSummaryListItemDto CreateRow() => new(
        Guid.NewGuid(), Guid.NewGuid(), "NV001", "Nguyen Van A", "Payroll", "Specialist", 7, 2026,
        1m, 2m, 3m, 4m, 5m, false, "note", new DateTime(2026, 7, 1), "creator", new DateTime(2026, 7, 2), "editor");

    private sealed class StubReadService(PayrollDeductionSummaryPageDto page) : IPayrollDeductionSummaryReadService
    {
        public Task<PayrollDeductionSummaryPageDto> SearchAsync(PayrollDeductionSummaryFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(page);
    }

    private sealed class StubExportService(IReadOnlyList<PayrollDeductionSummaryExportItemDto> rows) : IPayrollDeductionSummaryExportService
    {
        public Task<IReadOnlyList<PayrollDeductionSummaryExportItemDto>> ExportPeriodAsync(int payrollMonth, int payrollYear, PayrollDeductionSummaryExportFormat format, CancellationToken cancellationToken = default) => Task.FromResult(rows);
    }

    private sealed class UnsupportedSyncService : IPayrollDeductionSummarySyncService
    {
        public Task<SyncPayrollDeductionSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(SyncPayrollDeductionSummaryFromPreviousMonthRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnsupportedRefreshService : IPayrollDeductionSummaryRefreshService
    {
        public Task<RefreshPayrollDeductionSummaryResult> RefreshAsync(RefreshPayrollDeductionSummaryRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RecalculatePayrollDeductionSummaryPeriodResult> RecalculatePeriodAsync(RecalculatePayrollDeductionSummaryPeriodRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnsupportedManualAdjustmentService : IPayrollDeductionSummaryManualAdjustmentService
    {
        public Task<PayrollDeductionSummaryListItemDto> UpdateManualOtherDeductionAsync(UpdatePayrollDeductionSummaryManualOtherDeductionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnsupportedLockService : IPayrollDeductionSummaryLockService
    {
        public Task<PayrollDeductionSummaryListItemDto> SetLockStateAsync(SetPayrollDeductionSummaryLockStateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SetPayrollDeductionSummaryBatchLockStateResult> SetLockStateBatchAsync(SetPayrollDeductionSummaryBatchLockStateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
