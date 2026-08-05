namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

/// <summary>Explicit target boundary for a batch lock transition.</summary>
public enum AttendanceAllowanceBatchLockScope
{
    WholePeriod = 1,
    SelectedRows = 2
}

/// <summary>
/// Changes the lock state for one unambiguous target scope. Whole-period requests
/// must not carry row items; selected-row requests carry each row's concurrency version.
/// </summary>
public sealed record SetAttendanceAllowanceBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    AttendanceAllowanceBatchLockScope Scope,
    IReadOnlyList<AttendanceAllowanceLockItem>? Items = null);

public sealed record AttendanceAllowanceLockItem(Guid Id, DateTime? OriginalUpdatedAtUtc);
