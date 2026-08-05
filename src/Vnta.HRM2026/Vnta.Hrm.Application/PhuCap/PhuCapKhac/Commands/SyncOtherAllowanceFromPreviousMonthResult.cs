namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

/// <summary>Summarizes a safe previous-period copy without overwriting target data.</summary>
public sealed record SyncOtherAllowanceFromPreviousMonthResult(
    int SourcePayrollMonth,
    int SourcePayrollYear,
    int TargetPayrollMonth,
    int TargetPayrollYear,
    int SourceRowCount,
    int CreatedCount,
    int UpdatedFixedCount,
    int SkippedExistingCount,
    int SkippedTargetSummaryLockedCount,
    int SkippedTargetDetailLockedCount,
    int SkippedMissingTargetSummaryCount);
