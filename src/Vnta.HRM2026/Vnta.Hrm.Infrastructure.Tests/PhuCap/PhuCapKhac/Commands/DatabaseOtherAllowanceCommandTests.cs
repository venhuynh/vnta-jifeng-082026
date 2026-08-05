using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Exceptions;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapKhac.Commands;

public sealed class DatabaseOtherAllowanceCommandTests
{
    [Fact]
    public async Task Create_fixed_manual_adjustment_rounds_amount_and_refreshes_summary_total()
    {
        await using var dbContext = CreateDbContext();
        var summary = await SeedSummaryAsync(dbContext, otherAllowanceAmount: 100m);

        var result = await new DatabaseOtherAllowanceCreateService(dbContext).CreateAsync(
            new CreateOtherAllowanceRequest(
                summary.Id, "  Hỗ trợ ăn ca  ", true, 125_000.5m, "  Theo quyết định  ", "  payroll-admin  "));

        Assert.Equal("Hỗ trợ ăn ca", result.AllowanceName);
        Assert.Equal(125_001m, result.AllowanceAmount);
        Assert.Equal("Theo quyết định", result.Note);
        Assert.Equal("payroll-admin", result.CreatedBy);
        Assert.Equal(125_001m, (await dbContext.PayrollAllowanceSummaryRecords.SingleAsync()).OtherAllowanceAmount);
    }

    [Fact]
    public async Task Update_non_fixed_manual_adjustment_clears_entered_amount_and_recalculates_all_lines()
    {
        await using var dbContext = CreateDbContext();
        var summary = await SeedSummaryAsync(dbContext);
        var version = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc);
        var adjusted = CreateDetail(summary.Id, "Điều chỉnh", 200m, version);
        dbContext.PayrollOtherAllowanceRecords.AddRange(adjusted, CreateDetail(summary.Id, "Giữ nguyên", 50m, version));
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseOtherAllowanceUpdateService(dbContext).UpdateAsync(
            new UpdateOtherAllowanceRequest(adjusted.Id, "  Điều chỉnh mới  ", false, 999m, " ", version, "adjuster"));

