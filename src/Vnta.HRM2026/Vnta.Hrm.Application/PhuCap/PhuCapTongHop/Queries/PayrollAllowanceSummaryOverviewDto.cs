namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Queries;

/// <summary>Chỉ số tổng quan của danh sách tổng hợp phụ cấp trong kỳ đang xem.</summary>
public sealed record PayrollAllowanceSummaryOverviewDto(
    int TotalCount,
    int OpenCount,
    int LockedCount,
    decimal TotalAllowanceAmount);
