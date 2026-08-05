namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>
/// Yêu cầu tính lại snapshot từ các bảng phụ cấp nguồn. Khi có mã dòng, chỉ dòng đó được làm mới.
/// </summary>
public sealed record RefreshPayrollAllowanceSummaryRequest(
    int TargetPayrollMonth,
    int TargetPayrollYear,
    string? Actor,
    Guid? PayrollAllowanceSummaryRecordId = null);
