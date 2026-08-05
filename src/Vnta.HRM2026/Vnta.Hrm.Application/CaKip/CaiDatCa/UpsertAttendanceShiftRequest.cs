namespace Vnta.Hrm.Application.CaKip.CaiDatCa;

public sealed class UpsertAttendanceShiftRequest
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? ShortName { get; set; }

    public string? Description { get; set; }

    public string? DepartmentGroup { get; set; }

    public string? StartTime { get; set; }

    public string? EndTime { get; set; }

    public bool IsOvernight { get; set; }

    public string? BreakStartTime { get; set; }

    public string? BreakEndTime { get; set; }

    public int Status { get; set; }

    public string? ColorHex { get; set; }

    public string? WorkingDays { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
