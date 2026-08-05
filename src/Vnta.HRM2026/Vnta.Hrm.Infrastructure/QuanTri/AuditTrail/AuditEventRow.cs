using System.ComponentModel.DataAnnotations.Schema;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

/// <summary>
/// Append-only persistence row for one business audit event.
/// </summary>
public sealed class AuditEventRow
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string ActorId { get; set; } = string.Empty;

    public string ActorDisplayName { get; set; } = string.Empty;

    public AuditActorKind ActorKind { get; set; }

    public AuditSource Source { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? EntityDisplayName { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public Guid OperationId { get; set; }

    public string? EventKey { get; set; }

    /// <summary>
    /// JSON object serialized by the audit writer. It must never contain a secret or raw payload.
    /// </summary>
    public string? MetadataJson { get; set; }

    public short SchemaVersion { get; set; } = 1;

    public List<AuditPropertyChangeRow> PropertyChanges { get; } = [];

    /// <summary>
    /// Marks rows staged by a SaveChanges interception attempt. This is process-local only and
    /// allows the interceptor to remove rows after a failed or cancelled save without retaining
    /// mutable state in the singleton interceptor.
    /// </summary>
    [NotMapped]
    internal Guid? PendingCaptureToken { get; set; }
}
