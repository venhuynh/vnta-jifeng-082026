using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Queries;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapCom;

public sealed class MealAllowanceQueryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public MealAllowanceQueryEndpointTests(WebApplicationFactory<Program> factory)
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
                }).AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>(
                    TestHeaderAuthenticationHandler.SchemeName, _ => { });
            });
        });
    }

    [Theory]
    [InlineData("/api/payroll/meal-allowance/summary", true)]
    [InlineData("/api/payroll/meal-allowance/search", true)]
    [InlineData("/api/payroll/meal-allowance/search-page", true)]
    [InlineData("/api/payroll/meal-allowance/export-period/2026/7", false)]
    public async Task Meal_allowance_queries_require_payroll_administration_role(string path, bool isPost)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = isPost
            ? await client.PostAsJsonAsync(path, new MealAllowanceFilter(7, 2026, null))
            : await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_page_and_summary_forward_the_selected_filter_and_preserve_the_contract_result()
    {
        var service = new CapturingReadService();
        using var client = CreateClient(service);
        var filter = new MealAllowanceFilter(7, 2026, " NV-01 ", Take: 50, SummaryBucketKey: "manual", Skip: 50);

        var pageResponse = await client.PostAsJsonAsync("/api/payroll/meal-allowance/search-page", filter);
        var summaryResponse = await client.PostAsJsonAsync("/api/payroll/meal-allowance/summary", filter);

        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        Assert.Equal(filter, service.PageFilter);
        Assert.Equal(filter, service.SummaryFilter);
        var page = await pageResponse.Content.ReadFromJsonAsync<MealAllowancePageDto>();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<MealAllowanceSummaryDto>();
        Assert.NotNull(page);
        Assert.Equal(1, page.TotalCount);
        Assert.NotNull(summary);
        Assert.Equal(18_000m, summary.TotalAllowanceAmount);
    }

    [Fact]
    public async Task Export_uses_route_period_and_returns_bad_request_for_domain_validation_failure()
    {
        var service = new CapturingReadService();
        using var client = CreateClient(service);

        var success = await client.GetAsync("/api/payroll/meal-allowance/export-period/2026/7");
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        Assert.Equal((7, 2026), service.ExportPeriod);
        service.ThrowInvalidPeriod = true;
        var invalid = await client.GetAsync("/api/payroll/meal-allowance/export-period/2026/13");

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    private HttpClient CreateClient(CapturingReadService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMealAllowanceReadService>();
            services.RemoveAll<IMealAllowanceExportService>();
            services.AddSingleton<IMealAllowanceReadService>(service);
            services.AddSingleton<IMealAllowanceExportService>(service);
        }));
        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private sealed class CapturingReadService : IMealAllowanceReadService, IMealAllowanceExportService
    {
        public MealAllowanceFilter? PageFilter { get; private set; }
        public MealAllowanceFilter? SummaryFilter { get; private set; }
        public (int Month, int Year)? ExportPeriod { get; private set; }
        public bool ThrowInvalidPeriod { get; set; }

        public Task<IReadOnlyList<MealAllowanceListItemDto>> SearchAsync(MealAllowanceFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MealAllowanceListItemDto>>([Row]);

        public Task<MealAllowancePageDto> SearchPageAsync(MealAllowanceFilter filter, CancellationToken cancellationToken = default)
        {
            PageFilter = filter;
            return Task.FromResult(new MealAllowancePageDto([Row], 1));
        }

        public Task<MealAllowanceSummaryDto> GetSummaryAsync(MealAllowanceFilter filter, CancellationToken cancellationToken = default)
        {
            SummaryFilter = filter;
            return Task.FromResult(new MealAllowanceSummaryDto(1, 1, 0, 0, 0, 1, 0, 18_000m));
        }

        public Task<IReadOnlyList<MealAllowanceListItemDto>> ExportPeriodAsync(int payrollMonth, int payrollYear, CancellationToken cancellationToken = default)
        {
            ExportPeriod = (payrollMonth, payrollYear);
            if(ThrowInvalidPeriod)
                throw new InvalidOperationException("invalid period");
            return Task.FromResult<IReadOnlyList<MealAllowanceListItemDto>>([Row]);
        }

        private static MealAllowanceListItemDto Row => new(
            Guid.NewGuid(), Guid.NewGuid(), "NV-01", "Test Employee", null, null, 7, 2026,
            1, 1, 18_000m, 18_000m, "qualified-meal", "test", null, false,
            DateTime.UnixEpoch, DateTime.UnixEpoch, null);
    }
}
