using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapCom;

public sealed class DatabaseMealAllowanceManualAdjustmentServiceTests
{
    [Fact]
    public async Task UpdateManualValuesAsync_uses_qualified_meal_days_and_synchronizes_summary_projection()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var now = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Unspecified);
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 7,
            PayrollYear = 2026,
            MealAllowanceAmount = 18_000m,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        dbContext.PayrollMealAllowanceRecords.Add(new PayrollMealAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            QualifiedMealDays = 1,
            Overtime1900Days = 1,
            MealAllowancePerQualifiedDay = 18_000m,
            MealAllowanceAmount = 18_000m,
            RuleCode = MealAllowancePolicy.QualifiedMealRuleCode,
            RuleVersion = MealAllowancePolicy.QualifiedMealRuleVersion,
            CalculatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        await dbContext.SaveChangesAsync();

        await new DatabaseMealAllowanceManualAdjustmentService(dbContext, new MealAllowanceRequestValidator()).UpdateManualValuesAsync(
            new UpdateMealAllowanceManualValuesRequest(
                summaryId,
                QualifiedMealDays: 3,
                Note: "Điều chỉnh tăng ca 19:00",
                OriginalUpdatedAtUtc: null,
                Actor: "payroll-admin"));

        var persistedDetail = await dbContext.PayrollMealAllowanceRecords.SingleAsync();
        var persistedSummary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(3, persistedDetail.QualifiedMealDays);
        Assert.Equal(1, persistedDetail.Overtime1900Days);
        Assert.Equal(54_000m, persistedDetail.MealAllowanceAmount);
        Assert.Equal(54_000m, persistedSummary.MealAllowanceAmount);
        Assert.Equal(MealAllowancePolicy.ManualAdjustmentRuleCode, persistedDetail.RuleCode);
        Assert.Equal("payroll-admin", persistedSummary.UpdatedBy);
    }

    [Fact]
    public async Task UpdateManualValuesAsync_rejects_a_stale_concurrency_timestamp_before_persisting()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Unspecified);
        var updatedAt = createdAt.AddMinutes(1);
        dbContext.PayrollMealAllowanceRecords.Add(new PayrollMealAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            QualifiedMealDays = 1,
            Overtime1900Days = 1,
            MealAllowancePerQualifiedDay = 18_000m,
            MealAllowanceAmount = 18_000m,
            RuleCode = MealAllowancePolicy.QualifiedMealRuleCode,
            CalculatedAtUtc = createdAt,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = updatedAt
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<MealAllowanceConflictException>(() =>
            new DatabaseMealAllowanceManualAdjustmentService(dbContext, new MealAllowanceRequestValidator()).UpdateManualValuesAsync(
                new UpdateMealAllowanceManualValuesRequest(
                    summaryId,
                    QualifiedMealDays: 2,
                    Note: null,
                    OriginalUpdatedAtUtc: createdAt,
                    Actor: "payroll-admin")));

        Assert.Equal(1, (await dbContext.PayrollMealAllowanceRecords.SingleAsync()).QualifiedMealDays);
    }

    [Fact]
    public async Task UpdateManualValuesAsync_rejects_a_locked_snapshot_without_changing_the_summary_projection()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var now = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Unspecified);
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 7,
            PayrollYear = 2026,
            MealAllowanceAmount = 18_000m,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        dbContext.PayrollMealAllowanceRecords.Add(new PayrollMealAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            QualifiedMealDays = 1,
            Overtime1900Days = 1,
            MealAllowancePerQualifiedDay = 18_000m,
            MealAllowanceAmount = 18_000m,
            RuleCode = MealAllowancePolicy.QualifiedMealRuleCode,
            IsLocked = true,
            CalculatedAtUtc = now,
            CreatedAtUtc = now
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DatabaseMealAllowanceManualAdjustmentService(dbContext, new MealAllowanceRequestValidator()).UpdateManualValuesAsync(
                new UpdateMealAllowanceManualValuesRequest(summaryId, 3, "locked", null, "payroll-admin")));

        Assert.Equal(18_000m, (await dbContext.PayrollAllowanceSummaryRecords.SingleAsync()).MealAllowanceAmount);
        Assert.Equal(1, (await dbContext.PayrollMealAllowanceRecords.SingleAsync()).QualifiedMealDays);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"meal-allowance-manual-adjustment-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

}
