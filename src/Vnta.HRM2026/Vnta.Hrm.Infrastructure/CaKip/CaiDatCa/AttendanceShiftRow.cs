namespace Vnta.Hrm.Infrastructure.CaKip.CaiDatCa;

public sealed class AttendanceShiftRow
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string DepartmentGroup { get; set; } = string.Empty;

    public string StartTime { get; set; } = string.Empty;

    public string EndTime { get; set; } = string.Empty;

    public bool IsOvernight { get; set; }

    public string? BreakStartTime { get; set; }

    public string? BreakEndTime { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? ColorHex { get; set; }

    public string? ShortName { get; set; }

    public string? WorkingDays { get; set; }
}
