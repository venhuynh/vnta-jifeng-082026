using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruTongHop;

public sealed class PayrollDeductionSummaryManualOtherDeductionTests
{
    [Fact]
    public async Task Update_manual_other_deduction_updates_summary_child_and_audit_operation()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        dbContext.PayrollDeductionSummaryRecords.Add(summary);
        dbContext.PayrollDeductionOtherRecords.Add(new PayrollDeductionOtherRecordRow
        {
            PayrollDeductionSummaryRecordId = summary.Id,
            DeductionAmount = 100m,
            CreatedAtUtc = summary.CreatedAtUtc
        });
        await dbContext.SaveChangesAsync();

        var auditedMutation = new SavingAuditedMutation(dbContext);
        var service = new DatabasePayrollDeductionSummaryManualAdjustmentService(
            dbContext,
            new TestAuditScope(),
            auditedMutation,
            new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext));

        var result = await service.UpdateManualOtherDeductionAsync(
            new UpdatePayrollDeductionSummaryManualOtherDeductionRequest(
                summary.Id,
                125000.5m,
                "Điều chỉnh theo quyết định payroll",
                summary.CreatedAtUtc,
                "tester"));

        var savedSummary = await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == summary.Id);
        var savedChild = await dbContext.PayrollDeductionOtherRecords.SingleAsync(
            row => row.PayrollDeductionSummaryRecordId == summary.Id);
        Assert.Equal(125000.5m, result.OtherDeductionAmount);
        Assert.Equal(125000.5m, savedSummary.OtherDeductionAmount);
        Assert.Equal("Điều chỉnh theo quyết định payroll", savedSummary.Note);
        Assert.Equal(125000.5m, savedChild.DeductionAmount);
        Assert.Equal(AuditActions.DeductionSummary.ManualOtherDeductionUpdated, auditedMutation.LastAction);
    }

    [Fact]
    public async Task Update_manual_other_deduction_rejects_stale_version()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        summary.UpdatedAtUtc = summary.CreatedAtUtc.AddMinutes(1);
        dbContext.PayrollDeductionSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();

        var service = new DatabasePayrollDeductionSummaryManualAdjustmentService(
            dbContext,
            new TestAuditScope(),
            new SavingAuditedMutation(dbContext),
            new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext));

        var exception = await Assert.ThrowsAsync<PayrollDeductionSummaryConcurrencyException>(() =>
            service.UpdateManualOtherDeductionAsync(
                new UpdatePayrollDeductionSummaryManualOtherDeductionRequest(
                    summary.Id,
                    1m,
                    null,
                    summary.CreatedAtUtc)));

        Assert.Contains("thay đổi", exception.Message);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-deduction-summary-manual-other-{Guid.NewGuid():N}")
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
        public string? LastAction { get; private set; }

        public async Task<T> ExecuteAsync<T>(
            AuditCommand command,
            Func<CancellationToken, Task<T>> mutation,
            Func<T, AuditOperationEvent> eventFactory,
            CancellationToken cancellationToken = default)
        {
            var result = await mutation(cancellationToken);
            LastAction = eventFactory(result).Action;
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
