namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record SetPayrollDeductionSummaryLockStateRequest(
    Guid Id,
    bool IsLocked,
    DateTime? OriginalUpdatedAtUtc = null,
    string? Actor = null);
