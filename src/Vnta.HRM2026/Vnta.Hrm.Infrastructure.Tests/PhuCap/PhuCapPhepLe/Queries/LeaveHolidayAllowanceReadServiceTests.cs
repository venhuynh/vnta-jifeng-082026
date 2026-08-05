using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Persistence;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Queries;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapPhepLe.Queries;

public sealed class LeaveHolidayAllowanceReadServiceTests
{
    [Fact]
    public async Task Search_returns_only_the_requested_period_and_its_complete_rows()
    {
        await using var dbContext = CreateDbContext();
        var julySecond = CreateSummary(7);
        var julyFirst = CreateSummary(7);
        var august = CreateSummary(8);
        dbContext.AddRange(
            julySecond, julyFirst, august,
            CreateDetail(julySecond.Id, 200m, "B"),
            CreateDetail(julyFirst.Id, 100m, "A"),
            CreateDetail(august.Id, 999m, "Other period"));
        await dbContext.SaveChangesAsync();

        var rows = await new DatabaseLeaveHolidayAllowanceReadService(dbContext)
            .SearchAsync(new LeaveHolidayAllowanceFilter(7, 2026, SearchText: null, Take: 20));

        Assert.Equal(2, rows.Count);
        Assert.All(rows, row =>
        {
            Assert.Equal(7, row.PayrollMonth);
            Assert.Equal(2026, row.PayrollYear);
        });
        Assert.Equal(["A", "B"], rows.Select(row => row.Note).OrderBy(note => note));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-20, 1)]
    [InlineData(int.MaxValue, 2)]
    public async Task Search_clamps_the_result_limit_without_excluding_exportable_rows(int take, int expectedCount)
    {
        await using var dbContext = CreateDbContext();
        var first = CreateSummary(7);
        var second = CreateSummary(7);
        dbContext.AddRange(first, second, CreateDetail(first.Id, 100m, "First"), CreateDetail(second.Id, 200m, "Second"));
        await dbContext.SaveChangesAsync();

        var rows = await new DatabaseLeaveHolidayAllowanceReadService(dbContext)
            .SearchAsync(new LeaveHolidayAllowanceFilter(7, 2026, SearchText: null, Take: take));

        Assert.Equal(expectedCount, rows.Count);
    }

    [Fact]
    public async Task Search_limits_full_period_export_reads_to_the_safe_maximum_in_stable_order()
    {
        await using var dbContext = CreateDbContext();
        var summaries = Enumerable.Range(1, 5_001).Select(_ => CreateSummary(7)).ToArray();
        dbContext.PayrollAllowanceSummaryRecords.AddRange(summaries);
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.AddRange(
            summaries.Select((summary, index) => CreateDetail(summary.Id, index + 1, $"Export {index + 1:D4}")));
        await dbContext.SaveChangesAsync();

        var rows = await new DatabaseLeaveHolidayAllowanceReadService(dbContext)
            .SearchAsync(new LeaveHolidayAllowanceFilter(7, 2026, SearchText: null, Take: int.MaxValue));

        Assert.Equal(5_000, rows.Count);
        Assert.All(rows, row => Assert.StartsWith("Export ", row.Note));
    }

    [Theory]
    [InlineData(0, 2026)]
    [InlineData(13, 2026)]
    [InlineData(7, 1899)]
    public async Task Search_rejects_an_invalid_period_before_returning_rows(int month, int year)
    {
        await using var dbContext = CreateDbContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DatabaseLeaveHolidayAllowanceReadService(dbContext)
                .SearchAsync(new LeaveHolidayAllowanceFilter(month, year, SearchText: null)));
    }

    [Fact]
    public void Search_projection_is_translatable_by_the_postgres_provider()
    {
        using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=127.0.0.1;Port=1;Database=translation-only;Username=test;Password=test")
                .Options);

        var sql = LeaveHolidayAllowanceReadProjection
            .CreateItemsForPeriod(dbContext, 2026, 6, "%NV001%")
            .Take(20)
            .ToQueryString();

        Assert.Contains("PayrollYear", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PayrollMonth", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"leave-holiday-allowance-read-{Guid.NewGuid():N}")
            .Options);

    private static PayrollAllowanceSummaryRecordRow CreateSummary(short month) => new()
    {
        Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), PayrollMonth = month, PayrollYear = 2026,
        CreatedAtUtc = DateTime.UtcNow, CreatedBy = "test"
    };

    private static PayrollAllowanceSummaryLeaveHolidayRecordRow CreateDetail(Guid summaryId, decimal amount, string note) => new()
    {
        PayrollAllowanceSummaryRecordId = summaryId, DailyWageAmount = amount, LeaveDayCount = 1m,
        HolidayDayCount = 0m, LeaveHolidayAllowanceAmount = amount, Note = note,
        CreatedAtUtc = DateTime.UtcNow, CreatedBy = "test"
    };
}