        Assert.False(result.IsFixedAmount);
        Assert.Equal(0m, result.AllowanceAmount);
        Assert.Null(result.Note);
        Assert.Equal(50m, (await dbContext.PayrollAllowanceSummaryRecords.SingleAsync()).OtherAllowanceAmount);
        var persisted = await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == adjusted.Id);
        Assert.Equal("Điều chỉnh mới", persisted.AllowanceName);
        Assert.Equal("adjuster", persisted.UpdatedBy);
    }

    [Fact]
    public async Task Update_rejects_locked_or_stale_manual_adjustment_without_changing_detail_or_summary()
    {
        await using var dbContext = CreateDbContext();
        var summary = await SeedSummaryAsync(dbContext, otherAllowanceAmount: 200m);
        var createdAt = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc);
        var updatedAt = createdAt.AddMinutes(1);
        var locked = CreateDetail(summary.Id, "Đã khóa", 100m, createdAt, isLocked: true);
        var stale = CreateDetail(summary.Id, "Đã thay đổi", 100m, createdAt, updatedAt: updatedAt);
        dbContext.PayrollOtherAllowanceRecords.AddRange(locked, stale);
        await dbContext.SaveChangesAsync();
        var service = new DatabaseOtherAllowanceUpdateService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(
            new UpdateOtherAllowanceRequest(locked.Id, "Không được sửa", true, 999m, null, createdAt, "actor")));
        await Assert.ThrowsAsync<OtherAllowanceConflictException>(() => service.UpdateAsync(
            new UpdateOtherAllowanceRequest(stale.Id, "Không được sửa", true, 999m, null, createdAt, "actor")));

        Assert.Equal(100m, (await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == locked.Id)).AllowanceAmount);
        Assert.Equal(100m, (await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == stale.Id)).AllowanceAmount);
        Assert.Equal(200m, (await dbContext.PayrollAllowanceSummaryRecords.SingleAsync()).OtherAllowanceAmount);
    }

    [Fact]
    public async Task Lock_then_unlock_uses_fresh_version_and_preserves_summary_amount()
    {
        await using var dbContext = CreateDbContext();
        var summary = await SeedSummaryAsync(dbContext, otherAllowanceAmount: 100m);
        var version = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc);
        var detail = CreateDetail(summary.Id, "Khóa dòng", 100m, version);
        dbContext.PayrollOtherAllowanceRecords.Add(detail);
        await dbContext.SaveChangesAsync();
        var service = new DatabaseOtherAllowanceLockStateService(dbContext);

        await service.SetLockStateAsync(new SetOtherAllowanceLockStateRequest(detail.Id, true, version, "locker"));
        var locked = await dbContext.PayrollOtherAllowanceRecords.SingleAsync();
        Assert.True(locked.IsLocked);
        Assert.Equal("locker", locked.UpdatedBy);
        await Assert.ThrowsAsync<OtherAllowanceConflictException>(() => service.SetLockStateAsync(
            new SetOtherAllowanceLockStateRequest(detail.Id, false, version, "other-actor")));

        await service.SetLockStateAsync(new SetOtherAllowanceLockStateRequest(detail.Id, false, locked.UpdatedAtUtc, "unlocker"));
        Assert.False((await dbContext.PayrollOtherAllowanceRecords.SingleAsync()).IsLocked);
        Assert.Equal(100m, (await dbContext.PayrollAllowanceSummaryRecords.SingleAsync()).OtherAllowanceAmount);
    }

    [Fact]
    public async Task Batch_lock_changes_only_selected_rows_or_the_current_period()
    {
        await using var dbContext = CreateDbContext();
        var julySummary = await SeedSummaryAsync(dbContext);
        var augustSummary = new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), PayrollMonth = 8, PayrollYear = 2026,
            CreatedAtUtc = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc), CreatedBy = "seed"
        };
        var version = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc);
        var selected = CreateDetail(julySummary.Id, "Được chọn", 100m, version);
        var julyOther = CreateDetail(julySummary.Id, "Cùng kỳ", 100m, version);
        var august = CreateDetail(augustSummary.Id, "Khác kỳ", 100m, version);
        dbContext.AddRange(augustSummary, selected, julyOther, august);
        await dbContext.SaveChangesAsync();
        var service = new DatabaseOtherAllowanceLockStateService(dbContext);

        var selectedResult = await service.SetLockStateBatchAsync(
            new SetOtherAllowanceBatchLockStateRequest(7, 2026, true, [selected.Id], "locker"));

        Assert.Equal(1, selectedResult.TargetRowCount);
        Assert.Equal(1, selectedResult.UpdatedCount);
        Assert.True((await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == selected.Id)).IsLocked);
        Assert.False((await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == julyOther.Id)).IsLocked);

        var periodResult = await service.SetLockStateBatchAsync(
            new SetOtherAllowanceBatchLockStateRequest(7, 2026, true, null, "locker"));

        Assert.Equal(2, periodResult.TargetRowCount);
        Assert.Equal(1, periodResult.UpdatedCount);
        Assert.True((await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == julyOther.Id)).IsLocked);
        Assert.False((await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == august.Id)).IsLocked);
    }

    [Fact]
    public async Task Batch_lock_skips_summary_locked_rows_and_reports_the_outcome()
    {
        await using var dbContext = CreateDbContext();
        var openSummary = await SeedSummaryAsync(dbContext);
        var lockedSummary = new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), PayrollMonth = 7, PayrollYear = 2026,
            IsLocked = true, CreatedAtUtc = new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc), CreatedBy = "seed"
        };
        var version = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc);
        var actionable = CreateDetail(openSummary.Id, "Có thể khóa", 100m, version);
        var protectedBySummary = CreateDetail(lockedSummary.Id, "Summary đã khóa", 100m, version);
        dbContext.AddRange(lockedSummary, actionable, protectedBySummary);
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseOtherAllowanceLockStateService(dbContext).SetLockStateBatchAsync(
            new SetOtherAllowanceBatchLockStateRequest(7, 2026, true, null, "locker"));

        Assert.Equal(2, result.TargetRowCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(0, result.UnchangedCount);
        Assert.Equal(1, result.SkippedSummaryLockedCount);
        Assert.True(result.IsWholePeriod);
        Assert.True((await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == actionable.Id)).IsLocked);
        Assert.False((await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == protectedBySummary.Id)).IsLocked);

        var unlockResult = await new DatabaseOtherAllowanceLockStateService(dbContext).SetLockStateBatchAsync(
            new SetOtherAllowanceBatchLockStateRequest(7, 2026, false, null, "unlocker"));

        Assert.Equal(2, unlockResult.TargetRowCount);
        Assert.Equal(1, unlockResult.UpdatedCount);
        Assert.Equal(1, unlockResult.SkippedSummaryLockedCount);
        Assert.False((await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == actionable.Id)).IsLocked);
        Assert.False((await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == protectedBySummary.Id)).IsLocked);
    }

    [Fact]
    public async Task Batch_lock_rejects_a_selected_row_with_a_stale_version()
    {
        await using var dbContext = CreateDbContext();
        var summary = await SeedSummaryAsync(dbContext);
        var originalVersion = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc);
        var changedVersion = originalVersion.AddMinutes(1);
        var detail = CreateDetail(summary.Id, "Đã thay đổi", 100m, originalVersion, updatedAt: changedVersion);
        dbContext.PayrollOtherAllowanceRecords.Add(detail);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<OtherAllowanceConflictException>(() =>
            new DatabaseOtherAllowanceLockStateService(dbContext).SetLockStateBatchAsync(
                new SetOtherAllowanceBatchLockStateRequest(
                    7,
                    2026,
                    true,
                    [detail.Id],
                    "locker",
                    [new OtherAllowanceLockItem(detail.Id, originalVersion)])));

        Assert.False((await dbContext.PayrollOtherAllowanceRecords.SingleAsync()).IsLocked);
    }

    [Fact]
    public async Task Sync_from_previous_month_creates_only_missing_rows_for_open_target_summaries()
    {
        await using var dbContext = CreateDbContext();
        var createdAt = new DateTime(2026, 6, 30, 8, 0, 0, DateTimeKind.Utc);
        var copiedEmployeeId = Guid.NewGuid();
        var existingEmployeeId = Guid.NewGuid();
        var lockedEmployeeId = Guid.NewGuid();
        var missingEmployeeId = Guid.NewGuid();
        var sourceCopied = CreateSummary(copiedEmployeeId, 6, 2026, createdAt);
        var sourceExisting = CreateSummary(existingEmployeeId, 6, 2026, createdAt);
        var sourceLocked = CreateSummary(lockedEmployeeId, 6, 2026, createdAt);
        var sourceMissing = CreateSummary(missingEmployeeId, 6, 2026, createdAt);
        var targetCopied = CreateSummary(copiedEmployeeId, 7, 2026, createdAt);
        var targetExisting = CreateSummary(existingEmployeeId, 7, 2026, createdAt, otherAllowanceAmount: 50m);
        var targetLocked = CreateSummary(lockedEmployeeId, 7, 2026, createdAt, isLocked: true);
        dbContext.AddRange(sourceCopied, sourceExisting, sourceLocked, sourceMissing, targetCopied, targetExisting, targetLocked);
        dbContext.AddRange(
            CreateDetail(sourceCopied.Id, "Đi lại", 100m, createdAt),
            CreateDetail(sourceExisting.Id, "Meal", 0m, createdAt, isFixedAmount: false),
            CreateDetail(sourceLocked.Id, "Độc hại", 300m, createdAt),
            CreateDetail(sourceMissing.Id, "Điện thoại", 400m, createdAt),
            CreateDetail(targetExisting.Id, "meal", 50m, createdAt));
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseOtherAllowancePreviousMonthSyncService(dbContext).SyncFromPreviousMonthAsync(
            new SyncOtherAllowanceFromPreviousMonthRequest(7, 2026, "payroll-admin"));

        Assert.Equal(6, result.SourcePayrollMonth);
        Assert.Equal(2026, result.SourcePayrollYear);
        Assert.Equal(4, result.SourceRowCount);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.SkippedExistingCount);
        Assert.Equal(1, result.SkippedTargetSummaryLockedCount);
        Assert.Equal(1, result.SkippedMissingTargetSummaryCount);
        var copied = await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.PayrollAllowanceSummaryRecordId == targetCopied.Id);
        Assert.Equal("Đi lại", copied.AllowanceName);
        Assert.Equal(100m, copied.AllowanceAmount);
        Assert.False(copied.IsLocked);
        Assert.Equal("payroll-admin", copied.CreatedBy);
        Assert.Equal(100m, (await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == targetCopied.Id)).OtherAllowanceAmount);
        Assert.Equal(50m, (await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == targetExisting.Id)).OtherAllowanceAmount);
    }

    [Fact]
    public async Task Sync_from_previous_month_overwrites_an_existing_open_fixed_allowance()
    {
        await using var dbContext = CreateDbContext();
        var createdAt = new DateTime(2026, 6, 30, 8, 0, 0, DateTimeKind.Utc);
        var employeeId = Guid.NewGuid();
        var sourceSummary = CreateSummary(employeeId, 6, 2026, createdAt);
        var targetSummary = CreateSummary(employeeId, 7, 2026, createdAt, otherAllowanceAmount: 80m);
        var source = CreateDetail(sourceSummary.Id, "Nhà ở", 125m, createdAt);
        source.Note = "Theo quyết định tháng trước";
        var target = CreateDetail(targetSummary.Id, "nhà ở", 80m, createdAt, isFixedAmount: false);
        target.Note = "Điều chỉnh tay";
        dbContext.AddRange(sourceSummary, targetSummary, source, target);
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseOtherAllowancePreviousMonthSyncService(dbContext).SyncFromPreviousMonthAsync(
            new SyncOtherAllowanceFromPreviousMonthRequest(7, 2026, "payroll-admin"));

        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(1, result.UpdatedFixedCount);
        Assert.Equal(0, result.SkippedExistingCount);
        var persisted = await dbContext.PayrollOtherAllowanceRecords.SingleAsync(row => row.Id == target.Id);
        Assert.Equal("Nhà ở", persisted.AllowanceName);
        Assert.True(persisted.IsFixedAmount);
        Assert.Equal(125m, persisted.AllowanceAmount);
        Assert.Equal("Theo quyết định tháng trước", persisted.Note);
        Assert.Equal("payroll-admin", persisted.UpdatedBy);
        Assert.Equal(125m, (await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == targetSummary.Id)).OtherAllowanceAmount);
    }

    [Fact]
    public async Task Delete_open_manual_adjustment_removes_line_and_refreshes_summary_total()
    {
        await using var dbContext = CreateDbContext();
        var summary = await SeedSummaryAsync(dbContext, otherAllowanceAmount: 350m);
        var version = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Utc);
        var removed = CreateDetail(summary.Id, "Xóa", 300m, version);
        dbContext.PayrollOtherAllowanceRecords.AddRange(removed, CreateDetail(summary.Id, "Giữ", 50m, version));
        await dbContext.SaveChangesAsync();

        await new DatabaseOtherAllowanceDeleteService(dbContext).DeleteAsync(
            new DeleteOtherAllowanceRequest(removed.Id, version, "deleter"));

        Assert.DoesNotContain(await dbContext.PayrollOtherAllowanceRecords.ToListAsync(), row => row.Id == removed.Id);
        var persistedSummary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(50m, persistedSummary.OtherAllowanceAmount);
        Assert.Equal("deleter", persistedSummary.UpdatedBy);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"other-allowance-command-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static async Task<PayrollAllowanceSummaryRecordRow> SeedSummaryAsync(ApplicationDbContext dbContext, decimal otherAllowanceAmount = 0m)
    {
        var summary = new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), PayrollMonth = 7, PayrollYear = 2026,
            OtherAllowanceAmount = otherAllowanceAmount,
            CreatedAtUtc = new DateTime(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc), CreatedBy = "seed"
        };
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();
        return summary;
    }

    private static PayrollAllowanceSummaryRecordRow CreateSummary(
        Guid employeeId,
        short payrollMonth,
        short payrollYear,
        DateTime createdAt,
        bool isLocked = false,
        decimal otherAllowanceAmount = 0m) => new()
    {
        Id = Guid.NewGuid(), EmployeeId = employeeId, PayrollMonth = payrollMonth, PayrollYear = payrollYear,
        IsLocked = isLocked, OtherAllowanceAmount = otherAllowanceAmount, CreatedAtUtc = createdAt, CreatedBy = "seed"
    };

    private static PayrollOtherAllowanceRecordRow CreateDetail(
        Guid summaryId, string name, decimal amount, DateTime createdAt, bool isLocked = false, DateTime? updatedAt = null, bool isFixedAmount = true) => new()
    {
        Id = Guid.NewGuid(), PayrollAllowanceSummaryRecordId = summaryId, AllowanceName = name,
        IsFixedAmount = isFixedAmount, AllowanceAmount = amount, IsLocked = isLocked,
        CreatedAtUtc = createdAt, CreatedBy = "seed", UpdatedAtUtc = updatedAt
    };
}
