using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class PayrollDeductionDashboardEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PayrollDeductionDashboardEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Dashboard_requires_payroll_administration_role()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync(
            "/api/payroll/deduction-summary/dashboard",
            new PayrollDeductionDashboardFilter(7, 2026));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_uses_dashboard_contract_for_selected_period()
    {
        var service = new CapturingDashboardService();
        using var client = CreateClient(service);
        var filter = new PayrollDeductionDashboardFilter(7, 2026);

        var response = await client.PostAsJsonAsync("/api/payroll/deduction-summary/dashboard", filter);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(filter, service.Filter);
        var result = await response.Content.ReadFromJsonAsync<PayrollDeductionDashboardDto>();
        Assert.NotNull(result);
        Assert.Equal(7, result.PayrollMonth);
        Assert.Equal(2_000_000m, result.Overview.TotalDeductionAmount);
        Assert.Equal("BHXH-YT", result.DeductionBreakdown.Single().DeductionType);
    }

    private HttpClient CreateClient(CapturingDashboardService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPayrollDeductionDashboardService>();
                services.AddSingleton<IPayrollDeductionDashboardService>(service);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private sealed class CapturingDashboardService : IPayrollDeductionDashboardService
    {
        public PayrollDeductionDashboardFilter? Filter { get; private set; }

        public Task<PayrollDeductionDashboardDto> GetDashboardAsync(
            PayrollDeductionDashboardFilter filter,
            CancellationToken cancellationToken = default)
        {
            Filter = filter;
            var overview = new PayrollDeductionDashboardOverviewDto(4, 3, 1, 2_000_000m);
            var months = new[] { new PayrollDeductionDashboardMonthDto(filter.PayrollMonth, 2_000_000m) };
            return Task.FromResult(new PayrollDeductionDashboardDto(
                filter.PayrollMonth,
                filter.PayrollYear,
                overview,
                new PayrollDeductionDashboardOverviewDto(3, 3, 0, 1_500_000m),
                [new PayrollDeductionDashboardDeductionBreakdownDto("BHXH-YT", 500_000m)],
                [new PayrollDeductionDashboardTrendPointDto(filter.PayrollMonth, filter.PayrollYear, 4, 2_000_000m)],
                [new PayrollDeductionDashboardDeductionComparisonDto("BHXH-YT", months)],
                [new PayrollDeductionDashboardDepartmentTreeNodeDto("Khối sản xuất", months)]));
        }
    }
}
