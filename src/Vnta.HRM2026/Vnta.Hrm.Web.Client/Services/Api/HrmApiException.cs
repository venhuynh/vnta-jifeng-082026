using System.Net;

namespace Vnta.Hrm.Web.Client.Services.Api;

internal enum HrmApiErrorKind
{
    Validation,
    Unauthenticated,
    Forbidden,
    NotFound,
    Conflict,
    RateLimited,
    Unavailable,
    Unexpected
}

internal sealed class HrmApiException : InvalidOperationException
{
    public HrmApiException(
        HrmApiErrorKind kind,
        HttpStatusCode statusCode,
        string userMessage,
        string? traceId)
        : base(userMessage)
    {
        Kind = kind;
        StatusCode = statusCode;
        UserMessage = userMessage;
        TraceId = traceId;
    }

    public HrmApiErrorKind Kind { get; }

    public HttpStatusCode StatusCode { get; }

    public string UserMessage { get; }

    public string? TraceId { get; }

    public bool IsRetryable => Kind is HrmApiErrorKind.RateLimited or HrmApiErrorKind.Unavailable;
}
