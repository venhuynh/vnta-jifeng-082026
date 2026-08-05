namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Executes raw SQL, bulk, or other non-tracked writes with an audit event in the same transaction.
/// </summary>
public interface IAuditedMutation
{
    Task<T> ExecuteAsync<T>(
        AuditCommand command,
        Func<CancellationToken, Task<T>> mutation,
        Func<T, AuditOperationEvent> eventFactory,
        CancellationToken cancellationToken = default);
}
