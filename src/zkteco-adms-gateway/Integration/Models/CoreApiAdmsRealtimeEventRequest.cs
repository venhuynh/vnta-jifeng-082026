namespace Vnta.AttendanceGateway.Integration.Models;

public sealed record CoreApiAdmsRealtimeEventRequest(
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
