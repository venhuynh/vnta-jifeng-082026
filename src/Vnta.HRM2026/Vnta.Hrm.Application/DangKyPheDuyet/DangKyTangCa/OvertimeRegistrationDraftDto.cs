namespace Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

public sealed record OvertimeRegistrationDraftDto(
    Guid Id,
    DateOnly WorkDate,
    AttendanceWorkCalendarDayType DayType,
    string WorkshopCode,
    string WorkshopName,
    string RequestedBy,
    string ApprovedBy,
    OvertimeRegistrationStatus Status,
    string Note,
    IReadOnlyList<OvertimeRegistrationEmployeeAssignmentDto> EmployeeAssignments);
