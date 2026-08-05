namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

/// <summary>
/// Trang read-only của phụ cấp chuyên cần. Contract không trả toàn bộ snapshot
/// kỳ chỉ để grid phân trang ở client.
/// </summary>
public sealed record AttendanceAllowanceResultPageDto(
    IReadOnlyList<AttendanceAllowanceResultListItemDto> Rows,
    int TotalCount,
    int OpenCount,
    int LockedCount,
    int AttendanceClassACount = 0,
    int AttendanceClassBCount = 0,
    int AttendanceClassCCount = 0,
    int PeriodTotalCount = 0,
    int PeriodCanLockCount = 0,
    int PeriodCanUnlockCount = 0,
    int PeriodSummaryLockedCount = 0);
