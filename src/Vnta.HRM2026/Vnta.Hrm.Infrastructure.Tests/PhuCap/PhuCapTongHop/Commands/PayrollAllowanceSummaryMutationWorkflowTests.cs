using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Exceptions;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTongHop.Commands;

public sealed class PayrollAllowanceSummaryMutationWorkflowTests
{
    [Fact]
    public void PostgreSQL_timestamp_normalization_removes_submicrosecond_ticks()
    {
        var value = new DateTime(2026, 8, 3, 14, 30, 0, DateTimeKind.Utc).AddTicks(9);

        var normalized = PostgreSqlTimestamp.ToTimestampWithoutTimeZone(value);

        Assert.Equal(new DateTime(2026, 8, 3, 14, 30, 0, DateTimeKind.Unspecified), normalized);
    }

    [Fact]
    public async Task Selected_rows_can_be_locked_then_unlocked_with_the_latest_version()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary(updatedAtUtc: new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc).AddTicks(9));
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();
        var persistence = CreatePersistence(dbContext);

        await persistence.SetLockStateBatchAsync(new SetPayrollAllowanceSummaryBatchLockStateRequest(
            2026, 7, true, [summary.Id],
            [new PayrollAllowanceSummaryLockStateConcurrencyToken(summary.Id, summary.UpdatedAtUtc)], "tester"));

        var lockedVersion = (await dbContext.PayrollAllowanceSummaryRecords
            .AsNoTracking()
            .SingleAsync()).UpdatedAtUtc;
        await persistence.SetLockStateBatchAsync(new SetPayrollAllowanceSummaryBatchLockStateRequest(
            2026, 7, false, [summary.Id],
            [new PayrollAllowanceSummaryLockStateConcurrencyToken(summary.Id, lockedVersion)], "tester"));

        Assert.False((await dbContext.PayrollAllowanceSummaryRecords.AsNoTracking().SingleAsync()).IsLocked);
    }

    [Fact]
    public async Task Sync_from_previous_month_copies_the_source_snapshot_only_for_employee_with_target_period_attendance()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = Guid.NewGuid();
        var source = CreateSummary();
        source.EmployeeId = employeeId;
        source.PayrollMonth = 6;
        source.ResponsibilityAllowanceAmount = 12m;
        source.AttendanceAllowanceAmount = 600_000m;
        source.MealAllowanceAmount = 36_000m;
        source.Note = "sao chép từ kỳ trước";
        dbContext.PayrollAllowanceSummaryRecords.Add(source);
        dbContext.PayrollAttendanceAllowanceRecords.Add(new PayrollAttendanceAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = source.Id,
            StandardWorkdayCount = 26m,
            ActualWorkdayCount = 25m,
            AttendanceRate = 0.9615m,
            AllowanceAmount = 600_000m,
            AppliedRuleKey = "attendance-cc-a",
            AttendanceClass = "A",
            RefreshedAtUtc = new DateTime(2026, 6, 30),
            RefreshedBy = "source-calculation",
            CreatedAtUtc = new DateTime(2026, 6, 30),
            CreatedBy = "source-calculation"
        });
        dbContext.AttendanceWorkdaySummaries.Add(new AttendanceWorkdaySummaryRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            WorkDate = new DateOnly(2026, 7, 1),
            DayType = "Regular",
            ComputedAtUtc = new DateTime(2026, 7, 1),
            CreatedAtUtc = new DateTime(2026, 7, 1)
        });
        await dbContext.SaveChangesAsync();

        var result = await CreatePersistence(dbContext).SyncFromPreviousMonthAsync(
            new SyncPayrollAllowanceSummaryFromPreviousMonthRequest(7, 2026, "payroll-admin"));

        var copied = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.PayrollMonth == 7);
        Assert.Equal(6, result.SourcePayrollMonth);
        Assert.Equal(2026, result.SourcePayrollYear);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(employeeId, copied.EmployeeId);
        Assert.Equal(12m, copied.ResponsibilityAllowanceAmount);
        Assert.Equal(0m, copied.AttendanceAllowanceAmount);
        Assert.Equal(36_000m, copied.MealAllowanceAmount);
        Assert.Equal("sao chép từ kỳ trước", copied.Note);
        var copiedAttendance = await dbContext.PayrollAttendanceAllowanceRecords
            .SingleAsync(row => row.PayrollAllowanceSummaryRecordId == copied.Id);
        Assert.Equal(0m, copiedAttendance.StandardWorkdayCount);
        Assert.Equal(0m, copiedAttendance.ActualWorkdayCount);
        Assert.Equal(0m, copiedAttendance.AllowanceAmount);
        Assert.Null(copiedAttendance.AppliedRuleKey);
        Assert.Null(copiedAttendance.RefreshedAtUtc);
    }

    [Fact]
    public async Task Sync_from_previous_month_preserves_an_existing_target_attendance_projection()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = Guid.NewGuid();
        var source = CreateSummary();
        source.EmployeeId = employeeId;
        source.PayrollMonth = 6;
        source.ResponsibilityAllowanceAmount = 12m;
        source.AttendanceAllowanceAmount = 600_000m;
        var target = CreateSummary();
        target.EmployeeId = employeeId;
        target.PayrollMonth = 7;
        target.ResponsibilityAllowanceAmount = 1m;
        target.AttendanceAllowanceAmount = 300_000m;
        dbContext.PayrollAllowanceSummaryRecords.AddRange(source, target);
        dbContext.PayrollAttendanceAllowanceRecords.AddRange(
            new PayrollAttendanceAllowanceRecordRow
            {
                PayrollAllowanceSummaryRecordId = source.Id,
                AllowanceAmount = 600_000m,
                CreatedAtUtc = new DateTime(2026, 6, 30),
                CreatedBy = "source-calculation"
            },
            new PayrollAttendanceAllowanceRecordRow
            {
                PayrollAllowanceSummaryRecordId = target.Id,
                AllowanceAmount = 300_000m,
                AppliedRuleKey = "attendance-cc-b",
                CreatedAtUtc = new DateTime(2026, 7, 1),
                CreatedBy = "target-calculation"
            });
        dbContext.AttendanceWorkdaySummaries.Add(new AttendanceWorkdaySummaryRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            WorkDate = new DateOnly(2026, 7, 1),
            DayType = "Regular",
            ComputedAtUtc = new DateTime(2026, 7, 1),
            CreatedAtUtc = new DateTime(2026, 7, 1)
        });
        await dbContext.SaveChangesAsync();

        await CreatePersistence(dbContext).SyncFromPreviousMonthAsync(
            new SyncPayrollAllowanceSummaryFromPreviousMonthRequest(7, 2026, "payroll-admin"));

        var syncedTarget = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == target.Id);
        var targetAttendance = await dbContext.PayrollAttendanceAllowanceRecords
            .SingleAsync(row => row.PayrollAllowanceSummaryRecordId == target.Id);
        Assert.Equal(12m, syncedTarget.ResponsibilityAllowanceAmount);
        Assert.Equal(300_000m, syncedTarget.AttendanceAllowanceAmount);
        Assert.Equal(300_000m, targetAttendance.AllowanceAmount);
        Assert.Equal("attendance-cc-b", targetAttendance.AppliedRuleKey);
    }

    [Fact]
    public async Task Sync_from_previous_month_normalizes_utc_source_timestamps_before_creating_detail_rows()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = Guid.NewGuid();
        var source = CreateSummary();
        source.EmployeeId = employeeId;
        source.PayrollMonth = 6;
        dbContext.PayrollAllowanceSummaryRecords.Add(source);
        dbContext.PayrollResponsibilityAllowanceAbcRows.Add(new PayrollResponsibilityAllowanceAbcRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = source.Id,
            EmployeeId = employeeId,
            Year = 2026,
            Month = 6,
            GradeName = "Test",
            CreatedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 6, 1), DateTimeKind.Utc),
            CalculatedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 6, 30), DateTimeKind.Utc)
        });
        dbContext.AttendanceWorkdaySummaries.Add(new AttendanceWorkdaySummaryRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            WorkDate = new DateOnly(2026, 7, 1),
            DayType = "Regular",
            ComputedAtUtc = new DateTime(2026, 7, 1),
            CreatedAtUtc = new DateTime(2026, 7, 1)
        });
        await dbContext.SaveChangesAsync();

        await CreatePersistence(dbContext).SyncFromPreviousMonthAsync(
            new SyncPayrollAllowanceSummaryFromPreviousMonthRequest(7, 2026, "payroll-admin"));

        var copiedDetail = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .SingleAsync(row => row.PayrollAllowanceSummaryRecordId != source.Id);
        Assert.Equal(DateTimeKind.Unspecified, copiedDetail.CalculatedAtUtc!.Value.Kind);
    }

    [Fact]
    public async Task Sync_from_previous_month_creates_distinct_ids_for_each_copied_responsibility_snapshot()
    {
        await using var dbContext = CreateDbContext();
        var employeeIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        foreach (var employeeId in employeeIds)
        {
            var source = CreateSummary();
            source.EmployeeId = employeeId;
            source.PayrollMonth = 6;
            dbContext.PayrollAllowanceSummaryRecords.Add(source);
            dbContext.PayrollResponsibilityAllowanceAbcRows.Add(new PayrollResponsibilityAllowanceAbcRow
            {
                Id = Guid.NewGuid(),
                PayrollAllowanceSummaryRecordId = source.Id,
                EmployeeId = employeeId,
                Year = 2026,
                Month = 6,
                GradeName = "Test",
                CreatedAtUtc = new DateTime(2026, 6, 1)
            });
            dbContext.AttendanceWorkdaySummaries.Add(new AttendanceWorkdaySummaryRow
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                WorkDate = new DateOnly(2026, 7, 1),
                DayType = "Regular",
                ComputedAtUtc = new DateTime(2026, 7, 1),
                CreatedAtUtc = new DateTime(2026, 7, 1)
            });
        }

        await dbContext.SaveChangesAsync();

        await CreatePersistence(dbContext).SyncFromPreviousMonthAsync(
            new SyncPayrollAllowanceSummaryFromPreviousMonthRequest(7, 2026, "payroll-admin"));

        var copiedRows = await dbContext.PayrollResponsibilityAllowanceAbcRows
            .Where(row => row.Month == 7)
            .ToListAsync();
        Assert.Equal(2, copiedRows.Count);
        Assert.Equal(2, copiedRows.Select(row => row.Id).Distinct().Count());
        Assert.DoesNotContain(copiedRows, row => row.Id == Guid.Empty);
        Assert.Equal(
            employeeIds.OrderBy(id => id),
            copiedRows.Select(row => row.EmployeeId).OrderBy(id => id));
    }

    [Fact]
    public async Task Sync_from_previous_month_logs_the_target_period_when_it_fails()
    {
        await using var dbContext = CreateDbContext();
        var logger = new TestLogger<PayrollAllowanceSummaryPersistence>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreatePersistence(dbContext, logger).SyncFromPreviousMonthAsync(
                new SyncPayrollAllowanceSummaryFromPreviousMonthRequest(5, 2026, "payroll-admin")));

        var error = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Error);
        Assert.Contains("target 5/2026", error.Message);
        Assert.IsType<InvalidOperationException>(error.Exception);
    }

    [Fact]
    public async Task Sync_from_previous_month_removes_summary_without_target_attendance_and_all_dependent_allowances()
    {
        await using var dbContext = CreateDbContext();
        var obsoleteSummary = CreateSummary(isLocked: true);
        dbContext.PayrollAllowanceSummaryRecords.Add(obsoleteSummary);
        dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.Add(new PayrollResponsibilityAllowanceEmployeeAssignmentRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = obsoleteSummary.Id,
            CreatedAtUtc = new DateTime(2026, 7, 1)
        });
        dbContext.PayrollMealAllowanceRecords.Add(new PayrollMealAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = obsoleteSummary.Id,
            RuleCode = "test",
            CalculatedAtUtc = new DateTime(2026, 7, 1),
            CreatedAtUtc = new DateTime(2026, 7, 1),
            CreatedBy = "tester"
        });
        dbContext.PayrollOtherAllowanceRecords.Add(new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = obsoleteSummary.Id,
            AllowanceName = "Phụ cấp khác",
            IsFixedAmount = true,
            CreatedAtUtc = new DateTime(2026, 7, 1),
            CreatedBy = "tester"
        });
        await dbContext.SaveChangesAsync();

        var result = await CreatePersistence(dbContext).SyncFromPreviousMonthAsync(
            new SyncPayrollAllowanceSummaryFromPreviousMonthRequest(7, 2026, "payroll-admin"));

        Assert.Equal(1, result.RemovedCount);
        Assert.Empty(await dbContext.PayrollAllowanceSummaryRecords.ToArrayAsync());
        Assert.Empty(await dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.ToArrayAsync());
        Assert.Empty(await dbContext.PayrollMealAllowanceRecords.ToArrayAsync());
        Assert.Empty(await dbContext.PayrollOtherAllowanceRecords.ToArrayAsync());
    }

    [Fact]
    public async Task Manual_adjustment_updates_editable_values_note_and_lock_state_without_overwriting_attendance()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        summary.ResponsibilityAllowanceAmount = 12.5m;
        summary.AttendanceAllowanceAmount = 400m;
        summary.MealAllowanceAmount = 36_000m;
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();

        await CreatePersistence(dbContext).UpdateManualValuesAsync(
            new UpdatePayrollAllowanceSummaryManualValuesRequest(
                summary.Id,
                100m,
                200m,
                300m,
                null,
                500m,
                600m,
                700m,
                800m,
                "  Điều chỉnh theo quyết định  ",
                IsLocked: true,
                OriginalUpdatedAtUtc: null,
                Actor: " payroll-admin "));

        var saved = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal("Điều chỉnh theo quyết định", saved.Note);
        Assert.Equal(100m, saved.ResponsibilityAllowanceAmount);
        Assert.Equal(200m, saved.ResponsibilityOtherAllowanceAmount);
        Assert.Equal(300m, saved.SeniorityAllowanceAmount);
        Assert.Equal(400m, saved.AttendanceAllowanceAmount);
        Assert.Equal(500m, saved.MealAllowanceAmount);
        Assert.Equal(600m, saved.HazardAllowanceAmount);
        Assert.Equal(700m, saved.OtherAllowanceAmount);
        Assert.Equal(800m, saved.LeaveHolidayAllowanceAmount);
        Assert.True(saved.IsLocked);
        Assert.Equal("payroll-admin", saved.UpdatedBy);
    }

    [Fact]
    public async Task Manual_adjustment_rejects_a_legacy_attendance_override_without_mutating_the_summary()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        summary.ResponsibilityAllowanceAmount = 12.5m;
        summary.AttendanceAllowanceAmount = 400m;
        summary.Note = "ghi chú gốc";
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PayrollAllowanceSummaryValidationException>(() =>
            CreatePersistence(dbContext).UpdateManualValuesAsync(
                new UpdatePayrollAllowanceSummaryManualValuesRequest(
                    summary.Id,
                    100m,
                    200m,
                    300m,
                    401m,
                    500m,
                    600m,
                    700m,
                    800m,
                    "ghi chú mới",
                    IsLocked: true,
                    OriginalUpdatedAtUtc: null,
                    Actor: "payroll-admin")));

        Assert.Contains("Phụ cấp chuyên cần", exception.Message, StringComparison.OrdinalIgnoreCase);
        var saved = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(12.5m, saved.ResponsibilityAllowanceAmount);
        Assert.Equal(400m, saved.AttendanceAllowanceAmount);
        Assert.Equal("ghi chú gốc", saved.Note);
        Assert.False(saved.IsLocked);
    }

    [Fact]
    public async Task Locked_row_rejects_manual_adjustment_without_changing_the_existing_note()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary(isLocked: true);
        summary.Note = "note gốc";
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreatePersistence(dbContext).UpdateManualValuesAsync(
                new UpdatePayrollAllowanceSummaryManualNoteRequest(summary.Id, "note mới", null, "tester")));

        Assert.Equal("note gốc", (await dbContext.PayrollAllowanceSummaryRecords.SingleAsync()).Note);
    }

    [Fact]
    public async Task Batch_lock_rejects_a_stale_selected_version_before_mutating_any_selected_row()
    {
        await using var dbContext = CreateDbContext();
        var current = new DateTime(2026, 7, 1, 8, 0, 0);
        var first = CreateSummary(updatedAtUtc: current);
        var second = CreateSummary(updatedAtUtc: current);
        dbContext.PayrollAllowanceSummaryRecords.AddRange(first, second);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreatePersistence(dbContext).SetLockStateBatchAsync(
                new SetPayrollAllowanceSummaryBatchLockStateRequest(
                    2026,
                    7,
                    true,
                    [first.Id, second.Id],
                    [
                        new PayrollAllowanceSummaryLockStateConcurrencyToken(first.Id, current),
                        new PayrollAllowanceSummaryLockStateConcurrencyToken(second.Id, current.AddTicks(-1))
                    ],
                    "tester")));

        var rows = await dbContext.PayrollAllowanceSummaryRecords.OrderBy(row => row.Id).ToArrayAsync();
        Assert.All(rows, row => Assert.False(row.IsLocked));
    }

    [Fact]
    public async Task Delete_rejects_a_locked_row_without_deleting_other_requested_rows()
    {
        await using var dbContext = CreateDbContext();
        var open = CreateSummary();
        var locked = CreateSummary(isLocked: true);
        dbContext.PayrollAllowanceSummaryRecords.AddRange(open, locked);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreatePersistence(dbContext).DeleteAsync(
                new DeletePayrollAllowanceSummariesRequest(
                [
                    new PayrollAllowanceSummaryDeleteItem(open.Id, null),
                    new PayrollAllowanceSummaryDeleteItem(locked.Id, null)
                ])));

        Assert.Equal(2, await dbContext.PayrollAllowanceSummaryRecords.CountAsync());
    }

    private static PayrollAllowanceSummaryPersistence CreatePersistence(
        ApplicationDbContext dbContext,
        ILogger<PayrollAllowanceSummaryPersistence>? logger = null) =>
        new(dbContext, new TestAuditScope(), new SavingAuditedMutation(dbContext), logger);

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-allowance-summary-mutations-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));

        return new ApplicationDbContext(options.Options);
    }

    private static PayrollAllowanceSummaryRecordRow CreateSummary(bool isLocked = false, DateTime? updatedAtUtc = null) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = Guid.NewGuid(),
        PayrollMonth = 7,
        PayrollYear = 2026,
        IsLocked = isLocked,
        CreatedAtUtc = new DateTime(2026, 7, 1),
        CreatedBy = "tester",
        UpdatedAtUtc = updatedAtUtc
    };

    private sealed class TestAuditScope : IAuditScope
    {
        public AuditCommand? Current => null;
        public IDisposable Begin(AuditCommand command) => NoopDisposable.Instance;
        public void RefineAction(string finalAction) { }
        public void SetOperationOutcome(AuditOperationOutcome outcome) { }
    }

    private sealed class SavingAuditedMutation(ApplicationDbContext dbContext) : IAuditedMutation
    {
        public async Task<T> ExecuteAsync<T>(AuditCommand command, Func<CancellationToken, Task<T>> mutation, Func<T, AuditOperationEvent> eventFactory, CancellationToken cancellationToken = default)
        {
            var result = await mutation(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();
        public void Dispose() { }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
