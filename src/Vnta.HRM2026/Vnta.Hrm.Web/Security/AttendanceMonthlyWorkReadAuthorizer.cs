using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Vnta.Hrm.Application.Common.Security;

namespace Vnta.Hrm.Web.Security;

/// <summary>Applies the attendance policy for Interactive Server monthly-work reads.</summary>
public sealed class AttendanceMonthlyWorkReadAuthorizer(
    AuthenticationStateProvider authenticationStateProvider,
    IAuthorizationService authorizationService) : IAttendanceMonthlyWorkReadAuthorizer
{
    public async Task DemandAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        var result = await authorizationService.AuthorizeAsync(
            state.User,
            resource: null,
            InternalAccountPolicies.AttendanceAdministration);

        if(!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem bảng công tháng.");
        }
    }
}
