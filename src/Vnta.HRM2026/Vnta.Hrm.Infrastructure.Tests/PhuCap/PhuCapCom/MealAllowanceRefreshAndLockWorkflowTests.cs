using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapCom;

public sealed class MealAllowanceRefreshAndLockWorkflowTests
{
    [Fact]
    public async Task Refresh_recalculates_open_snapshots_creates_missing_rows_and_preserves_manual_and_locked_rows()
    {
        await using var dbContext = CreateDbContext();
        var open = AddSummary(dbContext, isLocked: false);
        var manual = AddSummary(dbContext, isLocked: false);
        var locked = AddSummary(dbContext, isLocked: false);
        var missing = AddSummary(dbContext, isLocked: false);
        var openSummaryId = open.SummaryId;
        var manualSummaryId = manual.SummaryId;
        var lockedSummaryId = locked.SummaryId;
        var missingSummaryId = missing.SummaryId;
        AddDetail(dbContext, openSummaryId, 1, MealAllowancePolicy.QualifiedMealRuleCode, false);
        AddDetail(dbContext, manualSummaryId, 4, MealAllowancePolicy.ManualAdjustmentRuleCode, false);
        AddDetail(dbContext, lockedSummaryId, 1, MealAllowancePolicy.QualifiedMealRuleCode, true);
        await dbContext.SaveChangesAsync();

        var service = new DatabaseMealAllowanceRefreshService(
            dbContext,
            new StubCalculator(new Dictionary<Guid, MealAllowanceCalculationResult>
            {
                [open.EmployeeId] = Result(2),
                [locked.EmployeeId] = Result(3)
            }),
            new MealAllowanceRequestValidator());

        var result = await service.RefreshAsync(new RefreshMealAllowanceRequest(7, 2026, Actor: "payroll-admin"));

        Assert.Equal(4, result.SummaryTargetCount);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(1, result.SkippedManualAdjustmentCount);
        var details = await dbContext.PayrollMealAllowanceRecords.ToDictionaryAsync(x => x.PayrollAllowanceSummaryRecordId);
        Assert.Equal(36_000m, details[openSummaryId].MealAllowanceAmount);
        Assert.Equal(4, details[manualSummaryId].QualifiedMealDays);
        Assert.Equal(18_000m, details[lockedSummaryId].MealAllowanceAmount);
        Assert.Equal(0m, details[missingSummaryId].MealAllowanceAmount);
        Assert.Equal(36_000m, await dbContext.PayrollAllowanceSummaryRecords.Where(x => x.Id == openSummaryId).Select(x => x.MealAllowanceAmount).SingleAsync());
    }

    [Fact]
    public async Task Lock_and_unlock_apply_to_selected_rows_or_the_whole_period_without_crossing_period_boundaries()
    {
        await using var dbContext = CreateDbContext();
        var selectedId = AddSummary(dbContext, month: 7).SummaryId;
        var peerId = AddSummary(dbContext, month: 7).SummaryId;
        var outsidePeriodId = AddSummary(dbContext, month: 8).SummaryId;
        AddDetail(dbContext, selectedId, 1, MealAllowancePolicy.QualifiedMealRuleCode, false);
        AddDetail(dbContext, peerId, 1, MealAllowancePolicy.QualifiedMealRuleCode, false);
        AddDetail(dbContext, outsidePeriodId, 1, MealAllowancePolicy.QualifiedMealRuleCode, false);
        await dbContext.SaveChangesAsync();
        var service = new DatabaseMealAllowanceLockService(dbContext, new MealAllowanceRequestValidator());

        var selected = await service.SetLockStateBatchAsync(new SetMealAllowanceLockStateBatchRequest(
            2026, 7, true, MealAllowanceLockActionScope.SelectedRows, [selectedId], "payroll-admin"));
        var wholePeriod = await service.SetLockStateBatchAsync(new SetMealAllowanceLockStateBatchRequest(
            2026, 7, false, MealAllowanceLockActionScope.WholePeriod, null, "payroll-admin"));

        Assert.Equal((1, 1), (selected.TargetRowCount, selected.UpdatedCount));
        Assert.Equal((2, 1), (wholePeriod.TargetRowCount, wholePeriod.UpdatedCount));
        var details = await dbContext.PayrollMealAllowanceRecords.ToDictionaryAsync(x => x.PayrollAllowanceSummaryRecordId);
        Assert.False(details[selectedId].IsLocked);
        Assert.False(details[peerId].IsLocked);
        Assert.False(details[outsidePeriodId].IsLocked);
    }

    [Fact]
    public async Task Failed_manual_adjustment_does_not_commit_a_partial_detail_change()
    {
        var databaseName = $"meal-allowance-failed-manual-{Guid.NewGuid():N}";
        var summaryId = Guid.NewGuid();
        await using (var seedContext = CreateDbContext(databaseName))
        {
            AddDetail(seedContext, summaryId, 1, MealAllowancePolicy.QualifiedMealRuleCode, false);
            await seedContext.SaveChangesAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => new DatabaseMealAllowanceManualAdjustmentService(seedContext, new MealAllowanceRequestValidator())
                .UpdateManualValuesAsync(new UpdateMealAllowanceManualValuesRequest(summaryId, 3, "must not persist", null, "payroll-admin")));
        }

        await using var verificationContext = CreateDbContext(databaseName);
        var persisted = await verificationContext.PayrollMealAllowanceRecords.SingleAsync();
        Assert.Equal(1, persisted.QualifiedMealDays);
        Assert.Equal(18_000m, persisted.MealAllowanceAmount);
        Assert.Equal(MealAllowancePolicy.QualifiedMealRuleCode, persisted.RuleCode);
    }

    private static MealAllowanceCalculationResult Result(int days) => new(days, days, 18_000m, days * 18_000m);

    private static ApplicationDbContext CreateDbContext(string? databaseName = null) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"meal-allowance-workflow-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static MealSummarySeed AddSummary(ApplicationDbContext dbContext, short month = 7, bool isLocked = false)
    {
        var id = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = id,
            EmployeeId = employeeId,
            PayrollMonth = month,
            PayrollYear = 2026,
            IsLocked = isLocked,
            CreatedAtUtc = new DateTime(2026, 7, 30, 9, 0, 0),
            CreatedBy = "seed"
        });
        return new MealSummarySeed(id, employeeId);
    }

    private static void AddDetail(ApplicationDbContext dbContext, Guid summaryId, int days, string ruleCode, bool isLocked) =>
        dbContext.PayrollMealAllowanceRecords.Add(new PayrollMealAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            QualifiedMealDays = days,
            Overtime1900Days = days,
            MealAllowancePerQualifiedDay = 18_000m,
            MealAllowanceAmount = days * 18_000m,
            RuleCode = ruleCode,
            RuleVersion = "seed",
            IsLocked = isLocked,
            CalculatedAtUtc = new DateTime(2026, 7, 30, 9, 0, 0),
            CreatedAtUtc = new DateTime(2026, 7, 30, 9, 0, 0)
        });

    private sealed class StubCalculator(IReadOnlyDictionary<Guid, MealAllowanceCalculationResult> results)
        : IMealAllowanceRefreshCalculator
    {
        public Task<IReadOnlyDictionary<Guid, MealAllowanceCalculationResult>> CalculateAsync(
            MealAllowanceRefreshPeriod period,
            CancellationToken cancellationToken = default) => Task.FromResult(results);
    }

    private sealed record MealSummarySeed(Guid SummaryId, Guid EmployeeId);
}
