namespace Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

public sealed record CreateOvertimeRegistrationDraftRequest(
    DateOnly WorkDate,
    AttendanceWorkCalendarDayType DayType);
