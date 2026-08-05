using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Queries;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapThamNien;

public sealed class PayrollEmployeeSeniorityAllowanceQueryTests
{
    private const short PayrollMonth = 7;
    private const short PayrollYear = 2026;

    [Fact]
    public async Task SearchPageAsync_applies_seniority_range_on_server_and_returns_full_total()
    {
        await using var dbContext = CreateDbContext();
        AddRecord(dbContext, 0, 0m);
        AddRecord(dbContext, 2, 150_000m);
        AddRecord(dbContext, 4, 200_000m);
        AddRecord(dbContext, 7, 250_000m);
        await dbContext.SaveChangesAsync();

        var service = new DatabasePayrollEmployeeSeniorityAllowanceReadService(dbContext);
        var page = await service.SearchPageAsync(new PayrollEmployeeSeniorityAllowanceFilter(
            PayrollMonth,
            PayrollYear,
            Take: 1,
            SeniorityRangeKey: "3-6"));

        Assert.Single(page.Rows);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(200_000m, page.TotalAllowanceAmount);
        Assert.Equal((short)4, page.Rows[0].CompletedSeniorityYears);
    }

    [Fact]
    public async Task GetRangeSummariesAsync_uses_the_same_period_data_without_loading_rows_to_ui()
    {
        await using var dbContext = CreateDbContext();
        AddRecord(dbContext, 0, 0m);
        AddRecord(dbContext, 2, 150_000m);
        AddRecord(dbContext, 4, 200_000m);
        AddRecord(dbContext, 7, 250_000m);
        await dbContext.SaveChangesAsync();

        var service = new DatabasePayrollEmployeeSeniorityAllowanceRangeSummaryService(dbContext);
        var summaries = await service.GetRangeSummariesAsync(
            new PayrollEmployeeSeniorityAllowanceFilter(PayrollMonth, PayrollYear));

        Assert.Equal(4, summaries.Single(item => item.RangeKey == string.Empty).Count);
        Assert.Equal(1, summaries.Single(item => item.RangeKey == "under-1").Count);
        Assert.Equal(1, summaries.Single(item => item.RangeKey == "1-3").Count);
        Assert.Equal(1, summaries.Single(item => item.RangeKey == "3-6").Count);
        Assert.Equal(1, summaries.Single(item => item.RangeKey == "6-10").Count);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"seniority-allowance-query-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void AddRecord(ApplicationDbContext dbContext, short completedSeniorityYears, decimal amount)
    {
        var summaryId = Guid.NewGuid();
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = PayrollMonth,
            PayrollYear = PayrollYear,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "test"
        });
        dbContext.PayrollEmployeeSeniorityAllowances.Add(new PayrollEmployeeSeniorityAllowanceRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            CompletedSeniorityYears = completedSeniorityYears,
            AllowanceAmount = amount,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "test"
        });
    }
}
