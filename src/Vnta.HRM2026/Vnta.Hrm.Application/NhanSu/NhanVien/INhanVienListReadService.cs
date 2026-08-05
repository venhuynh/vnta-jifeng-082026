namespace Vnta.Hrm.Application.NhanSu.NhanVien;

/// <summary>
/// Contract đọc danh sách nhân viên theo trang. UI chỉ nhận DTO đã được định hình cho màn hình,
/// không biết entity hoặc cách truy vấn persistence.
/// </summary>
public interface INhanVienListReadService
{
    Task<NhanVienListPageDto> SearchPageAsync(
        NhanVienListQuery query,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot bất biến của một lần tải danh sách nhân viên. Skip/Take được server chuẩn hóa
/// để không cho client yêu cầu toàn bộ dữ liệu chỉ nhằm phân trang tại giao diện.
/// </summary>
public sealed record NhanVienListQuery(
    string? SearchText,
    IReadOnlyList<int>? Statuses = null,
    int Skip = 0,
    int Take = 50);

public sealed record NhanVienListPageDto(
    IReadOnlyList<EmployeeListItemDto> Rows,
    int TotalCount);
