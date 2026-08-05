namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record PayrollDeductionSummaryListItemDto(
    Guid Id,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    decimal SocialInsuranceDeductionAmount,
    decimal PersonalIncomeTaxDeductionAmount,
    decimal UnionFeeDeductionAmount,
    decimal AdvanceDeductionAmount,
    decimal OtherDeductionAmount,
    bool IsLocked,
    string? Note,
    DateTime CreatedAtUtc,
    string CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);
