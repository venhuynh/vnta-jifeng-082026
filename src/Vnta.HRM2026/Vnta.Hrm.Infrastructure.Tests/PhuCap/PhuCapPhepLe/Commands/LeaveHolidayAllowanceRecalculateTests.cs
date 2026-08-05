using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Exceptions;
using Vnta.Hrm.Infrastructure.ChamCong.CodeKetQuaTinhCong;
using Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Persistence;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Infrastructure.TinhLuong.LuongCanBan;
using Xunit;

#pragma warning disable CS0618 // These tests characterize the retained compatibility facade.

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapPhepLe;

public sealed class LeaveHolidayAllowanceRecalculateTests
{
    [Fact]
    public async Task Prepare_period_creates_only_missing_detail_snapshots_and_preserves_the_summary_amount()
    {
        await using var dbContext = CreateDbContext();
        var existingSummaryId = Guid.NewGuid();
        var missingSummaryId = Guid.NewGuid();
        var existingSummary = CreateSummary(existingSummaryId, Guid.NewGuid());
        existingSummary.LeaveHolidayAllowanceAmount = 100_000m;
        var missingSummary = CreateSummary(missingSummaryId, Guid.NewGuid());
        missingSummary.LeaveHolidayAllowanceAmount = 275_000m;
        var existingDetail = CreateDetail(existingSummaryId, 100_000m, "existing");
        existingDetail.HolidayDayCount = 4m;

        dbContext.PayrollAllowanceSummaryRecords.AddRange(existingSummary, missingSummary);
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(existingDetail);
        await dbContext.SaveChangesAsync();

        var service = new DatabaseLeaveHolidayAllowanceCommandService(dbContext);
        await service.PreparePeriodAsync(PayrollYear, PayrollMonth);
        await service.PreparePeriodAsync(PayrollYear, PayrollMonth);

        var details = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords
            .OrderBy(row => row.PayrollAllowanceSummaryRecordId)
            .ToArrayAsync();
        var createdDetail = Assert.Single(details, row => row.PayrollAllowanceSummaryRecordId == missingSummaryId);

        Assert.Equal(2, details.Length);
        Assert.Equal(0m, createdDetail.DailyWageAmount);
        Assert.Equal(0m, createdDetail.LeaveDayCount);
        Assert.Equal(0m, createdDetail.HolidayDayCount);
        Assert.Equal(275_000m, createdDetail.LeaveHolidayAllowanceAmount);
        Assert.Equal("system", createdDetail.CreatedBy);
        Assert.Equal(4m, Assert.Single(details, row => row.PayrollAllowanceSummaryRecordId == existingSummaryId).HolidayDayCount);
    }

    private const int PayrollMonth = 7;
    private const int PayrollYear = 2026;
    private const string MissingBasicSalaryReferenceNote = "Không tồn tại lương căn bản để tham chiếu.";

    [Fact]
    public async Task Recalculate_uses_daily_salary_from_basic_salary_for_the_same_employee_and_period()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();

        dbContext.PayrollAllowanceSummaryRecords.Add(CreateSummary(summaryId, employeeId));
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(CreateDetail(summaryId, 100_000m, MissingBasicSalaryReferenceNote));
        dbContext.BasicSalaryRecords.Add(new BasicSalaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayrollMonth = PayrollMonth,
            PayrollYear = PayrollYear,
            BasicSalary = 3_000_000m,
            StandardWorkingDays = 26m,
            DailySalary = 150_000m,
            HourlySalary = 18_750m,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseLeaveHolidayAllowanceCommandService(dbContext)
            .RecalculateAsync(new RecalculateLeaveHolidayAllowanceRequest(PayrollMonth, PayrollYear));

        var detail = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.SingleAsync();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(150_000m, detail.DailyWageAmount);
        Assert.Equal(300_000m, detail.LeaveHolidayAllowanceAmount);
        Assert.Null(detail.Note);
        Assert.Equal(300_000m, summary.LeaveHolidayAllowanceAmount);
    }

    [Fact]
    public async Task Recalculate_marks_open_row_when_its_basic_salary_is_missing()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();

        dbContext.PayrollAllowanceSummaryRecords.Add(CreateSummary(summaryId, Guid.NewGuid()));
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(CreateDetail(summaryId, 100_000m, "Ghi chú nhập tay"));
        await dbContext.SaveChangesAsync();

