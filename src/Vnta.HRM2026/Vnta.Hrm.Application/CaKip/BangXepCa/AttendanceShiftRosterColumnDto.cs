namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record AttendanceShiftRosterColumnDto(
    DateOnly WorkDate,
    string HeaderText,
    string WeekdayText,
    bool IsSunday);
