namespace Vnta.Hrm.Infrastructure.CaKip.BangXepCa;

public sealed class AttendanceShiftAssignmentRow
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public Guid ShiftId { get; set; }

    public DateOnly WorkDate { get; set; }

    public string CreationType { get; set; } = string.Empty;

    public Guid? SourceBatchId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
