namespace Vnta.Hrm.Application.Integrations.AttendanceGateway;

public sealed record AttendanceGatewaySystemLogDto(
    string DeviceSn,
    string ConnectionId,
    string Direction,
    string EventType,
    string Message,
    DateTimeOffset OccurredAtUtc);
