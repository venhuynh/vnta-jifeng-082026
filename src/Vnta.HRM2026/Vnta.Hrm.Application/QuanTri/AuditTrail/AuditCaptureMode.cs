namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Controls whether an audited command creates entity-level changes or one operation-level event.
/// </summary>
public enum AuditCaptureMode
{
    EntityChanges,
    OperationOnly
}
