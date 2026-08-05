namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN.Models;

public sealed record PayrollPersonalIncomeTaxDeductionRecord(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    string PositionName,
    int PayrollMonth,
    int PayrollYear,
    decimal DeductionAmount,
    bool IsLocked,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public string EmployeeDisplay => string.IsNullOrWhiteSpace(EmployeeCode)
        ? EmployeeName
        : $"{EmployeeCode} - {EmployeeName}";

    public string DepartmentDisplay => DepartmentName;
    public string PositionDisplay => PositionName;
    public string PayrollPeriodDisplay => $"{PayrollMonth:00}/{PayrollYear:D4}";
    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";

    // These UI-only values keep the current grid templates stable while TNCN has no workday semantics.
    public decimal StandardWorkdayCount => 1m;
    public decimal ActualWorkdayCount => 1m;
    public decimal AttendanceRate => 1m;
}
