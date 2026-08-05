namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Kết quả thay đổi trạng thái khóa của một phạm vi phụ cấp độc hại.</summary>
public sealed record SetHazardAllowanceBatchLockStateResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount);
