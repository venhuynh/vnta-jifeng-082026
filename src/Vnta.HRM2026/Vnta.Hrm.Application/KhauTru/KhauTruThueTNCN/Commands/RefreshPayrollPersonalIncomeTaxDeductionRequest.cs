namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

/// <summary>
/// Xác định đúng dòng Thuế TNCN cần đồng bộ lại từ chi tiết hiện hữu sang tổng kết khấu trừ.
/// </summary>
public sealed record RefreshPayrollPersonalIncomeTaxDeductionRequest(
    int PayrollYear,
    int PayrollMonth,
    Guid PayrollDeductionSummaryRecordId);

/// <summary>
/// Kết quả đồng bộ một dòng Thuế TNCN; thao tác không tạo mới chi tiết và không tính lại công thức thuế.
/// </summary>
public sealed record RefreshPayrollPersonalIncomeTaxDeductionResult(
    int PayrollYear,
    int PayrollMonth,
    Guid PayrollDeductionSummaryRecordId,
    int UpdatedCount,
    int UnchangedCount,
    int SkippedLockedCount);
