namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record AttendanceShiftAssignmentEnsureIssueDto(
    string Code,
    string Message,
    Guid? EmployeeId = null,
    Guid? SettingId = null);
