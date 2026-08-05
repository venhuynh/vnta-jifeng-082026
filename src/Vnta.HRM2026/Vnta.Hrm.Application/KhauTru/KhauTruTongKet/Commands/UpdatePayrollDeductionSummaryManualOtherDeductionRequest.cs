namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record UpdatePayrollDeductionSummaryManualOtherDeductionRequest(
    Guid Id,
    decimal OtherDeductionAmount,
    string? Note,
    DateTime OriginalUpdatedAtUtc,
    string? Actor = null);
