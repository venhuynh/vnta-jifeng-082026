using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Persistence;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTongHop;

public sealed class PayrollAllowanceSummaryRefreshTests
{
    private const int PayrollMonth = 7;
    private const int PayrollYear = 2026;

    [Fact]
    public async Task RefreshAsync_rebuilds_all_allowance_amounts_from_details_and_preserves_note()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary(Guid.NewGuid(), Guid.NewGuid(), isLocked: false);
        summary.ResponsibilityAllowanceAmount = 1m;
        summary.ResponsibilityOtherAllowanceAmount = 1m;
        summary.SeniorityAllowanceAmount = 1m;
        summary.AttendanceAllowanceAmount = 1m;
        summary.MealAllowanceAmount = 1m;
        summary.HazardAllowanceAmount = 1m;
        summary.OtherAllowanceAmount = 1m;
        summary.LeaveHolidayAllowanceAmount = 1m;
        summary.Note = "Giữ lại ghi chú tổng hợp";
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        AddAllAllowanceDetails(dbContext, summary, 10m, 20m, 30m, 40m, 50m, 60m, 65m, 70m);
        await dbContext.SaveChangesAsync();

        var result = await CreateRefreshService(dbContext).RefreshAsync(
            new RefreshPayrollAllowanceSummaryRequest(PayrollMonth, PayrollYear, "payroll-admin"));

        var refreshed = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(1, result.SourceEmployeeCount);
        Assert.Equal(1, result.ResponsibilitySourceCount);
        Assert.Equal(1, result.SenioritySourceCount);
        Assert.Equal(1, result.AttendanceSourceCount);
        Assert.Equal(1, result.MealSourceCount);
        Assert.Equal(1, result.HazardSourceCount);
        Assert.Equal(1, result.OtherAllowanceSourceCount);
        Assert.Equal(1, result.OtherResponsibilitySourceCount);
        Assert.Equal(1, result.LeaveHolidaySourceCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(10m, refreshed.ResponsibilityAllowanceAmount);
        Assert.Equal(20m, refreshed.SeniorityAllowanceAmount);
        Assert.Equal(30m, refreshed.AttendanceAllowanceAmount);
        Assert.Equal(40m, refreshed.MealAllowanceAmount);
        Assert.Equal(50m, refreshed.HazardAllowanceAmount);
        Assert.Equal(60m, refreshed.ResponsibilityOtherAllowanceAmount);
        Assert.Equal(65m, refreshed.OtherAllowanceAmount);
        Assert.Equal(70m, refreshed.LeaveHolidayAllowanceAmount);
        Assert.Equal("Giữ lại ghi chú tổng hợp", refreshed.Note);
        Assert.Equal("payroll-admin", refreshed.UpdatedBy);
    }

    [Fact]
    public async Task RefreshAsync_resets_open_row_without_details_and_skips_locked_row()
    {
        await using var dbContext = CreateDbContext();
        var openSummary = CreateSummary(Guid.NewGuid(), Guid.NewGuid(), isLocked: false);
        SetAllAmounts(openSummary, 99m);
        var lockedSummary = CreateSummary(Guid.NewGuid(), Guid.NewGuid(), isLocked: true);
        SetAllAmounts(lockedSummary, 88m);
        dbContext.PayrollAllowanceSummaryRecords.AddRange(openSummary, lockedSummary);
        AddAllAllowanceDetails(dbContext, lockedSummary, 10m, 20m, 30m, 40m, 50m, 60m, 65m, 70m);
        await dbContext.SaveChangesAsync();

        var result = await CreateRefreshService(dbContext).RefreshAsync(
            new RefreshPayrollAllowanceSummaryRequest(PayrollMonth, PayrollYear, "payroll-admin"));

        var summaries = await dbContext.PayrollAllowanceSummaryRecords.ToDictionaryAsync(row => row.Id);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.All(GetAmounts(summaries[openSummary.Id]), amount => Assert.Equal(0m, amount));
        Assert.All(GetAmounts(summaries[lockedSummary.Id]), amount => Assert.Equal(88m, amount));
    }

    [Fact]
    public async Task RefreshAsync_with_summary_id_refreshes_only_the_requested_open_row()
    {
        await using var dbContext = CreateDbContext();
        var targetSummary = CreateSummary(Guid.NewGuid(), Guid.NewGuid(), isLocked: false);
        SetAllAmounts(targetSummary, 1m);
        var otherSummary = CreateSummary(Guid.NewGuid(), Guid.NewGuid(), isLocked: false);
        SetAllAmounts(otherSummary, 99m);
        dbContext.PayrollAllowanceSummaryRecords.AddRange(targetSummary, otherSummary);
        AddAllAllowanceDetails(dbContext, targetSummary, 10m, 20m, 30m, 40m, 50m, 60m, 65m, 70m);
        AddAllAllowanceDetails(dbContext, otherSummary, 11m, 21m, 31m, 41m, 51m, 61m, 66m, 71m);
        await dbContext.SaveChangesAsync();

        var result = await CreateRefreshService(dbContext).RefreshAsync(
            new RefreshPayrollAllowanceSummaryRequest(
                PayrollMonth,
                PayrollYear,
                "payroll-admin",
                targetSummary.Id));

        var summaries = await dbContext.PayrollAllowanceSummaryRecords.ToDictionaryAsync(row => row.Id);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(
            new decimal[] { 10m, 20m, 30m, 40m, 50m, 60m, 65m, 70m },
            GetAmounts(summaries[targetSummary.Id]).ToArray());
        Assert.All(GetAmounts(summaries[otherSummary.Id]), amount => Assert.Equal(99m, amount));
    }

