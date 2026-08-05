using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapChuyenCan;

public sealed class AttendanceAllowanceWorkdayAdjustmentPolicyTests
{
    [Theory]
    [InlineData(-0.25, 26)]
    [InlineData(26.25, 26)]
    [InlineData(20, 0)]
    public void Validate_rejects_an_invalid_atomic_workday_pair(decimal actualWorkdayCount, decimal standardWorkdayCount)
    {
        var result = new AttendanceAllowanceWorkdayAdjustmentPolicy().Validate(new UpdateAttendanceAllowanceWorkdaysRequest(
            Guid.NewGuid(), actualWorkdayCount, standardWorkdayCount, DateTime.UnixEpoch));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_rejects_a_missing_aggregate_id()
    {
        var result = new AttendanceAllowanceWorkdayAdjustmentPolicy().Validate(new UpdateAttendanceAllowanceWorkdaysRequest(
            Guid.Empty, 20m, 26m, DateTime.UnixEpoch));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_accepts_a_valid_atomic_workday_pair()
    {
        var result = new AttendanceAllowanceWorkdayAdjustmentPolicy().Validate(new UpdateAttendanceAllowanceWorkdaysRequest(
            Guid.NewGuid(), 20.5m, 26m, DateTime.UnixEpoch));

        Assert.True(result.IsValid);
    }
}
