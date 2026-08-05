using System.ComponentModel.DataAnnotations;
namespace Vnta.Hrm.Web.Client.Models;

public sealed class AttendanceWorkCalendarDayRecord : IValidatableObject
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Ngày làm việc không được để trống.")]
    public DateTime? WorkDate { get; set; }

    [Required(ErrorMessage = "Loại ngày không được để trống.")]
    public AttendanceWorkCalendarDayType DayType { get; set; } = AttendanceWorkCalendarDayType.DayOff;

    [StringLength(200, ErrorMessage = "Tên ngày không được vượt quá 200 ký tự.")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateOnly? WorkDateOnly =>
        WorkDate.HasValue ? DateOnly.FromDateTime(WorkDate.Value.Date) : null;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Name)
            ? AttendanceWorkCalendarDayTypes.GetDisplayName(DayType)
            : Name.Trim();

    public string ShortDayType => AttendanceWorkCalendarDayTypes.GetShortDisplayName(DayType);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(!AttendanceWorkCalendarDayTypes.All.Contains(DayType))
        {
            yield return new ValidationResult(
                "Loại ngày chỉ được là Ngày thường, Ngày nghỉ hoặc Ngày lễ.",
                [nameof(DayType)]);
        }

        if(DayType == AttendanceWorkCalendarDayType.Holiday
            && string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Tên ngày lễ không được để trống.",
                [nameof(Name)]);
        }

        if(Name is not null && Name.Length > 0 && string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Tên ngày không được chỉ gồm khoảng trắng.",
                [nameof(Name)]);
        }

        if(Note is not null && Note.Length > 0 && string.IsNullOrWhiteSpace(Note))
        {
            yield return new ValidationResult(
                "Ghi chú không được chỉ gồm khoảng trắng.",
                [nameof(Note)]);
        }
    }

    public AttendanceWorkCalendarDayRecord Clone() =>
        new()
        {
            Id = Id,
            WorkDate = WorkDate,
            DayType = DayType,
            Name = Name,
            Note = Note,
            CreatedAtUtc = CreatedAtUtc,
            UpdatedAtUtc = UpdatedAtUtc
        };
}
