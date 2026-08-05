using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.ChamCong.DashboardBangChamCong;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Web.Security;

namespace Vnta.Hrm.Web.Endpoints;

public static class AttendanceGatewayIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceGatewayIntegrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var integrationGroup = endpoints.MapGroup("/api/integration")
            .WithTags("Attendance Gateway Integration")
            .RequireRateLimiting("gateway-inbound")
            .AddEndpointFilter<GatewayInboundHmacEndpointFilter>();

        integrationGroup.MapPost("/attendance-gateway/attendance", HandleAttendanceAsync);
        integrationGroup.MapPost("/attendance-gateway/system-logs", HandleSystemLogAsync);
        integrationGroup.MapPost("/adms/realtime/events", HandleRealtimeEventAsync);

        var attendanceGroup = endpoints.MapGroup("/api/attendance")
            .WithTags("Attendance")
            .RequireAuthorization(InternalAccountPolicies.AttendanceAdministration);

        attendanceGroup.MapGet("/devices", GetAttendanceDevicesAsync);
        attendanceGroup.MapGet("/status-codes", GetAttendanceStatusCodesAsync);
        attendanceGroup.MapPut("/status-codes/{id:guid}", UpdateAttendanceStatusCodeFlagsAsync);
        attendanceGroup.MapDelete("/status-codes/{id:guid}", DeleteAttendanceStatusCodeAsync);
        attendanceGroup.MapGet("/work-calendar", GetAttendanceWorkCalendarAsync);
        attendanceGroup.MapPost("/work-calendar/sundays/day-off", EnsureAttendanceWorkCalendarSundayDayOffsAsync);
        attendanceGroup.MapPost("/work-calendar/validate", ValidateAttendanceWorkCalendarDayAsync);
        attendanceGroup.MapPost("/work-calendar", SaveAttendanceWorkCalendarDayAsync);
        attendanceGroup.MapDelete("/work-calendar/{id:guid}", DeleteAttendanceWorkCalendarDayAsync);
        attendanceGroup.MapPost("/devices/validate", ValidateAttendanceDeviceAsync);
        attendanceGroup.MapPost("/devices", SaveAttendanceDeviceAsync);
        attendanceGroup.MapPost("/devices/delete", DeleteAttendanceDevicesAsync);
        attendanceGroup.MapGet("/logs/recent", GetRecentAttendanceLogsAsync);
        attendanceGroup.MapGet("/logs/by-date-range", GetAttendanceLogsByDateRangeAsync);
        attendanceGroup.MapPost("/logs/search", SearchAttendanceLogsAsync);
        attendanceGroup.MapPost("/biometric-data/search", SearchAttendanceBiometricDataAsync);
        attendanceGroup.MapPost("/biometric-data/device-commands/push", CreateAttendanceBiometricPushCommandsAsync);
        attendanceGroup.MapPost("/biometric-data/device-commands/delete", CreateAttendanceBiometricDeleteCommandsAsync);
        attendanceGroup.MapGet("/biometric-data/refresh/progress", GetAttendanceBiometricDataRefreshProgressAsync);
        attendanceGroup.MapPost("/biometric-data/refresh", RefreshAttendanceBiometricDataAsync);
        attendanceGroup.MapPost("/biometric-data/refresh/{employeeId:guid}", RefreshAttendanceBiometricDataByEmployeeAsync);
        attendanceGroup.MapPost("/logs/daily-summary/search", SearchAttendanceLogDailySummariesAsync);
        attendanceGroup.MapPost("/logs/daily-summary/rebuild", RebuildAttendanceDailySummaryAsync);
        attendanceGroup.MapPost("/logs/workday-summary/search", SearchAttendanceWorkdaySummariesAsync);
        attendanceGroup.MapPost("/timesheet-dashboard", GetAttendanceTimesheetDashboardAsync);
        attendanceGroup.MapPost("/logs/workday-summary/rebuild", RebuildAttendanceWorkdaySummaryAsync);
        attendanceGroup.MapPost("/logs/workday-summary/delete", DeleteAttendanceWorkdaySummariesAsync);
        attendanceGroup.MapPost("/logs/workday-summary/update", UpdateAttendanceWorkdaySummaryAsync);
        attendanceGroup.MapPost("/logs/workday-summary/lock-state", SetAttendanceWorkdaySummaryLockStateAsync);
        attendanceGroup.MapPost("/overtime-registrations/search", SearchOvertimeRegistrationsAsync);
        attendanceGroup.MapPost("/overtime-registrations/draft", CreateOvertimeRegistrationDraftAsync);
        attendanceGroup.MapPost("/overtime-registrations", SaveOvertimeRegistrationAsync);
        attendanceGroup.MapPost("/overtime-registrations/status", ChangeOvertimeRegistrationStatusAsync);
        attendanceGroup.MapPost("/employees/search", SearchEmployeesAsync);
        attendanceGroup.MapPost("/employees/summary", GetEmployeeSummaryAsync);
        attendanceGroup.MapPost("/employees", CreateEmployeeAsync);
        attendanceGroup.MapPut("/employees/{id:guid}", UpdateEmployeeAsync);
        attendanceGroup.MapPost("/employees/delete", DeleteEmployeesAsync);
        attendanceGroup.MapPost("/employees/refresh", RefreshEmployeesAsync);

        var admsCommandGroup = endpoints.MapGroup("/api/adms/device-commands")
            .WithTags("ADMS Device Commands")
            .RequireAuthorization(InternalAccountPolicies.DeviceAdministration);

        admsCommandGroup.MapGet("/lookup-options", GetDeviceCommandLookupOptionsAsync);
        admsCommandGroup.MapPost("/search", SearchDeviceCommandsAsync);
        admsCommandGroup.MapGet("/latest-info-response", GetLatestDeviceInfoResponseAsync);
        admsCommandGroup.MapDelete("/all", DeleteAllDeviceCommandsAsync);
        admsCommandGroup.MapGet("/{id:int}", GetDeviceCommandDetailAsync);
        admsCommandGroup.MapPost("/", CreateDeviceCommandAsync);
        admsCommandGroup.MapPut("/{id:int}", UpdateDeviceCommandAsync);
        admsCommandGroup.MapDelete("/{id:int}", DeleteDeviceCommandAsync);

        return endpoints;
    }

    private static async Task<IResult> HandleAttendanceAsync(
        [FromQuery(Name = "sn")] string? deviceSn,
        [FromBody] IReadOnlyList<AttendanceGatewayAttendanceLogDto>? logs,
        [FromServices] IAttendanceGatewayInboundService inboundService,
        CancellationToken cancellationToken)
    {
        if(string.IsNullOrWhiteSpace(deviceSn))
        {
            return Results.BadRequest(new { message = "Thiếu query string `sn` của thiết bị." });
        }

        if(logs is null || logs.Count == 0)
        {
            return Results.Ok(new AttendanceGatewayIngestionResult(0, 0, 0));
        }

        var result = await inboundService.IngestAttendanceAsync(deviceSn, logs, cancellationToken);
        return Results.Accepted(null, result);
    }

    private static async Task<IResult> HandleSystemLogAsync(
        [FromBody] AttendanceGatewaySystemLogDto? log,
        [FromServices] IAttendanceGatewayInboundService inboundService,
        CancellationToken cancellationToken)
    {
        if(log is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload system log." });
        }

        var result = await inboundService.IngestSystemLogAsync(log, cancellationToken);
        return Results.Accepted(null, result);
    }

    private static async Task<IResult> HandleRealtimeEventAsync(
        [FromBody] AttendanceGatewayRealtimeEventDto? eventDto,
        [FromServices] IAttendanceGatewayInboundService inboundService,
        CancellationToken cancellationToken)
    {
        if(eventDto is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload realtime ADMS event." });
        }

        var result = await inboundService.IngestRealtimeEventAsync(eventDto, cancellationToken);
        return Results.Accepted(null, result);
    }

    private static async Task<IResult> GetAttendanceDevicesAsync(
        [FromServices] IAttendanceDeviceService attendanceDeviceService,
        CancellationToken cancellationToken)
    {
        var result = await attendanceDeviceService.GetAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAttendanceStatusCodesAsync(
        [FromServices] IAttendanceStatusCodeService attendanceStatusCodeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await attendanceStatusCodeService.GetAsync(cancellationToken);
            return Results.Ok(result);
        }
        catch (AttendanceStatusCodeCatalogUnavailableException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Danh mục code kết quả tính công chưa sẵn sàng.",
                detail: "Vui lòng thử lại sau khi dữ liệu chấm công được chuẩn bị xong.");
        }
    }

    private static async Task<IResult> UpdateAttendanceStatusCodeFlagsAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateAttendanceStatusCodeFlagsRequest? request,
        [FromServices] IAttendanceStatusCodeService attendanceStatusCodeService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu thông tin cờ phụ cấp/khấu trừ cần cập nhật." });
        }

        if (id == Guid.Empty || id != request.Id)
        {
            return Results.BadRequest(new { message = "Mã định danh mã kết quả tính công không khớp." });
        }

        try
        {
            var result = await attendanceStatusCodeService.UpdateFlagsAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (AttendanceStatusCodeConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteAttendanceStatusCodeAsync(
        [FromRoute] Guid id,
        [FromServices] IAttendanceStatusCodeService attendanceStatusCodeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await attendanceStatusCodeService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetAttendanceWorkCalendarAsync(
        [FromQuery] int year,
        [FromServices] IAttendanceWorkCalendarService attendanceWorkCalendarService,
        CancellationToken cancellationToken)
    {
        var effectiveYear = year == 0 ? DateTime.Today.Year : year;

        try
        {
            var result = await attendanceWorkCalendarService.GetYearAsync(effectiveYear, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> EnsureAttendanceWorkCalendarSundayDayOffsAsync(
        [FromQuery] int year,
        [FromServices] IAttendanceWorkCalendarService attendanceWorkCalendarService,
        CancellationToken cancellationToken)
    {
        var effectiveYear = year == 0 ? DateTime.Today.Year : year;

        try
        {
            var result = await attendanceWorkCalendarService.EnsureSundayDayOffsAsync(
                effectiveYear,
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ValidateAttendanceWorkCalendarDayAsync(
        [FromBody] UpsertAttendanceWorkCalendarDayRequest? request,
        [FromServices] IAttendanceWorkCalendarService attendanceWorkCalendarService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload lịch làm việc." });
        }

        try
        {
            var validationMessage = await attendanceWorkCalendarService.ValidateAsync(request, cancellationToken);
            return Results.Ok(validationMessage);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SaveAttendanceWorkCalendarDayAsync(
        [FromQuery] bool isNew,
        [FromBody] UpsertAttendanceWorkCalendarDayRequest? request,
        [FromServices] IAttendanceWorkCalendarService attendanceWorkCalendarService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload lịch làm việc." });
        }

        try
        {
            var result = await attendanceWorkCalendarService.SaveAsync(request, isNew, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteAttendanceWorkCalendarDayAsync(
        [FromRoute] Guid id,
        [FromServices] IAttendanceWorkCalendarService attendanceWorkCalendarService,
        CancellationToken cancellationToken)
    {
        try
        {
            await attendanceWorkCalendarService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ValidateAttendanceDeviceAsync(
        [FromBody] UpsertAttendanceDeviceRequest? request,
        [FromServices] IAttendanceDeviceService attendanceDeviceService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload máy chấm công." });
        }

        var validationMessage = await attendanceDeviceService.ValidateAsync(request, cancellationToken);
        return Results.Ok(validationMessage);
    }

    private static async Task<IResult> SaveAttendanceDeviceAsync(
        [FromQuery] bool isNew,
        [FromBody] UpsertAttendanceDeviceRequest? request,
        [FromServices] IAttendanceDeviceService attendanceDeviceService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload máy chấm công." });
        }

        try
        {
            var result = await attendanceDeviceService.SaveAsync(request, isNew, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteAttendanceDevicesAsync(
        [FromBody] IReadOnlyCollection<Guid>? ids,
        [FromServices] IAttendanceDeviceService attendanceDeviceService,
        CancellationToken cancellationToken)
    {
        if(ids is null)
        {
            return Results.BadRequest(new { message = "Thiếu danh sách thiết bị cần xóa." });
        }

        await attendanceDeviceService.DeleteAsync(ids, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetRecentAttendanceLogsAsync(
        [FromQuery] int take,
        [FromServices] IAttendanceLogReadService attendanceLogReadService,
        CancellationToken cancellationToken)
    {
        var result = await attendanceLogReadService.GetRecentAsync(take == 0 ? 500 : take, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> SearchAttendanceLogsAsync(
        [FromBody] AttendanceLogFilter? filter,
        [FromServices] IAttendanceLogReadService attendanceLogReadService,
        CancellationToken cancellationToken)
    {
        var result = await attendanceLogReadService.SearchAsync(
            filter ?? new AttendanceLogFilter(null, null, null),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> SearchAttendanceBiometricDataAsync(
        [FromBody] AttendanceBiometricDataFilter? filter,
        [FromServices] IAttendanceBiometricDataReadService attendanceBiometricDataReadService,
        CancellationToken cancellationToken)
    {
        var result = await attendanceBiometricDataReadService.SearchAsync(
            filter ?? new AttendanceBiometricDataFilter(null, null, null),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> CreateAttendanceBiometricPushCommandsAsync(
        [FromBody] AttendanceBiometricDeviceCommandBatchRequest? request,
        [FromServices] IAttendanceBiometricDeviceQueueService attendanceBiometricDeviceQueueService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload tạo lệnh cập nhật sinh trắc học." });
        }

        try
        {
            var result = await attendanceBiometricDeviceQueueService.CreatePushCommandsAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> CreateAttendanceBiometricDeleteCommandsAsync(
        [FromBody] AttendanceBiometricDeviceCommandBatchRequest? request,
        [FromServices] IAttendanceBiometricDeviceQueueService attendanceBiometricDeviceQueueService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload tạo lệnh xóa sinh trắc học." });
        }

        try
        {
            var result = await attendanceBiometricDeviceQueueService.CreateDeleteCommandsAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RefreshAttendanceBiometricDataAsync(
        [FromServices] IAttendanceBiometricDataRefreshService attendanceBiometricDataRefreshService,
        CancellationToken cancellationToken)
    {
        var result = await attendanceBiometricDataRefreshService.RefreshAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAttendanceBiometricDataRefreshProgressAsync(
        [FromServices] IAttendanceBiometricDataRefreshService attendanceBiometricDataRefreshService,
        CancellationToken cancellationToken)
    {
        var result = await attendanceBiometricDataRefreshService.GetProgressAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> RefreshAttendanceBiometricDataByEmployeeAsync(
        [FromRoute] Guid employeeId,
        [FromServices] IAttendanceBiometricDataRefreshService attendanceBiometricDataRefreshService,
        CancellationToken cancellationToken)
    {
        var result = await attendanceBiometricDataRefreshService.RefreshAsync(employeeId, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> SearchAttendanceLogDailySummariesAsync(
        [FromBody] AttendanceDailySummaryFilter? filter,
        [FromServices] IAttendanceDailySummaryReadService attendanceDailySummaryReadService,
        CancellationToken cancellationToken)
    {
        var result = await attendanceDailySummaryReadService.SearchAsync(
            filter ?? new AttendanceDailySummaryFilter(null, null, null),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetAttendanceLogsByDateRangeAsync(
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        [FromQuery] int take,
        [FromServices] IAttendanceLogReadService attendanceLogReadService,
        CancellationToken cancellationToken)
    {
        if (!TryParseDateOnly(fromDate, out var parsedFromDate)
            || !TryParseDateOnly(toDate, out var parsedToDate))
        {
            return Results.BadRequest(new { message = "Khoảng ngày không hợp lệ. Dùng định dạng yyyy-MM-dd." });
        }

        var result = await attendanceLogReadService.GetByDateRangeAsync(
            parsedFromDate,
            parsedToDate,
            take == 0 ? 2000 : take,
            cancellationToken);

        return Results.Ok(result);
    }

    private static bool TryParseDateOnly(string? value, out DateOnly date)
    {
        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out date);
    }

    private static async Task<IResult> RebuildAttendanceDailySummaryAsync(
        [FromBody] RebuildAttendanceDailySummaryRequest? request,
        [FromServices] IAttendanceDailySummaryService attendanceDailySummaryService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu khoảng ngày cần tổng hợp chấm công." });
        }

        var result = await attendanceDailySummaryService.RebuildAsync(request, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> SearchAttendanceWorkdaySummariesAsync(
        [FromBody] AttendanceWorkdaySummaryFilter? filter,
        [FromServices] IAttendanceWorkdaySummaryReadService attendanceWorkdaySummaryReadService,
        CancellationToken cancellationToken)
    {
        var result = await attendanceWorkdaySummaryReadService.SearchAsync(
            filter ?? new AttendanceWorkdaySummaryFilter(null, null, null),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetAttendanceTimesheetDashboardAsync(
        [FromBody] AttendanceTimesheetDashboardFilter? filter,
        [FromServices] IAttendanceTimesheetDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        if(filter is null)
        {
            return Results.BadRequest(new { message = "Thiếu điều kiện tải tổng quan bảng công." });
        }

        try
        {
            return Results.Ok(await dashboardService.GetDashboardAsync(filter, cancellationToken));
        }
        catch(InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> RebuildAttendanceWorkdaySummaryAsync(
        [FromBody] RebuildAttendanceWorkdaySummaryRequest? request,
        [FromServices] IAttendanceWorkdaySummaryService attendanceWorkdaySummaryService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu khoảng ngày cần tổng hợp bảng công ngày." });
        }

        try
        {
            var result = await attendanceWorkdaySummaryService.RebuildAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteAttendanceWorkdaySummariesAsync(
        [FromBody] IReadOnlyList<Guid>? ids,
        [FromServices] IAttendanceWorkdaySummaryService attendanceWorkdaySummaryService,
        CancellationToken cancellationToken)
    {
        if(ids is null || ids.Count == 0)
        {
            return Results.BadRequest(new { message = "Thiếu danh sách dòng bảng công ngày cần xóa." });
        }

        try
        {
            await attendanceWorkdaySummaryService.DeleteAsync(ids, cancellationToken);
            return Results.NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateAttendanceWorkdaySummaryAsync(
        [FromBody] UpdateAttendanceWorkdaySummaryRequest? request,
        [FromServices] IAttendanceWorkdaySummaryService attendanceWorkdaySummaryService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu thông tin dòng bảng công ngày cần cập nhật." });
        }

        try
        {
            var result = await attendanceWorkdaySummaryService.UpdateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetAttendanceWorkdaySummaryLockStateAsync(
        [FromBody] SetAttendanceWorkdaySummaryLockStateRequest? request,
        [FromServices] IAttendanceWorkdaySummaryService attendanceWorkdaySummaryService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu thông tin khóa hoặc mở khóa bảng công ngày." });
        }

        try
        {
            await attendanceWorkdaySummaryService.SetLockStateAsync(request, cancellationToken);
            return Results.NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SearchOvertimeRegistrationsAsync(
        [FromBody] OvertimeRegistrationFilter? filter,
        [FromServices] IOvertimeRegistrationService overtimeRegistrationService,
        CancellationToken cancellationToken)
    {
        var result = await overtimeRegistrationService.SearchAsync(
            filter ?? new OvertimeRegistrationFilter(null, null, null, null),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> CreateOvertimeRegistrationDraftAsync(
        [FromBody] CreateOvertimeRegistrationDraftRequest? request,
        HttpContext httpContext,
        [FromServices] IOvertimeRegistrationService overtimeRegistrationService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khởi tạo phiếu đăng ký tăng ca." });
        }

        try
        {
            var result = await overtimeRegistrationService.CreateDraftAsync(
                request,
                ResolveOvertimeRegistrationActorContext(httpContext.User),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SaveOvertimeRegistrationAsync(
        [FromQuery] bool submitAfterSave,
        [FromBody] UpsertOvertimeRegistrationRequest? request,
        HttpContext httpContext,
        [FromServices] IOvertimeRegistrationService overtimeRegistrationService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload phiếu đăng ký tăng ca." });
        }

        try
        {
            var result = await overtimeRegistrationService.SaveAsync(
                request,
                submitAfterSave,
                ResolveOvertimeRegistrationActorContext(httpContext.User),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ChangeOvertimeRegistrationStatusAsync(
        [FromBody] ChangeOvertimeRegistrationStatusRequest? request,
        HttpContext httpContext,
        [FromServices] IOvertimeRegistrationService overtimeRegistrationService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload cập nhật trạng thái phiếu đăng ký tăng ca." });
        }

        try
        {
            await overtimeRegistrationService.ChangeStatusAsync(
                request,
                ResolveOvertimeRegistrationActorContext(httpContext.User),
                cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SearchEmployeesAsync(
        [FromBody] EmployeeFilter? filter,
        [FromServices] IEmployeeService employeeService,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.SearchAsync(
            filter ?? new EmployeeFilter(null),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEmployeeSummaryAsync(
        [FromBody] EmployeeFilter? filter,
        [FromServices] IEmployeeService employeeService,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.GetSummaryAsync(
            filter ?? new EmployeeFilter(null),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateEmployeeAsync(
        [FromBody] CreateEmployeeRequest? request,
        [FromServices] IEmployeeService employeeService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload nhân viên." });
        }

        try
        {
            var result = await employeeService.CreateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateEmployeeAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateEmployeeRequest? request,
        [FromServices] IEmployeeService employeeService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload điều chỉnh nhân viên." });
        }

        if (id == Guid.Empty || id != request.Id)
        {
            return Results.BadRequest(new { message = "Mã định danh nhân viên không khớp." });
        }

        try
        {
            var result = await employeeService.UpdateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteEmployeesAsync(
        [FromBody] IReadOnlyCollection<Guid>? ids,
        [FromServices] IEmployeeService employeeService,
        CancellationToken cancellationToken)
    {
        if (ids is null)
        {
            return Results.BadRequest(new { message = "Thiếu danh sách nhân viên cần xóa." });
        }

        await employeeService.DeleteAsync(ids, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> RefreshEmployeesAsync(
        [FromServices] IEmployeeRefreshService employeeRefreshService,
        CancellationToken cancellationToken)
    {
        var result = await employeeRefreshService.RefreshFromDeviceUserProfilesAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static OvertimeRegistrationActorContext ResolveOvertimeRegistrationActorContext(
        ClaimsPrincipal user)
    {
        Guid? employeeId = null;
        var employeeIdValue = user.FindFirst(InternalAccountClaimTypes.EmployeeId)?.Value;
        if (Guid.TryParse(employeeIdValue, out var parsedEmployeeId))
        {
            employeeId = parsedEmployeeId;
        }

        return new OvertimeRegistrationActorContext(
            ResolveAuditActor(user),
            employeeId,
            InternalAccountRoles.AttendanceAdministrationRoles.Any(user.IsInRole));
    }

    private static string ResolveAuditActor(ClaimsPrincipal user) =>
        NormalizeAuditActor(user.FindFirst(ClaimTypes.Email)?.Value)
        ?? NormalizeAuditActor(user.FindFirst(ClaimTypes.Name)?.Value)
        ?? NormalizeAuditActor(user.FindFirst(ClaimTypes.NameIdentifier)?.Value)
        ?? "authenticated-user";

    private static string? NormalizeAuditActor(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<IResult> GetDeviceCommandLookupOptionsAsync(
        [FromServices] IAdmsDeviceCommandService deviceCommandService,
        CancellationToken cancellationToken)
    {
        var result = await deviceCommandService.GetLookupOptionsAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> SearchDeviceCommandsAsync(
        [FromBody] AdmsDeviceCommandFilter? filter,
        [FromServices] IAdmsDeviceCommandService deviceCommandService,
        CancellationToken cancellationToken)
    {
        var result = await deviceCommandService.SearchAsync(
            filter ?? new AdmsDeviceCommandFilter(null, null, null, null, null),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDeviceCommandDetailAsync(
        [FromRoute] int id,
        [FromServices] IAdmsDeviceCommandService deviceCommandService,
        CancellationToken cancellationToken)
    {
        var result = await deviceCommandService.GetDetailAsync(id, cancellationToken);
        return result is null
            ? Results.NotFound(new { message = "Không tìm thấy lệnh thiết bị." })
            : Results.Ok(result);
    }

    private static async Task<IResult> GetLatestDeviceInfoResponseAsync(
        [FromQuery] string? serialNumber,
        [FromServices] IAdmsDeviceCommandService deviceCommandService,
        CancellationToken cancellationToken)
    {
        if(string.IsNullOrWhiteSpace(serialNumber))
        {
            return Results.BadRequest(new { message = "Thiếu số serial thiết bị." });
        }

        var result = await deviceCommandService.GetLatestInfoResponseAsync(serialNumber, cancellationToken);
        return result is null
            ? Results.NotFound(new { message = "Chưa có phản hồi INFO cho thiết bị." })
            : Results.Ok(result);
    }

    private static async Task<IResult> CreateDeviceCommandAsync(
        [FromBody] UpsertAdmsDeviceCommandRequest? request,
        [FromServices] IAdmsDeviceCommandService deviceCommandService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload lệnh thiết bị." });
        }

        try
        {
            var result = await deviceCommandService.CreateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateDeviceCommandAsync(
        [FromRoute] int id,
        [FromBody] UpsertAdmsDeviceCommandRequest? request,
        [FromServices] IAdmsDeviceCommandService deviceCommandService,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload lệnh thiết bị." });
        }

        try
        {
            var result = await deviceCommandService.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteDeviceCommandAsync(
        [FromRoute] int id,
        [FromServices] IAdmsDeviceCommandService deviceCommandService,
        CancellationToken cancellationToken)
    {
        try
        {
            await deviceCommandService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteAllDeviceCommandsAsync(
        [FromServices] IAdmsDeviceCommandService deviceCommandService,
        CancellationToken cancellationToken)
    {
        await deviceCommandService.DeleteAllAsync(cancellationToken);
        return Results.NoContent();
    }
}
