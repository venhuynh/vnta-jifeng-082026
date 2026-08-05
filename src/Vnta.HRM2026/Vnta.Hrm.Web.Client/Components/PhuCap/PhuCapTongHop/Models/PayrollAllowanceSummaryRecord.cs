using System.Globalization;

namespace Vnta.Hrm.Web.Client.Models.Payroll;

/// <summary>
/// Model trình bày một dòng tổng hợp phụ cấp trên client.
/// Các thuộc tính <c>*Display</c> và tổng tiền chỉ phục vụ giao diện, không được gửi ngược để ghi dữ liệu.
/// </summary>
public sealed class PayrollAllowanceSummaryRecord
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    #region Dữ liệu snapshot từ API

    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public string? EmployeeCode { get; set; }

    public string? EmployeeName { get; set; }

    public string? DepartmentName { get; set; }

    public string? PositionName { get; set; }

    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public decimal ResponsibilityAllowanceAmount { get; set; }

    public decimal ResponsibilityOtherAllowanceAmount { get; set; }

    public decimal SeniorityAllowanceAmount { get; set; }

    public decimal AttendanceAllowanceAmount { get; set; }

    public decimal MealAllowanceAmount { get; set; }

    public decimal HazardAllowanceAmount { get; set; }

    public decimal OtherAllowanceAmount { get; set; }

    public decimal LeaveHolidayAllowanceAmount { get; set; }

    public bool IsLocked { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    #endregion

    #region Giá trị dẫn xuất để hiển thị

    public string EmployeeDisplay
    {
        get
        {
            var code = NormalizeDisplayText(EmployeeCode);
            var fullName = NormalizeDisplayText(EmployeeName);

            if(code is not null && fullName is not null)
            {
                return $"{code} - {fullName}";
            }

            return fullName ?? code ?? "Chưa có nhân viên";
        }
    }

    public string DepartmentDisplay => NormalizeDisplayText(DepartmentName) ?? "Chưa có phòng ban";

    public string PositionDisplay => NormalizeDisplayText(PositionName) ?? "Chưa có chức vụ";

    public string PayrollPeriodDisplay => $"{PayrollMonth:00}/{PayrollYear}";

    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";

    public decimal TotalAllowanceAmount =>
        ResponsibilityAllowanceAmount
        + ResponsibilityOtherAllowanceAmount
        + SeniorityAllowanceAmount
        + AttendanceAllowanceAmount
        + MealAllowanceAmount
        + HazardAllowanceAmount
        + OtherAllowanceAmount
        + LeaveHolidayAllowanceAmount;

    public string TotalAllowanceAmountDisplay => TotalAllowanceAmount.ToString("N0", DisplayCulture);

    private static string? NormalizeDisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    #endregion
}
