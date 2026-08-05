namespace Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

/// <summary>
/// Append-only persistence row for one property change belonging to an audit event.
/// </summary>
public sealed class AuditPropertyChangeRow
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AuditEventId { get; set; }

    public AuditEventRow AuditEvent { get; set; } = null!;

    public string PropertyName { get; set; } = string.Empty;

    public string PropertyLabel { get; set; } = string.Empty;

    public string? OldValueJson { get; set; }

    public string? NewValueJson { get; set; }

    public string? OldDisplay { get; set; }

    public string? NewDisplay { get; set; }

    public bool IsSensitive { get; set; }

    public byte[]? OldCiphertext { get; set; }

    public byte[]? NewCiphertext { get; set; }

    public string? EncryptionKeyId { get; set; }
}
