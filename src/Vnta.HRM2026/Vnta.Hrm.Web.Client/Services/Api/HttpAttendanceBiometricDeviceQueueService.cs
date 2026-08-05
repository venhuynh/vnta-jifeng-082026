using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceBiometricDeviceQueueService(NavigationManager navigationManager)
    : IAttendanceBiometricDeviceQueueService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<AttendanceBiometricDeviceCommandBatchResult> CreatePushCommandsAsync(
        AttendanceBiometricDeviceCommandBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/biometric-data/device-commands/push",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceBiometricDeviceCommandBatchResult>(cancellationToken);
    }

    public async Task<AttendanceBiometricDeviceCommandBatchResult> CreateDeleteCommandsAsync(
        AttendanceBiometricDeviceCommandBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/biometric-data/device-commands/delete",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<AttendanceBiometricDeviceCommandBatchResult>(cancellationToken);
    }
}
