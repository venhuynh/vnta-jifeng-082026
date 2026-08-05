namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Identifies the principal that initiated an audited command.
/// </summary>
public enum AuditActorKind
{
    User,
    System,
    Device,
    Service
}
