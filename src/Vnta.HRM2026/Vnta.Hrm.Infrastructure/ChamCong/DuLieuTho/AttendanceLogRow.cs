namespace Vnta.Hrm.Infrastructure.ChamCong.DuLieuTho;

public sealed class AttendanceLogRow
{
    public Guid Id { get; set; }

    public Guid DeviceId { get; set; }

    public Guid? EmployeeId { get; set; }

    public DateTime? AttTime { get; set; }

    public string? Status { get; set; }

    public string? Verify { get; set; }

    public string? WorkCode { get; set; }

    public string? Reserved1 { get; set; }

    public string? Reserved2 { get; set; }

    public string? DeviceCode { get; set; }

    public int? MaskFlag { get; set; }

    public string? Temperature { get; set; }

    public string DedupKey { get; set; } = string.Empty;

    public DateTime UpdateTime { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
