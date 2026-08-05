namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public interface INhanVienStatusService
{
    Task<EmployeeListItemDto> ChangeStatusAsync(
        ChangeEmployeeStatusRequest request,
        CancellationToken cancellationToken = default);
}
