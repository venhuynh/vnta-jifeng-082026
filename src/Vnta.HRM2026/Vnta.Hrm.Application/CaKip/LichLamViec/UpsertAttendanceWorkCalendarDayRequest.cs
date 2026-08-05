namespace Vnta.Hrm.Application.CaKip.LichLamViec;

public sealed class UpsertAttendanceWorkCalendarDayRequest
{
    public Guid Id { get; set; }

    public DateOnly WorkDate { get; set; }

    public AttendanceWorkCalendarDayType DayType { get; set; }

    public string? Name { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
