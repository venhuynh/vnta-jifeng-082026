namespace Vnta.Hrm.Application.DangTrienKhai.DuLieuSinhTracHoc;

public interface IAttendanceBiometricDeviceQueueService
{
    Task<AttendanceBiometricDeviceCommandBatchResult> CreatePushCommandsAsync(
        AttendanceBiometricDeviceCommandBatchRequest request,
        CancellationToken cancellationToken = default);

    Task<AttendanceBiometricDeviceCommandBatchResult> CreateDeleteCommandsAsync(
        AttendanceBiometricDeviceCommandBatchRequest request,
        CancellationToken cancellationToken = default);
}
