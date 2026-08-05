namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Queries;

/// <summary>
/// Kết quả truy vấn phụ cấp cơm theo trang. TotalCount là tổng số dòng sau filter,
/// không phải số dòng thực tế của trang đang trả về; giá trị -1 nghĩa là client không yêu cầu count.
/// </summary>
public sealed record MealAllowancePageDto(
    IReadOnlyList<MealAllowanceListItemDto> Rows,
    int TotalCount);
