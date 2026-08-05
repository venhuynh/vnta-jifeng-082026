namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Identifies the application boundary through which an audited command entered.
/// </summary>
public enum AuditSource
{
    InteractiveServer,
    Api,
    Worker,
    Adms,
    Import
}
