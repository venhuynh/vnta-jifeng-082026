namespace Vnta.AttendanceGateway.Domain;

public sealed class ZktecoOutboundSystemLog
{
    public Guid Id { get; set; }

    public string DeviceSn { get; set; } = string.Empty;

    public string ConnectionId { get; set; } = string.Empty;

    public string Direction { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

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
