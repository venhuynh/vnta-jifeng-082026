using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruKhac;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruKhac;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruKhac;

public sealed class DatabasePayrollEmployeeOtherDeductionAllowanceServiceTests
{
    [Fact]
    public async Task PreparePeriodAsync_creates_only_missing_detail_rows_and_preserves_existing_manual_values()
    {
        await using var dbContext = CreateDbContext();
        var existingSummaryId = Guid.NewGuid();
        var missingSummaryId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);
        dbContext.PayrollDeductionSummaryRecords.AddRange(
            CreateSummary(existingSummaryId, 125_000m, createdAtUtc),
            CreateSummary(missingSummaryId, 250_000m, createdAtUtc));
        dbContext.PayrollDeductionOtherRecords.Add(new PayrollDeductionOtherRecordRow
        {
            PayrollDeductionSummaryRecordId = existingSummaryId,
            DeductionAmount = 75_000m,
            Note = "Giữ nguyên điều chỉnh thủ công",
            IsLocked = true,
            CreatedAtUtc = createdAtUtc
        });
        await dbContext.SaveChangesAsync();

        await CreateService(dbContext).PreparePeriodAsync(2026, 7);

        var rows = await dbContext.PayrollDeductionOtherRecords
            .OrderBy(row => row.PayrollDeductionSummaryRecordId)
            .ToArrayAsync();
        Assert.Equal(2, rows.Length);
        var existingRow = Assert.Single(rows, row => row.PayrollDeductionSummaryRecordId == existingSummaryId);
        Assert.Equal(75_000m, existingRow.DeductionAmount);
        Assert.Equal("Giữ nguyên điều chỉnh thủ công", existingRow.Note);
        Assert.True(existingRow.IsLocked);
        var createdRow = Assert.Single(rows, row => row.PayrollDeductionSummaryRecordId == missingSummaryId);
        Assert.Equal(250_000m, createdRow.DeductionAmount);
        Assert.Null(createdRow.Note);
        Assert.False(createdRow.IsLocked);
        Assert.Equal(DateTimeKind.Unspecified, createdRow.CreatedAtUtc.Kind);
    }

    [Fact]
    public async Task PreparePeriodAsync_is_idempotent_for_the_same_payroll_period()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        dbContext.PayrollDeductionSummaryRecords.Add(CreateSummary(summaryId, 180_000m, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.PreparePeriodAsync(2026, 7);
        await service.PreparePeriodAsync(2026, 7);

        var row = Assert.Single(await dbContext.PayrollDeductionOtherRecords.ToArrayAsync());
        Assert.Equal(summaryId, row.PayrollDeductionSummaryRecordId);
        Assert.Equal(180_000m, row.DeductionAmount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.001)]
    public async Task UpdateManualValuesAsync_rejects_invalid_deduction_amount(decimal amount)
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateManualValuesAsync(
            new UpdatePayrollEmployeeOtherDeductionAllowanceManualValuesRequest(
                Guid.NewGuid(), amount, null, DateTime.UtcNow)));
    }

    [Fact]
    public async Task UpdateManualValuesAsync_rejects_a_missing_concurrency_token()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<PayrollEmployeeOtherDeductionConflictException>(() => service.UpdateManualValuesAsync(
            new UpdatePayrollEmployeeOtherDeductionAllowanceManualValuesRequest(
                Guid.NewGuid(), 1m, null, null)));
    }

    [Fact]
    public async Task UpdateManualValuesAsync_rejects_locked_or_stale_record()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);
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
        dbContext.PayrollDeductionOtherRecords.Add(new PayrollDeductionOtherRecordRow
        {
            PayrollDeductionSummaryRecordId = summaryId,
            CreatedAtUtc = createdAtUtc
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<PayrollEmployeeOtherDeductionConflictException>(() => CreateService(dbContext).UpdateManualValuesAsync(
            new UpdatePayrollEmployeeOtherDeductionAllowanceManualValuesRequest(
                summaryId, 1m, null, createdAtUtc.AddMinutes(-1))));
    }

    private static DatabasePayrollEmployeeOtherDeductionAllowanceService CreateService(ApplicationDbContext dbContext) =>
        new(dbContext, new TestAuditScope(), new TestAuditedMutation(dbContext));

    private static PayrollDeductionSummaryRecordRow CreateSummary(
        Guid id,
        decimal otherDeductionAmount,
        DateTime createdAtUtc) =>
        new()
        {
            Id = id,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 7,
            PayrollYear = 2026,
            OtherDeductionAmount = otherDeductionAmount,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "test-user"
        };

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"other-deduction-{Guid.NewGuid():N}")
            .Options);

    private sealed class TestAuditScope : IAuditScope
    {
        public AuditCommand? Current { get; } = new(
            Guid.NewGuid(),
            AuditActions.OtherDeduction.ManualValueUpdated,
            new AuditActor("test-user", "Test User", AuditActorKind.User, AuditSource.Api),
            "other-deduction-test");

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
