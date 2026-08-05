namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Exposes the audit command for the current logical async flow.
/// </summary>
public interface IAuditScope
{
    AuditCommand? Current { get; }

    IDisposable Begin(AuditCommand command);

    void RefineAction(string finalAction);

    void SetOperationOutcome(AuditOperationOutcome outcome);
}
