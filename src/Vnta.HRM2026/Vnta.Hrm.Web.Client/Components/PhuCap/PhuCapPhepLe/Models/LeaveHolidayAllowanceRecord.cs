using System.Globalization;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

public sealed class LeaveHolidayAllowanceRecord
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public string? EmployeeCode { get; set; }

    public string? EmployeeName { get; set; }

    public string? DepartmentName { get; set; }

    public string? PositionName { get; set; }

    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public decimal DailyWageAmount { get; set; }

    public decimal LeaveDayCount { get; set; }

    public decimal HolidayDayCount { get; set; }

    public decimal LeaveHolidayAllowanceAmount { get; set; }

    public string? Note { get; set; }

    public bool IsLocked { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DetailUpdatedAtUtc { get; set; }

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

    public string DepartmentDisplay => NormalizeDisplayText(DepartmentName) ?? "Chưa có bộ phận";

    public string PositionDisplay => NormalizeDisplayText(PositionName) ?? "Chưa có chức vụ";

    public string PayrollPeriodDisplay => $"{PayrollMonth:00}/{PayrollYear}";

    public string LockStatusText => IsLocked ? "Đã khóa" : "Đang mở";

    public string LockActionText => IsLocked ? "Mở khóa" : "Khóa";

    public string LeaveHolidayAllowanceAmountDisplay =>
        LeaveHolidayAllowanceAmount.ToString("N0", DisplayCulture);

    private static string? NormalizeDisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
