using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceWorkCalendarService(NavigationManager navigationManager)
    : IAttendanceWorkCalendarService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<AttendanceWorkCalendarYearDto> GetYearAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"api/attendance/work-calendar?year={year}",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceWorkCalendarYearDto>(cancellationToken);
    }

    public async Task<AttendanceWorkCalendarYearDto> EnsureSundayDayOffsAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"api/attendance/work-calendar/sundays/day-off?year={year}",
            content: null,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceWorkCalendarYearDto>(cancellationToken);
    }

    public async Task<string?> ValidateAsync(
        UpsertAttendanceWorkCalendarDayRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/work-calendar/validate",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<string?>(cancellationToken);
    }

    public async Task<AttendanceWorkCalendarDayDto> SaveAsync(
        UpsertAttendanceWorkCalendarDayRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/attendance/work-calendar?isNew={isNew}",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceWorkCalendarDayDto>(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync(
            $"api/attendance/work-calendar/{id}",
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }
}
