namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Sets the correlation identifier at a transport or worker boundary for the current async flow.
/// </summary>
public interface IAuditCorrelationScope : IAuditCorrelationAccessor
{
    IDisposable Begin(string correlationId);
}
