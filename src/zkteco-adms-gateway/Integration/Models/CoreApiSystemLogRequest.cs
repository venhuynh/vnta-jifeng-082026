namespace Vnta.AttendanceGateway.Integration.Models;

public sealed record CoreApiSystemLogRequest(
    string DeviceSn,
    string ConnectionId,
    string Direction,
    string EventType,
    string Message,
    DateTimeOffset OccurredAtUtc);
