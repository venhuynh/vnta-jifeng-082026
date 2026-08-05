namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public sealed record UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest(
    Guid PayrollAllowanceSummaryRecordId,
    decimal AllowanceAmount,
    string? Note,
    DateTime OriginalUpdatedAtUtc);
