namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

/// <summary>Updates only the manually editable actual-workday value; the server owns derived values and lock state.</summary>
public sealed record UpdateAttendanceAllowanceActualWorkdayRequest(Guid Id, decimal ActualWorkdayCount, DateTime? OriginalUpdatedAtUtc);
