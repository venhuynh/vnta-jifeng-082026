using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruTongHop;

public sealed class PayrollDeductionSummarySyncFromPreviousMonthTests
{
    private const short SourceMonth = 6;
    private const short TargetMonth = 7;
    private const short PayrollYear = 2026;

    [Fact]
    public async Task Sync_uses_target_attendance_to_copy_seed_and_remove_summary_snapshots()
    {
        await using var dbContext = CreateDbContext();
        var copiedEmployeeId = Guid.NewGuid();
        var newEmployeeId = Guid.NewGuid();
        var lockedEmployeeId = Guid.NewGuid();
        var obsoleteEmployeeId = Guid.NewGuid();
        AddAttendance(dbContext, copiedEmployeeId);
        AddAttendance(dbContext, newEmployeeId);
        AddAttendance(dbContext, lockedEmployeeId);

        var copiedSource = CreateSummary(copiedEmployeeId, SourceMonth);
        SetAmounts(copiedSource, 100m);
        var lockedSource = CreateSummary(lockedEmployeeId, SourceMonth);
        SetAmounts(lockedSource, 200m);
        var lockedTarget = CreateSummary(lockedEmployeeId, TargetMonth, isLocked: true);
        SetAmounts(lockedTarget, 900m);
        var obsoleteTarget = CreateSummary(obsoleteEmployeeId, TargetMonth, isLocked: true);
        SetAmounts(obsoleteTarget, 300m);
        dbContext.PayrollDeductionSummaryRecords.AddRange(copiedSource, lockedSource, lockedTarget, obsoleteTarget);
        AddAllDetails(dbContext, copiedSource, 100m);
        AddAllDetails(dbContext, lockedSource, 200m);
        AddAllDetails(dbContext, lockedTarget, 900m);
        AddAllDetails(dbContext, obsoleteTarget, 300m);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).SyncFromPreviousMonthAsync(
            new SyncPayrollDeductionSummaryFromPreviousMonthRequest(TargetMonth, PayrollYear, "payroll-admin"));

        var summaries = await dbContext.PayrollDeductionSummaryRecords
            .Where(row => row.PayrollMonth == TargetMonth && row.PayrollYear == PayrollYear)
            .ToDictionaryAsync(row => row.EmployeeId);
        Assert.Equal(3, result.AttendanceEmployeeCount);
        Assert.Equal(2, result.SourceRecordCount);
        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(1, result.RemovedCount);
        Assert.False(summaries.ContainsKey(obsoleteEmployeeId));

        var copiedTarget = summaries[copiedEmployeeId];
        Assert.Equal(100m, copiedTarget.SocialInsuranceDeductionAmount);
        Assert.Equal(100m, copiedTarget.OtherDeductionAmount);
        var emptyTarget = summaries[newEmployeeId];
        Assert.Equal(0m, emptyTarget.TotalDeductionAmount());
        Assert.Equal(900m, summaries[lockedEmployeeId].OtherDeductionAmount);
        Assert.Equal(0, await dbContext.PayrollDeductionSummaryRecords.CountAsync(row => row.EmployeeId == obsoleteEmployeeId));
        Assert.Equal(0, await dbContext.PayrollDeductionOtherRecords.CountAsync(row => row.PayrollDeductionSummaryRecordId == obsoleteTarget.Id));

