namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Queries;

/// <summary>
/// Điều kiện lọc và phân trang danh sách snapshot tổng hợp phụ cấp.
/// Bỏ trống kỳ lương để truy vấn toàn bộ dữ liệu trong phạm vi quyền của người dùng.
/// </summary>
public sealed record PayrollAllowanceSummaryFilter(
    int? PayrollMonth,
    int? PayrollYear,
    string? SearchText,
    bool? IsLocked = null,
    int Skip = 0,
    int Take = 50);
