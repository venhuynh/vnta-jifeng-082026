namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Changes lock state for selected hazard snapshots or a payroll period.</summary>
public interface IHazardAllowanceLockService
{
    Task SetLockStateAsync(
        SetHazardAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default);

    Task<SetHazardAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetHazardAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default);
}
