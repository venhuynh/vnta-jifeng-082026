using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruTongHop;

public sealed class PayrollDeductionSummaryPeriodTests
{
    [Fact]
    public async Task Search_rejects_period_before_June_2026()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateReadService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SearchAsync(new PayrollDeductionSummaryFilter(5, 2026, null)));

        Assert.Contains("06/2026", exception.Message);
    }

    [Fact]
    public async Task Sync_from_previous_month_rejects_target_before_June_2026()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateSyncService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SyncFromPreviousMonthAsync(new SyncPayrollDeductionSummaryFromPreviousMonthRequest(5, 2026, "test")));

        Assert.Contains("06/2026", exception.Message);
    }

    [Fact]
    public async Task Search_accepts_June_2026()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateReadService(dbContext);

        var records = await service.SearchAsync(new PayrollDeductionSummaryFilter(6, 2026, null));

        Assert.Empty(records.Rows);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-deduction-summary-period-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static DatabasePayrollDeductionSummaryReadService CreateReadService(ApplicationDbContext dbContext) =>
        new(dbContext, new TestAuditScope(), new PassThroughAuditedMutation());

    private static DatabasePayrollDeductionSummarySyncService CreateSyncService(ApplicationDbContext dbContext) =>
        new(dbContext, new TestAuditScope(), new PassThroughAuditedMutation(), new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext));

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
