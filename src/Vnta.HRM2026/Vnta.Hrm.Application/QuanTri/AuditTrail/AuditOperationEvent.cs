namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Describes the single event emitted for an operation-level mutation.
/// Values in <see cref="Metadata"/> must already be sanitized by the caller.
/// </summary>
public sealed record AuditOperationEvent(
    string Action,
    string EntityType,
    string? EntityId = null,
    string? EntityDisplayName = null,
    AuditOperationOutcome Outcome = AuditOperationOutcome.Succeeded,
    IReadOnlyDictionary<string, string>? Metadata = null);
