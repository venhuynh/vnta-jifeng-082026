using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruBHXHYT;

/// <summary>
/// View model độc lập của popup Điều chỉnh; không bind trực tiếp dòng đang hiển thị trên grid.
/// </summary>
public sealed class KhauTruBHXHYTEditModel
{
    public Guid PayrollDeductionSummaryRecordId { get; init; }

    public string EmployeeDisplay { get; init; } = string.Empty;

    public string PayrollPeriodDisplay { get; init; } = string.Empty;

    public string LockStatusText { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Tổng tiền lương đóng BHXH không được âm.")]
    public decimal InsuranceSalaryBaseAmount { get; set; }

    [Range(typeof(decimal), "0", "1", ErrorMessage = "Tỷ lệ BHXH phải nằm trong khoảng 0% đến 100%.")]
    public decimal SocialInsuranceRate { get; set; }

    [Range(typeof(decimal), "0", "1", ErrorMessage = "Tỷ lệ BHYT phải nằm trong khoảng 0% đến 100%.")]
    public decimal HealthInsuranceRate { get; set; }

    [Range(typeof(decimal), "0", "1", ErrorMessage = "Tỷ lệ BHTN phải nằm trong khoảng 0% đến 100%.")]
    public decimal UnemploymentInsuranceRate { get; set; }

    public bool IsParticipating { get; set; }

    [Range((short)0, (short)3, ErrorMessage = "Loại biến động tham gia không hợp lệ.")]
    public short ParticipationChangeType { get; set; }

    public DateOnly? EffectiveDate { get; set; }

    public decimal CurrentTotalInsuranceRate { get; init; }

    public decimal CurrentTotalDeductionAmount { get; init; }

    public DateTime OriginalUpdatedAtUtc { get; init; }
}

/// <summary>
/// Read-only row shown by the monthly-work popup. It intentionally contains only
/// attendance fields required for payroll-insurance verification.
/// </summary>
public sealed record KhauTruBHXHYTMonthlyWorkdayRow(
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
    bool IsLocked)
{
    public int LateEarlyTotalMinutes => Math.Max(0, LateMinutes) + Math.Max(0, EarlyLeaveMinutes);

    public bool HasCheckInOrOut => !string.IsNullOrWhiteSpace(CheckInAt)
        || !string.IsNullOrWhiteSpace(CheckOutAt);

    public bool IsRegularWorkday => string.Equals(DayType, "Ngày thường", StringComparison.OrdinalIgnoreCase)
        || string.Equals(DayType, "regular", StringComparison.OrdinalIgnoreCase);
}
