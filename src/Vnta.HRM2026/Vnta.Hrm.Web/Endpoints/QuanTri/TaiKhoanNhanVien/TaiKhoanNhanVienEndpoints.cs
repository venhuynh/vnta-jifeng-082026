using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace Vnta.Hrm.Web.Endpoints.QuanTri.TaiKhoanNhanVien;

public static class TaiKhoanNhanVienEndpoints
{
    public static IEndpointRouteBuilder MapTaiKhoanNhanVienEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var adminGroup = endpoints.MapGroup("/api/admin")
            .WithTags("Security");

        adminGroup.MapGet("/employee-accounts", GetEmployeeAccountsAsync)
            .RequireAuthorization(InternalAccountPolicies.EmployeeAccountAdministration);
        adminGroup.MapPost("/employee-accounts/open", OpenEmployeeAccountAsync)
            .RequireAuthorization(InternalAccountPolicies.EmployeeAccountAdministration);
        adminGroup.MapPost("/employee-accounts/reset-password", ResetEmployeeAccountPasswordAsync)
            .RequireAuthorization(InternalAccountPolicies.EmployeeAccountAdministration);
        adminGroup.MapPost("/employee-accounts/activate", ActivateEmployeeAccountAsync)
            .RequireAuthorization(InternalAccountPolicies.EmployeeAccountAdministration);
        adminGroup.MapPost("/employee-accounts/deactivate", DeactivateEmployeeAccountAsync)
            .RequireAuthorization(InternalAccountPolicies.EmployeeAccountAdministration);
        adminGroup.MapPost("/employee-accounts/approve", ApproveEmployeeAccountAsync)
            .RequireAuthorization(InternalAccountPolicies.EmployeeAccountApproval);
        adminGroup.MapPost("/employee-accounts/reject", RejectEmployeeAccountAsync)
            .RequireAuthorization(InternalAccountPolicies.EmployeeAccountApproval);

        return endpoints;
    }

    private static async Task<IResult> GetEmployeeAccountsAsync(
        [FromServices] IEmployeeAccountService employeeAccountService,
        CancellationToken cancellationToken)
    {
        var result = await employeeAccountService.GetAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> OpenEmployeeAccountAsync(
        [FromBody] OpenEmployeeAccountRequest? request,
        [FromServices] IEmployeeAccountService employeeAccountService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload mở tài khoản nhân viên." });
        }

        try
        {
            var result = await employeeAccountService.OpenAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ApproveEmployeeAccountAsync(
        HttpContext httpContext,
        [FromBody] ReviewEmployeeAccountRequest? request,
        [FromServices] IEmployeeAccountService employeeAccountService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload phê duyệt tài khoản nhân viên." });
        }

        var reviewedByUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(string.IsNullOrWhiteSpace(reviewedByUserId))
        {
            return Results.BadRequest(new { message = "Không xác định được người thực hiện phê duyệt." });
        }

        try
        {
            var result = await employeeAccountService.ApproveAsync(
                request with { ReviewedByUserId = reviewedByUserId },
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RejectEmployeeAccountAsync(
        HttpContext httpContext,
        [FromBody] ReviewEmployeeAccountRequest? request,
        [FromServices] IEmployeeAccountService employeeAccountService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload từ chối tài khoản nhân viên." });
        }

        var reviewedByUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(string.IsNullOrWhiteSpace(reviewedByUserId))
        {
            return Results.BadRequest(new { message = "Không xác định được người thực hiện phê duyệt." });
        }

        try
        {
            var result = await employeeAccountService.RejectAsync(
                request with { ReviewedByUserId = reviewedByUserId },
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ResetEmployeeAccountPasswordAsync(
        [FromBody] ResetEmployeeAccountPasswordRequest? request,
        [FromServices] IEmployeeAccountService employeeAccountService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload đặt lại mật khẩu tài khoản nhân viên." });
        }

        try
        {
            var result = await employeeAccountService.ResetPasswordAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ActivateEmployeeAccountAsync(
        [FromBody] EmployeeAccountStateChangeRequest? request,
        [FromServices] IEmployeeAccountService employeeAccountService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload kích hoạt lại tài khoản nhân viên." });
        }

        try
        {
            var result = await employeeAccountService.ActivateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeactivateEmployeeAccountAsync(
        [FromBody] EmployeeAccountStateChangeRequest? request,
        [FromServices] IEmployeeAccountService employeeAccountService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload ngưng kích hoạt tài khoản nhân viên." });
        }

        try
        {
            var result = await employeeAccountService.DeactivateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
