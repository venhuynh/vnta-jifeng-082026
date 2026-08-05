namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public sealed record RecalculateLeaveHolidayAllowanceRequest(
    int PayrollMonth,
    int PayrollYear,
    string? Actor = null,
    Guid? PayrollAllowanceSummaryRecordId = null);
