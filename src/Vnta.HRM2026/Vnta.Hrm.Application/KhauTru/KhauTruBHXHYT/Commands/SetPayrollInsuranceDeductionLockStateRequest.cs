namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Khóa hoặc mở khóa một detail BHXH-YT. Trạng thái khóa của summary khấu trừ
/// là khóa cấp cha dùng chung và không bị thay đổi bởi command này.
/// </summary>
public sealed record SetPayrollInsuranceDeductionLockStateRequest(
    Guid PayrollDeductionSummaryRecordId,
    bool IsLocked,
    DateTime OriginalUpdatedAtUtc);
