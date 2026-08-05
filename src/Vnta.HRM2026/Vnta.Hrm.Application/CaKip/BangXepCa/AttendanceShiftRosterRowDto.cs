namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record AttendanceShiftRosterRowDto(
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string EmployeeDisplay,
    Guid DepartmentId,
    string? DepartmentName,
    string? DepartmentPath,
    IReadOnlyList<AttendanceShiftRosterCellDto> Cells);
