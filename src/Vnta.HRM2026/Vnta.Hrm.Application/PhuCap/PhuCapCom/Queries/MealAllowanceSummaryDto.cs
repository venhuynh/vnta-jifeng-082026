namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Queries;

public sealed record MealAllowanceSummaryDto(
    int TotalCount,
    int QualifiedRuleCount,
    int ManualAdjustmentCount,
    int LockedCount,
    int OtherCount,
    int WithAllowanceCount,
    int WithoutAllowanceCount,
    decimal TotalAllowanceAmount);
