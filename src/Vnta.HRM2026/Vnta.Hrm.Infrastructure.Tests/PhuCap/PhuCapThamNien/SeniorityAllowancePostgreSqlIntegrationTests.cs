using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Vnta.Hrm.Application.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Persistence;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapThamNien;

[Collection(SeniorityAllowancePostgreSqlCollection.Name)]
public sealed class SeniorityAllowancePostgreSqlIntegrationTests(SeniorityAllowancePostgreSqlFixture fixture)
{
    private const int PayrollYear = 2026;
    private const int PayrollMonth = 7;

    [PostgreSqlSeniorityAllowanceFact]
    public async Task Refresh_creates_a_missing_snapshot_without_updating_the_period_summary_amount()
    {
        fixture.RequireDatabase();
        var seed = await SeedSummaryAsync(fixture, seniorityStartDate: new DateTime(2025, 7, 1));

        await using (var commandContext = fixture.CreateDbContext())
        {
            var result = await CreateRefreshService(commandContext, seed.EmployeeId, includedWorkdays: 6)
                .RefreshAsync(new RefreshPayrollEmployeeSeniorityAllowanceRequest(PayrollYear, PayrollMonth));

            Assert.Equal(1, result.TargetRowCount);
            Assert.Equal(1, result.UpdatedCount);
            Assert.Equal(0, result.SkippedLockedCount);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var detail = await verificationContext.PayrollEmployeeSeniorityAllowances.SingleAsync();
        var summary = await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal((short)1, detail.CompletedSeniorityYears);
        Assert.Equal(6m, detail.SalaryWorkDays);
        Assert.Equal("1-3", detail.AppliedRuleKey);
        Assert.Equal(150_000m, detail.AllowanceAmount);
        Assert.Equal(0m, summary.SeniorityAllowanceAmount);
    }

    [PostgreSqlSeniorityAllowanceFact]
    public async Task Refresh_skips_a_locked_snapshot_without_overwriting_its_detail_or_summary()
    {
        fixture.RequireDatabase();
        var seed = await SeedSummaryAsync(
            fixture, seniorityStartDate: new DateTime(2010, 7, 1), seniorityAllowanceAmount: 77_000m);
        await SeedDetailAsync(fixture, seed.SummaryId, allowanceAmount: 77_000m, isLocked: true);

        await using (var commandContext = fixture.CreateDbContext())
        {
            var result = await CreateRefreshService(commandContext, seed.EmployeeId, includedWorkdays: 26)
                .RefreshAsync(new RefreshPayrollEmployeeSeniorityAllowanceRequest(PayrollYear, PayrollMonth));

            Assert.Equal(1, result.TargetRowCount);
            Assert.Equal(0, result.UpdatedCount);
            Assert.Equal(1, result.SkippedLockedCount);
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.Equal(77_000m, (await verificationContext.PayrollEmployeeSeniorityAllowances.SingleAsync()).AllowanceAmount);
        Assert.Equal(77_000m, (await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync()).SeniorityAllowanceAmount);
    }

    [PostgreSqlSeniorityAllowanceFact]
    public async Task Refresh_for_one_summary_updates_only_the_requested_unlocked_row()
    {
        fixture.RequireDatabase();
        var target = await SeedSummaryAsync(
            fixture, seniorityStartDate: new DateTime(2025, 7, 1), seniorityAllowanceAmount: 10_000m);
        var untouched = await SeedSummaryAsync(
            fixture, seniorityStartDate: new DateTime(2025, 7, 1), seniorityAllowanceAmount: 77_000m);
        await SeedDetailAsync(fixture, target.SummaryId, allowanceAmount: 10_000m);
        await SeedDetailAsync(fixture, untouched.SummaryId, allowanceAmount: 77_000m);

        await using (var commandContext = fixture.CreateDbContext())
        {
            var result = await CreateRefreshService(commandContext, target.EmployeeId, includedWorkdays: 6)
                .RefreshAsync(new RefreshPayrollEmployeeSeniorityAllowanceRequest(PayrollYear, PayrollMonth, target.SummaryId));

            Assert.Equal(1, result.TargetRowCount);
            Assert.Equal(1, result.UpdatedCount);
            Assert.Equal(0, result.SkippedLockedCount);
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.Equal(150_000m, (await verificationContext.PayrollEmployeeSeniorityAllowances
            .SingleAsync(row => row.PayrollAllowanceSummaryRecordId == target.SummaryId)).AllowanceAmount);
        Assert.Equal(77_000m, (await verificationContext.PayrollEmployeeSeniorityAllowances
            .SingleAsync(row => row.PayrollAllowanceSummaryRecordId == untouched.SummaryId)).AllowanceAmount);
        Assert.Equal(10_000m, (await verificationContext.PayrollAllowanceSummaryRecords
            .SingleAsync(row => row.Id == target.SummaryId)).SeniorityAllowanceAmount);
        Assert.Equal(77_000m, (await verificationContext.PayrollAllowanceSummaryRecords
            .SingleAsync(row => row.Id == untouched.SummaryId)).SeniorityAllowanceAmount);
    }

    [PostgreSqlSeniorityAllowanceFact]
    public async Task Manual_adjustment_rounds_away_from_zero_syncs_summary_and_records_an_audited_operation()
    {
        fixture.RequireDatabase();
        var seed = await SeedSummaryAsync(fixture);
        var timestamp = await SeedDetailAsync(fixture, seed.SummaryId, allowanceAmount: 150_000m);

        await using (var commandContext = fixture.CreateDbContext())
        {
            var scope = new AsyncLocalAuditScope();
            using var audit = scope.Begin(CreateAuditCommand());
            var service = new DatabasePayrollEmployeeSeniorityAllowanceManualAdjustmentService(
                commandContext, scope, new AuditedMutation(commandContext, scope));

            var result = await service.UpdateManualValuesAsync(new UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest(
                seed.SummaryId, 150_000.5m, "  Điều chỉnh công tháng  ", timestamp));

            Assert.Equal(150_001m, result.AllowanceAmount);
            Assert.Equal("Điều chỉnh công tháng", result.Note);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var detail = await verificationContext.PayrollEmployeeSeniorityAllowances.SingleAsync();
        var summary = await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync();
        var auditEvent = await verificationContext.AuditEvents.SingleAsync();
        Assert.Equal(150_001m, detail.AllowanceAmount);
        Assert.Equal("Điều chỉnh công tháng", detail.Note);
        Assert.Equal(detail.AllowanceAmount, summary.SeniorityAllowanceAmount);
        Assert.Equal(AuditActions.SeniorityAllowance.ManualValueUpdated, auditEvent.Action);
    }

    [PostgreSqlSeniorityAllowanceFact]
    public async Task Manual_adjustment_rolls_back_the_bulk_update_when_its_audit_write_fails()
    {
        fixture.RequireDatabase();
        var seed = await SeedSummaryAsync(fixture);
        var timestamp = await SeedDetailAsync(fixture, seed.SummaryId, allowanceAmount: 150_000m);

        await using (var commandContext = fixture.CreateDbContext(new ThrowWhenAuditEventIsSavedInterceptor()))
        {
            var scope = new AsyncLocalAuditScope();
            using var audit = scope.Begin(CreateAuditCommand());
            var service = new DatabasePayrollEmployeeSeniorityAllowanceManualAdjustmentService(
                commandContext, scope, new AuditedMutation(commandContext, scope));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateManualValuesAsync(
                new UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest(seed.SummaryId, 200_000m, "rollback", timestamp)));
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.Equal(150_000m, (await verificationContext.PayrollEmployeeSeniorityAllowances.SingleAsync()).AllowanceAmount);
        Assert.Equal(0m, (await verificationContext.PayrollAllowanceSummaryRecords.SingleAsync()).SeniorityAllowanceAmount);
        Assert.Empty(await verificationContext.AuditEvents.ToListAsync());
    }

    [PostgreSqlSeniorityAllowanceFact]
    public async Task Lock_then_unlock_uses_the_returned_concurrency_token_and_preserves_the_manual_amount()
    {
        fixture.RequireDatabase();
        var seed = await SeedSummaryAsync(fixture);
        var timestamp = await SeedDetailAsync(fixture, seed.SummaryId, allowanceAmount: 150_000m);

        await using (var commandContext = fixture.CreateDbContext())
        {
            var scope = new AsyncLocalAuditScope();
            using var audit = scope.Begin(CreateAuditCommand());
            var service = new DatabasePayrollEmployeeSeniorityAllowanceLockService(
                commandContext, scope, new AuditedMutation(commandContext, scope));

            var locked = await service.SetLockStateAsync(new SetPayrollEmployeeSeniorityAllowanceLockStateRequest(seed.SummaryId, true, timestamp));
            var unlocked = await service.SetLockStateAsync(new SetPayrollEmployeeSeniorityAllowanceLockStateRequest(
                seed.SummaryId, false, locked.UpdatedAtUtc));

            Assert.False(unlocked.IsLocked);
            Assert.Equal(150_000m, unlocked.AllowanceAmount);
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.False((await verificationContext.PayrollEmployeeSeniorityAllowances.SingleAsync()).IsLocked);
        Assert.Equal(2, await verificationContext.AuditEvents.CountAsync());
    }

    [PostgreSqlSeniorityAllowanceFact]
    public async Task Batch_lock_changes_only_requested_period_rows_and_reports_idempotent_updates()
    {
        fixture.RequireDatabase();
        var inPeriod = await SeedSummaryAsync(fixture);
        var otherPeriod = await SeedSummaryAsync(fixture, payrollMonth: 8);
        await SeedDetailAsync(fixture, inPeriod.SummaryId, allowanceAmount: 150_000m);
        await SeedDetailAsync(fixture, otherPeriod.SummaryId, allowanceAmount: 250_000m);

        await using (var commandContext = fixture.CreateDbContext())
        {
            var scope = new AsyncLocalAuditScope();
            using var audit = scope.Begin(CreateAuditCommand());
            var service = new DatabasePayrollEmployeeSeniorityAllowanceLockService(
                commandContext, scope, new AuditedMutation(commandContext, scope));
            var request = new SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest(PayrollYear, PayrollMonth, true);

            var first = await service.SetLockStateBatchAsync(request);
            var second = await service.SetLockStateBatchAsync(request);

            Assert.Equal(1, first.TargetRowCount);
            Assert.Equal(1, first.UpdatedCount);
            Assert.Equal(1, second.TargetRowCount);
            Assert.Equal(0, second.UpdatedCount);
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.True((await verificationContext.PayrollEmployeeSeniorityAllowances.SingleAsync(x => x.PayrollAllowanceSummaryRecordId == inPeriod.SummaryId)).IsLocked);
        Assert.False((await verificationContext.PayrollEmployeeSeniorityAllowances.SingleAsync(x => x.PayrollAllowanceSummaryRecordId == otherPeriod.SummaryId)).IsLocked);
    }

    [PostgreSqlSeniorityAllowanceFact]
    public async Task Lock_actions_skip_details_whose_allowance_summary_record_is_locked()
    {
        fixture.RequireDatabase();
        var eligible = await SeedSummaryAsync(fixture);
        var summaryLocked = await SeedSummaryAsync(fixture, isLocked: true);
        await SeedDetailAsync(fixture, eligible.SummaryId, allowanceAmount: 150_000m);
        var lockedSummaryDetailTimestamp = await SeedDetailAsync(fixture, summaryLocked.SummaryId, allowanceAmount: 250_000m);

        await using (var commandContext = fixture.CreateDbContext())
        {
            var scope = new AsyncLocalAuditScope();
            using var audit = scope.Begin(CreateAuditCommand());
            var service = new DatabasePayrollEmployeeSeniorityAllowanceLockService(
                commandContext, scope, new AuditedMutation(commandContext, scope));

            var lockBatch = await service.SetLockStateBatchAsync(
                new SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest(PayrollYear, PayrollMonth, true));

            Assert.Equal(2, lockBatch.TargetRowCount);
            Assert.Equal(1, lockBatch.UpdatedCount);
            Assert.Equal(0, lockBatch.UnchangedCount);
            Assert.Equal(1, lockBatch.SkippedSummaryLockedCount);
            var skippedRow = Assert.Single(lockBatch.SkippedRows!);
            Assert.Equal(summaryLocked.SummaryId, skippedRow.PayrollAllowanceSummaryRecordId);
            Assert.Contains("Phụ cấp tổng hợp", skippedRow.Reason, StringComparison.Ordinal);

            var unlockBatch = await service.SetLockStateBatchAsync(
                new SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest(PayrollYear, PayrollMonth, false));

            Assert.Equal(2, unlockBatch.TargetRowCount);
            Assert.Equal(1, unlockBatch.UpdatedCount);
            Assert.Equal(0, unlockBatch.UnchangedCount);
            Assert.Equal(1, unlockBatch.SkippedSummaryLockedCount);
            Assert.Equal(summaryLocked.SummaryId, Assert.Single(unlockBatch.SkippedRows!).PayrollAllowanceSummaryRecordId);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetLockStateAsync(
                new SetPayrollEmployeeSeniorityAllowanceLockStateRequest(summaryLocked.SummaryId, false, lockedSummaryDetailTimestamp)));
        }

        await using var verificationContext = fixture.CreateDbContext();
        Assert.False((await verificationContext.PayrollEmployeeSeniorityAllowances
            .SingleAsync(row => row.PayrollAllowanceSummaryRecordId == eligible.SummaryId)).IsLocked);
        Assert.False((await verificationContext.PayrollEmployeeSeniorityAllowances
            .SingleAsync(row => row.PayrollAllowanceSummaryRecordId == summaryLocked.SummaryId)).IsLocked);
    }

    private static DatabasePayrollEmployeeSeniorityAllowanceRefreshService CreateRefreshService(
        ApplicationDbContext dbContext, Guid employeeId, int includedWorkdays) => new(new SeniorityAllowancePeriodWriter(
        dbContext,
        new PayrollEmployeeSeniorityAllowanceCalculator(),
        new PayrollEmployeeSeniorityAllowanceWorkdayCalculator(),
        new PayrollEmployeeSeniorityAllowanceTenureCalculator(),
        new FixedWorkdaySource(employeeId, includedWorkdays)));

    private static async Task<SeniorityAllowanceSeed> SeedSummaryAsync(
        SeniorityAllowancePostgreSqlFixture fixture,
        DateTime? seniorityStartDate = null,
        int payrollMonth = PayrollMonth,
        decimal seniorityAllowanceAmount = 0m,
        bool isLocked = false)
    {
        var timestamp = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Unspecified);
        var employeeId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Employees.Add(new AttendanceGatewayEmployeeRow
        {
            Id = employeeId,
            EmployeeCode = $"TN-{employeeId:N}"[..16],
            FirstName = "Seniority",
            LastName = "Integration",
            HireDate = seniorityStartDate ?? new DateTime(2020, 1, 1),
            SeniorityStartDate = seniorityStartDate,
            CreatedAtUtc = timestamp
        });
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = employeeId,
            PayrollYear = PayrollYear,
            PayrollMonth = checked((short)payrollMonth),
            SeniorityAllowanceAmount = seniorityAllowanceAmount,
            IsLocked = isLocked,
            CreatedAtUtc = timestamp,
            CreatedBy = "integration-test"
        });
        await dbContext.SaveChangesAsync();
        return new SeniorityAllowanceSeed(employeeId, summaryId);
    }

    private static async Task<DateTime> SeedDetailAsync(
        SeniorityAllowancePostgreSqlFixture fixture, Guid summaryId, decimal allowanceAmount, bool isLocked = false)
    {
        var timestamp = new DateTime(2026, 7, 30, 9, 0, 0, DateTimeKind.Unspecified);
        await using var dbContext = fixture.CreateDbContext();
        dbContext.PayrollEmployeeSeniorityAllowances.Add(new PayrollEmployeeSeniorityAllowanceRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            CompletedSeniorityYears = 1,
            CompletedSeniorityMonths = 0,
            SalaryWorkDays = 26m,
            AppliedRuleKey = "1-3",
            AllowanceAmount = allowanceAmount,
            IsLocked = isLocked,
            CreatedAtUtc = timestamp,
            CreatedBy = "integration-test",
            UpdatedAtUtc = timestamp
        });
        await dbContext.SaveChangesAsync();
        return timestamp;
    }

    private static AuditCommand CreateAuditCommand() => new(
        Guid.NewGuid(), "test.seniority", new AuditActor("payroll-admin", "Payroll Admin", AuditActorKind.User, AuditSource.Api),
        Guid.NewGuid().ToString("N"), AuditCaptureMode.OperationOnly);

    private sealed class FixedWorkdaySource(Guid employeeId, int includedWorkdays) : IPayrollEmployeeSeniorityAllowanceWorkdaySource
    {
        public Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<PayrollEmployeeSeniorityAllowanceWorkdayInput>>> LoadAsync(
            PayrollEmployeeSeniorityAllowanceWorkdaySourceQuery query, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<PayrollEmployeeSeniorityAllowanceWorkdayInput> workdays = Enumerable.Range(1, includedWorkdays)
                .Select(_ => new PayrollEmployeeSeniorityAllowanceWorkdayInput(
                    PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included, 0, 0)).ToArray();
            return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyCollection<PayrollEmployeeSeniorityAllowanceWorkdayInput>>>(
                new Dictionary<Guid, IReadOnlyCollection<PayrollEmployeeSeniorityAllowanceWorkdayInput>> { [employeeId] = workdays });
        }
    }

    private sealed class ThrowWhenAuditEventIsSavedInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<AuditEventRow>().Any(entry => entry.State == EntityState.Added) == true)
            {
                throw new InvalidOperationException("Forced audit persistence failure.");
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed record SeniorityAllowanceSeed(Guid EmployeeId, Guid SummaryId);
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SeniorityAllowancePostgreSqlCollection : ICollectionFixture<SeniorityAllowancePostgreSqlFixture>
{
    public const string Name = "Seniority allowance PostgreSQL integration";
}

public sealed class SeniorityAllowancePostgreSqlFixture : IAsyncLifetime
{
    private const string ConnectionStringEnvironmentVariable = "VNTA_SENIORITY_ALLOWANCE_TEST_DB";
    private string? connectionString;

    public async Task InitializeAsync()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(configuredConnectionString);
        if (string.IsNullOrWhiteSpace(builder.Database)
            || !builder.Database.StartsWith("vnta_seniority_allowance_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} must target a disposable database named vnta_seniority_allowance_test*.");
        }

        connectionString = builder.ConnectionString;
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (connectionString is null)
        {
            return;
        }

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    public void RequireDatabase()
    {
        if (connectionString is null)
        {
            throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} to run seniority-allowance PostgreSQL integration tests.");
        }
    }

    public ApplicationDbContext CreateDbContext(params IInterceptor[] interceptors)
    {
        RequireDatabase();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString);
        if (interceptors.Length > 0)
        {
            options.AddInterceptors(interceptors);
        }

        return new ApplicationDbContext(options.Options);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgreSqlSeniorityAllowanceFactAttribute : FactAttribute
{
    public PostgreSqlSeniorityAllowanceFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VNTA_SENIORITY_ALLOWANCE_TEST_DB")))
        {
            Skip = "Set VNTA_SENIORITY_ALLOWANCE_TEST_DB to a disposable vnta_seniority_allowance_test* PostgreSQL database to run these tests.";
        }
    }
}
