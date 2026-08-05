using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Vnta.Hrm.Web.Client.Models;

public sealed partial class AttendanceShiftRecord : IValidatableObject
{
    private const string NonWhitespacePattern = @".*\S.*";
    private const string OptionalNonWhitespacePattern = @"^$|.*\S.*";

    public Guid Id { get; set; }

    [Required(ErrorMessage = "Ma ca khong duoc de trong.")]
    [StringLength(50, ErrorMessage = "Ma ca khong duoc vuot qua 50 ky tu.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Ma ca khong duoc chi gom khoang trang.")]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Ten ca khong duoc de trong.")]
    [StringLength(200, ErrorMessage = "Ten ca khong duoc vuot qua 200 ky tu.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Ten ca khong duoc chi gom khoang trang.")]
    public string? Name { get; set; }

    [StringLength(50, ErrorMessage = "Ten ngan khong duoc vuot qua 50 ky tu.")]
    [RegularExpression(OptionalNonWhitespacePattern, ErrorMessage = "Ten ngan khong duoc chi gom khoang trang.")]
    public string? ShortName { get; set; }

    [StringLength(1000, ErrorMessage = "Mo ta khong duoc vuot qua 1000 ky tu.")]
    [RegularExpression(OptionalNonWhitespacePattern, ErrorMessage = "Mo ta khong duoc chi gom khoang trang.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Nhom bo phan khong duoc de trong.")]
    [StringLength(100, ErrorMessage = "Nhom bo phan khong duoc vuot qua 100 ky tu.")]
    [RegularExpression(NonWhitespacePattern, ErrorMessage = "Nhom bo phan khong duoc chi gom khoang trang.")]
    public string? DepartmentGroup { get; set; }

    [Required(ErrorMessage = "Gio bat dau khong duoc de trong.")]
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Gio bat dau phai dung dinh dang HH:mm.")]
    public string? StartTime { get; set; }

    [Required(ErrorMessage = "Gio ket thuc khong duoc de trong.")]
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Gio ket thuc phai dung dinh dang HH:mm.")]
    public string? EndTime { get; set; }

    public bool IsOvernight { get; set; }

    [RegularExpression(@"^$|^\d{2}:\d{2}$", ErrorMessage = "Gio bat dau nghi phai dung dinh dang HH:mm.")]
    public string? BreakStartTime { get; set; }

    [RegularExpression(@"^$|^\d{2}:\d{2}$", ErrorMessage = "Gio ket thuc nghi phai dung dinh dang HH:mm.")]
    public string? BreakEndTime { get; set; }

    public int Status { get; set; }

    [RegularExpression(@"^$|^#[0-9A-Fa-f]{6}$", ErrorMessage = "Mau hien thi phai dung dinh dang #RRGGBB.")]
    public string? ColorHex { get; set; }

    public string? WorkingDays { get; set; }

    public bool WorksMonday { get; set; }

    public bool WorksTuesday { get; set; }

    public bool WorksWednesday { get; set; }

    public bool WorksThursday { get; set; }

    public bool WorksFriday { get; set; }

    public bool WorksSaturday { get; set; }

    public bool WorksSunday { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TryParseTime(StartTime, out var start))
        {
            yield break;
        }

        if (!TryParseTime(EndTime, out var end))
        {
            yield break;
        }

        if (!IsOvernight && end <= start)
        {
            yield return new ValidationResult(
                "Gio ket thuc phai lon hon gio bat dau neu ca khong qua ngay.",
                [nameof(EndTime)]);
        }

        var hasBreakStart = !string.IsNullOrWhiteSpace(BreakStartTime);
        var hasBreakEnd = !string.IsNullOrWhiteSpace(BreakEndTime);
        if (hasBreakStart != hasBreakEnd)
        {
            yield return new ValidationResult(
                "Gio nghi bat dau va gio nghi ket thuc phai cung duoc nhap.",
                [nameof(BreakStartTime), nameof(BreakEndTime)]);
            yield break;
        }

        if (!hasBreakStart)
        {
            yield break;
        }

        if (!TryParseTime(BreakStartTime, out var breakStart)
            || !TryParseTime(BreakEndTime, out var breakEnd))
        {
            yield break;
        }

