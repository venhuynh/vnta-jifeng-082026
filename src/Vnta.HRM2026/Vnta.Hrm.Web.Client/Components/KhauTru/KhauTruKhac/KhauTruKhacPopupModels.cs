using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruKhac;

public sealed record MonthlyWorkdayPopupRow(
    Guid Id,
    DateOnly WorkDate,
    string DayType,
    string ShiftShortName,
    string? ShiftColorHex,
    string? CheckInAt,
    string? CheckOutAt,
    string Status,
    int LateMinutes,
    int EarlyLeaveMinutes,
    int OvertimeMinutes,
    int OvertimeMinutes15,
    int OvertimeMinutes20,
    int OvertimeMinutes30,
    string LockStatus,
    bool IsLocked)
{
    public int LateEarlyTotalMinutes => Math.Max(0, LateMinutes) + Math.Max(0, EarlyLeaveMinutes);

    public bool HasCheckInOrOut => !string.IsNullOrWhiteSpace(CheckInAt)
        || !string.IsNullOrWhiteSpace(CheckOutAt);

    public bool IsRegularWorkday => string.Equals(DayType, "Ngày thường", StringComparison.OrdinalIgnoreCase)
        || string.Equals(DayType, "regular", StringComparison.OrdinalIgnoreCase);

    public decimal SalaryWorkday => IsRegularWorkday ? 1m : 0m;
}

public sealed class KhauTruKhacEditModel
{
    public Guid PayrollDeductionSummaryRecordId { get; set; }

    public string EmployeeDisplay { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Số tiền khấu trừ phải từ 0 đến 9.999.999.999.999.999,99.")]
    public decimal DeductionAmount { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public bool IsLocked { get; set; }

    public DateTime? OriginalUpdatedAtUtc { get; set; }
}
