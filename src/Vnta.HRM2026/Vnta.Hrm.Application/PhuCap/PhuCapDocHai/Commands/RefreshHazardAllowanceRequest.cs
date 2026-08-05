namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

public sealed record RefreshHazardAllowanceRequest(
    int PayrollMonth,
    int PayrollYear,
    string RequestedBy,
    Guid? PayrollAllowanceSummaryRecordId = null);
