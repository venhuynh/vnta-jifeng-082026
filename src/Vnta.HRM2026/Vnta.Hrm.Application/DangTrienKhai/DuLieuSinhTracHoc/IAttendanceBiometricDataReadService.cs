namespace Vnta.Hrm.Application.DangTrienKhai.DuLieuSinhTracHoc;

public interface IAttendanceBiometricDataReadService
{
    Task<IReadOnlyList<AttendanceBiometricDataListItemDto>> SearchAsync(
        AttendanceBiometricDataFilter filter,
        CancellationToken cancellationToken = default);
}
