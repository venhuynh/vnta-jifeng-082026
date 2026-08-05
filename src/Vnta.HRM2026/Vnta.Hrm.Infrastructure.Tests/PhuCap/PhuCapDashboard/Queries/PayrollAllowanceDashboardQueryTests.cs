using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapDashboard.Queries;

public sealed class PayrollAllowanceDashboardQueryTests
{
    [Fact]
    public async Task Dashboard_returns_current_previous_kpis_and_breakdown_for_selected_period()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"dashboard-query-{Guid.NewGuid():N}").Options);
        db.PayrollAllowanceSummaryRecords.AddRange(
            CreateSummary(2026, 6, 10m, true),
            CreateSummary(2026, 7, 25m, false),
            CreateSummary(2026, 7, 5m, true));
        await db.SaveChangesAsync();

        var result = await new PayrollAllowanceSummaryPersistence(db, new TestAuditScope(), new PassThroughMutation())
            .GetDashboardAsync(new PayrollAllowanceDashboardFilter(7, 2026));

        Assert.Equal(2, result.Overview.TotalCount);
        Assert.Equal(1, result.Overview.OpenCount);
        Assert.Equal(1, result.Overview.LockedCount);
        Assert.Equal(240m, result.Overview.TotalAllowanceAmount);
        Assert.Equal(1, result.PreviousPeriodOverview.TotalCount);
        Assert.Equal(80m, result.PreviousPeriodOverview.TotalAllowanceAmount);
        Assert.Equal(8, result.AllowanceBreakdown.Count);
        Assert.Equal(30m, result.AllowanceBreakdown.Single(x => x.AllowanceType == "Trách nhiệm").Amount);
    }

    private static PayrollAllowanceSummaryRecordRow CreateSummary(int year, int month, decimal amount, bool locked) => new()
    {
        Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), PayrollYear = (short)year, PayrollMonth = (short)month,
        ResponsibilityAllowanceAmount = amount, ResponsibilityOtherAllowanceAmount = amount,
        SeniorityAllowanceAmount = amount, AttendanceAllowanceAmount = amount, MealAllowanceAmount = amount,
        HazardAllowanceAmount = amount, OtherAllowanceAmount = amount, LeaveHolidayAllowanceAmount = amount,
        IsLocked = locked, CreatedAtUtc = new DateTime(2026, 7, 1), CreatedBy = "test"
    };

    private sealed class TestAuditScope : IAuditScope
    {
        public AuditCommand? Current => null;
        public IDisposable Begin(AuditCommand command) => Noop.Instance;
        public void RefineAction(string finalAction) { }
        public void SetOperationOutcome(AuditOperationOutcome outcome) { }
    }
    private sealed class PassThroughMutation : IAuditedMutation
    {
        public Task<T> ExecuteAsync<T>(AuditCommand command, Func<CancellationToken, Task<T>> mutation, Func<T, AuditOperationEvent> eventFactory, CancellationToken cancellationToken = default) => mutation(cancellationToken);
    }
    private sealed class Noop : IDisposable { public static Noop Instance { get; } = new(); public void Dispose() { } }
}
