using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.ChamCong.CodeKetQuaTinhCong;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapChuyenCan;

public sealed class AttendanceAllowanceRuleTests
{
    [Fact]
    public async Task Get_rule_uses_attendance_allowance_status_flag_not_administrative_workday_flag()
    {
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"attendance-allowance-rule-{Guid.NewGuid():N}")
                .Options);
        var timestamp = DateTime.UtcNow;
        dbContext.AttendanceStatusCodes.AddRange(
            new AttendanceStatusCodeRow
            {
                Id = Guid.NewGuid(),
                Code = "ATTENDANCE",
                Name = "Chuyên cần",
                Kind = "Test",
                PhuCapChuyenCan = true,
                CongHanhChinh = false,
                IsActive = true,
                CreatedAtUtc = timestamp
            },
            new AttendanceStatusCodeRow
            {
                Id = Guid.NewGuid(),
                Code = "ADMINISTRATIVE",
                Name = "Công hành chính",
                Kind = "Test",
                PhuCapChuyenCan = false,
                CongHanhChinh = true,
                IsActive = true,
                CreatedAtUtc = timestamp
            });
        await dbContext.SaveChangesAsync();

        var service = new DatabaseAttendanceAllowanceReadService(
            dbContext,
            new DatabaseAttendanceAllowanceWorkdaySource(dbContext));

        var rule = await service.GetRuleAsync();

        Assert.Equal(["ATTENDANCE"], rule.EligibleStatusCodes);
        Assert.Equal(AttendanceAllowancePayrollPeriodPolicy.MinimumSupportedMonth, rule.Metadata.MinimumSupportedPayrollMonth);
        Assert.Equal(AttendanceAllowancePayrollPeriodPolicy.MinimumSupportedYear, rule.Metadata.MinimumSupportedPayrollYear);
        Assert.Equal(AttendanceAllowanceWorkdayMetricPolicy.LateEarlyMinutesPerWorkday, rule.Metadata.LateEarlyMinutesPerWorkday);
        Assert.Equal(AttendanceAllowanceCalculationPolicy.AttendanceClassAAmount, rule.Metadata.AttendanceClassAAmount);
    }
}
