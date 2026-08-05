namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

public sealed record SetAttendanceAllowanceLockStateRequest(Guid Id, bool IsLocked, DateTime? OriginalUpdatedAtUtc);
