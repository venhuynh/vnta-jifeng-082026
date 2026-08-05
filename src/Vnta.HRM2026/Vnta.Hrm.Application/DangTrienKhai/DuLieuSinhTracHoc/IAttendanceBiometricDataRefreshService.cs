namespace Vnta.Hrm.Application.DangTrienKhai.DuLieuSinhTracHoc;

public interface IAttendanceBiometricDataRefreshService
{
    Task<AttendanceBiometricDataRefreshProgress> GetProgressAsync(
        CancellationToken cancellationToken = default);

    Task<AttendanceBiometricDataRefreshResult> RefreshAsync(
        CancellationToken cancellationToken = default);

    Task<AttendanceBiometricDataRefreshResult> RefreshAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
