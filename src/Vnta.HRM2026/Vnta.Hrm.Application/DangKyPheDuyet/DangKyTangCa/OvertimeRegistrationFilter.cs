namespace Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

public sealed record OvertimeRegistrationFilter(
    DateOnly? WorkDate,
    AttendanceWorkCalendarDayType? DayType,
    OvertimeRegistrationStatus? Status,
    string? SearchText,
    int Take = 500);
