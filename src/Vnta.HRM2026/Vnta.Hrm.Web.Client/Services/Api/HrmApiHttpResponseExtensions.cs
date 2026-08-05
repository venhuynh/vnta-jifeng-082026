using System.Net.Http.Json;
using System.Text.Json;

namespace Vnta.Hrm.Web.Client.Services.Api;

internal static class HrmApiHttpResponseExtensions
{
    public static async Task<T> ReadRequiredFromJsonAsync<T>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        await EnsureSuccessAsync(response, cancellationToken);

        var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return value is null
            ? throw new InvalidOperationException("API response did not contain the expected payload.")
            : value;
    }

    public static async Task EnsureSuccessAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await ReadErrorAsync(response, cancellationToken);
        throw new HrmApiException(error.Kind, response.StatusCode, error.Message, error.TraceId);
    }

    private static async Task<ApiError> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var fallback = Describe(response.StatusCode);
        var traceId = response.Headers.TryGetValues("X-Trace-Id", out var traceIdValues)
            ? traceIdValues.FirstOrDefault()
            : null;
        var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return fallback with { TraceId = traceId };
        }

        try
        {
            using var document = JsonDocument.Parse(rawContent);
            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                return fallback with { Message = document.RootElement.GetString() ?? fallback.Message, TraceId = traceId };
            }

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var message = ReadString(document.RootElement, "message")
                    ?? ReadString(document.RootElement, "detail")
                    ?? ReadString(document.RootElement, "title")
                    ?? fallback.Message;
                traceId ??= ReadString(document.RootElement, "traceId");
                return fallback with { Message = message, TraceId = traceId };
            }
        }
        catch (JsonException)
        {
        }

        return fallback with { TraceId = traceId };
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static ApiError Describe(System.Net.HttpStatusCode statusCode) => statusCode switch
    {
        System.Net.HttpStatusCode.BadRequest => new(HrmApiErrorKind.Validation, "Yêu cầu không hợp lệ.", null),
        System.Net.HttpStatusCode.Unauthorized => new(HrmApiErrorKind.Unauthenticated, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", null),
        System.Net.HttpStatusCode.Forbidden => new(HrmApiErrorKind.Forbidden, "Bạn không có quyền thực hiện thao tác này.", null),
        System.Net.HttpStatusCode.NotFound => new(HrmApiErrorKind.NotFound, "Không tìm thấy dữ liệu yêu cầu.", null),
        System.Net.HttpStatusCode.Conflict => new(HrmApiErrorKind.Conflict, "Dữ liệu đã thay đổi. Vui lòng tải lại và thử lại.", null),
        System.Net.HttpStatusCode.TooManyRequests => new(HrmApiErrorKind.RateLimited, "Thao tác đang được thực hiện quá nhanh. Vui lòng thử lại sau.", null),
        System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.ServiceUnavailable or System.Net.HttpStatusCode.GatewayTimeout => new(HrmApiErrorKind.Unavailable, "Dịch vụ đang tạm thời không sẵn sàng. Vui lòng thử lại.", null),
        _ => new(HrmApiErrorKind.Unexpected, "Hệ thống gặp sự cố. Vui lòng thử lại sau.", null)
    };

    private sealed record ApiError(HrmApiErrorKind Kind, string Message, string? TraceId);
}
