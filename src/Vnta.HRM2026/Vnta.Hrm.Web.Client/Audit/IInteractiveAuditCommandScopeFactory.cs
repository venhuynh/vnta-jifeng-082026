using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Client.Audit;

/// <summary>
/// Opens an audit command boundary for an Interactive Server UI action.
/// Implementations capture the authenticated principal on the server; callers must only pass
/// server-defined action constants such as <see cref="AuditActions"/> members.
/// </summary>
public interface IInteractiveAuditCommandScopeFactory
{
    /// <summary>
    /// Runs one UI command inside an entity-change audit scope.
    /// </summary>
    Task ExecuteAsync(
        string actionIntent,
        Func<CancellationToken, Task> command,
        AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs one UI command inside an audit scope and returns its result.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        string actionIntent,
        Func<CancellationToken, Task<T>> command,
        AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}
