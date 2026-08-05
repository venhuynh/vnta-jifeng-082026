namespace Vnta.Hrm.Application.ChamCong.BangCongThang;

/// <summary>
/// TotalCount là tổng sau filter, không phải số dòng của page đang trả về.
/// </summary>
public sealed record AttendanceMonthlyWorkSummaryGridPageDto(
    IReadOnlyList<AttendanceMonthlyWorkSummaryGridRowDto> Rows,
    int TotalCount);
