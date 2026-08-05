namespace Vnta.Hrm.Web.Client.Models.Attendance;

/// <summary>
/// Request bất biến để tải một trang Bảng công tháng từ provider.
/// </summary>
public sealed record MonthlyWorkSummaryPageRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    string? SearchText,
    int Skip,
    int Take);
