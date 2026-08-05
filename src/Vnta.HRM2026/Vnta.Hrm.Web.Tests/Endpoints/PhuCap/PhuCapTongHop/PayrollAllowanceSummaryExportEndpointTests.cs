using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class PayrollAllowanceSummaryExportEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PayrollAllowanceSummaryExportEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Export_requires_payroll_administration_role()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsJsonAsync(
            "/api/payroll/allowance-summary/export",
            new PayrollAllowanceSummaryExportRequest(2026, 6, PayrollAllowanceSummaryExportFormat.Excel));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Export_uses_server_contract_for_applied_period_and_format()
    {
        var service = new CapturingAllowanceSummaryService();
        using var client = CreateClient(service);

        var response = await client.PostAsJsonAsync(
            "/api/payroll/allowance-summary/export",
            new PayrollAllowanceSummaryExportRequest(2026, 7, PayrollAllowanceSummaryExportFormat.Pdf));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new PayrollAllowanceSummaryExportRequest(2026, 7, PayrollAllowanceSummaryExportFormat.Pdf), service.ExportRequest);
        var rows = await response.Content.ReadFromJsonAsync<IReadOnlyList<PayrollAllowanceSummaryExportRowDto>>();
        var row = Assert.Single(rows ?? []);
        Assert.Equal("NV001", row.EmployeeCode);
    }

    private HttpClient CreateClient(CapturingAllowanceSummaryService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPayrollAllowanceSummaryExportService>();
                services.AddSingleton<IPayrollAllowanceSummaryExportService>(service);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private sealed class CapturingAllowanceSummaryService : IPayrollAllowanceSummaryExportService
    {
        public PayrollAllowanceSummaryExportRequest? ExportRequest { get; private set; }

        public Task<IReadOnlyList<PayrollAllowanceSummaryExportRowDto>> ExportAsync(
            PayrollAllowanceSummaryExportRequest request,
            CancellationToken cancellationToken = default)
        {
            ExportRequest = request;
            return Task.FromResult<IReadOnlyList<PayrollAllowanceSummaryExportRowDto>>(
            [
                new PayrollAllowanceSummaryExportRowDto(
                    "NV001", "Nhân viên kiểm thử", "Phòng kiểm thử", "Kiểm thử viên", 7, 2026,
                    1, 2, 3, 4, 5, 6, 7, 8, 36, false, null)
            ]);
        }

    }
}
