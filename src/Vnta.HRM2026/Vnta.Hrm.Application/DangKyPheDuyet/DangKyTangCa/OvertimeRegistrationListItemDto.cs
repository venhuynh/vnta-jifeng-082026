namespace Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

public sealed record OvertimeRegistrationListItemDto(
    Guid Id,
    DateOnly WorkDate,
    AttendanceWorkCalendarDayType DayType,
    string WorkshopCode,
    string WorkshopName,
    string RequestedBy,
    string ApprovedBy,
    OvertimeRegistrationStatus Status,
    string Note,
    DateTime LastActionAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? ApprovedAtUtc,
    IReadOnlyList<OvertimeRegistrationEmployeeAssignmentDto> EmployeeAssignments);
