using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;

namespace Vnta.Hrm.Web.Audit;

/// <summary>
/// Server-side command boundary for Interactive Server components. It snapshots the current
/// authentication state before a business write and keeps the audit scope alive for the whole
/// asynchronous command flow.
/// </summary>
public sealed class InteractiveAuditCommandScopeFactory : IInteractiveAuditCommandScopeFactory
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IAuditScope _auditScope;
    private readonly IAuditCorrelationAccessor _auditCorrelationAccessor;

    public InteractiveAuditCommandScopeFactory(
        AuthenticationStateProvider authenticationStateProvider,
        IAuditScope auditScope,
        IAuditCorrelationAccessor auditCorrelationAccessor)
    {
        _authenticationStateProvider = authenticationStateProvider
            ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
        _auditScope = auditScope ?? throw new ArgumentNullException(nameof(auditScope));
        _auditCorrelationAccessor = auditCorrelationAccessor
            ?? throw new ArgumentNullException(nameof(auditCorrelationAccessor));
    }

    public Task ExecuteAsync(
        string actionIntent,
        Func<CancellationToken, Task> command,
        AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return ExecuteAsync<object?>(
            actionIntent,
            async token =>
            {
                await command(token).ConfigureAwait(false);
                return null;
            },
            captureMode,
            metadata,
            cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(
        string actionIntent,
        Func<CancellationToken, Task<T>> command,
        AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ValidateActionIntent(actionIntent);
        ArgumentNullException.ThrowIfNull(command);

        var authenticationState = await _authenticationStateProvider
            .GetAuthenticationStateAsync()
            .ConfigureAwait(false);
        var actor = CreateActor(authenticationState.User);
        var parent = _auditScope.Current;

        var auditCommand = new AuditCommand(
            parent?.OperationId ?? Guid.NewGuid(),
            actionIntent,
            actor,
            parent?.CorrelationId ?? _auditCorrelationAccessor.Current ?? CreateCorrelationId(),
            captureMode,
            Metadata: MergeMetadata(parent?.Metadata, metadata));

        using var scope = _auditScope.Begin(auditCommand);
        return await command(cancellationToken).ConfigureAwait(false);
    }

    private static AuditActor CreateActor(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "An authenticated user is required to execute an audited Interactive Server command.");
        }

        var actorId = Normalize(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        if (actorId is null)
        {
            throw new InvalidOperationException(
                "The authenticated user does not have a stable NameIdentifier claim for audit.");
        }

        var displayName = Normalize(user.FindFirst(ClaimTypes.Name)?.Value)
            ?? Normalize(user.FindFirst(ClaimTypes.Email)?.Value)
            ?? Normalize(user.Identity.Name)
            ?? actorId;

        return new AuditActor(
            actorId,
            displayName,
            AuditActorKind.User,
            AuditSource.InteractiveServer);
    }

    private static string CreateCorrelationId()
    {
        var traceId = Normalize(Activity.Current?.TraceId.ToString());
        return traceId ?? Guid.NewGuid().ToString("N");
    }

    private static IReadOnlyDictionary<string, string>? MergeMetadata(
        IReadOnlyDictionary<string, string>? parent,
        IReadOnlyDictionary<string, string>? current)
    {
        if ((parent is null || parent.Count == 0) && (current is null || current.Count == 0))
        {
            return null;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        if (parent is not null)
        {
            foreach (var (key, value) in parent)
            {
                values[key] = value;
            }
        }

        if (current is not null)
        {
            foreach (var (key, value) in current)
            {
                values[key] = value;
            }
        }

        return new ReadOnlyDictionary<string, string>(values);
    }

    private static void ValidateActionIntent(string actionIntent)
    {
        if (string.IsNullOrWhiteSpace(actionIntent) || actionIntent.Length > 100)
        {
            throw new ArgumentException(
                "An audit action between 1 and 100 characters is required.",
                nameof(actionIntent));
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
