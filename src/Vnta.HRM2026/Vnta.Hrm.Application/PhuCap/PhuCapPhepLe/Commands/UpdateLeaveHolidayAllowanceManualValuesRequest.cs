namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public sealed record UpdateLeaveHolidayAllowanceManualValuesRequest(
    Guid PayrollAllowanceSummaryRecordId,
    decimal DailyWageAmount,
    decimal LeaveDayCount,
    decimal HolidayDayCount,
    string? Note,
    string? Actor = null,
    DateTime? OriginalUpdatedAtUtc = null);