    [Fact]
    public async Task UpdateManualNoteAsync_preserves_meal_allowance_projection()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary(Guid.NewGuid(), Guid.NewGuid(), isLocked: false);
        summary.MealAllowanceAmount = 36_000m;
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();

        await CreateManualAdjustmentService(dbContext).UpdateManualValuesAsync(
            new UpdatePayrollAllowanceSummaryManualNoteRequest(
                summary.Id,
                "Cập nhật tổng hợp",
                OriginalUpdatedAtUtc: null,
                Actor: "payroll-admin"));

        var persisted = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(36_000m, persisted.MealAllowanceAmount);
        Assert.Equal(0m, persisted.ResponsibilityAllowanceAmount);
        Assert.Equal("payroll-admin", persisted.UpdatedBy);
    }

    [Fact]
    public async Task UpdateManualNoteAsync_rejects_stale_concurrency_token()
    {
        await using var dbContext = CreateDbContext();
        var summary = CreateSummary(Guid.NewGuid(), Guid.NewGuid(), isLocked: false);
        summary.UpdatedAtUtc = DateTime.UtcNow;
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateManualAdjustmentService(dbContext).UpdateManualValuesAsync(
                new UpdatePayrollAllowanceSummaryManualNoteRequest(
                    summary.Id,
                    null,
                    OriginalUpdatedAtUtc: summary.UpdatedAtUtc!.Value.AddSeconds(-1),
                    Actor: "payroll-admin")));

        Assert.Contains("phiên khác", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DatabasePayrollAllowanceSummaryRefreshService CreateRefreshService(ApplicationDbContext dbContext) =>
        new(new PayrollAllowanceSummaryPersistence(dbContext, null!, null!));

    private static DatabasePayrollAllowanceSummaryManualAdjustmentService CreateManualAdjustmentService(ApplicationDbContext dbContext) =>
        new(new PayrollAllowanceSummaryPersistence(dbContext, null!, null!));

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-allowance-summary-refresh-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PayrollAllowanceSummaryRecordRow CreateSummary(Guid summaryId, Guid employeeId, bool isLocked) =>
        new()
        {
            Id = summaryId,
            EmployeeId = employeeId,
            PayrollMonth = PayrollMonth,
            PayrollYear = PayrollYear,
            IsLocked = isLocked,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "test"
        };

    private static void AddAllAllowanceDetails(
        ApplicationDbContext dbContext,
        PayrollAllowanceSummaryRecordRow summary,
        decimal responsibility,
        decimal seniority,
        decimal attendance,
        decimal meal,
        decimal hazard,
        decimal otherResponsibility,
        decimal otherAllowance,
        decimal leaveHoliday)
    {
        var now = DateTime.UtcNow;
        dbContext.PayrollResponsibilityAllowanceAbcRows.Add(new PayrollResponsibilityAllowanceAbcRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = summary.Id,
            EmployeeId = summary.EmployeeId,
            Year = PayrollYear,
            Month = PayrollMonth,
            ActualResponsibilityAllowanceAmount = responsibility,
            CreatedAtUtc = now
        });
        dbContext.PayrollEmployeeSeniorityAllowances.Add(new PayrollEmployeeSeniorityAllowanceRow
        {
            PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceAmount = seniority,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        dbContext.PayrollAttendanceAllowanceRecords.Add(new PayrollAttendanceAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceAmount = attendance,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        dbContext.PayrollMealAllowanceRecords.Add(new PayrollMealAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summary.Id,
            MealAllowanceAmount = meal,
            RuleCode = "test",
            CalculatedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        dbContext.PayrollHazardAllowanceRecords.Add(new PayrollHazardAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summary.Id,
            HazardAllowanceAmount = hazard,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        dbContext.PayrollAllowanceOtherResponsibilityRecords.Add(new PayrollAllowanceOtherResponsibilityRecordRow
        {
            PayrollAllowanceSummaryRecordId = summary.Id,
            ActualResponsibilityAllowanceAmount = otherResponsibility,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        dbContext.PayrollOtherAllowanceRecords.Add(new PayrollOtherAllowanceRecordRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = summary.Id,
            AllowanceName = "Kiểm thử",
            IsFixedAmount = true,
            AllowanceAmount = otherAllowance,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(new PayrollAllowanceSummaryLeaveHolidayRecordRow
        {
            PayrollAllowanceSummaryRecordId = summary.Id,
            LeaveHolidayAllowanceAmount = leaveHoliday,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
    }

    private static void SetAllAmounts(PayrollAllowanceSummaryRecordRow summary, decimal amount)
    {
        summary.ResponsibilityAllowanceAmount = amount;
        summary.SeniorityAllowanceAmount = amount;
        summary.AttendanceAllowanceAmount = amount;
        summary.MealAllowanceAmount = amount;
        summary.HazardAllowanceAmount = amount;
        summary.ResponsibilityOtherAllowanceAmount = amount;
        summary.OtherAllowanceAmount = amount;
        summary.LeaveHolidayAllowanceAmount = amount;
    }

    private static IEnumerable<decimal> GetAmounts(PayrollAllowanceSummaryRecordRow summary) =>
    [
        summary.ResponsibilityAllowanceAmount,
        summary.SeniorityAllowanceAmount,
        summary.AttendanceAllowanceAmount,
        summary.MealAllowanceAmount,
        summary.HazardAllowanceAmount,
        summary.ResponsibilityOtherAllowanceAmount,
        summary.OtherAllowanceAmount,
        summary.LeaveHolidayAllowanceAmount
    ];
}
