namespace Vnta.Hrm.Application.TinhLuong.BangCongTongHop;

/// <summary>
/// Yêu cầu tổng hợp dữ liệu công đầu vào cho một kỳ lương.
/// </summary>
public sealed record RefreshPayrollMonthlyWorkInputsRequest(
    int PayrollMonth,
    int PayrollYear);
