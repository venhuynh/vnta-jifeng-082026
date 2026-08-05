using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Queries;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTrachNhiemKhac;

public sealed class OtherResponsibilityAllowanceReadAndLockServiceTests
{
    [Fact]
    public async Task SearchAsync_filters_locked_snapshot_and_applies_the_result_limit_before_returning_the_summary_projection()
    {
        await using var dbContext = CreateDbContext();
        var visibleSummaryId = Guid.NewGuid();
        var hiddenSummaryId = Guid.NewGuid();
        var otherPeriodSummaryId = Guid.NewGuid();
        var updatedAt = new DateTime(2026, 6, 30, 10, 15, 0, DateTimeKind.Utc);
        var createdAt = updatedAt.AddDays(-1);

        dbContext.PayrollAllowanceSummaryRecords.AddRange(
            Summary(visibleSummaryId, true, 6, 2026, updatedAt, "summary-editor"),
            Summary(hiddenSummaryId, false, 6, 2026, updatedAt, "summary-editor"),
            Summary(otherPeriodSummaryId, true, 7, 2026, updatedAt, "summary-editor"));
        dbContext.PayrollAllowanceOtherResponsibilityRecords.AddRange(
            Detail(visibleSummaryId, createdAt, updatedAt, 19.25m, 1_200_000m, 888_888.88m, "approved"),
            Detail(hiddenSummaryId, createdAt, updatedAt, 20m, 2_000_000m, 2_000_000m, "open"),
            Detail(otherPeriodSummaryId, createdAt, updatedAt, 21m, 3_000_000m, 3_000_000m, "other period"));
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseOtherResponsibilityAllowanceReadService(dbContext).SearchAsync(
            new OtherResponsibilityAllowanceFilter(6, 2026, "   ", IsLocked: true, Take: 0));

        var row = Assert.Single(result);
        Assert.Equal(visibleSummaryId, row.PayrollAllowanceSummaryRecordId);
        Assert.True(row.IsLocked);
        Assert.Equal(19.25m, row.AllowanceWorkdayCount);
        Assert.Equal(1_200_000m, row.StandardResponsibilityAllowanceAmount);
        Assert.Equal(888_888.88m, row.ActualResponsibilityAllowanceAmount);
        Assert.Equal("approved", row.Note);
        Assert.Equal(updatedAt, row.UpdatedAtUtc);
        Assert.Equal("summary-editor", row.UpdatedBy);
    }

    [Fact]
    public async Task SetLockStateBatchAsync_locks_the_whole_period_without_client_concurrency_tokens_and_leaves_other_periods_untouched()
    {
        await using var dbContext = CreateDbContext();
        var firstSummaryId = Guid.NewGuid();
        var secondSummaryId = Guid.NewGuid();
        var otherPeriodSummaryId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        dbContext.PayrollAllowanceSummaryRecords.AddRange(
            Summary(firstSummaryId, false, 6, 2026, now, null),
            Summary(secondSummaryId, false, 6, 2026, now, null),
            Summary(otherPeriodSummaryId, false, 7, 2026, now, null));
        dbContext.PayrollAllowanceOtherResponsibilityRecords.AddRange(
            Detail(firstSummaryId, now, null, 0m, 0m, 0m, null),
            Detail(secondSummaryId, now, null, 0m, 0m, 0m, null),
            Detail(otherPeriodSummaryId, now, null, 0m, 0m, 0m, null));
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseOtherResponsibilityAllowanceLockService(dbContext).SetLockStateBatchAsync(
            new SetOtherResponsibilityAllowanceBatchLockStateRequest(2026, 6, true, null, null),
            "period-controller");

        Assert.Equal(2, result.TargetRowCount);
        Assert.Equal(2, result.UpdatedCount);
        var currentPeriod = await dbContext.PayrollAllowanceSummaryRecords
            .Where(row => row.PayrollYear == 2026 && row.PayrollMonth == 6)
            .ToListAsync();
        Assert.All(currentPeriod, row =>
        {
            Assert.True(row.IsLocked);
            Assert.Equal("period-controller", row.UpdatedBy);
        });
        Assert.False((await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == otherPeriodSummaryId)).IsLocked);
        Assert.All(
            await dbContext.PayrollAllowanceOtherResponsibilityRecords
                .Where(row => row.PayrollAllowanceSummaryRecordId == firstSummaryId || row.PayrollAllowanceSummaryRecordId == secondSummaryId)
                .ToListAsync(),
            row => Assert.True(row.IsLocked));
    }

    [Fact]
    public async Task SetLockStateBatchAsync_rejects_a_mixed_period_selection_without_partially_updating_the_valid_row()
    {
        await using var dbContext = CreateDbContext();
        var currentPeriodSummaryId = Guid.NewGuid();
        var otherPeriodSummaryId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        dbContext.PayrollAllowanceSummaryRecords.AddRange(
            Summary(currentPeriodSummaryId, false, 6, 2026, now, null),
            Summary(otherPeriodSummaryId, false, 7, 2026, now, null));
        dbContext.PayrollAllowanceOtherResponsibilityRecords.AddRange(
            Detail(currentPeriodSummaryId, now, now, 0m, 0m, 0m, null),
            Detail(otherPeriodSummaryId, now, now, 0m, 0m, 0m, null));
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DatabaseOtherResponsibilityAllowanceLockService(dbContext).SetLockStateBatchAsync(
                new SetOtherResponsibilityAllowanceBatchLockStateRequest(
                    2026,
                    6,
                    true,
                    [currentPeriodSummaryId, otherPeriodSummaryId],
                    [
                        new OtherResponsibilityAllowanceLockStateConcurrencyToken(currentPeriodSummaryId, now),
                        new OtherResponsibilityAllowanceLockStateConcurrencyToken(otherPeriodSummaryId, now)
                    ]),
                "period-controller"));

        Assert.False((await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == currentPeriodSummaryId)).IsLocked);
        Assert.False((await dbContext.PayrollAllowanceOtherResponsibilityRecords.SingleAsync(row => row.PayrollAllowanceSummaryRecordId == currentPeriodSummaryId)).IsLocked);
    }

    private static PayrollAllowanceSummaryRecordRow Summary(
        Guid id,
        bool isLocked,
        short month,
        short year,
        DateTime updatedAt,
        string? updatedBy) => new()
        {
            Id = id,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = month,
            PayrollYear = year,
            IsLocked = isLocked,
            CreatedAtUtc = updatedAt.AddDays(-1),
            CreatedBy = "test",
            UpdatedAtUtc = updatedBy is null ? null : updatedAt,
            UpdatedBy = updatedBy
        };

    private static PayrollAllowanceOtherResponsibilityRecordRow Detail(
        Guid summaryId,
        DateTime createdAt,
        DateTime? updatedAt,
        decimal workdays,
        decimal standardAmount,
        decimal actualAmount,
        string? note) => new()
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            AllowanceWorkdayCount = workdays,
            StandardResponsibilityAllowanceAmount = standardAmount,
            ActualResponsibilityAllowanceAmount = actualAmount,
            Note = note,
            CreatedAtUtc = createdAt,
            CreatedBy = "detail-creator",
            UpdatedAtUtc = updatedAt,
            UpdatedBy = "detail-editor"
        };

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"other-responsibility-read-lock-{Guid.NewGuid():N}")
            .Options);
}
