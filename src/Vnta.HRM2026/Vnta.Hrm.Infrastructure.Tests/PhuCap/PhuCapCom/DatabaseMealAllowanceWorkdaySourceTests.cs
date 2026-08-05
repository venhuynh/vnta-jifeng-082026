using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.CaKip.CaiDatCa;
using Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapCom;

public sealed class DatabaseMealAllowanceWorkdaySourceTests
{
    [Fact]
    public async Task Load_reads_only_active_employee_workdays_in_the_requested_payroll_month_and_employee_scope()
    {
        await using var dbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"meal-allowance-workday-source-{Guid.NewGuid():N}")
            .Options);
        var includedEmployeeId = AddEmployee(dbContext, "NV-IN", false);
        var deletedEmployeeId = AddEmployee(dbContext, "NV-DEL", true);
        var shiftId = Guid.NewGuid();
        dbContext.Shifts.Add(new AttendanceShiftRow
        {
            Id = shiftId,
            Code = "SX-A",
            Name = "Production A",
            ShortName = "SX",
            DepartmentGroup = "Production",
            StartTime = "08:00",
            EndTime = "17:00",
            CreatedAtUtc = DateTime.UnixEpoch
        });
        AddWorkday(dbContext, includedEmployeeId, new DateOnly(2026, 7, 31), shiftId, 120);
        AddWorkday(dbContext, includedEmployeeId, new DateOnly(2026, 8, 1), shiftId, 150);
        AddWorkday(dbContext, deletedEmployeeId, new DateOnly(2026, 7, 10), shiftId, 150);
        await dbContext.SaveChangesAsync();

        var rows = await new DatabaseMealAllowanceWorkdaySource(dbContext).LoadAsync(
            new(7, 2026, includedEmployeeId));

        var row = Assert.Single(rows);
        Assert.Equal(includedEmployeeId, row.EmployeeId);
        Assert.Equal("regular", row.Workday.WorkdayType);
        Assert.Equal("SX-A", row.Workday.Shift.Code);
        Assert.Equal(120, row.Workday.OvertimeMinutesAtRate15);
    }

    private static Guid AddEmployee(ApplicationDbContext dbContext, string code, bool isDeleted)
    {
        var id = Guid.NewGuid();
        dbContext.Employees.Add(new AttendanceGatewayEmployeeRow
        {
            Id = id,
            EmployeeCode = code,
            FirstName = "Test",
            LastName = code,
            IsDeleted = isDeleted,
            CreatedAtUtc = DateTime.UnixEpoch
        });
        return id;
    }

    private static void AddWorkday(ApplicationDbContext dbContext, Guid employeeId, DateOnly date, Guid shiftId, int overtimeMinutes) =>
        dbContext.AttendanceWorkdaySummaries.Add(new AttendanceWorkdaySummaryRow
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            WorkDate = date,
            DayType = "regular",
            ShiftId = shiftId,
            OvertimeMinutes15 = overtimeMinutes,
            ComputedAtUtc = DateTime.UnixEpoch,
            CreatedAtUtc = DateTime.UnixEpoch
        });
}
