namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Commands;

public sealed record RecalculateOtherResponsibilityAllowanceResult(
    int RecalculatedCount,
    int SkippedLockedCount);
