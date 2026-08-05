using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruTongHop;

public sealed class PayrollDeductionSummaryPeriodRecalculationTests
{
    [Fact]
    public async Task Recalculate_period_updates_open_snapshots_preserves_details_and_skips_locked_rows()
    {
        await using var dbContext = CreateDbContext();
        var openSummary = CreateSummary();
        openSummary.SocialInsuranceDeductionAmount = 1m;
        openSummary.PersonalIncomeTaxDeductionAmount = 2m;
        openSummary.UnionFeeDeductionAmount = 3m;
        openSummary.AdvanceDeductionAmount = 4m;
        openSummary.OtherDeductionAmount = 5m;
        var lockedSummary = CreateSummary();
        lockedSummary.IsLocked = true;
        lockedSummary.OtherDeductionAmount = 900m;
        dbContext.PayrollDeductionSummaryRecords.AddRange(openSummary, lockedSummary);
        dbContext.PayrollDeductionInsuranceRecords.Add(new PayrollDeductionInsuranceRecordRow
        {
            PayrollDeductionSummaryRecordId = openSummary.Id,
            TotalDeductionAmount = 100m,
            CreatedAtUtc = openSummary.CreatedAtUtc
        });
        dbContext.PayrollDeductionTaxRecords.Add(new PayrollDeductionTaxRecordRow
        {
            PayrollDeductionSummaryRecordId = openSummary.Id,
            DeductionAmount = 200m,
            CreatedAtUtc = openSummary.CreatedAtUtc
        });
        dbContext.PayrollDeductionOtherRecords.Add(new PayrollDeductionOtherRecordRow
        {
            PayrollDeductionSummaryRecordId = openSummary.Id,
            DeductionAmount = 500m,
            CreatedAtUtc = openSummary.CreatedAtUtc
        });
        await dbContext.SaveChangesAsync();

        var auditedMutation = new SavingAuditedMutation(dbContext);
        var service = new DatabasePayrollDeductionSummaryRefreshService(
            dbContext,
            new TestAuditScope(),
            auditedMutation,
            new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext));

        var result = await service.RecalculatePeriodAsync(
            new RecalculatePayrollDeductionSummaryPeriodRequest(2026, 6, "tester"));

        var savedOpenSummary = await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == openSummary.Id);
        var savedLockedSummary = await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == lockedSummary.Id);
        var savedOtherDetail = await dbContext.PayrollDeductionOtherRecords.SingleAsync(
            row => row.PayrollDeductionSummaryRecordId == openSummary.Id);
        Assert.Equal(2, result.TargetRowCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(0, result.UnchangedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(2, result.MissingSourceCount);
        Assert.Equal(100m, savedOpenSummary.SocialInsuranceDeductionAmount);
        Assert.Equal(200m, savedOpenSummary.PersonalIncomeTaxDeductionAmount);
        Assert.Equal(0m, savedOpenSummary.UnionFeeDeductionAmount);
        Assert.Equal(0m, savedOpenSummary.AdvanceDeductionAmount);
        Assert.Equal(500m, savedOpenSummary.OtherDeductionAmount);
        Assert.Equal(500m, savedOtherDetail.DeductionAmount);
        Assert.Equal(900m, savedLockedSummary.OtherDeductionAmount);
        Assert.Equal(AuditActions.DeductionSummary.PeriodRecalculated, auditedMutation.LastEvent?.Action);
        Assert.Equal("existing-summary-records", auditedMutation.LastEvent?.Metadata?["scope"]);
    }

    [Fact]
    public async Task Recalculate_period_reports_unchanged_rows_without_mutating_them()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary();
        summary.OtherDeductionAmount = 500m;
        dbContext.PayrollDeductionSummaryRecords.Add(summary);
        dbContext.PayrollDeductionOtherRecords.Add(new PayrollDeductionOtherRecordRow
        {
            PayrollDeductionSummaryRecordId = summary.Id,
            DeductionAmount = 500m,
            CreatedAtUtc = summary.CreatedAtUtc
        });
        await dbContext.SaveChangesAsync();

        var service = new DatabasePayrollDeductionSummaryRefreshService(
            dbContext,
            new TestAuditScope(),
            new SavingAuditedMutation(dbContext),
            new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext));

        var result = await service.RecalculatePeriodAsync(
            new RecalculatePayrollDeductionSummaryPeriodRequest(2026, 6, "tester"));

        var savedSummary = await dbContext.PayrollDeductionSummaryRecords.SingleAsync(row => row.Id == summary.Id);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(1, result.UnchangedCount);
        Assert.Equal(4, result.MissingSourceCount);
        Assert.Null(savedSummary.UpdatedAtUtc);
    }

    [Fact]
    public async Task Recalculate_period_does_not_create_summary_for_an_orphan_detail_record()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PayrollDeductionOtherRecords.Add(new PayrollDeductionOtherRecordRow
        {
            PayrollDeductionSummaryRecordId = Guid.NewGuid(),
            DeductionAmount = 500m,
            CreatedAtUtc = new DateTime(2026, 6, 30, 8, 0, 0)
        });
        await dbContext.SaveChangesAsync();

        var service = new DatabasePayrollDeductionSummaryRefreshService(
            dbContext,
            new TestAuditScope(),
            new SavingAuditedMutation(dbContext),
            new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext));

        var result = await service.RecalculatePeriodAsync(
            new RecalculatePayrollDeductionSummaryPeriodRequest(2026, 6, "tester"));

        Assert.Equal(0, result.TargetRowCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Empty(await dbContext.PayrollDeductionSummaryRecords.ToListAsync());
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-deduction-summary-period-recalculation-{Guid.NewGuid():N}")
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
