namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public sealed record SetPayrollInsuranceDeductionBatchLockStateResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount);
