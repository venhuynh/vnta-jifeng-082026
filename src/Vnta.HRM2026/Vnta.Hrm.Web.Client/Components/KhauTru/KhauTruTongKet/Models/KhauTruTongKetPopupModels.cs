using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop;

/// <summary>Dòng bảng công dùng bởi popup đối chiếu của Tổng kết khấu trừ.</summary>
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
}

/// <summary>Model điều chỉnh thủ công khoản khấu trừ khác.</summary>
public sealed class KhauTruTongKetEditModel
{
    public Guid Id { get; init; }

    public string EmployeeDisplay { get; init; } = string.Empty;

    public string PayrollPeriodDisplay { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Khoản khấu trừ khác phải từ 0 đến 9.999.999.999.999.999,99.")]
    public decimal OtherDeductionAmount { get; set; }

    public string? Note { get; set; }

    public DateTime OriginalUpdatedAtUtc { get; init; }

    public bool IsLocked { get; init; }
}
