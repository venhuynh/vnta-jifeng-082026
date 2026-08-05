namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>Yêu cầu khóa hoặc mở khóa một snapshot, có kèm phiên bản ban đầu để kiểm soát đồng thời.</summary>
public sealed record SetPayrollAllowanceSummaryLockStateRequest(
    Guid Id,
    bool IsLocked,
    DateTime? OriginalUpdatedAtUtc,
    string? Actor);
