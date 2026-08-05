namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Khóa hoặc mở khóa nhiều detail BHXH-YT trong một kỳ. Danh sách định danh null
/// biểu thị toàn bộ kỳ; danh sách rỗng luôn biểu thị không có dòng nào.
/// </summary>
public sealed record SetPayrollInsuranceDeductionBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    IReadOnlyList<Guid>? PayrollDeductionSummaryRecordIds = null);
