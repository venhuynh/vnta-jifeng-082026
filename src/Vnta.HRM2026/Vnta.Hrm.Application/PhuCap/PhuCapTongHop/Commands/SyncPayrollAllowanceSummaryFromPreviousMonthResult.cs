namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>Thống kê kết quả sao chép snapshot từ kỳ trước, gồm cả các dòng bị bỏ qua và xóa do không còn nhân sự nguồn.</summary>
public sealed record SyncPayrollAllowanceSummaryFromPreviousMonthResult(
    int SourcePayrollMonth,
    int SourcePayrollYear,
    int TargetPayrollMonth,
    int TargetPayrollYear,
    int SourceRecordCount,
    int CreatedCount,
    int UpdatedCount,
    int SkippedLockedCount,
    int AttendanceEmployeeCount,
    int RemovedCount);
