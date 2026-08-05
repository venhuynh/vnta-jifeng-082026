namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

/// <summary>
/// Reconciles one existing deduction-summary snapshot with its detail records.
/// </summary>
public sealed record RefreshPayrollDeductionSummaryRequest(
    Guid SummaryRecordId,
    int PayrollYear,
    int PayrollMonth,
    DateTime OriginalUpdatedAtUtc,
    string? Actor = null);