        if (!IsRangeInsideShift(start, end, IsOvernight, breakStart, breakEnd))
        {
            yield return new ValidationResult(
                "Khoang nghi phai nam trong khoang gio ca.",
                [nameof(BreakStartTime), nameof(BreakEndTime)]);
        }
    }

    public void SyncWorkingDaysFromFlags()
    {
        var selected = new List<string>();
        if (WorksMonday)
        {
            selected.Add("Mon");
        }

        if (WorksTuesday)
        {
            selected.Add("Tue");
        }

        if (WorksWednesday)
        {
            selected.Add("Wed");
        }

        if (WorksThursday)
        {
            selected.Add("Thu");
        }

        if (WorksFriday)
        {
            selected.Add("Fri");
        }

        if (WorksSaturday)
        {
            selected.Add("Sat");
        }

        if (WorksSunday)
        {
            selected.Add("Sun");
        }

        WorkingDays = selected.Count == 0 ? null : string.Join(',', selected);
    }

    public void SyncWorkingDayFlags()
    {
        var selected = (WorkingDays ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        WorksMonday = selected.Contains("Mon");
        WorksTuesday = selected.Contains("Tue");
        WorksWednesday = selected.Contains("Wed");
        WorksThursday = selected.Contains("Thu");
        WorksFriday = selected.Contains("Fri");
        WorksSaturday = selected.Contains("Sat");
        WorksSunday = selected.Contains("Sun");
    }

    public string WorkingDaysText
    {
        get
        {
            var labels = new List<string>();
            if (WorksMonday)
            {
                labels.Add("T2");
            }

            if (WorksTuesday)
            {
                labels.Add("T3");
            }

            if (WorksWednesday)
            {
                labels.Add("T4");
            }

            if (WorksThursday)
            {
                labels.Add("T5");
            }

            if (WorksFriday)
            {
                labels.Add("T6");
            }

            if (WorksSaturday)
            {
                labels.Add("T7");
            }

            if (WorksSunday)
            {
                labels.Add("CN");
            }

            return labels.Count == 0 ? "Chua chon" : string.Join(", ", labels);
        }
    }

    public string TimeRangeText => $"{StartTime} - {EndTime}";

    public string BreakTimeText =>
        string.IsNullOrWhiteSpace(BreakStartTime) || string.IsNullOrWhiteSpace(BreakEndTime)
            ? "Khong nghi"
            : $"{BreakStartTime} - {BreakEndTime}";

    public string StatusText => Status == 1 ? "Dang su dung" : "Ngung su dung";

    public string ShiftLookupText
    {
        get
        {
            var title = string.IsNullOrWhiteSpace(Name)
                ? "Chua dat ten ca"
                : Name.Trim();

            return string.IsNullOrWhiteSpace(StartTime) || string.IsNullOrWhiteSpace(EndTime)
                ? title
                : $"{title} ({StartTime} - {EndTime})";
        }
    }

    private static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(
            value,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);

    private static bool IsRangeInsideShift(
        TimeOnly shiftStart,
        TimeOnly shiftEnd,
        bool isOvernight,
        TimeOnly breakStart,
        TimeOnly breakEnd)
    {
        var shiftStartMinutes = ToMinutes(shiftStart);
        var shiftEndMinutes = ToMinutes(shiftEnd);
        if (isOvernight && shiftEndMinutes <= shiftStartMinutes)
        {
            shiftEndMinutes += 24 * 60;
        }

        var breakStartMinutes = NormalizeToShiftDay(ToMinutes(breakStart), shiftStartMinutes, isOvernight);
        var breakEndMinutes = NormalizeToShiftDay(ToMinutes(breakEnd), shiftStartMinutes, isOvernight);
        if (breakEndMinutes <= breakStartMinutes)
        {
            breakEndMinutes += 24 * 60;
        }

        return breakStartMinutes >= shiftStartMinutes
            && breakEndMinutes <= shiftEndMinutes
            && breakEndMinutes > breakStartMinutes;
    }

    private static int NormalizeToShiftDay(int minutes, int shiftStartMinutes, bool isOvernight) =>
        isOvernight && minutes < shiftStartMinutes
            ? minutes + 24 * 60
            : minutes;

    private static int ToMinutes(TimeOnly value) => value.Hour * 60 + value.Minute;
}
