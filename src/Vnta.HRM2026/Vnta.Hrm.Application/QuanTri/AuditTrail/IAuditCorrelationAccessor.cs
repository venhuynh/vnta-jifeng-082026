namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Provides the correlation identifier captured at a request, circuit, or worker boundary.
/// </summary>
public interface IAuditCorrelationAccessor
{
    string? Current { get; }
}
