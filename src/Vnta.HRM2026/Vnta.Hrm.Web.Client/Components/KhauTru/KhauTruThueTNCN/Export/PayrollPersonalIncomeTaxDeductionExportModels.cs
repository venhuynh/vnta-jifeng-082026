namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN.Export;

public enum PayrollPersonalIncomeTaxDeductionExportFormat
{
    Excel,
    Pdf
}

public sealed record PayrollPersonalIncomeTaxDeductionExportRow(
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    string PositionName,
    string PayrollPeriodDisplay,
    decimal DeductionAmount,
    string LockStatusText);
