namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Cursor for the fixed audit sort order: occurred-at descending, then id descending.
/// </summary>
public sealed record AuditEventCursor(DateTimeOffset OccurredAtUtc, Guid Id);
