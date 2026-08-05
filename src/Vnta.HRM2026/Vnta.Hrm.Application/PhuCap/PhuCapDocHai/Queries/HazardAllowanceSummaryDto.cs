namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>
/// Số lượng các nhóm hiển thị trên thanh tổng hợp của màn Phụ cấp độc hại.
/// Các số liệu luôn được tính trên toàn bộ tập filter tìm kiếm, không phụ thuộc trang hiện tại.
/// </summary>
/// <param name="TotalCount">Tổng snapshot sau filter text/kỳ lương.</param>
/// <param name="EligibleCount">Số snapshot đang được hưởng phụ cấp.</param>
/// <param name="ExceptionCount">Số snapshot đang thuộc ngoại lệ, không hưởng phụ cấp.</param>
/// <param name="LockedCount">Số snapshot đã khóa.</param>
/// <param name="OpenCount">Số snapshot đang mở.</param>
public sealed record HazardAllowanceSummaryDto(
    int TotalCount,
    int EligibleCount,
    int ExceptionCount,
    int LockedCount,
    int OpenCount);
