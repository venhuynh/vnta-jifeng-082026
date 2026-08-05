using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("attendance_logs")]
public sealed class ZktecoAttendanceLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid DeviceId { get; set; }

    public Guid? EmployeeId { get; set; }

    public DateTime? AttTime { get; set; }

    [MaxLength(10)]
    public string? Status { get; set; }

    [MaxLength(10)]
    public string? Verify { get; set; }

    [MaxLength(50)]
    public string? WorkCode { get; set; }

    [MaxLength(50)]
    public string? Reserved1 { get; set; }

    [MaxLength(50)]
    public string? Reserved2 { get; set; }

    [MaxLength(50)]
    public string? DeviceCode { get; set; }

    public int? MaskFlag { get; set; }

    [MaxLength(50)]
    public string? Temperature { get; set; }

    [MaxLength(200)]
    public string DedupKey { get; set; } = string.Empty;

    public DateTime UpdateTime { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
