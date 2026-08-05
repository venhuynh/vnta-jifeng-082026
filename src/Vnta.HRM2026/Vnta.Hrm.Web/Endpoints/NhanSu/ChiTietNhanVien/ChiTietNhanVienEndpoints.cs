using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;
namespace Vnta.Hrm.Web.Endpoints.NhanSu.ChiTietNhanVien;

public static class ChiTietNhanVienEndpoints
{
    public static IEndpointRouteBuilder MapChiTietNhanVienEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/nhan-su/chi-tiet-nhan-vien")
            .WithTags("Nhân sự - Chi tiết nhân viên")
            .RequireAuthorization(InternalAccountPolicies.HumanResourcesAdministration);

        group.MapPost("/search", SearchAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/{id:guid}/contact-profile", GetContactProfileAsync);
        group.MapPut("/{id:guid}/contact-profile", UpsertContactProfileAsync);
        group.MapGet("/{id:guid}/citizen-identity", GetCitizenIdentityAsync);
        group.MapPut("/{id:guid}/citizen-identity", UpsertCitizenIdentityAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPost("/delete", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> SearchAsync(
        [FromBody] ChiTietNhanVienFilter? filter,
        [FromServices] IChiTietNhanVienService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SearchAsync(
            filter ?? new ChiTietNhanVienFilter(null),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] Guid id,
        [FromServices] IChiTietNhanVienService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result is null
            ? Results.NotFound(new { message = "Không tìm thấy nhân viên." })
            : Results.Ok(result);
    }

    private static async Task<IResult> GetContactProfileAsync(
        Guid id,
        [FromServices] IChiTietNhanVienService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetContactProfileAsync(id, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpsertContactProfileAsync(
        Guid id,
        [FromBody] UpsertEmployeeContactProfileRequest? request,
        [FromServices] IChiTietNhanVienService service,
        CancellationToken cancellationToken)
    {
        if(request is null || id == Guid.Empty || id != request.EmployeeId) return Results.BadRequest(new { message = "Thông tin liên hệ nhân viên không hợp lệ." });
        try { return Results.Ok(await service.UpsertContactProfileAsync(request, cancellationToken)); }
        catch(InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> GetCitizenIdentityAsync(
        Guid id,
        [FromServices] IChiTietNhanVienService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetCitizenIdentityAsync(id, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpsertCitizenIdentityAsync(
        Guid id,
        [FromBody] UpsertCitizenIdentityRequest? request,
        [FromServices] IChiTietNhanVienService service,
        CancellationToken cancellationToken)
    {
        if(request is null || id == Guid.Empty || id != request.EmployeeId) return Results.BadRequest(new { message = "Thông tin căn cước công dân không hợp lệ." });
        try { return Results.Ok(await service.UpsertCitizenIdentityAsync(request, cancellationToken)); }
        catch(InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateChiTietNhanVienRequest? request,
        [FromServices] IChiTietNhanVienService service,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu thông tin hồ sơ nhân viên." });
        }

        try
        {
            var result = await service.CreateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateChiTietNhanVienRequest? request,
        [FromServices] IChiTietNhanVienService service,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu thông tin hồ sơ nhân viên cần điều chỉnh." });
        }

        if(id == Guid.Empty || id != request.Id)
        {
            return Results.BadRequest(new { message = "Mã định danh nhân viên không khớp." });
        }

        try
        {
            var result = await service.UpdateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteAsync(
        [FromBody] IReadOnlyCollection<Guid>? ids,
        [FromServices] IChiTietNhanVienService service,
        CancellationToken cancellationToken)
    {
        if(ids is null)
        {
            return Results.BadRequest(new { message = "Thiếu danh sách nhân viên cần xóa." });
        }

        await service.DeleteAsync(ids, cancellationToken);
        return Results.NoContent();
    }
}
