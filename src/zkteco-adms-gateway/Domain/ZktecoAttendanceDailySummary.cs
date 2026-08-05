using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("attendance_daily_summaries")]
public sealed class ZktecoAttendanceDailySummary
{
    [Key]
    public Guid Id { get; set; }

    public Guid? EmployeeId { get; set; }

    public DateOnly WorkDate { get; set; }

    public int PunchCount { get; set; }

    public string PunchMomentsText { get; set; } = string.Empty;

    public DateTime? FirstPunchTime { get; set; }

    public DateTime? LastPunchTime { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
