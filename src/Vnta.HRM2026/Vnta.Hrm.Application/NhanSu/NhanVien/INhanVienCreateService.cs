namespace Vnta.Hrm.Application.NhanSu.NhanVien;

/// <summary>
/// Command tạo mới Nhân viên. Read và refresh dùng contract riêng.
/// </summary>
public interface INhanVienCreateService
{
    Task<EmployeeListItemDto> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default);
}
