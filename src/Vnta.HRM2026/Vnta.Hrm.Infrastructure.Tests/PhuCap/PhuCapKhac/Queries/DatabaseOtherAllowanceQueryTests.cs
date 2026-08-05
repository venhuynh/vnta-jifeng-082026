using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapKhac.Queries;

public sealed class DatabaseOtherAllowanceQueryTests
{
    [Fact]
    public async Task Search_filters_period_and_lock_state_then_pages_rows_while_summary_covers_all_filtered_rows()
    {
        await using var dbContext = CreateDbContext();
        var julySummary = CreateSummary(7);
        var augustSummary = CreateSummary(8);
        dbContext.AddRange(
            julySummary, augustSummary,
            CreateDetail(julySummary.Id, "B", 100m, isLocked: false),
            CreateDetail(julySummary.Id, "A", 200m, isLocked: false),
            CreateDetail(julySummary.Id, "Khóa", 300m, isLocked: true),
            CreateDetail(augustSummary.Id, "Kỳ khác", 999m, isLocked: false));
        await dbContext.SaveChangesAsync();

        var page = await new DatabaseOtherAllowanceQueryService(dbContext).SearchPageAsync(
            new OtherAllowanceFilter(7, 2026, IsLocked: false, Take: 1, Skip: 1));

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(300m, page.TotalAllowanceAmount);
        var row = Assert.Single(page.Rows);
        Assert.Equal("B", row.AllowanceName);
        Assert.Equal(7, row.PayrollMonth);
        Assert.Equal(2026, row.PayrollYear);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    [InlineData(10_000, 1)]
    public async Task Search_clamps_invalid_paging_without_excluding_the_single_exportable_row(int take, int expectedRows)
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary(7);
        dbContext.AddRange(summary, CreateDetail(summary.Id, "Xuất danh sách", 10m, isLocked: false));
        await dbContext.SaveChangesAsync();

        var page = await new DatabaseOtherAllowanceQueryService(dbContext).SearchPageAsync(
            new OtherAllowanceFilter(7, 2026, Take: take, Skip: -1));

        Assert.Equal(expectedRows, page.Rows.Count);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(10m, page.TotalAllowanceAmount);
    }

    [Fact]
    public async Task Search_supports_export_consumers_by_returning_the_maximum_safe_page_while_retaining_full_summary()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary(7);
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        dbContext.PayrollOtherAllowanceRecords.AddRange(
            Enumerable.Range(1, 5_001)
                .Select(index => CreateDetail(summary.Id, $"Export {index:D4}", 1m, isLocked: false)));
        await dbContext.SaveChangesAsync();

        var page = await new DatabaseOtherAllowanceQueryService(dbContext).SearchPageAsync(
            new OtherAllowanceFilter(7, 2026, Take: int.MaxValue));

        Assert.Equal(5_001, page.TotalCount);
        Assert.Equal(5_001m, page.TotalAllowanceAmount);
        Assert.Equal(5_000, page.Rows.Count);
        Assert.Equal("Export 0001", page.Rows.First().AllowanceName);
    }

    [Theory]
    [InlineData(0, 2026)]
    [InlineData(13, 2026)]
    [InlineData(7, 0)]
    public async Task Search_rejects_an_invalid_payroll_period_before_querying(int month, int year)
    {
        await using var dbContext = CreateDbContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DatabaseOtherAllowanceQueryService(dbContext).SearchPageAsync(new OtherAllowanceFilter(month, year)));
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"other-allowance-query-{Guid.NewGuid():N}")
            .Options);

    private static PayrollAllowanceSummaryRecordRow CreateSummary(short month) => new()
    {
        Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), PayrollMonth = month, PayrollYear = 2026,
        CreatedAtUtc = new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc), CreatedBy = "seed"
    };

    private static PayrollOtherAllowanceRecordRow CreateDetail(Guid summaryId, string name, decimal amount, bool isLocked) => new()
    {
        Id = Guid.NewGuid(), PayrollAllowanceSummaryRecordId = summaryId, AllowanceName = name,
        IsFixedAmount = true, AllowanceAmount = amount, IsLocked = isLocked,
        CreatedAtUtc = new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc), CreatedBy = "seed"
    };
}
