using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>
/// Hợp đồng đọc phân trang cho màn Phụ cấp chuyên cần. Kỳ lương và giới hạn
/// phân trang được kiểm tra ở server để UI không trở thành nguồn quyết định dữ liệu.
/// </summary>
public interface IAttendanceAllowanceReadService
{
    /// <summary>
    /// Đọc các mã được cấu hình tính CTL từ danh mục kết quả chấm công.
    /// </summary>
    Task<AttendanceAllowanceRuleDto> GetRuleAsync(
        CancellationToken cancellationToken = default);

    Task<AttendanceAllowanceResultPageDto> SearchPageAsync(
        AttendanceAllowanceResultFilter filter,
        CancellationToken cancellationToken = default);

}
