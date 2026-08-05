using System.Security.Claims;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>
/// Shared HTTP audit boundary for payroll commands. Feature endpoints provide their
/// own command-specific error mapping while this helper owns authenticated actor and correlation capture.
/// </summary>
internal static class PayrollEndpointExecution
{
    public static async Task ExecuteAsync(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string actionIntent,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        await ExecuteAsync<object?>(
            httpContext,
            auditScope,
            correlationAccessor,
            actionIntent,
            async token =>
            {
                await action(token);
                return null;
            },
            cancellationToken,
            captureMode,
            metadata);
    }

    public static async Task<T> ExecuteAsync<T>(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string actionIntent,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken,
        AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        using var scope = auditScope.Begin(new AuditCommand(
            Guid.NewGuid(),
            actionIntent,
            CreateActor(httpContext.User),
            correlationAccessor.Current ?? httpContext.TraceIdentifier,
            captureMode,
            Metadata: metadata));
        return await action(cancellationToken);
    }

    private static AuditActor CreateActor(ClaimsPrincipal user)
    {
        var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if(string.IsNullOrWhiteSpace(actorId))
        {
            throw new UnauthorizedAccessException("Không xác định được người dùng thực hiện thao tác.");
        }

        var displayName = user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? actorId;
        return new AuditActor(actorId, displayName, AuditActorKind.User, AuditSource.Api);
    }
}
