using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Infrastructure.ChamCong.CodeKetQuaTinhCong;
using Vnta.Hrm.Infrastructure.DangTrienKhai.BangCongNgay;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapThamNien;

public sealed class DatabasePayrollEmployeeSeniorityAllowanceWorkdaySourceTests
{
    [Fact]
    public async Task Load_uses_the_seniority_status_flag_to_identify_administrative_workdays()
    {
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"seniority-allowance-workday-source-{Guid.NewGuid():N}")
                .Options);
        var employeeId = Guid.NewGuid();
        var seniorityStatusId = Guid.NewGuid();
        var administrativeOnlyStatusId = Guid.NewGuid();
        var timestamp = DateTime.UnixEpoch;

        dbContext.AttendanceStatusCodes.AddRange(
            new AttendanceStatusCodeRow
            {
                Id = seniorityStatusId,
                Code = "SENIORITY",
                Name = "Thâm niên",
                Kind = "Test",
                PhuCapThamNien = true,
                CongHanhChinh = false,
                IsActive = true,
                CreatedAtUtc = timestamp
            },
            new AttendanceStatusCodeRow
            {
                Id = administrativeOnlyStatusId,
                Code = "ADMINISTRATIVE",
                Name = "Công hành chính",
                Kind = "Test",
                PhuCapThamNien = false,
                CongHanhChinh = true,
                IsActive = true,
                CreatedAtUtc = timestamp
            });
        dbContext.AttendanceWorkdaySummaries.AddRange(
            CreateWorkday(employeeId, new DateOnly(2026, 8, 1), seniorityStatusId, 15, 5, timestamp),
            CreateWorkday(employeeId, new DateOnly(2026, 8, 2), administrativeOnlyStatusId, 30, 10, timestamp));
        await dbContext.SaveChangesAsync();

        var results = await new DatabasePayrollEmployeeSeniorityAllowanceWorkdaySource(dbContext).LoadAsync(
            new PayrollEmployeeSeniorityAllowanceWorkdaySourceQuery(2026, 8, [employeeId]));

        var workdays = results[employeeId].OrderBy(x => x.LateMinutes).ToArray();
        Assert.Collection(
            workdays,
            workday =>
            {
                Assert.Equal(PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Included, workday.Eligibility);
                Assert.Equal(15, workday.LateMinutes);
                Assert.Equal(5, workday.EarlyLeaveMinutes);
            },
            workday =>
            {
                Assert.Equal(PayrollEmployeeSeniorityAllowanceWorkdayEligibility.Excluded, workday.Eligibility);
                Assert.Equal(30, workday.LateMinutes);
                Assert.Equal(10, workday.EarlyLeaveMinutes);
            });
    }

    private static AttendanceWorkdaySummaryRow CreateWorkday(
        Guid employeeId,
        DateOnly workDate,
        Guid statusCodeId,
        int lateMinutes,
        int earlyLeaveMinutes,
        DateTime timestamp) => new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            WorkDate = workDate,
            CodeKetQuaTinhCongId = statusCodeId,
            LateMinutes = lateMinutes,
            EarlyLeaveMinutes = earlyLeaveMinutes,
            ComputedAtUtc = timestamp,
            CreatedAtUtc = timestamp
        };
}
