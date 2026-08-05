namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;

public sealed record RefreshMealAllowanceResult(
    int TargetPayrollMonth,
    int TargetPayrollYear,
    int SummaryTargetCount,
    int QualifiedEmployeeCount,
    int CreatedCount,
    int UpdatedCount,
    int SkippedLockedCount,
    int SkippedManualAdjustmentCount);
