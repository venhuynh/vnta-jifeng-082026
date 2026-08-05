namespace Vnta.Hrm.Application.NhanSu.NhanVien;

/// <summary>
/// Contract hẹp cho summary badge của màn Nhân viên.
/// Contract legacy <see cref="IEmployeeService"/> vẫn được giữ cho consumer cũ.
/// </summary>
public interface INhanVienSummaryReadService
{
    Task<EmployeeSummaryDto> GetSummaryAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default);
}
