namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record AttendanceShiftRosterSnapshotDto(
    IReadOnlyList<AttendanceShiftRosterColumnDto> Columns,
    IReadOnlyList<AttendanceShiftRosterRowDto> Rows,
    DateTime GeneratedAtUtc);
