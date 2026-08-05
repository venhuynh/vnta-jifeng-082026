namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>Kết quả cấp thấp của thao tác sao chép snapshot từ tháng trước; được giữ để tái sử dụng trong nghiệp vụ nội bộ.</summary>
public sealed record CopyPayrollAllowanceSummaryFromPreviousMonthResult(
    int PreviousPayrollYear,
    int PreviousPayrollMonth,
    int PayrollYear,
    int PayrollMonth,
    int SourceRowCount,
    int CopiedCount,
    int SkippedLockedCount,
    int SkippedMissingSourceCount);
