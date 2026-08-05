using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.TinhLuong.BangCongTongHop;
using Vnta.Hrm.Infrastructure.ChamCong.CodeKetQuaTinhCong;
using Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.TinhLuong.BangCongTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.TinhLuong.BangCongTongHop;

public sealed class PayrollMonthlyWorkInputRefreshServiceTests
{
    [Fact]
    public async Task RefreshAsync_creates_all_active_employee_rows_and_aggregates_the_period()
    {
        await using var dbContext = CreateDbContext();
        var administrativeStatus = CreateStatusCode(isAdministrativeWorkday: true);
        var nonAdministrativeStatus = CreateStatusCode(isAdministrativeWorkday: false);
        var employeeWithWorkdays = CreateEmployee();
        var employeeWithoutWorkdays = CreateEmployee();
        var deletedEmployee = CreateEmployee(isDeleted: true);
        dbContext.AttendanceStatusCodes.AddRange(administrativeStatus, nonAdministrativeStatus);
        dbContext.Employees.AddRange(employeeWithWorkdays, employeeWithoutWorkdays, deletedEmployee);
        dbContext.AttendanceWorkdaySummaries.AddRange(
            CreateWorkday(employeeWithWorkdays.Id, administrativeStatus.Id, new DateOnly(2026, 7, 1), 30, 15, 120, 60, 30),
            CreateWorkday(employeeWithWorkdays.Id, administrativeStatus.Id, new DateOnly(2026, 7, 2), 10, 5, 20, 0, 0),
            CreateWorkday(employeeWithWorkdays.Id, nonAdministrativeStatus.Id, new DateOnly(2026, 7, 3), 0, 0, 5, 10, 15),
            CreateWorkday(employeeWithWorkdays.Id, administrativeStatus.Id, new DateOnly(2026, 8, 1), 99, 99, 99, 99, 99));
        await dbContext.SaveChangesAsync();

        var result = await new DatabasePayrollMonthlyWorkInputRefreshService(dbContext).RefreshAsync(
            new RefreshPayrollMonthlyWorkInputsRequest(7, 2026));

        var rows = await dbContext.PayrollMonthlyWorkInputs
            .OrderBy(row => row.EmployeeId)
            .ToListAsync();
        var aggregated = rows.Single(row => row.EmployeeId == employeeWithWorkdays.Id);
        var empty = rows.Single(row => row.EmployeeId == employeeWithoutWorkdays.Id);

        Assert.Equal(2, result.EmployeeCount);
        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(2, rows.Count);
        Assert.Equal(2m, aggregated.AdministrativeWorkDays);
        Assert.Equal(60, aggregated.LateEarlyLeaveMinutes);
        Assert.Equal(145, aggregated.OvertimeMinutes15);
        Assert.Equal(70, aggregated.OvertimeMinutes20);
        Assert.Equal(45, aggregated.OvertimeMinutes30);
        Assert.Equal(1.8750m, aggregated.PayrollWorkDays);
        Assert.Equal(0m, empty.AdministrativeWorkDays);
        Assert.Equal(0m, empty.PayrollWorkDays);
    }

    [Fact]
    public async Task RefreshAsync_preserves_locked_row()
    {
        await using var dbContext = CreateDbContext();
        var status = CreateStatusCode(isAdministrativeWorkday: true);
        var employee = CreateEmployee();
        var lockedRow = new PayrollMonthlyWorkInputRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            PayrollYear = 2026,
            PayrollMonth = 7,
            AdministrativeWorkDays = 9m,
            LateEarlyLeaveMinutes = 1,
            OvertimeMinutes15 = 2,
            OvertimeMinutes20 = 3,
            OvertimeMinutes30 = 4,
            PayrollWorkDays = 8.9979m,
            IsLocked = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.AttendanceStatusCodes.Add(status);
        dbContext.Employees.Add(employee);
        dbContext.AttendanceWorkdaySummaries.Add(
            CreateWorkday(employee.Id, status.Id, new DateOnly(2026, 7, 1), 60, 0, 100, 0, 0));
        dbContext.PayrollMonthlyWorkInputs.Add(lockedRow);
        await dbContext.SaveChangesAsync();

        var result = await new DatabasePayrollMonthlyWorkInputRefreshService(dbContext).RefreshAsync(
            new RefreshPayrollMonthlyWorkInputsRequest(7, 2026));

        var refreshed = await dbContext.PayrollMonthlyWorkInputs.SingleAsync();
        Assert.Equal(1, result.SkippedLockedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(9m, refreshed.AdministrativeWorkDays);
        Assert.Equal(1, refreshed.LateEarlyLeaveMinutes);
        Assert.Equal(8.9979m, refreshed.PayrollWorkDays);
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"payroll-monthly-work-inputs-{Guid.NewGuid():N}")
            .Options);

    private static AttendanceGatewayEmployeeRow CreateEmployee(bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        DepartmentId = Guid.NewGuid(),
        PositionId = Guid.NewGuid(),
        Status = 1,
        EmployeeCode = Guid.NewGuid().ToString("N"),
        FirstName = "Test",
        LastName = "Employee",
        HireDate = DateTime.UtcNow,
        CreatedAtUtc = DateTime.UtcNow,
        IsDeleted = isDeleted
    };

    private static AttendanceStatusCodeRow CreateStatusCode(bool isAdministrativeWorkday) => new()
    {
        Id = Guid.NewGuid(),
        Code = Guid.NewGuid().ToString("N"),
        Name = "Test",
        Kind = "Test",
        CongHanhChinh = isAdministrativeWorkday,
        IsActive = true,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static AttendanceWorkdaySummaryRow CreateWorkday(
        Guid employeeId,
        Guid statusCodeId,
        DateOnly workDate,
        int lateMinutes,
        int earlyLeaveMinutes,
        int overtimeMinutes15,
        int overtimeMinutes20,
        int overtimeMinutes30) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = employeeId,
        WorkDate = workDate,
        DayType = "Ngày thường",
        LateMinutes = lateMinutes,
        EarlyLeaveMinutes = earlyLeaveMinutes,
        OvertimeMinutes15 = overtimeMinutes15,
        OvertimeMinutes20 = overtimeMinutes20,
        OvertimeMinutes30 = overtimeMinutes30,
        CodeKetQuaTinhCongId = statusCodeId,
        ComputedAtUtc = DateTime.UtcNow,
        CreatedAtUtc = DateTime.UtcNow
    };
}
