namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// A property-level change with values already masked according to <see cref="AuditReadAccess"/>.
/// Raw JSON, ciphertext, and secrets are intentionally excluded from this contract.
/// </summary>
public sealed record AuditPropertyChangeDto(
    string PropertyName,
    string PropertyLabel,
    string? OldDisplay,
    string? NewDisplay,
    bool IsSensitive,
    bool Changed);
