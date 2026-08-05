namespace Vnta.Hrm.Application.Integrations.AttendanceGateway;

public sealed record AttendanceGatewayRealtimeEventDto(
    string? Id,
    string? FlowId,
    string? ConnectionId,
    string? Sn,
    string? DeviceName,
    string RequestMethod,
    string RequestUrl,
    string Direction,
    string EventType,
    string RawBody,
    string LogStatus,
    string? RejectionReason,
    DateTimeOffset ReceivedAtUtc,
    string? SummaryText = null,
    bool IsSemantic = false);
