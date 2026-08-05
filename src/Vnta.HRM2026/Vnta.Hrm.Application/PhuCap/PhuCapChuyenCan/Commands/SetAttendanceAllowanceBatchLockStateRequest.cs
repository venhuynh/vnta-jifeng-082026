namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

public sealed record SetAttendanceAllowanceBatchLockStateRequest(int PayrollYear, int PayrollMonth, bool IsLocked, IReadOnlyList<Guid>? AttendanceAllowanceRecordIds = null, IReadOnlyList<AttendanceAllowanceLockItem>? Items = null);

public sealed record AttendanceAllowanceLockItem(Guid Id, DateTime? OriginalUpdatedAtUtc);
