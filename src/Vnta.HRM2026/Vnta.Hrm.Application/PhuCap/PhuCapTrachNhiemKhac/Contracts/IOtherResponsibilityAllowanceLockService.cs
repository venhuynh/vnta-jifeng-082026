namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Commands;

public interface IOtherResponsibilityAllowanceLockService
{
    Task<SetOtherResponsibilityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetOtherResponsibilityAllowanceBatchLockStateRequest request,
        string? requestedBy = null,
        CancellationToken cancellationToken = default);
}
