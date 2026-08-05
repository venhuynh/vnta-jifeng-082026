namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record SetPayrollDeductionSummaryBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    IReadOnlyList<Guid>? PayrollDeductionSummaryRecordIds = null,
    string? Actor = null,
    IReadOnlyList<PayrollDeductionSummaryLockItem>? Items = null);

public sealed record PayrollDeductionSummaryLockItem(Guid Id, DateTime? OriginalUpdatedAtUtc);
