namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>
/// Yêu cầu xuất trọn kỳ đã áp dụng; client không được gửi filter, phân trang hoặc selection.
/// </summary>
public sealed record PayrollAllowanceSummaryExportRequest(
    int PayrollYear,
    int PayrollMonth,
    PayrollAllowanceSummaryExportFormat Format);
