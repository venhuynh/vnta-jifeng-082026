namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Immutable snapshot of the actor captured at the command boundary.
/// </summary>
public sealed record AuditActor(
    string ActorId,
    string DisplayName,
    AuditActorKind Kind,
    AuditSource Source);
