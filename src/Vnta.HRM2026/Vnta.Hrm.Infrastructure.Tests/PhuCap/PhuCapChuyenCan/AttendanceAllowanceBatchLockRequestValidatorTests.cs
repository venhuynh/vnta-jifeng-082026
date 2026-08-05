using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapChuyenCan;

public sealed class AttendanceAllowanceBatchLockRequestValidatorTests
{
    private readonly AttendanceAllowanceRequestValidator validator = new();

    [Fact]
    public void Whole_period_scope_rejects_row_items_instead_of_inferring_a_selected_scope()
    {
        var result = validator.Validate(new SetAttendanceAllowanceBatchLockStateRequest(
            2026,
            7,
            true,
            AttendanceAllowanceBatchLockScope.WholePeriod,
            [new AttendanceAllowanceLockItem(Guid.NewGuid(), DateTime.UtcNow)]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Selected_rows_scope_requires_at_least_one_versioned_row()
    {
        var result = validator.Validate(new SetAttendanceAllowanceBatchLockStateRequest(
            2026,
            7,
            true,
            AttendanceAllowanceBatchLockScope.SelectedRows));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Selected_rows_scope_accepts_a_versioned_row()
    {
        var result = validator.Validate(new SetAttendanceAllowanceBatchLockStateRequest(
            2026,
            7,
            true,
            AttendanceAllowanceBatchLockScope.SelectedRows,
            [new AttendanceAllowanceLockItem(Guid.NewGuid(), DateTime.UtcNow)]));

        Assert.True(result.IsValid);
    }
}
