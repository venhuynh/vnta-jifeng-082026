namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public sealed record SyncPayrollInsuranceDeductionFromPreviousMonthRequest(
    int TargetPayrollMonth,
    int TargetPayrollYear);
