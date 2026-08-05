namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>
/// Một trang kết quả Phụ cấp độc hại. <see cref="TotalCount"/> là tổng dòng sau
/// khi áp dụng đầy đủ filter server-side, không phải số dòng trong <see cref="Rows"/>.
/// </summary>
/// <param name="Rows">Các dòng thuộc đúng trang requested, đã sắp xếp xác định tại server.</param>
/// <param name="TotalCount">Tổng dòng sau filter; -1 nghĩa là caller chủ động không yêu cầu count.</param>
public sealed record HazardAllowancePageDto(
    IReadOnlyList<HazardAllowanceListItemDto> Rows,
    int TotalCount);
