namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

public sealed record PayrollUnionFeeDeductionListItemDto(
    Guid PayrollDeductionSummaryRecordId,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    int PayrollMonth,
    int PayrollYear,
    decimal DeductionAmount,
    bool IsSummaryLocked,
    bool IsLocked,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
