namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruKhac;

/// <summary>
/// Read model dành riêng cho grid và popup của màn Khấu trừ khác.
/// </summary>
public sealed class KhauTruKhacRecord
{
    public Guid Id { get; set; }

    public Guid PayrollDeductionSummaryRecordId { get; set; }

    public Guid EmployeeId { get; set; }

    public string? EmployeeCode { get; set; }

    public string? EmployeeName { get; set; }

    public string? DepartmentName { get; set; }

    public string? PositionName { get; set; }

    public short PayrollMonth { get; set; }

    public short PayrollYear { get; set; }

    public DateTime? EmploymentStartDate { get; set; }

    public decimal? SalaryWorkDays { get; set; }

    public string? Description { get; set; }

    public decimal DeductionAmount { get; set; }

    public string? Note { get; set; }

    public bool IsLocked { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? VersionAtUtc { get; set; }

    public string? RefreshedBy { get; set; }

    public string EmployeeDisplay
    {
        get
        {
            var parts = new[]
            {
                string.IsNullOrWhiteSpace(EmployeeCode) ? null : EmployeeCode.Trim(),
                string.IsNullOrWhiteSpace(EmployeeName) ? null : EmployeeName.Trim()
            };

            return string.Join(" - ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }

    public string DepartmentDisplay => string.IsNullOrWhiteSpace(DepartmentName) ? "--" : DepartmentName.Trim();

    public string PositionDisplay => string.IsNullOrWhiteSpace(PositionName) ? "--" : PositionName.Trim();

    public string DescriptionDisplay => string.IsNullOrWhiteSpace(Description) ? "--" : Description.Trim();

    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";

    public string LockActionText => IsLocked ? "Mở khóa" : "Khóa";
}
