namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Queries;

/// <summary>
/// Trang read-only của bảng tổng hợp phụ cấp. UI không nhận toàn bộ snapshot kỳ
/// chỉ để DxGrid tự phân trang ở client.
/// </summary>
public sealed record PayrollAllowanceSummaryPageDto(
    IReadOnlyList<PayrollAllowanceSummaryListItemDto> Rows,
    int TotalCount);
