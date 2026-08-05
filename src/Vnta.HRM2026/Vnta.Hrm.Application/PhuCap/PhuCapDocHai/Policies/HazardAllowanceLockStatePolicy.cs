namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

public enum HazardAllowanceRowLockState
{
    Open = 0,
    Locked = 1
}

/// <summary>Pure idempotency rule for a requested hazard allowance lock transition.</summary>
public sealed class HazardAllowanceLockStatePolicy
{
    public bool ShouldUpdate(
        HazardAllowanceRowLockState currentLockState,
        HazardAllowanceRowLockState requestedLockState) =>
        currentLockState != requestedLockState;
}