        foreach(var summary in summaries.Values.Where(row => !row.IsLocked))
        {
            Assert.Equal(1, await dbContext.PayrollDeductionInsuranceRecords.CountAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id));
            Assert.Equal(1, await dbContext.PayrollDeductionTaxRecords.CountAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id));
            Assert.Equal(1, await dbContext.PayrollDeductionUnionFeeRecords.CountAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id));
            Assert.Equal(1, await dbContext.PayrollDeductionAdvanceRecords.CountAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id));
            Assert.Equal(1, await dbContext.PayrollDeductionOtherRecords.CountAsync(row => row.PayrollDeductionSummaryRecordId == summary.Id));
        }
    }

    [Fact]
    public async Task Sync_removes_target_snapshots_when_the_target_period_has_no_attendance()
    {
        await using var dbContext = CreateDbContext();
        var obsoleteTarget = CreateSummary(Guid.NewGuid(), TargetMonth, isLocked: true);
        dbContext.PayrollDeductionSummaryRecords.Add(obsoleteTarget);
        AddAllDetails(dbContext, obsoleteTarget, 100m);
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).SyncFromPreviousMonthAsync(
            new SyncPayrollDeductionSummaryFromPreviousMonthRequest(TargetMonth, PayrollYear, "payroll-admin"));

        Assert.Equal(0, result.AttendanceEmployeeCount);
        Assert.Equal(0, result.SourceRecordCount);
        Assert.Equal(1, result.RemovedCount);
        Assert.Empty(await dbContext.PayrollDeductionSummaryRecords.ToListAsync());
        Assert.Empty(await dbContext.PayrollDeductionInsuranceRecords.ToListAsync());
        Assert.Empty(await dbContext.PayrollDeductionTaxRecords.ToListAsync());
        Assert.Empty(await dbContext.PayrollDeductionUnionFeeRecords.ToListAsync());
        Assert.Empty(await dbContext.PayrollDeductionAdvanceRecords.ToListAsync());
        Assert.Empty(await dbContext.PayrollDeductionOtherRecords.ToListAsync());
    }

    private static DatabasePayrollDeductionSummarySyncService CreateService(ApplicationDbContext dbContext) =>
        new(dbContext, new TestAuditScope(), new PassThroughAuditedMutation(), new DatabasePayrollDeductionSummaryTargetRosterPolicy(dbContext));

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-deduction-summary-sync-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PayrollDeductionSummaryRecordRow CreateSummary(Guid employeeId, short month, bool isLocked = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayrollMonth = month,
            PayrollYear = PayrollYear,
            IsLocked = isLocked,
            CreatedAtUtc = new DateTime(2026, month, 28, 8, 0, 0),
            CreatedBy = "test"
        };

    private static void AddAttendance(ApplicationDbContext dbContext, Guid employeeId) =>
        dbContext.AttendanceWorkdaySummaries.Add(new AttendanceWorkdaySummaryRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            WorkDate = new DateOnly(PayrollYear, TargetMonth, 1),
            DayType = "Ngày thường",
            ComputedAtUtc = new DateTime(2026, TargetMonth, 1, 8, 0, 0),
            CreatedAtUtc = new DateTime(2026, TargetMonth, 1, 8, 0, 0)
        });

    private static void SetAmounts(PayrollDeductionSummaryRecordRow summary, decimal amount)
    {
        summary.SocialInsuranceDeductionAmount = amount;
        summary.PersonalIncomeTaxDeductionAmount = amount;
        summary.UnionFeeDeductionAmount = amount;
        summary.AdvanceDeductionAmount = amount;
        summary.OtherDeductionAmount = amount;
    }

    private static void AddAllDetails(ApplicationDbContext dbContext, PayrollDeductionSummaryRecordRow summary, decimal amount)
    {
        dbContext.PayrollDeductionInsuranceRecords.Add(new PayrollDeductionInsuranceRecordRow
        {
            PayrollDeductionSummaryRecordId = summary.Id,
            TotalDeductionAmount = amount,
            CreatedAtUtc = summary.CreatedAtUtc
        });
        dbContext.PayrollDeductionTaxRecords.Add(new PayrollDeductionTaxRecordRow { PayrollDeductionSummaryRecordId = summary.Id, DeductionAmount = amount, CreatedAtUtc = summary.CreatedAtUtc });
        dbContext.PayrollDeductionUnionFeeRecords.Add(new PayrollDeductionUnionFeeRecordRow { PayrollDeductionSummaryRecordId = summary.Id, DeductionAmount = amount, CreatedAtUtc = summary.CreatedAtUtc });
        dbContext.PayrollDeductionAdvanceRecords.Add(new PayrollDeductionAdvanceRecordRow { PayrollDeductionSummaryRecordId = summary.Id, DeductionAmount = amount, CreatedAtUtc = summary.CreatedAtUtc });
        dbContext.PayrollDeductionOtherRecords.Add(new PayrollDeductionOtherRecordRow { PayrollDeductionSummaryRecordId = summary.Id, DeductionAmount = amount, CreatedAtUtc = summary.CreatedAtUtc });
    }

    private sealed class TestAuditScope : IAuditScope
    {
        public AuditCommand? Current => null;
        public IDisposable Begin(AuditCommand command) => NoopDisposable.Instance;
        public void RefineAction(string finalAction) { }
        public void SetOperationOutcome(AuditOperationOutcome outcome) { }
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
        public void Dispose() { }
    }
}

internal static class PayrollDeductionSummaryRecordRowTestExtensions
{
    public static decimal TotalDeductionAmount(this PayrollDeductionSummaryRecordRow summary) =>
        summary.SocialInsuranceDeductionAmount
        + summary.PersonalIncomeTaxDeductionAmount
        + summary.UnionFeeDeductionAmount
        + summary.AdvanceDeductionAmount
        + summary.OtherDeductionAmount;
}
