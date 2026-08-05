using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;

/// <summary>Changes an other-allowance row lock state.</summary>
public interface IOtherAllowanceLockService
{
    Task SetLockStateAsync(
        SetOtherAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default);

    Task<SetOtherAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetOtherAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default);
}
