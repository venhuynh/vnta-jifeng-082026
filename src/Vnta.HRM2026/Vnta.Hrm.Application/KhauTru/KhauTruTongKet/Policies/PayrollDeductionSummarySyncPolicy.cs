namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;

/// <summary>
/// Decides how one employee's target snapshot participates in synchronization from the previous month.
/// Data access and row mutation remain the responsibility of the calling application service.
/// </summary>
public static class PayrollDeductionSummarySyncPolicy
{
    public static PayrollDeductionSummarySyncAction Decide(
        PayrollDeductionSummaryPreviousMonthSyncInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if(input.TargetSnapshotState == PayrollDeductionSummarySyncTargetState.Locked)
        {
            return PayrollDeductionSummarySyncAction.PreserveLockedTarget;
        }

        return (input.TargetSnapshotState, input.PreviousMonthSnapshotState) switch
        {
            (PayrollDeductionSummarySyncTargetState.Absent, PayrollDeductionSummarySyncSourceState.Available)
                => PayrollDeductionSummarySyncAction.CreateTargetFromPreviousMonth,
            (PayrollDeductionSummarySyncTargetState.Absent, PayrollDeductionSummarySyncSourceState.Missing)
                => PayrollDeductionSummarySyncAction.CreateEmptyTarget,
            (PayrollDeductionSummarySyncTargetState.Unlocked, PayrollDeductionSummarySyncSourceState.Available)
                => PayrollDeductionSummarySyncAction.UpdateUnlockedTargetFromPreviousMonth,
            (PayrollDeductionSummarySyncTargetState.Unlocked, PayrollDeductionSummarySyncSourceState.Missing)
                => PayrollDeductionSummarySyncAction.KeepUnlockedTargetAndEnsureDetails,
            _ => throw new ArgumentOutOfRangeException(nameof(input), input, "Trạng thái đồng bộ tổng kết khấu trừ không hợp lệ.")
        };
    }
}

public sealed record PayrollDeductionSummaryPreviousMonthSyncInput(
    PayrollDeductionSummarySyncTargetState TargetSnapshotState,
    PayrollDeductionSummarySyncSourceState PreviousMonthSnapshotState);

public enum PayrollDeductionSummarySyncTargetState
{
    Absent,
    Unlocked,
    Locked
}

public enum PayrollDeductionSummarySyncSourceState
{
    Missing,
    Available
}

public enum PayrollDeductionSummarySyncAction
{
    PreserveLockedTarget,
    CreateTargetFromPreviousMonth,
    CreateEmptyTarget,
    UpdateUnlockedTargetFromPreviousMonth,
    KeepUnlockedTargetAndEnsureDetails
}
