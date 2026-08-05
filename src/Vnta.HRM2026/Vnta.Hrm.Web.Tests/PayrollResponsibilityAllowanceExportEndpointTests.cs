using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class PayrollResponsibilityAllowanceExportEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PayrollResponsibilityAllowanceExportEndpointTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Port=5432;Database=vnta_security_test;Username=test;Password=test");

        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestHeaderAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestHeaderAuthenticationHandler.SchemeName;
                        options.DefaultForbidScheme = TestHeaderAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>(
                        TestHeaderAuthenticationHandler.SchemeName,
                        _ => { });
            });
        });
    }

    [Fact]
    public async Task Export_uses_narrow_server_contract_and_returns_allowlisted_row()
    {
        var service = new CapturingExportService();
        using var client = CreateClient(service);
        var request = new PayrollResponsibilityAllowanceAbcExportRequest(2026, 7, "xlsx");

        var response = await client.PostAsJsonAsync(
            "/api/payroll/responsibility-allowance/export",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(request, service.Request);
        var rows = await response.Content.ReadFromJsonAsync<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>>();
        var row = Assert.Single(rows ?? []);
        Assert.Equal("NV001", row.EmployeeCode);
        Assert.Equal("Nhân viên kiểm thử", row.EmployeeName);
        Assert.True(row.IsLocked);
    }

    private HttpClient CreateClient(CapturingExportService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPayrollResponsibilityAllowanceMonthlyAbcExportService>();
                services.AddSingleton<IPayrollResponsibilityAllowanceMonthlyAbcExportService>(service);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private sealed class CapturingExportService : IPayrollResponsibilityAllowanceMonthlyAbcExportService
    {
        public PayrollResponsibilityAllowanceAbcExportRequest? Request { get; private set; }

        public Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>> ExportAsync(
            PayrollResponsibilityAllowanceAbcExportRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>>(
            [
                new PayrollResponsibilityAllowanceAbcExportItemDto(
                    "NV001", "Nhân viên kiểm thử", "Phòng kiểm thử", "Kiểm thử viên",
                    "TN01", "Trách nhiệm 01", 26, 26, "A", 500_000m, false,
                    1_000_000m, 1_500_000m, true, DateTime.UtcNow)
            ]);
        }
    }
}
