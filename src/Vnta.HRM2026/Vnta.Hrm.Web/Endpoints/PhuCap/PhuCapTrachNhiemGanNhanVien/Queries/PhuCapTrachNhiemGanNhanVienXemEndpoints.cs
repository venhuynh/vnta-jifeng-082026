using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>HTTP boundary riêng cho use case Xem của màn gán phụ cấp trách nhiệm theo nhân viên.</summary>
public static class PhuCapTrachNhiemGanNhanVienXemEndpoints
{
    public static RouteGroupBuilder MapPhuCapTrachNhiemGanNhanVienXemEndpoints(
        this RouteGroupBuilder payrollGroup)
    {
        payrollGroup.MapPost(
            "/responsibility-allowance/employee-assignments/view",
            ExecuteAsync);
        return payrollGroup;
    }

    private static async Task<IResult> ExecuteAsync(
        [FromBody] XemPhuCapTrachNhiemGanNhanVienRequest? request,
        [FromServices] IPhuCapTrachNhiemGanNhanVienXemService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu điều kiện xem danh sách gán cấp bậc nhân viên." });
        }

        try
        {
            return Results.Ok(await service.ExecuteAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }
}
