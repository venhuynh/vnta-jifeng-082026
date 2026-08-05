using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruThueTNCN.Commands;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruThueTNCN;

public sealed class DatabasePayrollPersonalIncomeTaxDeductionCommandServiceTests
{
    [Fact]
    public async Task RefreshAsync_synchronizes_the_summary_from_the_existing_tax_detail()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Utc);
        dbContext.PayrollDeductionSummaryRecords.Add(new PayrollDeductionSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 7,
            PayrollYear = 2026,
            PersonalIncomeTaxDeductionAmount = 0m,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "test-user"
        });
        dbContext.PayrollDeductionTaxRecords.Add(new PayrollDeductionTaxRecordRow
        {
            PayrollDeductionSummaryRecordId = summaryId,
            DeductionAmount = 123.45m,
            CreatedAtUtc = createdAtUtc
        });
        await dbContext.SaveChangesAsync();

        var result = await CreateRefreshService(dbContext).RefreshAsync(
            new RefreshPayrollPersonalIncomeTaxDeductionRequest(2026, 7, summaryId));

        var savedSummary = await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == summaryId);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(0, result.UnchangedCount);
        Assert.Equal(123.45m, savedSummary.PersonalIncomeTaxDeductionAmount);
    }

    [Fact]
    public async Task RefreshAsync_skips_a_locked_detail_without_overwriting_the_summary()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Utc);
        dbContext.PayrollDeductionSummaryRecords.Add(new PayrollDeductionSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 7,
            PayrollYear = 2026,
            PersonalIncomeTaxDeductionAmount = 50m,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "test-user"
        });
        dbContext.PayrollDeductionTaxRecords.Add(new PayrollDeductionTaxRecordRow
        {
            PayrollDeductionSummaryRecordId = summaryId,
            DeductionAmount = 123.45m,
            IsLocked = true,
            CreatedAtUtc = createdAtUtc
        });
        await dbContext.SaveChangesAsync();

        var result = await CreateRefreshService(dbContext).RefreshAsync(
            new RefreshPayrollPersonalIncomeTaxDeductionRequest(2026, 7, summaryId));

        var savedSummary = await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == summaryId);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(50m, savedSummary.PersonalIncomeTaxDeductionAmount);
    }

    [Fact]
    public async Task RefreshAsync_rejects_a_row_outside_the_requested_payroll_period()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Utc);
        dbContext.PayrollDeductionSummaryRecords.Add(new PayrollDeductionSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 7,
            PayrollYear = 2026,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "test-user"
        });
        dbContext.PayrollDeductionTaxRecords.Add(new PayrollDeductionTaxRecordRow
        {
            PayrollDeductionSummaryRecordId = summaryId,
            DeductionAmount = 123.45m,
            CreatedAtUtc = createdAtUtc
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateRefreshService(dbContext).RefreshAsync(
            new RefreshPayrollPersonalIncomeTaxDeductionRequest(2026, 8, summaryId)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.001)]
    public async Task UpdateManualValueAsync_rejects_invalid_deduction_amount(decimal amount)
    {
        await using var dbContext = CreateDbContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateManualAdjustmentService(dbContext).UpdateManualValueAsync(
            new UpdatePayrollPersonalIncomeTaxDeductionManualValueRequest(Guid.NewGuid(), amount, DateTime.UtcNow)));
    }

    [Fact]
    public async Task UpdateManualValueAsync_rejects_a_missing_concurrency_token()
    {
        await using var dbContext = CreateDbContext();

        await Assert.ThrowsAsync<PayrollPersonalIncomeTaxDeductionConflictException>(() => CreateManualAdjustmentService(dbContext).UpdateManualValueAsync(
            new UpdatePayrollPersonalIncomeTaxDeductionManualValueRequest(Guid.NewGuid(), 1m, null)));
    }

    [Fact]
    public async Task UpdateManualValueAsync_rejects_a_locked_summary()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Utc);
        dbContext.PayrollDeductionSummaryRecords.Add(new PayrollDeductionSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 7,
            PayrollYear = 2026,
            IsLocked = true,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "test-user"
        });
        dbContext.PayrollDeductionTaxRecords.Add(new PayrollDeductionTaxRecordRow
        {
            PayrollDeductionSummaryRecordId = summaryId,
            CreatedAtUtc = createdAtUtc
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<PayrollPersonalIncomeTaxDeductionConflictException>(() => CreateManualAdjustmentService(dbContext).UpdateManualValueAsync(
            new UpdatePayrollPersonalIncomeTaxDeductionManualValueRequest(summaryId, 1m, createdAtUtc)));
    }

    [Fact]
    public async Task SetLockStateBatchAsync_locks_only_selected_tax_details_without_locking_summaries()
    {
        await using var dbContext = CreateDbContext();
        var selectedSummaryId = Guid.NewGuid();
        var unselectedSummaryId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Utc);
        dbContext.PayrollDeductionSummaryRecords.AddRange(
            new PayrollDeductionSummaryRecordRow
            {
                Id = selectedSummaryId,
                EmployeeId = Guid.NewGuid(),
                PayrollMonth = 7,
                PayrollYear = 2026,
                CreatedAtUtc = createdAtUtc,
                CreatedBy = "test-user"
            },
            new PayrollDeductionSummaryRecordRow
            {
                Id = unselectedSummaryId,
                EmployeeId = Guid.NewGuid(),
                PayrollMonth = 7,
                PayrollYear = 2026,
                CreatedAtUtc = createdAtUtc,
                CreatedBy = "test-user"
            });
        dbContext.PayrollDeductionTaxRecords.AddRange(
            new PayrollDeductionTaxRecordRow { PayrollDeductionSummaryRecordId = selectedSummaryId, CreatedAtUtc = createdAtUtc },
            new PayrollDeductionTaxRecordRow { PayrollDeductionSummaryRecordId = unselectedSummaryId, CreatedAtUtc = createdAtUtc });
        await dbContext.SaveChangesAsync();

        var result = await CreateLockService(dbContext).SetLockStateBatchAsync(
            new SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest(
                2026, 7, IsLocked: true, PayrollPersonalIncomeTaxDeductionLockActionScope.SelectedRows, [selectedSummaryId]));

        Assert.Equal(1, result.TargetRowCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.True((await dbContext.PayrollDeductionTaxRecords.SingleAsync(row => row.PayrollDeductionSummaryRecordId == selectedSummaryId)).IsLocked);
        Assert.False((await dbContext.PayrollDeductionTaxRecords.SingleAsync(row => row.PayrollDeductionSummaryRecordId == unselectedSummaryId)).IsLocked);
        Assert.False((await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == selectedSummaryId)).IsLocked);
    }

    [Fact]
    public async Task SetLockStateBatchAsync_unlocks_every_tax_detail_in_the_period()
    {
        await using var dbContext = CreateDbContext();
        var inPeriodSummaryId = Guid.NewGuid();
        var otherPeriodSummaryId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Utc);
        dbContext.PayrollDeductionSummaryRecords.AddRange(
            new PayrollDeductionSummaryRecordRow { Id = inPeriodSummaryId, EmployeeId = Guid.NewGuid(), PayrollMonth = 7, PayrollYear = 2026, CreatedAtUtc = createdAtUtc, CreatedBy = "test-user" },
            new PayrollDeductionSummaryRecordRow { Id = otherPeriodSummaryId, EmployeeId = Guid.NewGuid(), PayrollMonth = 8, PayrollYear = 2026, CreatedAtUtc = createdAtUtc, CreatedBy = "test-user" });
        dbContext.PayrollDeductionTaxRecords.AddRange(
            new PayrollDeductionTaxRecordRow { PayrollDeductionSummaryRecordId = inPeriodSummaryId, IsLocked = true, CreatedAtUtc = createdAtUtc },
            new PayrollDeductionTaxRecordRow { PayrollDeductionSummaryRecordId = otherPeriodSummaryId, IsLocked = true, CreatedAtUtc = createdAtUtc });
        await dbContext.SaveChangesAsync();

        var result = await CreateLockService(dbContext).SetLockStateBatchAsync(
            new SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest(
                2026, 7, IsLocked: false, PayrollPersonalIncomeTaxDeductionLockActionScope.WholePeriod));

        Assert.Equal(1, result.TargetRowCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.False((await dbContext.PayrollDeductionTaxRecords.SingleAsync(row => row.PayrollDeductionSummaryRecordId == inPeriodSummaryId)).IsLocked);
        Assert.True((await dbContext.PayrollDeductionTaxRecords.SingleAsync(row => row.PayrollDeductionSummaryRecordId == otherPeriodSummaryId)).IsLocked);
    }

    [Fact]
    public async Task SetLockStateBatchAsync_requires_targets_for_the_selected_rows_scope()
    {
        await using var dbContext = CreateDbContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateLockService(dbContext).SetLockStateBatchAsync(
            new SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest(
                2026,
                7,
                IsLocked: true,
                PayrollPersonalIncomeTaxDeductionLockActionScope.SelectedRows)));
    }

    [Fact]
    public async Task SetLockStateBatchAsync_rejects_targets_for_the_whole_period_scope()
    {
        await using var dbContext = CreateDbContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateLockService(dbContext).SetLockStateBatchAsync(
            new SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest(
                2026,
                7,
                IsLocked: true,
                PayrollPersonalIncomeTaxDeductionLockActionScope.WholePeriod,
                [Guid.NewGuid()])));
    }

    [Fact]
    public async Task SetLockStateBatchAsync_rejects_a_locked_summary()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Utc);
        dbContext.PayrollDeductionSummaryRecords.Add(new PayrollDeductionSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 7,
            PayrollYear = 2026,
            IsLocked = true,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "test-user"
        });
        dbContext.PayrollDeductionTaxRecords.Add(new PayrollDeductionTaxRecordRow
        {
            PayrollDeductionSummaryRecordId = summaryId,
            CreatedAtUtc = createdAtUtc
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<PayrollPersonalIncomeTaxDeductionConflictException>(() =>
            CreateLockService(dbContext).SetLockStateBatchAsync(
                new SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest(
                    2026,
                    7,
                    IsLocked: true,
                    PayrollPersonalIncomeTaxDeductionLockActionScope.WholePeriod)));
    }

    private static DatabasePayrollPersonalIncomeTaxDeductionRefreshService CreateRefreshService(ApplicationDbContext dbContext) =>
        new(dbContext, new TestAuditScope(), new TestAuditedMutation(dbContext), new PayrollPersonalIncomeTaxDeductionPeriodPolicy(), new PayrollPersonalIncomeTaxDeductionRefreshPolicy());

    private static DatabasePayrollPersonalIncomeTaxDeductionManualAdjustmentService CreateManualAdjustmentService(ApplicationDbContext dbContext) =>
        new(dbContext, new TestAuditScope(), new TestAuditedMutation(dbContext), new PayrollPersonalIncomeTaxDeductionManualValuePolicy());

    private static DatabasePayrollPersonalIncomeTaxDeductionLockService CreateLockService(ApplicationDbContext dbContext) =>
        new(dbContext, new TestAuditScope(), new TestAuditedMutation(dbContext), new PayrollPersonalIncomeTaxDeductionPeriodPolicy());

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"personal-income-tax-deduction-{Guid.NewGuid():N}")
            .Options);

    private sealed class TestAuditScope : IAuditScope
    {
        public AuditCommand? Current { get; } = new(
            Guid.NewGuid(),
            AuditActions.PersonalIncomeTaxDeduction.ManualValueUpdated,
            new AuditActor("test-user", "Test User", AuditActorKind.User, AuditSource.Api),
            "personal-income-tax-deduction-test");

        public IDisposable Begin(AuditCommand command) => new NoOpDisposable();
        public void RefineAction(string finalAction) { }
        public void SetOperationOutcome(AuditOperationOutcome outcome) { }
    }

    private sealed class TestAuditedMutation(ApplicationDbContext dbContext) : IAuditedMutation
    {
        public async Task<T> ExecuteAsync<T>(
            AuditCommand command,
            Func<CancellationToken, Task<T>> mutation,
            Func<T, AuditOperationEvent> eventFactory,
            CancellationToken cancellationToken = default)
        {
            var result = await mutation(cancellationToken);
            _ = eventFactory(result);
            await dbContext.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
