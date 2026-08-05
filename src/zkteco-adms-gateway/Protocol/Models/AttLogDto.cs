namespace Vnta.AttendanceGateway.Protocol.Models;

public record AttLogDto(
    string EmployeeCode,
    DateTime TapTime,
    int VerificationMode,
    int InOutMode
);
