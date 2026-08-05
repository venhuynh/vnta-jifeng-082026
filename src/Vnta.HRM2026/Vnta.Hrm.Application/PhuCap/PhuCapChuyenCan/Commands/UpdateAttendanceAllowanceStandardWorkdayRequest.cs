namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

/// <summary>Updates only the manually editable standard-workday value for an unlocked row.</summary>
public sealed record UpdateAttendanceAllowanceStandardWorkdayRequest(Guid Id, decimal StandardWorkdayCount, DateTime? OriginalUpdatedAtUtc);
