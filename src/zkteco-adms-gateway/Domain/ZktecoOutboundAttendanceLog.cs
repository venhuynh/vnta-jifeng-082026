namespace Vnta.AttendanceGateway.Domain;

public sealed class ZktecoOutboundAttendanceLog
{
    public Guid Id { get; set; }

    public Guid AttendanceLogId { get; set; }

    public string DeviceSn { get; set; } = string.Empty;

    public string EmployeeCode { get; set; } = string.Empty;

    public DateTime TapTime { get; set; }

    public int VerificationMode { get; set; }

    public int InOutMode { get; set; }

    public int AttemptCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    public DateTimeOffset? NextAttemptAtUtc { get; set; }

    public DateTimeOffset? DeliveredAtUtc { get; set; }

    public DateTimeOffset? FailedAtUtc { get; set; }
}
