using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruTongHop;

public sealed class PayrollDeductionSummaryBatchLockStateTests
{
    [Fact]
    public async Task Batch_lock_normalizes_duplicate_selected_ids_and_writes_audit_metadata()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        dbContext.PayrollDeductionSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();

        var auditedMutation = new SavingAuditedMutation(dbContext);
        var service = CreateService(dbContext, auditedMutation);

        var result = await service.SetLockStateBatchAsync(
            new SetPayrollDeductionSummaryBatchLockStateRequest(
                2026,
                6,
                true,
                [summary.Id, summary.Id],
                "tester"));

        var savedSummary = await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == summary.Id);
        Assert.True(savedSummary.IsLocked);
        Assert.Equal(1, result.TargetRowCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(AuditActions.DeductionSummary.BatchLockStateChanged, auditedMutation.LastEvent?.Action);
        Assert.Equal("selected-rows", auditedMutation.LastEvent?.Metadata?["scope"]);
        Assert.Equal("True", auditedMutation.LastEvent?.Metadata?["isLocked"]);
    }

    [Fact]
    public async Task Batch_lock_rejects_selected_id_outside_requested_period_without_partial_mutation()
    {
        await using var dbContext = CreateDbContext();
        var inPeriod = CreateSummary();
        var outsidePeriod = CreateSummary();
        outsidePeriod.PayrollMonth = 7;
        dbContext.PayrollDeductionSummaryRecords.AddRange(inPeriod, outsidePeriod);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new SavingAuditedMutation(dbContext));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetLockStateBatchAsync(
                new SetPayrollDeductionSummaryBatchLockStateRequest(
                    2026,
                    6,
                    true,
                    [inPeriod.Id, outsidePeriod.Id],
                    "tester")));

        Assert.Contains("không tồn tại hoặc không thuộc kỳ lương", exception.Message);
        Assert.False((await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == inPeriod.Id)).IsLocked);
        Assert.False((await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == outsidePeriod.Id)).IsLocked);
    }

    [Fact]
    public async Task Batch_lock_is_idempotent_for_rows_already_in_target_state()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        summary.IsLocked = true;
        dbContext.PayrollDeductionSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new SavingAuditedMutation(dbContext));

        var result = await service.SetLockStateBatchAsync(
            new SetPayrollDeductionSummaryBatchLockStateRequest(2026, 6, true, null, "tester"));

        Assert.Equal(1, result.TargetRowCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(1, result.SkippedCount);
    }

    private static DatabasePayrollDeductionSummaryLockService CreateService(
        ApplicationDbContext dbContext,
        SavingAuditedMutation auditedMutation) =>
        new(dbContext, new TestAuditScope(), auditedMutation, new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext));

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-deduction-summary-batch-lock-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PayrollDeductionSummaryRecordRow CreateSummary() => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = Guid.NewGuid(),
        PayrollMonth = 6,
        PayrollYear = 2026,
        CreatedAtUtc = new DateTime(2026, 6, 30, 8, 0, 0),
        CreatedBy = "tester"
    };

    private sealed class TestAuditScope : IAuditScope
    {
        public AuditCommand? Current => null;

        public IDisposable Begin(AuditCommand command) => NoopDisposable.Instance;

        public void RefineAction(string finalAction)
        {
        }

        public void SetOperationOutcome(AuditOperationOutcome outcome)
        {
        }
    }

    private sealed class SavingAuditedMutation(ApplicationDbContext dbContext) : IAuditedMutation
    {
        public AuditOperationEvent? LastEvent { get; private set; }

        public async Task<T> ExecuteAsync<T>(
            AuditCommand command,
            Func<CancellationToken, Task<T>> mutation,
            Func<T, AuditOperationEvent> eventFactory,
            CancellationToken cancellationToken = default)
        {
            var result = await mutation(cancellationToken);
            LastEvent = eventFactory(result);
            await dbContext.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
