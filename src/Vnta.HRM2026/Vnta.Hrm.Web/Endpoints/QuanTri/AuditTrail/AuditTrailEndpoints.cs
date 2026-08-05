using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.QuanTri.AuditTrail;

/// <summary>
/// Read-only HTTP boundary for audit data. Interactive Server currently reads through DI, while
/// these endpoints provide the same server-authorized and masked contract for future callers.
/// </summary>
public static class AuditTrailEndpoints
{
    public static IEndpointRouteBuilder MapAuditTrailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/audit-events")
            .WithTags("Audit trail")
            .RequireAuthorization(InternalAccountPolicies.AuditRead);

        group.MapGet("", GetPageAsync);
        group.MapGet("/{id:guid}", GetDetailAsync);
        group.MapGet("/{id:guid}/context", GetContextAsync);

        return endpoints;
    }

    private static async Task<IResult> GetPageAsync(
        HttpContext httpContext,
        [AsParameters] AuditEventPageRequest request,
        [FromServices] IAuditTrailQueryService auditTrailQueryService,
        CancellationToken cancellationToken)
    {
        SetNoStore(httpContext);

        try
        {
            var filter = request.ToFilter();
            var result = await auditTrailQueryService
                .GetPageAsync(filter, CreateReadAccess(httpContext), cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetDetailAsync(
        Guid id,
        HttpContext httpContext,
        [FromServices] IAuditTrailQueryService auditTrailQueryService,
        CancellationToken cancellationToken)
    {
        SetNoStore(httpContext);

        var result = await auditTrailQueryService
            .GetDetailAsync(id, CreateReadAccess(httpContext), cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetContextAsync(
        Guid id,
        int? take,
        HttpContext httpContext,
        [FromServices] IAuditTrailQueryService auditTrailQueryService,
        CancellationToken cancellationToken)
    {
        SetNoStore(httpContext);

        try
        {
            var result = await auditTrailQueryService
                .GetContextAsync(
                    id,
                    CreateReadAccess(httpContext),
                    take ?? 50,
                    cancellationToken)
                .ConfigureAwait(false);

            if (result is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static AuditReadAccess CreateReadAccess(HttpContext httpContext) =>
        new(InternalAccountCapabilityResolver.HasCapability(
            httpContext.User,
            InternalAccountCapabilities.AuditSensitiveRead));

    private static void SetNoStore(HttpContext httpContext) =>
        httpContext.Response.Headers.CacheControl = "no-store";

    private sealed record AuditEventPageRequest(
        DateTimeOffset? FromUtc,
        DateTimeOffset? ToUtc,
        string? ActorId,
        string? Action,
        string? EntityType,
        string? EntityId,
        string? CorrelationId,
        DateTimeOffset? CursorOccurredAtUtc,
        Guid? CursorId,
        int? PageSize)
    {
        public AuditEventFilter ToFilter()
        {
            if (CursorOccurredAtUtc is null && CursorId is not null
                || CursorOccurredAtUtc is not null && CursorId is null)
            {
                throw new ArgumentException(
                    "CursorOccurredAtUtc and CursorId must be supplied together.");
            }

            return new AuditEventFilter(
                FromUtc,
                ToUtc,
                ActorId,
                Action,
                EntityType,
                EntityId,
                CorrelationId,
                CursorOccurredAtUtc is { } occurredAtUtc && CursorId is { } id
                    ? new AuditEventCursor(occurredAtUtc, id)
                    : null,
                PageSize ?? 50);
        }
    }
}
