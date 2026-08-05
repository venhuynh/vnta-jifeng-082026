namespace Vnta.Hrm.Application.NhanSu.NhanVien;

/// <summary>
/// Command đồng bộ dữ liệu Nhân viên từ nguồn attendance.
/// </summary>
public interface INhanVienRefreshService
{
    Task<EmployeeRefreshResult> RefreshFromDeviceUserProfilesAsync(
        CancellationToken cancellationToken = default);
}
