using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;

/// <summary>Translates persisted/API primitives at the infrastructure boundary into policy concepts.</summary>
internal static class OtherAllowancePolicyAdapter
{
    public static OtherAllowanceAmountType ToAmountType(bool isFixedAmount) =>
        isFixedAmount ? OtherAllowanceAmountType.Fixed : OtherAllowanceAmountType.NonFixed;

    public static OtherAllowanceLockState ToLockState(bool isLocked) =>
        isLocked ? OtherAllowanceLockState.Locked : OtherAllowanceLockState.Unlocked;
}
