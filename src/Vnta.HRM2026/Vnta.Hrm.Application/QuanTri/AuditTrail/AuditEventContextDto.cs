namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Bounded set of masked events that share a correlation identifier.
/// </summary>
public sealed record AuditEventContextDto(
    string CorrelationId,
    IReadOnlyList<AuditEventListItemDto> Items);
