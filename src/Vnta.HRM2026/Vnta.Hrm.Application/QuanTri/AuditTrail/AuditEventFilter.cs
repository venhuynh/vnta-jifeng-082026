namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Server-side filter for cursor-paged audit events.
/// Validation of range, lengths, and page size belongs to the caller or query service.
/// </summary>
public sealed record AuditEventFilter(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    string? ActorId = null,
    string? Action = null,
    string? EntityType = null,
    string? EntityId = null,
    string? CorrelationId = null,
    AuditEventCursor? Cursor = null,
    int PageSize = 50);
