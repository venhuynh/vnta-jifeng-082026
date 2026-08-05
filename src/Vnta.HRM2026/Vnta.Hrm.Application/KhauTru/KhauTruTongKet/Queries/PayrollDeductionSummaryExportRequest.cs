namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

/// <summary>
/// Yêu cầu xuất trọn kỳ đã áp dụng; không nhận filter, phân trang hoặc selection từ client.
/// </summary>
public sealed record PayrollDeductionSummaryExportRequest(
    int PayrollYear,
    int PayrollMonth,
    PayrollDeductionSummaryExportFormat Format);
