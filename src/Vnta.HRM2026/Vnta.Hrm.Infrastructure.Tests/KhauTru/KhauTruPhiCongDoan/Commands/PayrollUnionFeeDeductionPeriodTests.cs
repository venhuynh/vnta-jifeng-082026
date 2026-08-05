using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruPhiCongDoan;

public sealed class PayrollUnionFeeDeductionPeriodTests
{
    [Fact]
    public async Task Prepare_period_creates_only_missing_details_from_summary_roster()
    {
        await using var dbContext = CreateDbContext();
        var summaryWithMissingDetail = CreateSummary(125_000m, isLocked: false);
        var summaryWithExistingDetail = CreateSummary(200_000m, isLocked: true);
        dbContext.PayrollDeductionSummaryRecords.AddRange(summaryWithMissingDetail, summaryWithExistingDetail);
        dbContext.PayrollDeductionUnionFeeRecords.Add(new PayrollDeductionUnionFeeRecordRow
        {
            PayrollDeductionSummaryRecordId = summaryWithExistingDetail.Id,
            DeductionAmount = 75_000m,
            IsLocked = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).PreparePeriodAsync(2026, 6);

        Assert.Equal(2, result.SummaryCount);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.ExistingCount);
        Assert.Equal(1, result.LockedSummaryCount);

        var details = await dbContext.PayrollDeductionUnionFeeRecords
            .OrderBy(item => item.PayrollDeductionSummaryRecordId)
            .ToListAsync();
        Assert.Equal(2, details.Count);
        Assert.Equal(125_000m, details.Single(item => item.PayrollDeductionSummaryRecordId == summaryWithMissingDetail.Id).DeductionAmount);
        Assert.Equal(75_000m, details.Single(item => item.PayrollDeductionSummaryRecordId == summaryWithExistingDetail.Id).DeductionAmount);
    }

    [Fact]
    public async Task Prepare_period_rejects_period_before_June_2026()
    {
        await using var dbContext = CreateDbContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(dbContext).PreparePeriodAsync(2026, 5));

        Assert.Contains("06/2026", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.001)]
    public async Task UpdateManualValueAsync_rejects_invalid_deduction_amount(decimal amount)
    {
        await using var dbContext = CreateDbContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(dbContext).UpdateManualValueAsync(
            new UpdatePayrollUnionFeeDeductionManualValueRequest(Guid.NewGuid(), amount, DateTime.UtcNow)));
    }

    [Fact]
    public async Task UpdateManualValueAsync_rejects_a_locked_summary()
    {
        await using var dbContext = CreateDbContext();
        var createdAtUtc = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Unspecified);
        var summary = new PayrollDeductionSummaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 7,
            PayrollYear = 2026,
            IsLocked = true,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "test"
        };
        dbContext.PayrollDeductionSummaryRecords.Add(summary);
        dbContext.PayrollDeductionUnionFeeRecords.Add(new PayrollDeductionUnionFeeRecordRow
        {
            PayrollDeductionSummaryRecordId = summary.Id,
            CreatedAtUtc = createdAtUtc
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<PayrollUnionFeeDeductionConflictException>(() => CreateService(dbContext).UpdateManualValueAsync(
            new UpdatePayrollUnionFeeDeductionManualValueRequest(summary.Id, 1m, createdAtUtc)));
    }

    [Fact]
    public async Task UpdateManualValueAsync_rejects_a_stale_detail_version()
    {
        await using var dbContext = CreateDbContext();
        var createdAtUtc = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Unspecified);
        var summary = new PayrollDeductionSummaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 7,
            PayrollYear = 2026,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "test"
        };
        dbContext.PayrollDeductionSummaryRecords.Add(summary);
        dbContext.PayrollDeductionUnionFeeRecords.Add(new PayrollDeductionUnionFeeRecordRow
        {
            PayrollDeductionSummaryRecordId = summary.Id,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc.AddMinutes(1)
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<PayrollUnionFeeDeductionConflictException>(() => CreateService(dbContext).UpdateManualValueAsync(
            new UpdatePayrollUnionFeeDeductionManualValueRequest(summary.Id, 1m, createdAtUtc)));
    }

    private static PayrollDeductionSummaryRecordRow CreateSummary(decimal unionFeeAmount, bool isLocked) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = Guid.NewGuid(),
        PayrollMonth = 6,
        PayrollYear = 2026,
        UnionFeeDeductionAmount = unionFeeAmount,
        IsLocked = isLocked,
        CreatedAtUtc = DateTime.UtcNow,
        CreatedBy = "test"
    };

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-union-fee-period-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static DatabasePayrollUnionFeeDeductionCommandService CreateService(ApplicationDbContext dbContext) =>
        new(dbContext, new TestAuditScope(), new PassThroughAuditedMutation());

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

    private sealed class PassThroughAuditedMutation : IAuditedMutation
    {
        public Task<T> ExecuteAsync<T>(
            AuditCommand command,
            Func<CancellationToken, Task<T>> mutation,
            Func<T, AuditOperationEvent> eventFactory,
            CancellationToken cancellationToken = default) => mutation(cancellationToken);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
