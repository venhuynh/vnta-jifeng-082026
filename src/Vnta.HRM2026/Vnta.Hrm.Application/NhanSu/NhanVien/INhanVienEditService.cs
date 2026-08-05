namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public interface INhanVienEditService
{
    Task<EmployeeListItemDto> UpdateAsync(
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);
}
