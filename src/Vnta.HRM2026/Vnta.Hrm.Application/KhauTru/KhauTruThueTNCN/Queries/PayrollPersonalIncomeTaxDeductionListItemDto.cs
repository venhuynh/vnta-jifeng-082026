namespace Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

public sealed record PayrollPersonalIncomeTaxDeductionListItemDto(
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
