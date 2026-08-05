namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

/// <summary>
/// Đồng bộ lại các snapshot tổng kết khấu trừ đã tồn tại của một kỳ từ các dòng chi tiết cùng kỳ.
/// </summary>
public sealed record RecalculatePayrollDeductionSummaryPeriodRequest(
    int PayrollYear,
    int PayrollMonth,
    string? Actor = null);
