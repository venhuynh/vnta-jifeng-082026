namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public sealed record ClearLeaveHolidayAllowanceManualValuesRequest(
    IReadOnlyCollection<Guid> PayrollAllowanceSummaryRecordIds,
    string? Actor = null);
