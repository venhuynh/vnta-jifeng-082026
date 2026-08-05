using System.Diagnostics;
using Serilog.Context;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Audit;

/// <summary>
/// Captures one trusted correlation identifier for each HTTP request. Business endpoints and
/// Serilog use the same value without accepting an arbitrary browser-supplied correlation id.
/// </summary>
public sealed class AuditRequestContextMiddleware
{
    public const string CorrelationIdItemKey = "AuditCorrelationId";

    private readonly RequestDelegate _next;

    public AuditRequestContextMiddleware(RequestDelegate next) =>
        _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(HttpContext context, IAuditCorrelationScope correlationScope)
    {
        var correlationId = CreateCorrelationId(context);
        context.Items[CorrelationIdItemKey] = correlationId;
        context.Response.Headers.TryAdd("X-Correlation-Id", correlationId);

        using var scope = correlationScope.Begin(correlationId);
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    private static string CreateCorrelationId(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(traceId) && traceId.Length <= 128)
        {
            return traceId;
        }

        return !string.IsNullOrWhiteSpace(context.TraceIdentifier)
            && context.TraceIdentifier.Length <= 128
                ? context.TraceIdentifier
                : Guid.NewGuid().ToString("N");
    }
}
