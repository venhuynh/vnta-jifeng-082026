using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpAttendanceBiometricDataReadService(NavigationManager navigationManager)
    : IAttendanceBiometricDataReadService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task<IReadOnlyList<AttendanceBiometricDataListItemDto>> SearchAsync(
        AttendanceBiometricDataFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/attendance/biometric-data/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<AttendanceBiometricDataListItemDto>>(cancellationToken);
    }
}
