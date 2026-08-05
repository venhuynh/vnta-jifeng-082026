using System.Data.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.ErrorHandling;

internal sealed class HrmExceptionHandler(
    ILogger<HrmExceptionHandler> logger,
    HrmResilienceMetrics metrics) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var (statusCode, title, code) = Describe(exception);
        metrics.RecordRequestFailure(code, statusCode);
        logger.LogError(
            exception,
            "Unhandled request failure. ErrorCode={ErrorCode} TraceId={TraceId} Path={Path}",
            code,
            httpContext.TraceIdentifier,
            httpContext.Request.Path);

        if (!httpContext.Request.Path.StartsWithSegments("/api"))
        {
            httpContext.Response.Redirect($"/Error?traceId={Uri.EscapeDataString(httpContext.TraceIdentifier)}");
            return true;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}"
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title, string Code) Describe(Exception exception) => exception switch
    {
        BadHttpRequestException =>
            (StatusCodes.Status400BadRequest, "Yêu cầu không hợp lệ.", "invalid-request"),
        ArgumentException =>
            (StatusCodes.Status400BadRequest, "Dữ liệu gửi lên không hợp lệ.", "invalid-argument"),
        UnauthorizedAccessException =>
            (StatusCodes.Status403Forbidden, "Bạn không có quyền thực hiện thao tác này.", "forbidden"),
        KeyNotFoundException =>
            (StatusCodes.Status404NotFound, "Không tìm thấy dữ liệu yêu cầu.", "not-found"),
        TimeoutException =>
            (StatusCodes.Status503ServiceUnavailable, "Dịch vụ đang phản hồi chậm. Vui lòng thử lại.", "dependency-timeout"),
        _ when ContainsDatabaseException(exception) =>
            (StatusCodes.Status503ServiceUnavailable, "Dịch vụ dữ liệu đang tạm thời không sẵn sàng. Vui lòng thử lại.", "dependency-unavailable"),
        _ =>
            (StatusCodes.Status500InternalServerError, "Hệ thống gặp sự cố. Vui lòng thử lại sau.", "unexpected-error")
    };

    private static bool ContainsDatabaseException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException)
            {
                return true;
            }
        }

        return false;
    }
}
