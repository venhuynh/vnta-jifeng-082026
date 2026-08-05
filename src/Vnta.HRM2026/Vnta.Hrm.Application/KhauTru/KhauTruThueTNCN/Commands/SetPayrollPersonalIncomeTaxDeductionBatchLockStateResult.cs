namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

/// <summary>Kết quả xử lý khóa hoặc mở khóa theo lô cho Thuế TNCN.</summary>
public sealed record SetPayrollPersonalIncomeTaxDeductionBatchLockStateResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount,
    int UnchangedCount);

