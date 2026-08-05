namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// A cursor page in the fixed descending audit-event order.
/// </summary>
public sealed record AuditEventPageDto(
    IReadOnlyList<AuditEventListItemDto> Items,
    AuditEventCursor? NextCursor);
