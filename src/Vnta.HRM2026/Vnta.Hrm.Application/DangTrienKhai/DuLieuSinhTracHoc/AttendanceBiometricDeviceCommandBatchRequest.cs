namespace Vnta.Hrm.Application.DangTrienKhai.DuLieuSinhTracHoc;

public sealed record AttendanceBiometricDeviceCommandBatchRequest(
    IReadOnlyList<Guid> EmployeeIds,
    IReadOnlyList<string> DeviceSerialNumbers);
