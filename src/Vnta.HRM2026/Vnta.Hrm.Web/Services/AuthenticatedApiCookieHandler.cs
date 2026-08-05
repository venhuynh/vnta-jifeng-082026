using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Vnta.Hrm.Web.Services;

/// <summary>
/// Chuyển tiếp cookie Identity của circuit hiện tại khi UI Interactive Server gọi API nội bộ.
/// API vẫn tự áp dụng authorization; handler không tạo hoặc nâng quyền cho request.
/// </summary>
public sealed class AuthenticatedApiCookieHandler(
    IHttpContextAccessor httpContextAccessor,
    IOptionsMonitor<CookieAuthenticationOptions> cookieOptions)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var cookieName = cookieOptions.Get(IdentityConstants.ApplicationScheme).Cookie.Name;

        if(httpContext is not null
           && !string.IsNullOrWhiteSpace(cookieName)
           && httpContext.Request.Cookies.TryGetValue(cookieName, out var cookieValue)
           && !string.IsNullOrWhiteSpace(cookieValue))
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{cookieName}={cookieValue}");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
