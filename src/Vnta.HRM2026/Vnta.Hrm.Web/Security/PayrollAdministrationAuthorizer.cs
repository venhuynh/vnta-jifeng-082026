using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Vnta.Hrm.Application.Common.Security;

namespace Vnta.Hrm.Web.Security;

/// <summary>Applies the payroll policy for Interactive Server command calls.</summary>
public sealed class PayrollAdministrationAuthorizer(
    AuthenticationStateProvider authenticationStateProvider,
    IAuthorizationService authorizationService) : IPayrollAdministrationAuthorizer
{
    public async Task DemandAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        var result = await authorizationService.AuthorizeAsync(
            state.User,
            resource: null,
            InternalAccountPolicies.PayrollAdministration);

        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền quản trị nghiệp vụ lương để thực hiện thao tác này.");
        }
    }
}
