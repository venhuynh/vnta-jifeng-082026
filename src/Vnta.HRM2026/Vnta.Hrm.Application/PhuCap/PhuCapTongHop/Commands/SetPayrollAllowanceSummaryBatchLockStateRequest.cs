namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>
/// Yêu cầu khóa hoặc mở khóa theo kỳ. Nếu không chỉ định mã dòng thì áp dụng cho toàn bộ kỳ;
/// token đồng thời, khi có, được đối chiếu trước khi cập nhật.
/// </summary>
public sealed record SetPayrollAllowanceSummaryBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    IReadOnlyList<Guid>? PayrollAllowanceSummaryRecordIds = null,
    IReadOnlyList<PayrollAllowanceSummaryLockStateConcurrencyToken>? ConcurrencyTokens = null,
    string? Actor = null);
