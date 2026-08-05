using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Vnta.Hrm.Web.Components.Account {
    public class CookieEvents : CookieAuthenticationEvents {
        public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context) {
            if(IsProgrammaticEndpoint(context.Request.Path)) {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            var path = context.Request.Path;
            var redirectUri = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/Login",
                QueryString.Create("ReturnUrl", path.HasValue ? path.Value.TrimStart('/') : string.Empty)
            );
            context.RedirectUri = redirectUri;
            return base.RedirectToLogin(context);
        }

        public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) {
            if(IsProgrammaticEndpoint(context.Request.Path)) {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            return base.RedirectToAccessDenied(context);
        }

        private static bool IsProgrammaticEndpoint(PathString path) =>
            path.StartsWithSegments("/api") || path.StartsWithSegments("/hubs");
    }
}

