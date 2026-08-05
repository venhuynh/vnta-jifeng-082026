namespace Vnta.Hrm.Application.TinhLuong.BangCongTongHop;

/// <summary>
/// Kết quả tổng hợp dữ liệu công đầu vào cho một kỳ lương.
/// </summary>
public sealed record RefreshPayrollMonthlyWorkInputsResult(
    int PayrollMonth,
    int PayrollYear,
    int EmployeeCount,
    int CreatedCount,
    int UpdatedCount,
    int SkippedLockedCount);
