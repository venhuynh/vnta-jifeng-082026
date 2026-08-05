using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruTongHop;

public sealed class PayrollDeductionSummaryRefreshTests
{
    [Fact]
    public async Task Refresh_reconciles_parent_snapshot_and_preserves_manual_other_detail()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        summary.SocialInsuranceDeductionAmount = 100m;
        summary.PersonalIncomeTaxDeductionAmount = 200m;
        summary.UnionFeeDeductionAmount = 300m;
        summary.AdvanceDeductionAmount = 400m;
        summary.OtherDeductionAmount = 500m;
        dbContext.PayrollDeductionSummaryRecords.Add(summary);
        dbContext.PayrollDeductionOtherRecords.Add(new PayrollDeductionOtherRecordRow
        {
            PayrollDeductionSummaryRecordId = summary.Id,
            DeductionAmount = 125000.5m,
            CreatedAtUtc = summary.CreatedAtUtc
        });
        await dbContext.SaveChangesAsync();

        var auditedMutation = new SavingAuditedMutation(dbContext);
        var service = new DatabasePayrollDeductionSummaryRefreshService(
            dbContext,
            new TestAuditScope(),
            auditedMutation,
            new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext));

        var result = await service.RefreshAsync(new RefreshPayrollDeductionSummaryRequest(
            summary.Id,
            summary.PayrollYear,
            summary.PayrollMonth,
            summary.CreatedAtUtc,
            "tester"));

        var savedSummary = await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == summary.Id);
        var savedOther = await dbContext.PayrollDeductionOtherRecords.SingleAsync(
            row => row.PayrollDeductionSummaryRecordId == summary.Id);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(4, result.MissingSourceCount);
        Assert.Equal(0m, savedSummary.SocialInsuranceDeductionAmount);
        Assert.Equal(0m, savedSummary.PersonalIncomeTaxDeductionAmount);
        Assert.Equal(0m, savedSummary.UnionFeeDeductionAmount);
        Assert.Equal(0m, savedSummary.AdvanceDeductionAmount);
        Assert.Equal(125000.5m, savedSummary.OtherDeductionAmount);
        Assert.Equal(125000.5m, savedOther.DeductionAmount);
        Assert.Equal(AuditActions.DeductionSummary.Refreshed, auditedMutation.LastAction);
    }

    [Fact]
    public async Task Refresh_skips_locked_summary_without_mutation()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        summary.IsLocked = true;
        summary.OtherDeductionAmount = 500m;
        dbContext.PayrollDeductionSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();

        var service = new DatabasePayrollDeductionSummaryRefreshService(
            dbContext,
            new TestAuditScope(),
            new SavingAuditedMutation(dbContext),
            new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext));

        var result = await service.RefreshAsync(new RefreshPayrollDeductionSummaryRequest(
            summary.Id,
            summary.PayrollYear,
            summary.PayrollMonth,
            summary.CreatedAtUtc));

        var savedSummary = await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == summary.Id);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(500m, savedSummary.OtherDeductionAmount);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-deduction-summary-refresh-{Guid.NewGuid():N}")
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
