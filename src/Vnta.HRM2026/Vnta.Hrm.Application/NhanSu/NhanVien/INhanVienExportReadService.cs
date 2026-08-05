namespace Vnta.Hrm.Application.NhanSu.NhanVien;

/// <summary>
/// Đọc toàn bộ danh sách nhân viên đang hoạt động cho export. Không dùng paging của grid.
/// </summary>
public interface INhanVienExportReadService
{
    Task<IReadOnlyList<EmployeeListItemDto>> ExportAllAsync(
        CancellationToken cancellationToken = default);
}
