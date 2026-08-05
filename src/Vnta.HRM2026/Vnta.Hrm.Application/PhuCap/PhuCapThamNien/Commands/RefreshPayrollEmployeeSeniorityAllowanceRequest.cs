namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>
/// Xác định phạm vi refresh. Không có mã summary nghĩa là refresh các detail chưa khóa đã tồn tại của cả kỳ.
/// </summary>
public sealed record RefreshPayrollEmployeeSeniorityAllowanceRequest(
    int PayrollYear,
    int PayrollMonth,
    Guid? PayrollAllowanceSummaryRecordId = null);
