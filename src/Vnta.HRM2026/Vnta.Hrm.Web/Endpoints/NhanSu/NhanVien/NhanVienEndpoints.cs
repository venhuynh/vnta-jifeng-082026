using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.NhanSu.NhanVien;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.NhanSu.NhanVien;

/// <summary>
/// HTTP boundary dành cho consumer Nhân sự. API attendance legacy được giữ riêng để không đổi
/// contract của integration cũ.
/// </summary>
public static class NhanVienEndpoints
{
    public static IEndpointRouteBuilder MapNhanVienEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/nhan-su/nhan-vien")
            .WithTags("Nhân sự - Nhân viên")
            .RequireAuthorization(InternalAccountPolicies.HumanResourcesAdministration);

        group.MapPost("/search-page", SearchPageAsync);
        group.MapPost("/summary", GetSummaryAsync);
        group.MapPost("/nhansu-workbook-preview", PreviewNhanSuWorkbookAsync);
        group.MapPost("/nhansu-workbook-import", ImportNhanSuWorkbookAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPost("/delete", DeleteAsync);
        group.MapPost("/{id:guid}/status", ChangeStatusAsync);
        group.MapPost("/refresh", RefreshAsync);

        return endpoints;
    }

    private static async Task<IResult> SearchPageAsync(
        [FromBody] NhanVienListQuery? query,
        [FromServices] INhanVienListReadService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SearchPageAsync(
            query ?? new NhanVienListQuery(null),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetSummaryAsync(
        [FromBody] EmployeeFilter? filter,
        [FromServices] INhanVienSummaryReadService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetSummaryAsync(
            filter ?? new EmployeeFilter(null),
            cancellationToken);
        return Results.Ok(result);
    }

    /// <summary>
    /// Nhận raw body là tệp .xlsx để preview/đối soát sheet NhanSu. Endpoint không ghi dữ liệu
    /// và không tạo audit command vì chỉ thực hiện đọc.
    /// </summary>
    private static async Task<IResult> PreviewNhanSuWorkbookAsync(
        HttpRequest request,
        [FromServices] INhanSuWorkbookPreviewService service,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength is 0)
        {
            return Results.BadRequest(new { message = "Thiếu tệp Excel để đối soát." });
        }

        if (request.ContentLength is { } contentLength
            && contentLength > NhanSuWorkbookPreviewLimits.MaxWorkbookBytes)
        {
            return Results.BadRequest(new
            {
                message = $"Tệp Excel không được vượt quá {NhanSuWorkbookPreviewLimits.MaxWorkbookBytes / (1024 * 1024)} MB."
            });
        }

        try
        {
            var result = await service.PreviewAsync(request.Body, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Nhận raw body là tệp .xlsx và tạo mới nhân viên từ sheet NhanSu. Import là create-only:
    /// mã nhân viên đang hoạt động đã có sẽ được bỏ qua, không bị ghi đè. Toàn bộ workbook phải
    /// qua preflight trước khi bất kỳ nhân viên nào được ghi.
    /// </summary>
    private static async Task<IResult> ImportNhanSuWorkbookAsync(
        HttpContext httpContext,
        HttpRequest request,
        [FromQuery] DateTime? missingHireDateFallback,
        [FromQuery] int? activeStatusFallback,
        [FromServices] INhanSuWorkbookImportService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength is 0)
        {
            return Results.BadRequest(new { message = "Thiếu tệp Excel để import nhân viên." });
        }

        if (request.ContentLength is { } contentLength
            && contentLength > NhanSuWorkbookPreviewLimits.MaxWorkbookBytes)
        {
            return Results.BadRequest(new
            {
                message = $"Tệp Excel không được vượt quá {NhanSuWorkbookPreviewLimits.MaxWorkbookBytes / (1024 * 1024)} MB."
            });
        }

        try
        {
            var result = await ExecuteAuditedAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.NhanVien.ImportFromNhanSuWorkbook,
                token => service.ImportAsync(
                    request.Body,
                    missingHireDateFallback,
                    activeStatusFallback,
                    token),
                cancellationToken,
                AuditCaptureMode.OperationOnly);
            return Results.Ok(result);
        }
        catch (NhanSuWorkbookImportValidationException exception)
        {
            return Results.BadRequest(new
            {
                message = exception.Message,
                issues = exception.Issues
            });
        }
        catch (DbUpdateException)
        {
            // A concurrent writer may have inserted an employee code after preflight. The
            // transaction is rolled back by IAuditedMutation, and the caller can preview/retry.
            return Results.Conflict(new
            {
                message = "Dữ liệu nhân viên đã thay đổi trong lúc import. Vui lòng đối soát và thử lại."
            });
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext,
        [FromBody] CreateEmployeeRequest? request,
        [FromServices] INhanVienCreateService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu thông tin nhân viên." });
        }

        try
        {
            var result = await ExecuteAuditedAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.NhanVien.Create,
                token => service.CreateAsync(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RefreshAsync(
        HttpContext httpContext,
        [FromServices] INhanVienRefreshService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAuditedAsync(
            httpContext,
            auditScope,
            correlationAccessor,
            AuditActions.NhanVien.RefreshFromAttendance,
            token => service.RefreshFromDeviceUserProfilesAsync(token),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateAsync(
        HttpContext httpContext,
        [FromRoute] Guid id,
        [FromBody] UpdateEmployeeRequest? request,
        [FromServices] INhanVienEditService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null || id == Guid.Empty || id != request.Id)
        {
            return Results.BadRequest(new { message = "Thông tin nhân viên cần điều chỉnh không hợp lệ." });
        }

        try
        {
            var result = await ExecuteAuditedAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.NhanVien.Update,
                token => service.UpdateAsync(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteAsync(
        HttpContext httpContext,
        [FromBody] IReadOnlyCollection<Guid>? ids,
        [FromServices] INhanVienDeleteService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (ids is null || ids.Count == 0)
        {
            return Results.BadRequest(new { message = "Thiếu nhân viên cần xóa." });
        }

        await ExecuteAuditedAsync(
            httpContext,
            auditScope,
            correlationAccessor,
            AuditActions.NhanVien.Delete,
            async token =>
            {
                await service.DeleteAsync(ids, token);
                return true;
            },
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ChangeStatusAsync(
        HttpContext httpContext,
        [FromRoute] Guid id,
        [FromBody] ChangeEmployeeStatusRequest? request,
        [FromServices] INhanVienStatusService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null || id == Guid.Empty || id != request.Id)
        {
            return Results.BadRequest(new { message = "Thông tin thay đổi tình trạng nhân viên không hợp lệ." });
        }

        try
        {
            var result = await ExecuteAuditedAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.NhanVien.ChangeStatus,
                token => service.ChangeStatusAsync(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<T> ExecuteAuditedAsync<T>(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string actionIntent,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken,
        AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges)
    {
        using var scope = auditScope.Begin(new AuditCommand(
            Guid.NewGuid(),
            actionIntent,
            CreateActor(httpContext.User),
            correlationAccessor.Current ?? httpContext.TraceIdentifier,
            captureMode));
        return await action(cancellationToken);
    }

    private static AuditActor CreateActor(ClaimsPrincipal user)
    {
        var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new UnauthorizedAccessException("Không xác định được người dùng thực hiện thao tác.");
        }

        var displayName = user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? actorId;
        return new AuditActor(actorId, displayName, AuditActorKind.User, AuditSource.Api);
    }
}
