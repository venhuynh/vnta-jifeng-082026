using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Queries;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTongHop.Queries;

public sealed class PayrollAllowanceSummaryReadAndExportTests
{
    [Fact]
    public async Task Search_and_overview_apply_period_lock_filter_and_paging_to_the_same_population()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PayrollAllowanceSummaryRecords.AddRange(
            CreateSummary(isLocked: true, amount: 10m),
            CreateSummary(isLocked: true, amount: 20m),
            CreateSummary(isLocked: false, amount: 30m),
            CreateSummary(isLocked: true, amount: 40m, month: 8));
        await dbContext.SaveChangesAsync();
        var service = CreatePersistence(dbContext);
        var filter = new PayrollAllowanceSummaryFilter(7, 2026, null, IsLocked: true, Skip: 1, Take: 1);

        var page = await service.SearchAsync(filter);
        var overview = await service.GetSummaryAsync(new PayrollAllowanceSummaryFilter(7, 2026, null, IsLocked: true));

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Rows);
        Assert.Equal(2, overview.TotalCount);
        Assert.Equal(0, overview.OpenCount);
        Assert.Equal(2, overview.LockedCount);
        Assert.Equal(240m, overview.TotalAllowanceAmount);
    }

    [Fact]
    public async Task Search_rejects_period_before_the_feature_data_boundary()
    {
        await using var dbContext = CreateDbContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreatePersistence(dbContext).SearchAsync(new PayrollAllowanceSummaryFilter(5, 2026, null)));

        Assert.Contains("06/2026", exception.Message);
    }

    [Fact]
    public async Task Export_returns_the_whole_selected_period_with_total_and_spreadsheet_safe_text()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PayrollAllowanceSummaryRecords.AddRange(
            CreateSummary(isLocked: false, amount: 5m, note: "  =formula  "),
            CreateSummary(isLocked: false, amount: 99m, month: 8));
        await dbContext.SaveChangesAsync();
        var persistence = CreatePersistence(dbContext);

        var rows = await persistence.ExportAsync(
            new PayrollAllowanceSummaryExportRequest(2026, 7, PayrollAllowanceSummaryExportFormat.Excel));

        var row = Assert.Single(rows);
        Assert.Equal(40m, row.TotalAllowanceAmount);
        Assert.Equal("'=formula", row.Note);
        Assert.Equal(7, row.PayrollMonth);
        Assert.Equal(2026, row.PayrollYear);
    }

    [Fact]
    public async Task Dashboard_comparisons_stop_at_the_selected_month()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PayrollAllowanceSummaryRecords.AddRange(
            Enumerable.Range(1, 8).Select(month => CreateSummary(false, month * 10m, month)));
        await dbContext.SaveChangesAsync();

        var dashboard = await CreatePersistence(dbContext).GetDashboardAsync(
            new PayrollAllowanceDashboardFilter(7, 2026));

        Assert.Equal(7, dashboard.Trend.Count);
        Assert.All(dashboard.AllowanceMonthlyComparison, row => Assert.Equal(7, row.Months.Count));
        Assert.All(dashboard.DepartmentMonthlyComparison, row => Assert.Equal(7, row.Months.Count));
    }

    private static PayrollAllowanceSummaryPersistence CreatePersistence(ApplicationDbContext dbContext) =>
        new(dbContext, new TestAuditScope(), new PassThroughAuditedMutation());

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-allowance-summary-read-{Guid.NewGuid():N}")
            .Options);

    private static PayrollAllowanceSummaryRecordRow CreateSummary(bool isLocked, decimal amount, int month = 7, string? note = null) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = Guid.NewGuid(),
        PayrollMonth = (short)month,
        PayrollYear = 2026,
        ResponsibilityAllowanceAmount = amount,
        ResponsibilityOtherAllowanceAmount = amount,
        SeniorityAllowanceAmount = amount,
        AttendanceAllowanceAmount = amount,
        MealAllowanceAmount = amount,
        HazardAllowanceAmount = amount,
        OtherAllowanceAmount = amount,
        LeaveHolidayAllowanceAmount = amount,
        IsLocked = isLocked,
        Note = note,
        CreatedAtUtc = new DateTime(2026, 7, 1),
        CreatedBy = "tester"
    };

    private sealed class TestAuditScope : IAuditScope
    {
        public AuditCommand? Current => null;
        public IDisposable Begin(AuditCommand command) => NoopDisposable.Instance;
        public void RefineAction(string finalAction) { }
        public void SetOperationOutcome(AuditOperationOutcome outcome) { }
    }

    private sealed class PassThroughAuditedMutation : IAuditedMutation
    {
        public Task<T> ExecuteAsync<T>(AuditCommand command, Func<CancellationToken, Task<T>> mutation, Func<T, AuditOperationEvent> eventFactory, CancellationToken cancellationToken = default) =>
            mutation(cancellationToken);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();
        public void Dispose() { }
    }
}
