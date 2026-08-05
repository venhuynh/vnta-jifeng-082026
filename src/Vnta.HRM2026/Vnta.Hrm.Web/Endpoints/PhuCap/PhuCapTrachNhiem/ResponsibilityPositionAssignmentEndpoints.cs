using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Versioned use-case boundary for position assignments. The legacy aggregate
/// responsibility workflow endpoints remain available for its other consumers.
/// </summary>
public static class ResponsibilityPositionAssignmentEndpoints
{
    public static IEndpointRouteBuilder MapResponsibilityPositionAssignmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/payroll/responsibility-position-assignments")
            .WithTags("Payroll - Responsibility position assignments")
            .RequireAuthorization(InternalAccountPolicies.PayrollAdministration);

        group.MapPost("/search", SearchAsync);
        group.MapGet("/grade-options", GetGradeOptionsAsync);
        group.MapGet("/export", ExportAsync);
        group.MapPost("", SaveAsync);
        group.MapPost("/deactivate", DeactivateAsync);
        group.MapPost("/copy-from-previous", CopyFromPreviousAsync);

        return endpoints;
    }

    private static async Task<IResult> SearchAsync(
        [FromBody] ResponsibilityPositionAssignmentQuery? query,
        [FromServices] IResponsibilityPositionAssignmentReadService service,
        CancellationToken cancellationToken)
    {
        if (query is null)
        {
            return Results.BadRequest(new { message = "Thiếu điều kiện tìm kiếm gán chức vụ." });
        }

        try
        {
            return Results.Ok(await service.SearchPageAsync(query, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> GetGradeOptionsAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IResponsibilityPositionAssignmentReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.GetGradeOptionsAsync(year, month, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> ExportAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IResponsibilityPositionAssignmentExportReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.ExportAllAsync(year, month, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> SaveAsync(
        HttpContext httpContext,
        [FromBody] SaveResponsibilityPositionAssignmentRequest? request,
        [FromServices] IResponsibilityPositionAssignmentCommandService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu thông tin gán chức vụ." });
        }

        var action = request.Id.HasValue && request.Id.Value != Guid.Empty
            ? AuditActions.ResponsibilityPositionAssignment.Update
            : AuditActions.ResponsibilityPositionAssignment.Create;
        try
        {
            var result = await ExecuteAuditedAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                action,
                token => service.SaveAsync(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ResponsibilityPositionAssignmentConflictException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> DeactivateAsync(
        HttpContext httpContext,
        [FromBody] DeactivateResponsibilityPositionAssignmentRequest? request,
        [FromServices] IResponsibilityPositionAssignmentCommandService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu thông tin ngừng dùng gán chức vụ." });
        }

        try
        {
            var result = await ExecuteAuditedAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.ResponsibilityPositionAssignment.Deactivate,
                token => service.DeactivateAsync(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ResponsibilityPositionAssignmentConflictException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> CopyFromPreviousAsync(
        HttpContext httpContext,
        [FromBody] CopyResponsibilityPositionAssignmentsRequest? request,
        [FromServices] IResponsibilityPositionAssignmentCopyService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu kỳ cần lấy gán chức vụ." });
        }

        try
        {
            var result = await ExecuteAuditedAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.ResponsibilityPositionAssignment.CopyFromPreviousPeriod,
                token => service.CopyFromPreviousPeriodAsync(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ResponsibilityPositionAssignmentConflictException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<T> ExecuteAuditedAsync<T>(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string actionIntent,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        using var scope = auditScope.Begin(new AuditCommand(
            Guid.NewGuid(),
            actionIntent,
            CreateActor(httpContext.User),
            correlationAccessor.Current ?? httpContext.TraceIdentifier,
            AuditCaptureMode.EntityChanges));
        return await action(cancellationToken);
    }

    private static AuditActor CreateActor(ClaimsPrincipal user)
    {
        var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
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
