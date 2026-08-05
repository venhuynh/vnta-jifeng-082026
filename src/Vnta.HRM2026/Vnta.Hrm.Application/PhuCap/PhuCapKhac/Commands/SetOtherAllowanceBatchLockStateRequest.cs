using System.Text.Json.Serialization;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

public sealed record SetOtherAllowanceBatchLockStateRequest(
    int PayrollMonth,
    int PayrollYear,
    bool IsLocked,
    IReadOnlyList<Guid>? Ids,
    [property: JsonIgnore] string RequestedBy = "",
    IReadOnlyList<OtherAllowanceLockItem>? Items = null);

/// <summary>One selected row and its optimistic-concurrency version.</summary>
public sealed record OtherAllowanceLockItem(Guid Id, DateTime? OriginalUpdatedAtUtc);

/// <summary>Outcome of a row-selection or whole-period lock-state transition.</summary>
public sealed record SetOtherAllowanceBatchLockStateResult(
    int TargetRowCount,
    int UpdatedCount,
    int UnchangedCount = 0,
    int SkippedSummaryLockedCount = 0,
    bool IsLocked = false,
    bool IsWholePeriod = false);
