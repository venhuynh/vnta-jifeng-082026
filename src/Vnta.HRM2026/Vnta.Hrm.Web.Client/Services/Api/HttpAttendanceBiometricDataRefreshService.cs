using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceBiometricDataRefreshService(NavigationManager navigationManager)
    : IAttendanceBiometricDataRefreshService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<AttendanceBiometricDataRefreshProgress> GetProgressAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            "api/attendance/biometric-data/refresh/progress",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceBiometricDataRefreshProgress>(cancellationToken);
    }

    public async Task<AttendanceBiometricDataRefreshResult> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            "api/attendance/biometric-data/refresh",
            content: null,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceBiometricDataRefreshResult>(cancellationToken);
    }

    public async Task<AttendanceBiometricDataRefreshResult> RefreshAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"api/attendance/biometric-data/refresh/{employeeId}",
            content: null,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceBiometricDataRefreshResult>(cancellationToken);
    }
}
