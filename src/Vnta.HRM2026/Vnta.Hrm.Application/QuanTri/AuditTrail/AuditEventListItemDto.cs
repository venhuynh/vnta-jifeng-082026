namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Masked audit-event data safe for the list and correlation-context views.
/// </summary>
public sealed record AuditEventListItemDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    string ActorId,
    string ActorDisplayName,
    AuditActorKind ActorKind,
    AuditSource Source,
    string Action,
    string EntityType,
    string? EntityId,
    string? EntityDisplayName,
    string CorrelationId,
    Guid OperationId);
