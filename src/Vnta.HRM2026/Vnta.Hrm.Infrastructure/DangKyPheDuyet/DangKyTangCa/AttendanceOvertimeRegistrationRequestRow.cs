namespace Vnta.Hrm.Infrastructure.DangKyPheDuyet.DangKyTangCa;

public sealed class AttendanceOvertimeRegistrationRequestRow
{
    public Guid Id { get; set; }

    public DateOnly WorkDate { get; set; }

    public AttendanceWorkCalendarDayType DayType { get; set; }

    public string WorkshopCode { get; set; } = string.Empty;

    public string WorkshopName { get; set; } = string.Empty;

    public Guid? RequestedByEmployeeId { get; set; }

    public string RequestedBy { get; set; } = string.Empty;

    public Guid? ApprovedByEmployeeId { get; set; }

    public string ApprovedBy { get; set; } = string.Empty;

    public OvertimeRegistrationStatus Status { get; set; }

    public string Note { get; set; } = string.Empty;

    public DateTime LastActionAtUtc { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
