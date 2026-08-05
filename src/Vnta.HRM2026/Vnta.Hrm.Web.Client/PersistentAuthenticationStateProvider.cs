using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Vnta.Hrm.Web.Client;

namespace Vnta.Hrm.Web.Client.Authorization {
    // This is a client-side AuthenticationStateProvider that determines the user's authentication state by
    // looking for data persisted in the page when it was rendered on the server. This authentication state will
    // be fixed for the lifetime of the WebAssembly application. So, if the user needs to log in or out, a full
    // page reload is required.
    //
    // This only provides a user name and email for display purposes. It does not actually include any tokens
    // that authenticate to the server when making subsequent requests. That works separately using a
    // cookie that will be included on HttpClient requests to the server.
    public class PersistentAuthenticationStateProvider : AuthenticationStateProvider {
        private static readonly Task<AuthenticationState> defaultUnauthenticatedTask =
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        private readonly Task<AuthenticationState> authenticationStateTask = defaultUnauthenticatedTask;

        public PersistentAuthenticationStateProvider(PersistentComponentState state) {
            if(!state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null) {
                return;
            }

            if(string.IsNullOrWhiteSpace(userInfo.UserId)) {
                return;
            }

            var claims = new List<Claim> {
                new(ClaimTypes.NameIdentifier, userInfo.UserId)
            };

            if(!string.IsNullOrWhiteSpace(userInfo.Name)) {
                claims.Add(new Claim(ClaimTypes.Name, userInfo.Name));
            }

            if(!string.IsNullOrWhiteSpace(userInfo.Email)) {
                claims.Add(new Claim(ClaimTypes.Email, userInfo.Email));
            }

            foreach(var role in ResolveRoles(userInfo)) {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            foreach(var permission in ResolvePermissions(userInfo)) {
                claims.Add(new Claim(InternalAccountClaimTypes.Permission, permission));
            }

            if(!string.IsNullOrWhiteSpace(userInfo.EmployeeId)) {
                claims.Add(new Claim(InternalAccountClaimTypes.EmployeeId, userInfo.EmployeeId));
            }

            if(!string.IsNullOrWhiteSpace(userInfo.AccessLevel)) {
                claims.Add(new Claim(InternalAccountClaimTypes.AccessLevel, userInfo.AccessLevel));
            }

            if(!string.IsNullOrWhiteSpace(userInfo.ApprovalStatus)) {
                claims.Add(new Claim(InternalAccountClaimTypes.ApprovalStatus, userInfo.ApprovalStatus));
            }

            claims.Add(new Claim(InternalAccountClaimTypes.IsActive, userInfo.IsActive.ToString()));

            authenticationStateTask = Task.FromResult(
                new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims,
                    authenticationType: nameof(PersistentAuthenticationStateProvider)))));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => authenticationStateTask;

        private static IReadOnlyList<string> ResolveRoles(UserInfo userInfo) {
            var roles = userInfo.Roles
                .Where(static role => !string.IsNullOrWhiteSpace(role))
                .Select(static role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if(roles.Length > 0) {
                return roles;
            }

            return string.IsNullOrWhiteSpace(userInfo.Role)
                ? []
                : [userInfo.Role.Trim()];
        }

        private static IReadOnlyList<string> ResolvePermissions(UserInfo userInfo)
            => userInfo.Permissions
                .Where(static permission => !string.IsNullOrWhiteSpace(permission))
                .Select(static permission => permission.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}

