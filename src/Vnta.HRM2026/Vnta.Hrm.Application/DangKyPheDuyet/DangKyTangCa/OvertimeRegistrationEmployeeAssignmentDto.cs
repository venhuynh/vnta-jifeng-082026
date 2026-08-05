namespace Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

public sealed record OvertimeRegistrationEmployeeAssignmentDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string PositionName,
    string TeamCode,
    string TeamName,
    OvertimeEmployeeAssignmentType AssignmentType,
    string RegistrationHint);
