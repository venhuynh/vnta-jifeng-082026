namespace Vnta.Hrm.Application.Integrations.AttendanceGateway;

public sealed record AttendanceGatewayAttendanceLogDto(
    string EmployeeCode,
    DateTime TapTime,
    int VerificationMode,
    int InOutMode);
