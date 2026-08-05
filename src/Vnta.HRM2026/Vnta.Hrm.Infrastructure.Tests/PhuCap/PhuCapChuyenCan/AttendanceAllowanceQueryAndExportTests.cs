using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.Common;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapChuyenCan;

public sealed class AttendanceAllowanceQueryAndExportTests
{
    [Fact]
    public async Task SearchPage_filters_lock_state_pages_rows_and_keeps_period_summary_counts()
    {
        await using var dbContext = CreateDbContext();
        AddRecord(dbContext, AttendanceAllowanceClass.A, detailLocked: false, summaryLocked: false, createdAtUtc: new DateTime(2026, 7, 1));
        AddRecord(dbContext, AttendanceAllowanceClass.B, detailLocked: true, summaryLocked: false, createdAtUtc: new DateTime(2026, 7, 2));
        AddRecord(dbContext, AttendanceAllowanceClass.C, detailLocked: false, summaryLocked: true, createdAtUtc: new DateTime(2026, 7, 3));
        AddRecord(dbContext, AttendanceAllowanceClass.A, detailLocked: false, summaryLocked: false, createdAtUtc: new DateTime(2026, 7, 4));
        AddRecord(dbContext, AttendanceAllowanceClass.A, detailLocked: false, summaryLocked: false, createdAtUtc: new DateTime(2026, 8, 1), payrollMonth: 8);
        await dbContext.SaveChangesAsync();

        var service = new DatabaseAttendanceAllowanceReadService(dbContext, new EmptyEligibleStatusCodeSource());
        var page = await service.SearchPageAsync(new AttendanceAllowanceResultFilter(
            PayrollAllowanceKind.Attendance, 7, 2026, null, Take: 1, Skip: 1,
            LockState: AttendanceAllowanceLockState.Locked));

        Assert.Single(page.Rows);
        Assert.True(page.Rows[0].IsLocked);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.OpenCount);
        Assert.Equal(2, page.LockedCount);
        Assert.Equal(2, page.AttendanceClassACount);
        Assert.Equal(1, page.AttendanceClassBCount);
        Assert.Equal(1, page.AttendanceClassCCount);
        Assert.Equal(4, page.PeriodTotalCount);
        Assert.Equal(2, page.PeriodCanLockCount);
        Assert.Equal(1, page.PeriodCanUnlockCount);
        Assert.Equal(1, page.PeriodSummaryLockedCount);
    }

    [Fact]
    public async Task SearchPage_rejects_a_period_before_the_supported_attendance_allowance_range()
    {
        await using var dbContext = CreateDbContext();
        var service = new DatabaseAttendanceAllowanceReadService(dbContext, new EmptyEligibleStatusCodeSource());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SearchPageAsync(new AttendanceAllowanceResultFilter(
            PayrollAllowanceKind.Attendance, 5, 2026, null)));

        Assert.Equal(AttendanceAllowancePayrollPeriodPolicy.GetValidationError(5, 2026), exception.Message);
    }

    [Fact]
    public async Task SearchPage_filters_and_projects_attendance_class_as_a_domain_value()
    {
        await using var dbContext = CreateDbContext();
        AddRecord(dbContext, AttendanceAllowanceClass.A, detailLocked: false, summaryLocked: false, createdAtUtc: new DateTime(2026, 7, 1));
        AddRecord(dbContext, AttendanceAllowanceClass.B, detailLocked: false, summaryLocked: false, createdAtUtc: new DateTime(2026, 7, 2));
        await dbContext.SaveChangesAsync();

        var service = new DatabaseAttendanceAllowanceReadService(dbContext, new EmptyEligibleStatusCodeSource());
        var page = await service.SearchPageAsync(new AttendanceAllowanceResultFilter(
            PayrollAllowanceKind.Attendance,
            7,
            2026,
            null,
            AttendanceClass: AttendanceAllowanceClass.B));

        var row = Assert.Single(page.Rows);
        Assert.Equal(AttendanceAllowanceClass.B, row.AttendanceClass);
    }

    private static ApplicationDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"attendance-allowance-query-{Guid.NewGuid():N}")
        .Options);

    private static string AddRecord(
        ApplicationDbContext dbContext,
        AttendanceAllowanceClass attendanceClass,
        bool detailLocked,
        bool summaryLocked,
        DateTime createdAtUtc,
        short payrollMonth = 7,
        string? employeeCode = null,
        string? firstName = null)
    {
        var employeeId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();
        dbContext.Employees.Add(new AttendanceGatewayEmployeeRow
        {
            Id = employeeId,
            EmployeeCode = employeeCode ?? $"NV-{summaryId:N}",
            FirstName = firstName ?? "Test",
            LastName = "Allowance",
            Status = 1,
            CreatedAtUtc = createdAtUtc
        });
        dbContext.PayrollAllowanceSummaryRecords.Add(new PayrollAllowanceSummaryRecordRow
        {
            Id = summaryId,
            EmployeeId = employeeId,
            PayrollMonth = payrollMonth,
            PayrollYear = 2026,
            IsLocked = summaryLocked,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "test"
        });
        dbContext.PayrollAttendanceAllowanceRecords.Add(new PayrollAttendanceAllowanceRecordRow
        {
            PayrollAllowanceSummaryRecordId = summaryId,
            StandardWorkdayCount = 26m,
            ActualWorkdayCount = 25m,
            AttendanceRate = 0.9615m,
            AllowanceAmount = 600_000m,
            AttendanceClass = attendanceClass.ToStorageValue(),
            IsLocked = detailLocked,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = "test"
        });
        return employeeCode ?? $"NV-{summaryId:N}";
    }

    private sealed class EmptyEligibleStatusCodeSource : IAttendanceAllowanceEligibleStatusCodeSource
    {
        public Task<IReadOnlyList<string>> LoadEligibleStatusCodesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }

}
