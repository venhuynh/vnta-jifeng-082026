namespace Vnta.Hrm.Application.ChamCong.BangCongThang;

/// <summary>
/// Filter diễn tả một truy vấn read-only; implementation phải tự chuẩn hóa range và giới hạn paging.
/// </summary>
public sealed record AttendanceMonthlyWorkSummaryGridFilter(
    DateOnly FromDate,
    DateOnly ToDate,
    string? SearchText,
    int Skip = 0,
    int Take = 50,
    Guid? EmployeeId = null,
    bool IncludeShiftDetails = true);
