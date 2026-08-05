namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Describes the business outcome of an operation-level audit event.
/// </summary>
public enum AuditOperationOutcome
{
    Succeeded,
    NoChanges,
    Failed,
    Cancelled
}
