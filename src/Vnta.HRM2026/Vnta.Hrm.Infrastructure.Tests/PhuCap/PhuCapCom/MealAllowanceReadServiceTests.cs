using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Queries;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Queries;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapCom;

public sealed class MealAllowanceReadServiceTests
{
    [Fact]
    public async Task Search_page_filters_the_selected_period_and_bucket_before_paging()
    {
        await using var dbContext = CreateDbContext();
        var first = AddRecord(dbContext, "NV-01", 7, 2026, MealAllowancePolicy.QualifiedMealRuleCode, false, 1, 18_000m);
        var second = AddRecord(dbContext, "NV-02", 7, 2026, MealAllowancePolicy.QualifiedMealRuleCode, false, 2, 36_000m);
        AddRecord(dbContext, "NV-03", 7, 2026, MealAllowancePolicy.ManualAdjustmentRuleCode, false, 3, 54_000m);
        AddRecord(dbContext, "NV-04", 7, 2026, MealAllowancePolicy.QualifiedMealRuleCode, true, 4, 72_000m);
        AddRecord(dbContext, "NV-05", 8, 2026, MealAllowancePolicy.QualifiedMealRuleCode, false, 5, 90_000m);
        await dbContext.SaveChangesAsync();

        var page = await new DatabaseMealAllowanceReadService(dbContext, new MealAllowanceRequestValidator()).SearchPageAsync(
            new MealAllowanceFilter(7, 2026, null, Take: 1, SummaryBucketKey: "qualified", Skip: 1));

        Assert.Equal(2, page.TotalCount);
        var row = Assert.Single(page.Rows);
        Assert.Equal(second, row.Id);
        Assert.DoesNotContain(page.Rows, item => item.Id == first);
    }

    [Fact]
    public async Task Summary_counts_business_buckets_and_sums_only_the_filtered_period()
    {
        await using var dbContext = CreateDbContext();
        AddRecord(dbContext, "NV-Q", 7, 2026, MealAllowancePolicy.QualifiedMealRuleCode, false, 1, 18_000m);
        AddRecord(dbContext, "NV-M", 7, 2026, MealAllowancePolicy.ManualAdjustmentRuleCode, false, 2, 36_000m);
        AddRecord(dbContext, "NV-L", 7, 2026, MealAllowancePolicy.QualifiedMealRuleCode, true, 0, 0m);
        AddRecord(dbContext, "NV-O", 7, 2026, "legacy-import", false, 0, 0m);
        AddRecord(dbContext, "NV-X", 8, 2026, MealAllowancePolicy.QualifiedMealRuleCode, false, 10, 180_000m);
        await dbContext.SaveChangesAsync();

        var summary = await new DatabaseMealAllowanceReadService(dbContext, new MealAllowanceRequestValidator()).GetSummaryAsync(
            new MealAllowanceFilter(7, 2026, null));

        Assert.Equal(4, summary.TotalCount);
        Assert.Equal(1, summary.QualifiedRuleCount);
        Assert.Equal(1, summary.ManualAdjustmentCount);
        Assert.Equal(1, summary.LockedCount);
        Assert.Equal(1, summary.OtherCount);
        Assert.Equal(2, summary.WithAllowanceCount);
        Assert.Equal(2, summary.WithoutAllowanceCount);
        Assert.Equal(54_000m, summary.TotalAllowanceAmount);
    }

    [Fact]
    public async Task Export_returns_the_entire_selected_period_in_employee_code_order_and_rejects_invalid_period()
    {
        await using var dbContext = CreateDbContext();
        AddRecord(dbContext, "NV-20", 7, 2026, MealAllowancePolicy.QualifiedMealRuleCode, false, 1, 18_000m);
        AddRecord(dbContext, "NV-10", 7, 2026, MealAllowancePolicy.QualifiedMealRuleCode, false, 2, 36_000m);
        AddRecord(dbContext, "NV-30", 8, 2026, MealAllowancePolicy.QualifiedMealRuleCode, false, 3, 54_000m);
        await dbContext.SaveChangesAsync();
        var service = new DatabaseMealAllowanceReadService(dbContext, new MealAllowanceRequestValidator());

        var exported = await service.ExportPeriodAsync(7, 2026);

        Assert.Equal(["NV-10", "NV-20"], exported.Select(item => item.EmployeeCode));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportPeriodAsync(13, 2026));
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"meal-allowance-read-{Guid.NewGuid():N}")
            .Options);

    private static Guid AddRecord(
        ApplicationDbContext dbContext,
        string employeeCode,
        short month,
        short year,
        string ruleCode,
        bool isLocked,
        int overtimeDays,
        decimal amount)
    {
        var now = new DateTime(2026, 7, 30, 9, 0, 0);
        var employeeId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();
        dbContext.Employees.Add(new()
        {
            Id = employeeId,
            EmployeeCode = employeeCode,
            FirstName = "Test",
            LastName = employeeCode,
            IsDeleted = false,
            CreatedAtUtc = now
        });
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = employeeId,
            PayrollMonth = month,
            PayrollYear = year,
            CreatedAtUtc = now,
            CreatedBy = "seed"
        });
        dbContext.PayrollMealAllowanceRecords.Add(new PayrollMealAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            QualifiedMealDays = overtimeDays,
            Overtime1900Days = overtimeDays,
            MealAllowancePerQualifiedDay = 18_000m,
            MealAllowanceAmount = amount,
            RuleCode = ruleCode,
            RuleVersion = "test",
            IsLocked = isLocked,
            CalculatedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        return summaryId;
    }
}
