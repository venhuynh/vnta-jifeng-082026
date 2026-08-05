namespace Vnta.Hrm.Application.DangTrienKhai.DuLieuSinhTracHoc;

public sealed record AttendanceBiometricDeviceCommandBatchResult(
    int CommandsCreated,
    int MatchedEmployees,
    IReadOnlyList<string> DeviceSerialNumbers,
    IReadOnlyList<Guid> EmployeeIds);
