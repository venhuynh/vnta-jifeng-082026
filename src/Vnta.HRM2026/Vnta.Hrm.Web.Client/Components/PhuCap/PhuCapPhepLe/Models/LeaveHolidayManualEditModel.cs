using System.ComponentModel.DataAnnotations;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Policies;
namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;

/// <summary>Đại diện kiểu <c>LeaveHolidayManualEditModel</c> phục vụ màn hình phụ cấp phép lễ.</summary>
public sealed class LeaveHolidayManualEditModel
{
    /// <summary>Giá trị <c>Id</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    public Guid Id { get; set; }

    /// <summary>Giá trị <c>EmployeeDisplay</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    public string EmployeeDisplay { get; set; } = string.Empty;

    /// <summary>Giá trị <c>PayrollPeriodDisplay</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    public string PayrollPeriodDisplay { get; set; } = string.Empty;

    /// <summary>Giá trị <c>OriginalUpdatedAtUtc</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    public DateTime? OriginalUpdatedAtUtc { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Lương theo ngày không được nhỏ hơn 0.")]
    /// <summary>Giá trị <c>DailyWageAmount</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    public decimal DailyWageAmount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Số ngày phép không được nhỏ hơn 0.")]
    /// <summary>Giá trị <c>LeaveDayCount</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    public decimal LeaveDayCount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Số ngày lễ không được nhỏ hơn 0.")]
    /// <summary>Giá trị <c>HolidayDayCount</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    public decimal HolidayDayCount { get; set; }

    /// <summary>Giá trị <c>LeaveHolidayAllowanceAmount</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    public decimal LeaveHolidayAllowanceAmount =>
        LeaveHolidayAllowancePreviewPolicy.Calculate(
            new LeaveHolidayAllowancePreviewCalculationInput(
                DailyWageAmount,
                LeaveDayCount,
                HolidayDayCount)).AllowanceAmount;

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    /// <summary>Giá trị <c>Note</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    public string? Note { get; set; }

    /// <summary>Chuẩn hóa cho luồng <c>Normalize</c>.</summary>
    public void Normalize()
    {
        Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
    }

    /// <summary>Thực hiện xử lý cho luồng <c>FromRecord</c>.</summary>
    public static LeaveHolidayManualEditModel FromRecord(LeaveHolidayAllowanceRecord source) =>
        new()
        {
            Id = source.Id,
            EmployeeDisplay = source.EmployeeDisplay,
            PayrollPeriodDisplay = source.PayrollPeriodDisplay,
            OriginalUpdatedAtUtc = source.DetailUpdatedAtUtc ?? source.CreatedAtUtc,
            DailyWageAmount = source.DailyWageAmount,
            LeaveDayCount = source.LeaveDayCount,
            HolidayDayCount = source.HolidayDayCount,
            Note = source.Note
        };
}
