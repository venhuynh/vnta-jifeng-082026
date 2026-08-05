using System.Security.Claims;
using Vnta.Hrm.Infrastructure.Identity;
using Vnta.Hrm.Web.Client.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Routing {
    internal static class IdentityComponentsEndpointRouteBuilderExtensions {
        // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
        public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints) {
            ArgumentNullException.ThrowIfNull(endpoints);

            var accountGroup = endpoints.MapGroup("Account");

            accountGroup.MapPost("/PerformExternalLogin", () =>
                Results.BadRequest(new { message = "Hệ thống VNTA HRM không hỗ trợ đăng nhập qua nhà cung cấp bên ngoài." }));

            accountGroup.MapGet("/Logout", async (
                ClaimsPrincipal user,
                SignInManager<ApplicationUser> signInManager,
                [FromQuery(Name = "ReturnUrl")] string returnUrl
            ) => {
                await signInManager.SignOutAsync();
                return TypedResults.LocalRedirect($"~/{returnUrl}");
            });

            var manageGroup = accountGroup.MapGroup("Manage").RequireAuthorization();

            manageGroup.MapPost("/LinkExternalLogin", () =>
                Results.BadRequest(new { message = "Hệ thống VNTA HRM không hỗ trợ đăng nhập qua nhà cung cấp bên ngoài." }));

            var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

            manageGroup.MapPost("/DownloadPersonalData", async (
                HttpContext context,
                [FromServices] UserManager<ApplicationUser> userManager
            ) => {
                var userId = userManager.GetUserId(context.User);
                downloadLogger.LogInformation(
                    "User with ID '{UserId}' attempted to download personal data, but self-service download is disabled for this internal HRM system.",
                    userId);
                return Results.BadRequest(new
                {
                    message = "Hệ thống VNTA HRM không hỗ trợ tự tải dữ liệu cá nhân. Vui lòng liên hệ Phòng Nhân sự hoặc Quản trị hệ thống để được hỗ trợ."
                });
            });

            return accountGroup;
        }
    }
}


