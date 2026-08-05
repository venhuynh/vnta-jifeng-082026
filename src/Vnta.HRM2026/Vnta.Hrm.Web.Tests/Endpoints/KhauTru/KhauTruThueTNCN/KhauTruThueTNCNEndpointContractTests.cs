using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.KhauTru.KhauTruThueTNCN;

public sealed class KhauTruThueTNCNEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public KhauTruThueTNCNEndpointContractTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", "Host=localhost;Port=5432;Database=vnta_security_test;Username=test;Password=test");
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestHeaderAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestHeaderAuthenticationHandler.SchemeName;
                        options.DefaultForbidScheme = TestHeaderAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>(TestHeaderAuthenticationHandler.SchemeName, _ => { });
            });
        });
    }

    [Theory]
    [InlineData("/api/payroll/personal-income-tax-deductions/search")]
    [InlineData("/api/payroll/personal-income-tax-deductions/refresh")]
    [InlineData("/api/payroll/personal-income-tax-deductions/manual-value")]
    [InlineData("/api/payroll/personal-income-tax-deductions/lock-state/batch")]
    public async Task Personal_income_tax_endpoints_require_payroll_administration(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(path, JsonContent("{}"))).StatusCode);
    }

    [Theory]
    [InlineData("/api/payroll/personal-income-tax-deductions/refresh")]
    [InlineData("/api/payroll/personal-income-tax-deductions/manual-value")]
    [InlineData("/api/payroll/personal-income-tax-deductions/lock-state/batch")]
    public async Task Commands_reject_null_payload_with_400(string path)
    {
        using var client = CreatePayrollAdminClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(path, JsonContent("null"))).StatusCode);
    }

    private HttpClient CreatePayrollAdminClient()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");
}
