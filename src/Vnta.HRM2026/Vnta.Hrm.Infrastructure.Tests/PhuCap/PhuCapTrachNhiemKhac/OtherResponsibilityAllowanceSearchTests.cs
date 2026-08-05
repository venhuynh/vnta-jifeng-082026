using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Queries;
using Vnta.Hrm.Infrastructure.ChamCong.CodeKetQuaTinhCong;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Infrastructure.TinhLuong.LuongCanBan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTrachNhiemKhac;

public sealed class OtherResponsibilityAllowanceSearchTests
{
    [Fact]
    public async Task PreparePeriodAsync_seeds_missing_detail_row_for_the_selected_period()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 6,
            PayrollYear = 2026,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await dbContext.SaveChangesAsync();

        var service = new DatabaseOtherResponsibilityAllowancePeriodPreparationService(dbContext);
        await service.PreparePeriodAsync(2026, 6, "audit-user");

        var records = await new DatabaseOtherResponsibilityAllowanceReadService(dbContext)
            .SearchAsync(new OtherResponsibilityAllowanceFilter(6, 2026, null));

        var detail = await dbContext.PayrollAllowanceOtherResponsibilityRecords.SingleAsync();
        Assert.Single(records);
        Assert.Equal(summaryId, records[0].PayrollAllowanceSummaryRecordId);
        Assert.Equal(summaryId, detail.PayrollAllowanceSummaryRecordId);
        Assert.Equal("audit-user", detail.CreatedBy);
    }

    [Fact]
    public async Task SearchAsync_does_not_seed_missing_detail_row()
    {
        await using var dbContext = CreateDbContext();
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 6,
            PayrollYear = 2026,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "test"
        });
        await dbContext.SaveChangesAsync();

        var records = await new DatabaseOtherResponsibilityAllowanceReadService(dbContext)
            .SearchAsync(new OtherResponsibilityAllowanceFilter(6, 2026, null));

        Assert.Empty(records);
        Assert.Empty(dbContext.PayrollAllowanceOtherResponsibilityRecords);
    }

    [Fact]
    public async Task PreparePeriodAsync_keeps_existing_detail_rows_for_a_large_june_2026_dataset()
    {
        await using var dbContext = CreateDbContext();
        var summaryRows = Enumerable.Range(0, 2_000)
            .Select(index => new PayrollAllowanceSummaryRecordRow
            {
                Id = Guid.NewGuid(),
                EmployeeId = Guid.NewGuid(),
                PayrollMonth = 6,
                PayrollYear = 2026,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = "test"
            })
            .ToArray();

        dbContext.PayrollAllowanceSummaryRecords.AddRange(summaryRows);
        dbContext.PayrollAllowanceOtherResponsibilityRecords.AddRange(
            summaryRows.Select(summary => new PayrollAllowanceOtherResponsibilityRecordRow
            {
                PayrollAllowanceSummaryRecordId = summary.Id,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = "test"
            }));
        await dbContext.SaveChangesAsync();

        await new DatabaseOtherResponsibilityAllowancePeriodPreparationService(dbContext)
            .PreparePeriodAsync(2026, 6, "test");

        Assert.Equal(
            summaryRows.Length,
            await dbContext.PayrollAllowanceOtherResponsibilityRecords.CountAsync());
    }

    [Fact]
    public async Task SearchAsync_rejects_period_before_june_2026()
    {
        await using var dbContext = CreateDbContext();
        var service = new DatabaseOtherResponsibilityAllowanceReadService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SearchAsync(new OtherResponsibilityAllowanceFilter(5, 2026, null)));

        Assert.Contains("06/2026", exception.Message);
    }

    [Fact]
    public async Task RecalculateAsync_updates_open_detail_only_and_keeps_detail_locked_rows_unchanged()
    {
        await using var dbContext = CreateDbContext();
        var openEmployeeId = Guid.NewGuid();
        var lockedEmployeeId = Guid.NewGuid();
        var statusCodeId = Guid.NewGuid();
        var openSummaryId = Guid.NewGuid();
        var lockedSummaryId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        dbContext.AttendanceStatusCodes.Add(new AttendanceStatusCodeRow
        {
            Id = statusCodeId,
            Code = "HC",
            Name = "Hành chính",
            Kind = "Workday",
            IsActive = true,
            CongHanhChinh = true,
            PhuCapTrachNhiemKhac = false,
            CreatedAtUtc = now
        });
        dbContext.PayrollAllowanceSummaryRecords.AddRange(
            new PayrollAllowanceSummaryRecordRow
            {
                Id = openSummaryId,
                EmployeeId = openEmployeeId,
                PayrollMonth = 6,
                PayrollYear = 2026,
                CreatedAtUtc = now,
                CreatedBy = "test"
            },
            new PayrollAllowanceSummaryRecordRow
            {
                Id = lockedSummaryId,
                EmployeeId = lockedEmployeeId,
                PayrollMonth = 6,
                PayrollYear = 2026,
                ResponsibilityOtherAllowanceAmount = 123m,
                IsLocked = true,
                CreatedAtUtc = now,
                CreatedBy = "test"
            });
        dbContext.PayrollAllowanceOtherResponsibilityRecords.Add(new PayrollAllowanceOtherResponsibilityRecordRow
        {
            PayrollAllowanceSummaryRecordId = lockedSummaryId,
            AllowanceWorkdayCount = 2m,
            StandardResponsibilityAllowanceAmount = 999m,
            ActualResponsibilityAllowanceAmount = 999m,
            IsLocked = true,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        dbContext.BasicSalaryRecords.Add(new BasicSalaryRecordRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = openEmployeeId,
            PayrollMonth = 6,
            PayrollYear = 2026,
            StandardWorkingDays = 3m,
            CreatedAtUtc = now
        });
        dbContext.PayrollResponsibilityAllowanceAbcRows.Add(new PayrollResponsibilityAllowanceAbcRow
        {
            Id = Guid.NewGuid(),
            PayrollAllowanceSummaryRecordId = openSummaryId,
            EmployeeId = openEmployeeId,
            Year = 2026,
            Month = 6,
            StandardResponsibilityAllowanceAmount = 1_000m,
            CreatedAtUtc = now
        });
        dbContext.AttendanceWorkdaySummaries.AddRange(
            new AttendanceWorkdaySummaryRow
            {
                Id = Guid.NewGuid(),
                EmployeeId = openEmployeeId,
                WorkDate = new DateOnly(2026, 6, 1),
                CodeKetQuaTinhCongId = statusCodeId,
                LateMinutes = 120,
                ComputedAtUtc = now,
                CreatedAtUtc = now
            },
            new AttendanceWorkdaySummaryRow
            {
                Id = Guid.NewGuid(),
                EmployeeId = openEmployeeId,
                WorkDate = new DateOnly(2026, 6, 2),
                CodeKetQuaTinhCongId = statusCodeId,
                EarlyLeaveMinutes = 120,
                ComputedAtUtc = now,
                CreatedAtUtc = now
            });
        await dbContext.SaveChangesAsync();

        var result = await new DatabaseOtherResponsibilityAllowanceRecalculationService(
                dbContext,
                new DatabaseOtherResponsibilityAllowancePeriodPreparationService(dbContext),
                new OtherResponsibilityAllowanceCalculator(),
                new OtherResponsibilityAllowanceWorkdayCalculator(),
                new DatabaseBasicSalaryWorkdaySource(dbContext))
            .RecalculateAsync(new RecalculateOtherResponsibilityAllowanceRequest(2026, 6), "actual-actor");

        var openDetail = await dbContext.PayrollAllowanceOtherResponsibilityRecords.SingleAsync(
            row => row.PayrollAllowanceSummaryRecordId == openSummaryId);
        var openSummary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync(row => row.Id == openSummaryId);
        var lockedDetail = await dbContext.PayrollAllowanceOtherResponsibilityRecords.SingleAsync(
            row => row.PayrollAllowanceSummaryRecordId == lockedSummaryId);

        Assert.Equal(1, result.RecalculatedCount);
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(1.5m, openDetail.AllowanceWorkdayCount);
        Assert.Equal(1_000m, openDetail.StandardResponsibilityAllowanceAmount);
        Assert.Equal(500m, openDetail.ActualResponsibilityAllowanceAmount);
        Assert.Equal(500m, openSummary.ResponsibilityOtherAllowanceAmount);
        Assert.Equal(0m, openSummary.ResponsibilityAllowanceAmount);
        Assert.Equal("actual-actor", openDetail.RefreshedBy);
        Assert.Equal(2m, lockedDetail.AllowanceWorkdayCount);
        Assert.Equal(999m, lockedDetail.ActualResponsibilityAllowanceAmount);
    }

    [Fact]
    public async Task RecalculateAsync_uses_CongHanhChinh_and_adjusts_duplicate_attendance_rows_per_day()
    {
        await using var dbContext = CreateDbContext();
        var employeeId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();
        var administrativeStatusId = Guid.NewGuid();
        var otherStatusId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        dbContext.AttendanceStatusCodes.AddRange(
            new AttendanceStatusCodeRow { Id = administrativeStatusId, Code = "HC", Name = "HC", Kind = "Workday", IsActive = true, CongHanhChinh = true, PhuCapTrachNhiemKhac = false, CreatedAtUtc = now },
            new AttendanceStatusCodeRow { Id = otherStatusId, Code = "OTHER", Name = "Other", Kind = "Workday", IsActive = true, CongHanhChinh = false, PhuCapTrachNhiemKhac = true, CreatedAtUtc = now });
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow { Id = summaryId, EmployeeId = employeeId, PayrollMonth = 6, PayrollYear = 2026, CreatedAtUtc = now, CreatedBy = "test" });
        dbContext.BasicSalaryRecords.Add(new BasicSalaryRecordRow { Id = Guid.NewGuid(), EmployeeId = employeeId, PayrollMonth = 6, PayrollYear = 2026, StandardWorkingDays = 2m, CreatedAtUtc = now });
        dbContext.PayrollResponsibilityAllowanceAbcRows.Add(new PayrollResponsibilityAllowanceAbcRow { Id = Guid.NewGuid(), PayrollAllowanceSummaryRecordId = summaryId, EmployeeId = employeeId, Year = 2026, Month = 6, StandardResponsibilityAllowanceAmount = 1_000m, CreatedAtUtc = now });
        dbContext.AttendanceWorkdaySummaries.AddRange(
            new AttendanceWorkdaySummaryRow { Id = Guid.NewGuid(), EmployeeId = employeeId, WorkDate = new DateOnly(2026, 6, 1), CodeKetQuaTinhCongId = administrativeStatusId, LateMinutes = 240, ComputedAtUtc = now, CreatedAtUtc = now },
            new AttendanceWorkdaySummaryRow { Id = Guid.NewGuid(), EmployeeId = employeeId, WorkDate = new DateOnly(2026, 6, 1), CodeKetQuaTinhCongId = administrativeStatusId, EarlyLeaveMinutes = 600, ComputedAtUtc = now, CreatedAtUtc = now },
            new AttendanceWorkdaySummaryRow { Id = Guid.NewGuid(), EmployeeId = employeeId, WorkDate = new DateOnly(2026, 6, 2), CodeKetQuaTinhCongId = otherStatusId, ComputedAtUtc = now, CreatedAtUtc = now },
            new AttendanceWorkdaySummaryRow { Id = Guid.NewGuid(), EmployeeId = employeeId, WorkDate = new DateOnly(2026, 6, 3), CodeKetQuaTinhCongId = null, LateMinutes = 600, ComputedAtUtc = now, CreatedAtUtc = now });
        await dbContext.SaveChangesAsync();

        await new DatabaseOtherResponsibilityAllowanceRecalculationService(
                dbContext,
                new DatabaseOtherResponsibilityAllowancePeriodPreparationService(dbContext),
                new OtherResponsibilityAllowanceCalculator(),
                new OtherResponsibilityAllowanceWorkdayCalculator(),
                new DatabaseBasicSalaryWorkdaySource(dbContext))
            .RecalculateAsync(new RecalculateOtherResponsibilityAllowanceRequest(2026, 6), "actual-actor");

        var detail = await dbContext.PayrollAllowanceOtherResponsibilityRecords.SingleAsync();
        Assert.Equal(0m, detail.AllowanceWorkdayCount);
        Assert.Equal(0m, detail.ActualResponsibilityAllowanceAmount);
    }

    [Fact]
    public async Task SetLockStateBatchAsync_changes_detail_and_summary_locks_and_rejects_stale_selected_row()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow { Id = summaryId, EmployeeId = Guid.NewGuid(), PayrollMonth = 6, PayrollYear = 2026, IsLocked = false, CreatedAtUtc = now, CreatedBy = "test" });
        dbContext.PayrollAllowanceOtherResponsibilityRecords.Add(new PayrollAllowanceOtherResponsibilityRecordRow { PayrollAllowanceSummaryRecordId = summaryId, IsLocked = false, CreatedAtUtc = now, CreatedBy = "test", UpdatedAtUtc = now });
        await dbContext.SaveChangesAsync();
        var service = new DatabaseOtherResponsibilityAllowanceLockService(dbContext);

        var result = await service.SetLockStateBatchAsync(
            new SetOtherResponsibilityAllowanceBatchLockStateRequest(2026, 6, true, [summaryId], [new OtherResponsibilityAllowanceLockStateConcurrencyToken(summaryId, now)]),
            "audit-user");

        var summary = await dbContext.PayrollAllowanceSummaryRecords.SingleAsync();
        var detail = await dbContext.PayrollAllowanceOtherResponsibilityRecords.SingleAsync();
        Assert.Equal(1, result.UpdatedCount);
        Assert.True(summary.IsLocked);
        Assert.True(detail.IsLocked);
        Assert.Equal("audit-user", detail.UpdatedBy);
        await Assert.ThrowsAsync<OtherResponsibilityAllowanceConcurrencyException>(() => service.SetLockStateBatchAsync(
            new SetOtherResponsibilityAllowanceBatchLockStateRequest(2026, 6, false, [summaryId], [new OtherResponsibilityAllowanceLockStateConcurrencyToken(summaryId, now)]),
            "audit-user"));
    }

    [Fact]
    public async Task SetLockStateBatchAsync_rejects_duplicate_selected_concurrency_tokens_with_the_feature_conflict_contract()
    {
        await using var dbContext = CreateDbContext();
        var summaryId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = Guid.NewGuid(),
            PayrollMonth = 6,
            PayrollYear = 2026,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        dbContext.PayrollAllowanceOtherResponsibilityRecords.Add(new PayrollAllowanceOtherResponsibilityRecordRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            CreatedAtUtc = now,
            CreatedBy = "test"
        });
        await dbContext.SaveChangesAsync();

        var service = new DatabaseOtherResponsibilityAllowanceLockService(dbContext);
        var request = new SetOtherResponsibilityAllowanceBatchLockStateRequest(
            2026,
            6,
            true,
            [summaryId],
            [
                new OtherResponsibilityAllowanceLockStateConcurrencyToken(summaryId, null),
                new OtherResponsibilityAllowanceLockStateConcurrencyToken(summaryId, null)
            ]);

        await Assert.ThrowsAsync<OtherResponsibilityAllowanceConcurrencyException>(
            () => service.SetLockStateBatchAsync(request, "audit-user"));
    }

    [Fact]
    public async Task Other_responsibility_detail_timestamp_is_configured_as_a_concurrency_token()
    {
        await using var dbContext = CreateDbContext();

        var entityType = dbContext.Model.FindEntityType(typeof(PayrollAllowanceOtherResponsibilityRecordRow));
        var updatedAtProperty = entityType?.FindProperty(nameof(PayrollAllowanceOtherResponsibilityRecordRow.UpdatedAtUtc));

        Assert.NotNull(updatedAtProperty);
        Assert.True(updatedAtProperty!.IsConcurrencyToken);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"other-responsibility-allowance-{Guid.NewGuid():N}")
            .Options);
}
