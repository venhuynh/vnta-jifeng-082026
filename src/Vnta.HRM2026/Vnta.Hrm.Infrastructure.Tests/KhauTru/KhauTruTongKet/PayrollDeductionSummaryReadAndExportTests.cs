using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruTongHop;

public sealed class PayrollDeductionSummaryReadAndExportTests
{
    [Fact]
    public async Task Search_filters_period_and_lock_state_pages_rows_and_summarizes_all_matching_rows()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PayrollDeductionSummaryRecords.AddRange(
            CreateSummary("00000000-0000-0000-0000-000000000001", 7, false, 1m),
            CreateSummary("00000000-0000-0000-0000-000000000002", 7, false, 2m),
            CreateSummary("00000000-0000-0000-0000-000000000003", 7, true, 50m),
            CreateSummary("00000000-0000-0000-0000-000000000004", 8, false, 99m));
        await dbContext.SaveChangesAsync();

        var page = await CreateService(dbContext).SearchAsync(
            new PayrollDeductionSummaryFilter(7, 2026, null, IsLocked: false, Take: 1, Skip: 1));

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(new PayrollDeductionSummaryLockStatusCountsDto(3, 2, 1), page.LockStatusCounts);
        Assert.Equal(15m, page.Totals.TotalDeductionAmount);
        var row = Assert.Single(page.Rows);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000002"), row.Id);
        Assert.Equal(2m, row.OtherDeductionAmount);
    }

    [Fact]
    public async Task Search_clamps_invalid_page_size_and_returns_an_empty_page_beyond_the_filtered_result_set()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PayrollDeductionSummaryRecords.Add(CreateSummary("00000000-0000-0000-0000-000000000001", 7, false, 2m));
        await dbContext.SaveChangesAsync();

        var firstPage = await CreateService(dbContext).SearchAsync(
            new PayrollDeductionSummaryFilter(7, 2026, null, Take: 0, Skip: -5));
        var beyondEnd = await CreateService(dbContext).SearchAsync(
            new PayrollDeductionSummaryFilter(7, 2026, null, Take: 50, Skip: 1));

        Assert.Single(firstPage.Rows);
        Assert.Equal(1, firstPage.TotalCount);
        Assert.Empty(beyondEnd.Rows);
        Assert.Equal(10m, beyondEnd.Totals.TotalDeductionAmount);
    }

    [Fact]
    public async Task Export_includes_locked_and_open_period_rows_with_calculated_total_and_operation_audit()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PayrollDeductionSummaryRecords.AddRange(
            CreateSummary("00000000-0000-0000-0000-000000000001", 7, false, 2m),
            CreateSummary("00000000-0000-0000-0000-000000000002", 7, true, 3m),
            CreateSummary("00000000-0000-0000-0000-000000000003", 8, false, 99m));
        await dbContext.SaveChangesAsync();
        var auditedMutation = new RecordingAuditedMutation();

        var rows = await new DatabasePayrollDeductionSummaryReadService(
            dbContext, new EmptyAuditScope(), auditedMutation).ExportPeriodAsync(
            7, 2026, PayrollDeductionSummaryExportFormat.Excel);

        Assert.Equal(2, rows.Count);
        Assert.Equal(10m, rows[0].TotalDeductionAmount);
        Assert.Equal("Đang mở", rows[0].LockStatusText);
        Assert.Equal(15m, rows[1].TotalDeductionAmount);
        Assert.Equal("Đã khóa", rows[1].LockStatusText);
        Assert.Equal(AuditActions.DeductionSummary.Exported, auditedMutation.Event?.Action);
        Assert.Equal("2", auditedMutation.Event?.Metadata?["rowCount"]);
        Assert.Equal("Excel", auditedMutation.Event?.Metadata?["format"]);
    }

    private static DatabasePayrollDeductionSummaryReadService CreateService(ApplicationDbContext dbContext) =>
        new(dbContext, new EmptyAuditScope(), new RecordingAuditedMutation());

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-deduction-summary-read-{Guid.NewGuid():N}")
            .Options);

    private static PayrollDeductionSummaryRecordRow CreateSummary(string id, short month, bool isLocked, decimal amount) => new()
    {
        Id = Guid.Parse(id),
        EmployeeId = Guid.NewGuid(),
        PayrollMonth = month,
        PayrollYear = 2026,
        SocialInsuranceDeductionAmount = amount,
        PersonalIncomeTaxDeductionAmount = amount,
        UnionFeeDeductionAmount = amount,
        AdvanceDeductionAmount = amount,
        OtherDeductionAmount = amount,
        IsLocked = isLocked,
        CreatedAtUtc = new DateTime(2026, 7, 31, 8, 0, 0),
        CreatedBy = "seed"
    };

    private sealed class EmptyAuditScope : IAuditScope
    {
        public AuditCommand? Current => null;
        public IDisposable Begin(AuditCommand command) => NoopDisposable.Instance;
        public void RefineAction(string finalAction) { }
        public void SetOperationOutcome(AuditOperationOutcome outcome) { }
    }

    private sealed class RecordingAuditedMutation : IAuditedMutation
    {
        public AuditOperationEvent? Event { get; private set; }

        public async Task<T> ExecuteAsync<T>(AuditCommand command, Func<CancellationToken, Task<T>> mutation,
            Func<T, AuditOperationEvent> eventFactory, CancellationToken cancellationToken = default)
        {
            var result = await mutation(cancellationToken);
            Event = eventFactory(result);
            return result;
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();
        public void Dispose() { }
    }
}
