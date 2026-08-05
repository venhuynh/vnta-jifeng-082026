namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record AttendanceShiftRosterCellDto(
    DateOnly WorkDate,
    Guid? ShiftAssignmentId,
    Guid? ShiftId,
    string? ShiftCode,
    string? ShiftName,
    string? ShiftShortName,
    string? ShiftColorHex,
    string? CreationType,
    bool IsSunday,
    bool HasConflict);