        await new DatabaseLeaveHolidayAllowanceCommandService(dbContext)
            .RecalculateAsync(new RecalculateLeaveHolidayAllowanceRequest(PayrollMonth, PayrollYear));

        var detail = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.SingleAsync();
        Assert.Equal(0m, detail.DailyWageAmount);
        Assert.Equal(0m, detail.LeaveHolidayAllowanceAmount);
        Assert.Equal(MissingBasicSalaryReferenceNote, detail.Note);
    }

    [Fact]
    public async Task Recalculate_with_summary_id_refreshes_only_the_requested_open_row()
    {
        await using var dbContext = CreateDbContext();
        var refreshedSummaryId = Guid.NewGuid();
        var untouchedSummaryId = Guid.NewGuid();
        var refreshedEmployeeId = Guid.NewGuid();
        var untouchedEmployeeId = Guid.NewGuid();

        dbContext.PayrollAllowanceSummaryRecords.AddRange(
            CreateSummary(refreshedSummaryId, refreshedEmployeeId),
            CreateSummary(untouchedSummaryId, untouchedEmployeeId));
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.AddRange(
            CreateDetail(refreshedSummaryId, 100_000m, null),
            CreateDetail(untouchedSummaryId, 200_000m, "Giữ nguyên"));
        dbContext.BasicSalaryRecords.AddRange(
            CreateBasicSalary(refreshedEmployeeId, 150_000m),
            CreateBasicSalary(untouchedEmployeeId, 250_000m));
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseLeaveHolidayAllowanceCommandService(dbContext)
            .RecalculateAsync(new RecalculateLeaveHolidayAllowanceRequest(
                PayrollMonth,
                PayrollYear,
                PayrollAllowanceSummaryRecordId: refreshedSummaryId));

        var details = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.ToDictionaryAsync(row => row.PayrollAllowanceSummaryRecordId);
        Assert.Equal(1, result.TotalRowCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(150_000m, details[refreshedSummaryId].DailyWageAmount);
        Assert.Equal(200_000m, details[untouchedSummaryId].DailyWageAmount);
        Assert.Equal("Giữ nguyên", details[untouchedSummaryId].Note);
    }

    [Fact]
    public async Task Recalculate_counts_each_workday_with_an_enabled_leave_holiday_code_and_preserves_manual_holidays()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();
        var includedCodeId = Guid.NewGuid();
        var excludedCodeId = Guid.NewGuid();

        dbContext.PayrollAllowanceSummaryRecords.Add(CreateSummary(summaryId, employeeId));
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(CreateDetail(summaryId, 100_000m, null));
        dbContext.BasicSalaryRecords.Add(CreateBasicSalary(employeeId, 150_000m));
        dbContext.AttendanceStatusCodes.AddRange(
            CreateStatusCode(includedCodeId, appliesToLeaveHolidayAllowance: true),
            CreateStatusCode(excludedCodeId, appliesToLeaveHolidayAllowance: false));
        dbContext.AttendanceWorkdaySummaries.AddRange(
            CreateWorkday(employeeId, includedCodeId, new DateOnly(PayrollYear, PayrollMonth, 1)),
            CreateWorkday(employeeId, includedCodeId, new DateOnly(PayrollYear, PayrollMonth, 2)),
            CreateWorkday(employeeId, excludedCodeId, new DateOnly(PayrollYear, PayrollMonth, 3)),
            CreateWorkday(employeeId, includedCodeId, new DateOnly(PayrollYear, PayrollMonth, 1).AddMonths(-1)));
        await dbContext.SaveChangesAsync();

        await new DatabaseLeaveHolidayAllowanceCommandService(dbContext)
            .RecalculateAsync(new RecalculateLeaveHolidayAllowanceRequest(PayrollMonth, PayrollYear));

        var detail = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.SingleAsync();
        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(2m, detail.LeaveDayCount);
        Assert.Equal(2m, detail.HolidayDayCount);
        Assert.Equal(150_000m, detail.DailyWageAmount);
        Assert.Equal(600_000m, detail.LeaveHolidayAllowanceAmount);
        Assert.Equal(detail.LeaveHolidayAllowanceAmount, summary.LeaveHolidayAllowanceAmount);
    }

    [Fact]
    public async Task Recalculate_skips_locked_summary_rows_without_changing_their_detail_or_summary_amount()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();
        var summary = CreateSummary(summaryId, employeeId);
        summary.IsLocked = true;
        summary.LeaveHolidayAllowanceAmount = 700_000m;
        var detail = CreateDetail(summaryId, 100_000m, null);
        detail.LeaveDayCount = 5m;
        detail.HolidayDayCount = 2m;
        detail.LeaveHolidayAllowanceAmount = 700_000m;
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(detail);
        dbContext.BasicSalaryRecords.Add(CreateBasicSalary(employeeId, 150_000m));
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseLeaveHolidayAllowanceCommandService(dbContext)
            .RecalculateAsync(new RecalculateLeaveHolidayAllowanceRequest(PayrollMonth, PayrollYear));

        var persistedDetail = await dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.SingleAsync();
        var persistedSummary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(5m, persistedDetail.LeaveDayCount);
        Assert.Equal(700_000m, persistedDetail.LeaveHolidayAllowanceAmount);
        Assert.Equal(700_000m, persistedSummary.LeaveHolidayAllowanceAmount);
    }

    [Fact]
    public async Task Save_manual_edit_only_changes_manual_holiday_days_with_submicrosecond_remainder()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var persistedVersion = new DateTime(2026, 7, 23, 8, 30, 0, DateTimeKind.Unspecified).AddTicks(1_230);
        var summary = CreateSummary(summaryId, employeeId);
        summary.UpdatedAtUtc = persistedVersion;
        var detail = CreateDetail(summaryId, 100_000m, null);
        detail.UpdatedAtUtc = persistedVersion;
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(detail);
        await dbContext.SaveChangesAsync();

        var saved = await new DatabaseLeaveHolidayAllowanceCommandService(dbContext)
            .UpdateManualValuesAsync(new UpdateLeaveHolidayAllowanceManualValuesRequest(
                summaryId,
                100_000m,
                1m,
                2m,
                "Lưu từ biểu mẫu",
                OriginalUpdatedAtUtc: persistedVersion.AddTicks(9)));

        Assert.Equal(1m, saved.LeaveDayCount);
        Assert.Equal("Lưu từ biểu mẫu", saved.Note);
    }

    [Fact]
    public async Task Save_manual_edit_rejects_changes_to_the_calculated_daily_wage_or_leave_workdays()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        dbContext.PayrollAllowanceSummaryRecords.Add(CreateSummary(summaryId, Guid.NewGuid()));
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(CreateDetail(summaryId, 100_000m, null));
        await dbContext.SaveChangesAsync();

        var service = new DatabaseLeaveHolidayAllowanceCommandService(dbContext);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateManualValuesAsync(
            new UpdateLeaveHolidayAllowanceManualValuesRequest(summaryId, 100_000m, 2m, 2m, null)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateManualValuesAsync(
            new UpdateLeaveHolidayAllowanceManualValuesRequest(summaryId, 120_000m, 1m, 2m, null)));
    }

    [Fact]
    public async Task Save_manual_edit_rejects_a_genuinely_stale_version()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var persistedVersion = new DateTime(2026, 7, 23, 8, 30, 0, DateTimeKind.Unspecified).AddTicks(1_230);
        var summary = CreateSummary(summaryId, employeeId);
        summary.UpdatedAtUtc = persistedVersion;
        var detail = CreateDetail(summaryId, 100_000m, null);
        detail.UpdatedAtUtc = persistedVersion;
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(detail);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<LeaveHolidayAllowanceConflictException>(() =>
            new DatabaseLeaveHolidayAllowanceCommandService(dbContext).UpdateManualValuesAsync(
                new UpdateLeaveHolidayAllowanceManualValuesRequest(
                    summaryId,
                    100_000m,
                    1.5m,
                    2m,
                    "Token cũ",
                    OriginalUpdatedAtUtc: persistedVersion.AddMicroseconds(-1))));
    }

    [Fact]
    public async Task Save_manual_edit_accepts_the_detail_version_when_only_the_shared_summary_changes()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var detailVersion = new DateTime(2026, 7, 23, 8, 30, 0, DateTimeKind.Unspecified);
        var summary = CreateSummary(summaryId, employeeId);
        summary.UpdatedAtUtc = detailVersion.AddMinutes(1);
        var detail = CreateDetail(summaryId, 100_000m, null);
        detail.UpdatedAtUtc = detailVersion;
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(detail);
        await dbContext.SaveChangesAsync();

        var saved = await new DatabaseLeaveHolidayAllowanceCommandService(dbContext)
            .UpdateManualValuesAsync(new UpdateLeaveHolidayAllowanceManualValuesRequest(
                summaryId,
                100_000m,
                1m,
                3m,
                "Chỉ thay đổi Phép - Lễ",
                OriginalUpdatedAtUtc: detailVersion));

        Assert.Equal(1m, saved.LeaveDayCount);
        Assert.Equal(3m, saved.HolidayDayCount);
        Assert.Equal("Chỉ thay đổi Phép - Lễ", saved.Note);
    }

    [Fact]
    public async Task Set_lock_state_updates_the_summary_and_returns_the_persisted_record()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;

        var summary = CreateSummary(summaryId, Guid.NewGuid());
        summary.CreatedAtUtc = createdAtUtc;
        var detail = CreateDetail(summaryId, 100_000m, null);
        detail.CreatedAtUtc = createdAtUtc;
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(detail);
        await dbContext.SaveChangesAsync();

        var service = new DatabaseLeaveHolidayAllowanceCommandService(dbContext);
        var updated = await service.SetLockStateAsync(
            new SetLeaveHolidayAllowanceLockStateRequest(summaryId, true, "payroll-admin", createdAtUtc));

        var persistedSummary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        Assert.True(persistedSummary.IsLocked);
        Assert.Equal("payroll-admin", persistedSummary.UpdatedBy);
        Assert.True(updated.IsLocked);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateManualValuesAsync(
            new UpdateLeaveHolidayAllowanceManualValuesRequest(summaryId, 100_000m, 1m, 2m, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Set_lock_state_rejects_a_stale_record_version()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var originalVersion = DateTime.UtcNow;

        var summary = CreateSummary(summaryId, Guid.NewGuid());
        summary.CreatedAtUtc = originalVersion;
        summary.UpdatedAtUtc = originalVersion.AddMinutes(1);
        var detail = CreateDetail(summaryId, 100_000m, null);
        detail.CreatedAtUtc = originalVersion;
        dbContext.PayrollAllowanceSummaryRecords.Add(summary);
        dbContext.PayrollAllowanceSummaryLeaveHolidayRecords.Add(detail);
        await dbContext.SaveChangesAsync();

        var service = new DatabaseLeaveHolidayAllowanceCommandService(dbContext);

        await Assert.ThrowsAsync<LeaveHolidayAllowanceConflictException>(() => service.SetLockStateAsync(
            new SetLeaveHolidayAllowanceLockStateRequest(summaryId, true, "payroll-admin", originalVersion)));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"leave-holiday-allowance-recalculate-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PayrollAllowanceSummaryRecordRow CreateSummary(Guid summaryId, Guid employeeId) =>
        new()
        {
            Id = summaryId,
            EmployeeId = employeeId,
            PayrollMonth = PayrollMonth,
            PayrollYear = PayrollYear,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "test"
        };

    private static PayrollAllowanceSummaryLeaveHolidayRecordRow CreateDetail(
        Guid summaryId,
        decimal dailyWageAmount,
        string? note) =>
        new()
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            DailyWageAmount = dailyWageAmount,
            LeaveDayCount = 1m,
            HolidayDayCount = 2m,
            LeaveHolidayAllowanceAmount = 300_000m,
            Note = note,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "test"
        };

    private static AttendanceStatusCodeRow CreateStatusCode(Guid id, bool appliesToLeaveHolidayAllowance) =>
        new()
        {
            Id = id,
            Code = $"TEST-{id:N}",
            Name = "Test",
            Kind = "Test",
            PhuCapPhepLe = appliesToLeaveHolidayAllowance,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

    private static AttendanceWorkdaySummaryRow CreateWorkday(
        Guid employeeId,
        Guid statusCodeId,
        DateOnly workDate) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            WorkDate = workDate,
            DayType = "Regular",
            CodeKetQuaTinhCongId = statusCodeId,
            ComputedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

    private static BasicSalaryRecordRow CreateBasicSalary(Guid employeeId, decimal dailySalary) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayrollMonth = PayrollMonth,
            PayrollYear = PayrollYear,
            BasicSalary = dailySalary * 26m,
            StandardWorkingDays = 26m,
            DailySalary = dailySalary,
            HourlySalary = dailySalary / 8m,
            CreatedAtUtc = DateTime.UtcNow
        };
}
