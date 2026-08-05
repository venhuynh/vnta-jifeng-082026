namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Immutable authorization result supplied to audit queries after the transport layer authorizes the caller.
/// </summary>
public sealed record AuditReadAccess(bool CanReadSensitiveValues)
{
    public static AuditReadAccess Masked { get; } = new(false);
}
