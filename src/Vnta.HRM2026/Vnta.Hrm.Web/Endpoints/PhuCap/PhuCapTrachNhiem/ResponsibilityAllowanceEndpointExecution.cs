using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiem;

internal static class ResponsibilityAllowanceEndpointExecution
{
    internal static IResult MissingPayload(string message) => Results.BadRequest(new { message });

    internal static async Task<IResult> QueryAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await operation(cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    internal static async Task<IResult> CommandAsync<T>(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string auditAction,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken,
        AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges)
    {
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                auditAction,
                operation,
                cancellationToken,
                captureMode);
            return Results.Ok(result);
        }
        catch (ResponsibilityAllowanceConflictException exception)
        {
            return Results.Conflict(new { message = exception.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { message = "Dữ liệu đã thay đổi bởi thao tác khác. Vui lòng tải lại." });
        }
        catch (KeyNotFoundException exception)
        {
            return Results.NotFound(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }
}
