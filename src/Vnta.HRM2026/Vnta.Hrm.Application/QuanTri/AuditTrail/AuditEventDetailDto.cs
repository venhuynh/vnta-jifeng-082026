namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// A masked audit event with its property-level changes.
/// </summary>
public sealed record AuditEventDetailDto(
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
    Guid OperationId,
    IReadOnlyList<AuditPropertyChangeDto> PropertyChanges);
