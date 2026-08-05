namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public interface IEmployeeRefreshService
{
    Task<EmployeeRefreshResult> RefreshFromDeviceUserProfilesAsync(CancellationToken cancellationToken = default);
}
