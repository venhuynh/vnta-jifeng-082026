namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>
/// Yêu cầu khóa hoặc mở khóa phụ cấp độc hại. Không chỉ định dòng nghĩa là áp dụng cho toàn bộ kỳ lương.
/// </summary>
public sealed record SetHazardAllowanceBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    IReadOnlyList<Guid>? PayrollAllowanceSummaryRecordIds,
    string RequestedBy);
