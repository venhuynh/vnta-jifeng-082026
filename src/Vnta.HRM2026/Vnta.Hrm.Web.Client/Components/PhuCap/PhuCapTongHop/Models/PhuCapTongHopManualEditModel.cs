using System.ComponentModel.DataAnnotations;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>
/// UI model owned by the allowance-summary screen for the manual-value workflow.
/// It deliberately remains separate from the API request and persistence row.
/// </summary>
public sealed class PhuCapTongHopManualEditModel
{
    public Guid Id { get; init; }

    public string EmployeeDisplay { get; init; } = string.Empty;

    public string DepartmentDisplay { get; init; } = string.Empty;

    public string PositionDisplay { get; init; } = string.Empty;

    public string PayrollPeriodDisplay { get; init; } = string.Empty;

    public string CurrentLockStatusText { get; init; } = string.Empty;

    /// <summary>Trạng thái khóa sẽ được áp dụng cùng lần lưu điều chỉnh.</summary>
    public bool IsLocked { get; set; }

    public string LockStatusAfterSaveText => IsLocked ? "Đã khóa" : "Đang mở";

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Phụ cấp trách nhiệm không được nhỏ hơn 0.")]
    public decimal ResponsibilityAllowanceAmount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Phụ cấp trách nhiệm khác không được nhỏ hơn 0.")]
    public decimal ResponsibilityOtherAllowanceAmount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Phụ cấp thâm niên không được nhỏ hơn 0.")]
    public decimal SeniorityAllowanceAmount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Phụ cấp chuyên cần không được nhỏ hơn 0.")]
    public decimal AttendanceAllowanceAmount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Phụ cấp cơm không được nhỏ hơn 0.")]
    public decimal MealAllowanceAmount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Phụ cấp độc hại không được nhỏ hơn 0.")]
    public decimal HazardAllowanceAmount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Phụ cấp khác không được nhỏ hơn 0.")]
    public decimal OtherAllowanceAmount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Phụ cấp phép/lễ không được nhỏ hơn 0.")]
    public decimal LeaveHolidayAllowanceAmount { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    public string? Note { get; set; }

    public decimal TotalAllowanceAmount =>
        ResponsibilityAllowanceAmount
        + ResponsibilityOtherAllowanceAmount
        + SeniorityAllowanceAmount
        + AttendanceAllowanceAmount
        + MealAllowanceAmount
        + HazardAllowanceAmount
        + OtherAllowanceAmount
        + LeaveHolidayAllowanceAmount;

    public DateTime? OriginalUpdatedAtUtc { get; init; }

    public UpdatePayrollAllowanceSummaryManualValuesRequest ToRequest() =>
        new(
            Id,
            ResponsibilityAllowanceAmount,
            ResponsibilityOtherAllowanceAmount,
            SeniorityAllowanceAmount,
            AttendanceAllowanceAmount,
            MealAllowanceAmount,
            HazardAllowanceAmount,
            OtherAllowanceAmount,
            LeaveHolidayAllowanceAmount,
            Note,
            IsLocked,
            OriginalUpdatedAtUtc,
            Actor: null);

    public static PhuCapTongHopManualEditModel FromRecord(PayrollAllowanceSummaryRecord source) =>
        new()
        {
            Id = source.Id,
            EmployeeDisplay = source.EmployeeDisplay,
            DepartmentDisplay = source.DepartmentDisplay,
            PositionDisplay = source.PositionDisplay,
            PayrollPeriodDisplay = source.PayrollPeriodDisplay,
            CurrentLockStatusText = source.LockStatusText,
            IsLocked = true,
            ResponsibilityAllowanceAmount = source.ResponsibilityAllowanceAmount,
            ResponsibilityOtherAllowanceAmount = source.ResponsibilityOtherAllowanceAmount,
            SeniorityAllowanceAmount = source.SeniorityAllowanceAmount,
            AttendanceAllowanceAmount = source.AttendanceAllowanceAmount,
            MealAllowanceAmount = source.MealAllowanceAmount,
            HazardAllowanceAmount = source.HazardAllowanceAmount,
            OtherAllowanceAmount = source.OtherAllowanceAmount,
            LeaveHolidayAllowanceAmount = source.LeaveHolidayAllowanceAmount,
            Note = source.Note,
            OriginalUpdatedAtUtc = source.UpdatedAtUtc
        };
}
