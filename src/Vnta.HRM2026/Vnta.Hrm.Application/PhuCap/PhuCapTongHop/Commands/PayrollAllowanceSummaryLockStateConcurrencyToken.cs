namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>
/// Phiên bản của một dòng tại thời điểm người dùng mở grid, dùng để chặn thao tác khóa/mở khóa ghi đè dữ liệu mới hơn.
/// </summary>
public sealed record PayrollAllowanceSummaryLockStateConcurrencyToken(
    Guid Id,
    DateTime? OriginalUpdatedAtUtc);
