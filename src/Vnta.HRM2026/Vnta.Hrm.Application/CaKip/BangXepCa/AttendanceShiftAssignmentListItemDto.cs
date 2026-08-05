namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record AttendanceShiftAssignmentListItemDto(
    Guid Id,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    Guid DepartmentId,
    string? DepartmentCode,
    string? DepartmentName,
    string? DepartmentPath,
    Guid ShiftId,
    string? ShiftCode,
    string? ShiftName,
    string? ShiftShortName,
    string? ShiftColorHex,
    bool IsOvernight,
    DateOnly WorkDate,
    string CreationType,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
