using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Persistence;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapPhepLe.Commands;

#pragma warning disable CS0618 // Legacy commands remain supported feature behavior.

public sealed class LeaveHolidayAllowanceLegacyCommandWorkflowTests
{
    [Fact]
    public async Task Clear_manual_values_clears_open_rows_and_reports_locked_empty_and_unknown_requests()
    {
        await using var dbContext = CreateDbContext();
        var open = CreateSummary(7);
        var locked = CreateSummary(7, isLocked: true);
        var empty = CreateSummary(7);
        dbContext.AddRange(
            open, locked, empty,
            CreateDetail(open.Id, 100m, 1m, 2m, "manual"),
            CreateDetail(locked.Id, 100m, 1m, 2m, "locked"),
            CreateDetail(empty.Id, 0m, 0m, 0m, null));
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseLeaveHolidayAllowanceClearManualValuesService(dbContext, new LeaveHolidayAllowanceRequestValidator())
            .ClearManualValuesAsync(new([open.Id, locked.Id, empty.Id, Guid.NewGuid()], "payroll-admin"));

        var details = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.ToDictionaryAsync(row => row.PayrollAllowanceSummaryRecordId);
        Assert.Equal(4, result.RequestedCount);
        Assert.Equal(1, result.ClearedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(1, result.SkippedWithoutManualInputCount);
        Assert.Equal(0m, details[open.Id].DailyWageAmount);
        Assert.Equal(0m, details[open.Id].LeaveHolidayAllowanceAmount);
        Assert.Null(details[open.Id].Note);
        Assert.Equal(2m, details[locked.Id].HolidayDayCount);
    }

    [Fact]
    public async Task Sync_previous_month_copies_matching_open_employee_and_preserves_locked_or_unmatched_targets()
    {
        await using var dbContext = CreateDbContext();
        var copiedEmployee = Guid.NewGuid();
        var lockedEmployee = Guid.NewGuid();
        var unmatchedEmployee = Guid.NewGuid();
        var sourceCopied = CreateSummary(6, copiedEmployee);
        var sourceLocked = CreateSummary(6, lockedEmployee);
        var targetCopied = CreateSummary(7, copiedEmployee);
        var targetLocked = CreateSummary(7, lockedEmployee, isLocked: true);
        var targetUnmatched = CreateSummary(7, unmatchedEmployee);
        dbContext.AddRange(
            sourceCopied, sourceLocked, targetCopied, targetLocked, targetUnmatched,
            CreateDetail(sourceCopied.Id, 150m, 1m, 2m, "copied"),
            CreateDetail(sourceLocked.Id, 200m, 3m, 4m, "must remain source"),
            CreateDetail(targetCopied.Id, 0m, 0m, 0m, null),
            CreateDetail(targetLocked.Id, 5m, 6m, 7m, "locked target"),
            CreateDetail(targetUnmatched.Id, 8m, 9m, 10m, "unmatched target"));
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseLeaveHolidayAllowancePreviousMonthSyncService(dbContext, new LeaveHolidayAllowanceRequestValidator())
            .SyncFromPreviousMonthAsync(new(7, 2026, "payroll-admin"));

        var details = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.ToDictionaryAsync(row => row.PayrollAllowanceSummaryRecordId);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(1, result.MissingSourceCount);
        Assert.Equal(150m, details[targetCopied.Id].DailyWageAmount);
        Assert.Equal(2m, details[targetCopied.Id].HolidayDayCount);
        Assert.Equal("copied", details[targetCopied.Id].Note);
        Assert.Equal(7m, details[targetLocked.Id].HolidayDayCount);
        Assert.Equal(10m, details[targetUnmatched.Id].HolidayDayCount);
    }

    [Fact]
    public async Task Batch_lock_changes_only_requested_rows_in_the_period_and_counts_invalid_targets_once()
    {
        await using var dbContext = CreateDbContext();
        var july = CreateSummary(7);
        var august = CreateSummary(8);
        dbContext.AddRange(july, august);
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseLeaveHolidayAllowanceLockService(dbContext, new LeaveHolidayAllowanceRequestValidator())
            .SetLockStateBatchAsync(new(2026, 7, true, [july.Id, july.Id, august.Id, Guid.NewGuid(), Guid.Empty], "payroll-admin"));

        Assert.Equal(1, result.TargetRowCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(3, result.SkippedCount);
        Assert.True((await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == july.Id)).IsLocked);
        Assert.False((await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == august.Id)).IsLocked);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"leave-holiday-allowance-command-{Guid.NewGuid():N}")
            .Options);

    private static PayrollAllowanceSummaryRecordRow CreateSummary(short month, Guid? employeeId = null, bool isLocked = false) => new()
    {
        Id = Guid.NewGuid(), EmployeeId = employeeId ?? Guid.NewGuid(), PayrollMonth = month, PayrollYear = 2026,
        IsLocked = isLocked, CreatedAtUtc = DateTime.UtcNow, CreatedBy = "test"
    };

    private static PayrollAllowanceSummaryLeaveHolidayRecordRow CreateDetail(
        Guid summaryId, decimal dailyWage, decimal leaveDays, decimal holidayDays, string? note) => new()
    {
        PayrollAllowanceSummaryRecordId = summaryId, DailyWageAmount = dailyWage, LeaveDayCount = leaveDays,
        HolidayDayCount = holidayDays, LeaveHolidayAllowanceAmount = dailyWage * (leaveDays + holidayDays), Note = note,
        CreatedAtUtc = DateTime.UtcNow, CreatedBy = "test"
    };
}
