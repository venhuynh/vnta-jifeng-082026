using Vnta.Hrm.Web.Client.Authorization;
using Vnta.Hrm.Web.Client.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Globalization;

public static class ClientProgram
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        var vietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = vietnameseCulture;
        CultureInfo.DefaultThreadCurrentUICulture = vietnameseCulture;

builder.Services.AddAppServices();
builder.Services.AddBrowserApiServices();
builder.Services.AddChatClient(builder.HostEnvironment.BaseAddress + "api/chat", "proxykey", "proxychat");
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(
        InternalAccountPolicies.EmployeeAccountAdministration,
        policy => policy.RequireAssertion(context =>
            InternalAccountCapabilityResolver.HasCapability(context.User, InternalAccountCapabilities.EmployeeAccountsOpen)));
    options.AddPolicy(
        InternalAccountPolicies.EmployeeAccountApproval,
        policy => policy.RequireAssertion(context =>
            InternalAccountCapabilityResolver.HasCapability(context.User, InternalAccountCapabilities.EmployeeAccountsApprove)));
    options.AddPolicy(
        InternalAccountPolicies.HumanResourcesAdministration,
        policy => policy.RequireRole(InternalAccountRoles.HumanResourcesRoles.ToArray()));
    options.AddPolicy(
        InternalAccountPolicies.ShiftManagement,
        policy => policy.RequireRole(InternalAccountRoles.ShiftManagementRoles.ToArray()));
    options.AddPolicy(
        InternalAccountPolicies.AttendanceAdministration,
        policy => policy.RequireRole(InternalAccountRoles.AttendanceAdministrationRoles.ToArray()));
    options.AddPolicy(
        InternalAccountPolicies.PayrollAdministration,
        policy => policy.RequireRole(InternalAccountRoles.PayrollAdministrationRoles.ToArray()));
    options.AddPolicy(
        InternalAccountPolicies.DeviceAdministration,
        policy => policy.RequireRole(InternalAccountRoles.DeviceAdministrationRoles.ToArray()));
    options.AddPolicy(
        InternalAccountPolicies.AuditRead,
        policy => policy.RequireAssertion(context =>
            InternalAccountCapabilityResolver.HasCapability(context.User, InternalAccountCapabilities.AuditRead)));
    options.AddPolicy(
        InternalAccountPolicies.AuditSensitiveRead,
        policy => policy.RequireAssertion(context =>
            InternalAccountCapabilityResolver.HasCapability(context.User, InternalAccountCapabilities.AuditSensitiveRead)));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

        await builder.Build().RunAsync();
    }
}

