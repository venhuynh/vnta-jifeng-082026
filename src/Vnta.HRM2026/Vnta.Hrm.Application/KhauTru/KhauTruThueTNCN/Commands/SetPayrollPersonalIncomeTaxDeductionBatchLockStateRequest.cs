namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

/// <summary>Phạm vi khóa Thuế TNCN được khai báo tường minh, không suy luận từ danh sách dòng rỗng.</summary>
public enum PayrollPersonalIncomeTaxDeductionLockActionScope
{
    SelectedRows = 1,
    WholePeriod = 2
}

/// <summary>Yêu cầu khóa hoặc mở khóa các dòng Thuế TNCN.</summary>
public sealed record SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    PayrollPersonalIncomeTaxDeductionLockActionScope Scope,
    IReadOnlyList<Guid>? PayrollDeductionSummaryRecordIds = null);

