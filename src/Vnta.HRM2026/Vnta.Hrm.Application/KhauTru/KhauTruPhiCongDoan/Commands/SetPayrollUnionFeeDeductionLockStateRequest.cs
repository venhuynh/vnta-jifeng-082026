namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public sealed record SetPayrollUnionFeeDeductionLockStateRequest(
    Guid Id,
    bool IsLocked,
    DateTime? OriginalUpdatedAtUtc);
