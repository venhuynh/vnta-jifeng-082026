using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapChuyenCan;

internal sealed class HttpAttendanceAllowanceResultService(NavigationManager navigationManager)
    : IAttendanceAllowanceReadService,
      IAttendanceAllowanceExportService,
      IAttendanceAllowanceRefreshService,
      IAttendanceAllowanceManualAdjustmentService,
      IAttendanceAllowanceWorkdayAdjustmentService,
      IAttendanceAllowanceLockService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<AttendanceAllowanceRuleDto> GetRuleAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            "api/payroll/attendance-allowance/rule",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceAllowanceRuleDto>(cancellationToken);
    }

    public async Task<AttendanceAllowanceResultPageDto> SearchPageAsync(
        AttendanceAllowanceResultFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/attendance-allowance/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceAllowanceResultPageDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceAllowanceExportRowDto>> ExportAsync(
        AttendanceAllowanceExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/attendance-allowance/export",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<AttendanceAllowanceExportRowDto>>(cancellationToken);
    }

    public async Task<RefreshAttendanceAllowanceResult> RefreshAsync(
        RefreshAttendanceAllowanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/attendance-allowance/refresh",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RefreshAttendanceAllowanceResult>(cancellationToken);
    }

    public async Task<AttendanceAllowanceResultListItemDto> UpdateActualWorkdayAsync(
        UpdateAttendanceAllowanceActualWorkdayRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/attendance-allowance/actual-workday",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceAllowanceResultListItemDto>(cancellationToken);
    }

    public async Task<AttendanceAllowanceResultListItemDto> UpdateStandardWorkdayAsync(
        UpdateAttendanceAllowanceStandardWorkdayRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/attendance-allowance/standard-workday",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceAllowanceResultListItemDto>(cancellationToken);
    }

    public async Task<AttendanceAllowanceResultListItemDto> UpdateWorkdaysAsync(
        UpdateAttendanceAllowanceWorkdaysRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/attendance-allowance/workdays",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceAllowanceResultListItemDto>(cancellationToken);
    }

    public async Task<AttendanceAllowanceResultListItemDto> SetLockStateAsync(
        SetAttendanceAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/attendance-allowance/lock-state",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceAllowanceResultListItemDto>(cancellationToken);
    }

    public async Task<SetAttendanceAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetAttendanceAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/attendance-allowance/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetAttendanceAllowanceBatchLockStateResult>(cancellationToken);
    }
}

/// <summary>
/// Feature composition boundary for the HTTP implementations of the focused
/// attendance-allowance application contracts.
/// </summary>
public static class AttendanceAllowanceResultApiServiceCollectionExtensions
{
    public static IServiceCollection AddAttendanceAllowanceResultApi(this IServiceCollection services)
    {
        services.AddScoped<HttpAttendanceAllowanceResultService>();
        services.AddScoped<IAttendanceAllowanceReadService>(sp =>
            sp.GetRequiredService<HttpAttendanceAllowanceResultService>());
        services.AddScoped<IAttendanceAllowanceExportService>(sp =>
            sp.GetRequiredService<HttpAttendanceAllowanceResultService>());
        services.AddScoped<IAttendanceAllowanceRefreshService>(sp =>
            sp.GetRequiredService<HttpAttendanceAllowanceResultService>());
        services.AddScoped<IAttendanceAllowanceManualAdjustmentService>(sp =>
            sp.GetRequiredService<HttpAttendanceAllowanceResultService>());
        services.AddScoped<IAttendanceAllowanceWorkdayAdjustmentService>(sp =>
            sp.GetRequiredService<HttpAttendanceAllowanceResultService>());
        services.AddScoped<IAttendanceAllowanceLockService>(sp =>
            sp.GetRequiredService<HttpAttendanceAllowanceResultService>());
        return services;
    }
}
