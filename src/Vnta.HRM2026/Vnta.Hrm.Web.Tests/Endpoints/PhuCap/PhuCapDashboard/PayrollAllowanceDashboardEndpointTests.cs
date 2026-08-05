using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class PayrollAllowanceDashboardEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PayrollAllowanceDashboardEndpointTests(WebApplicationFactory<Program> factory)
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
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsJsonAsync(
            "/api/payroll/allowance-summary/dashboard",
            new PayrollAllowanceDashboardFilter(7, 2026));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_uses_dashboard_contract_for_selected_period()
    {
        var service = new CapturingDashboardService();
        using var client = CreateClient(service);
        var filter = new PayrollAllowanceDashboardFilter(7, 2026);

        var response = await client.PostAsJsonAsync(
            "/api/payroll/allowance-summary/dashboard",
            filter);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(filter, service.Filter);
        var result = await response.Content.ReadFromJsonAsync<PayrollAllowanceDashboardDto>();
        Assert.NotNull(result);
        Assert.Equal(7, result.PayrollMonth);
        Assert.Equal(2026, result.PayrollYear);
        Assert.Equal(2_000_000m, result.Overview.TotalAllowanceAmount);
    }

    [Fact]
    public async Task Dashboard_rejects_null_json_body_with_bad_request()
    {
        var service = new CapturingDashboardService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/allowance-summary/dashboard",
            JsonContent.Create<PayrollAllowanceDashboardFilter?>(null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.Filter);
    }

    [Fact]
    public async Task Breakdown_uses_its_narrow_dashboard_query_contract()
    {
        var service = new CapturingDashboardService();
        using var client = CreateClient(service);
        var filter = new PayrollAllowanceDashboardFilter(7, 2026);

        var response = await client.PostAsJsonAsync(
            "/api/payroll/allowance-summary/dashboard/breakdown",
            filter);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(filter, service.BreakdownFilter);
    }

    [Fact]
    public async Task Trend_uses_its_narrow_dashboard_query_contract()
    {
        var service = new CapturingDashboardService();
        using var client = CreateClient(service);
        var filter = new PayrollAllowanceDashboardFilter(7, 2026);

        var response = await client.PostAsJsonAsync(
            "/api/payroll/allowance-summary/dashboard/trend",
            filter);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(filter, service.TrendFilter);
    }

    [Fact]
    public async Task Monthly_comparison_uses_its_narrow_dashboard_query_contract()
    {
        var service = new CapturingDashboardService();
        using var client = CreateClient(service);
        var filter = new PayrollAllowanceDashboardFilter(7, 2026);

        var response = await client.PostAsJsonAsync(
            "/api/payroll/allowance-summary/dashboard/monthly-comparison",
            filter);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(filter, service.MonthlyComparisonFilter);
    }

    [Fact]
    public async Task Department_monthly_comparison_uses_its_narrow_dashboard_query_contract()
    {
        var service = new CapturingDashboardService();
        using var client = CreateClient(service);
        var filter = new PayrollAllowanceDashboardFilter(7, 2026);

        var response = await client.PostAsJsonAsync(
            "/api/payroll/allowance-summary/dashboard/department-monthly-comparison",
            filter);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(filter, service.DepartmentComparisonFilter);
    }

    private HttpClient CreateClient(CapturingDashboardService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPayrollAllowanceDashboardReadService>();
                services.RemoveAll<IPayrollAllowanceDashboardBreakdownQueryService>();
                services.RemoveAll<IPayrollAllowanceDashboardTrendQueryService>();
                services.RemoveAll<IPayrollAllowanceDashboardMonthlyComparisonQueryService>();
                services.RemoveAll<IPayrollAllowanceDashboardDepartmentComparisonQueryService>();
                services.AddSingleton<IPayrollAllowanceDashboardReadService>(service);
                services.AddSingleton<IPayrollAllowanceDashboardBreakdownQueryService>(service);
                services.AddSingleton<IPayrollAllowanceDashboardTrendQueryService>(service);
                services.AddSingleton<IPayrollAllowanceDashboardMonthlyComparisonQueryService>(service);
                services.AddSingleton<IPayrollAllowanceDashboardDepartmentComparisonQueryService>(service);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private sealed class CapturingDashboardService :
        IPayrollAllowanceDashboardReadService,
        IPayrollAllowanceDashboardBreakdownQueryService,
        IPayrollAllowanceDashboardTrendQueryService,
        IPayrollAllowanceDashboardMonthlyComparisonQueryService,
        IPayrollAllowanceDashboardDepartmentComparisonQueryService
    {
        public PayrollAllowanceDashboardFilter? Filter { get; private set; }
        public PayrollAllowanceDashboardFilter? BreakdownFilter { get; private set; }
        public PayrollAllowanceDashboardFilter? TrendFilter { get; private set; }
        public PayrollAllowanceDashboardFilter? MonthlyComparisonFilter { get; private set; }
        public PayrollAllowanceDashboardFilter? DepartmentComparisonFilter { get; private set; }

        public Task<PayrollAllowanceDashboardDto> GetDashboardAsync(
            PayrollAllowanceDashboardFilter filter,
            CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return Task.FromResult(new PayrollAllowanceDashboardDto(
                filter.PayrollMonth,
                filter.PayrollYear,
                new PayrollAllowanceDashboardOverviewDto(4, 3, 1, 2_000_000m),
                new PayrollAllowanceDashboardOverviewDto(3, 3, 0, 1_500_000m),
                [new PayrollAllowanceDashboardAllowanceBreakdownDto("Cơm", 500_000m)],
                [new PayrollAllowanceDashboardTrendPointDto(filter.PayrollMonth, filter.PayrollYear, 4, 2_000_000m)],
                [new PayrollAllowanceDashboardDepartmentDto("Khối sản xuất", 4, 2_000_000m)],
                [new PayrollAllowanceDashboardAllowanceComparisonDto(
                    "Cơm",
                    [new PayrollAllowanceDashboardAllowanceMonthDto(filter.PayrollMonth, 500_000m)])],
                [new PayrollAllowanceDashboardDepartmentTreeNodeDto(
                    "Khối sản xuất",
                    [new PayrollAllowanceDashboardAllowanceMonthDto(filter.PayrollMonth, 2_000_000m)])]));
        }

        public Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceBreakdownDto>> GetAllowanceBreakdownAsync(
            PayrollAllowanceDashboardFilter filter,
            CancellationToken cancellationToken = default)
        {
            BreakdownFilter = filter;
            return Task.FromResult<IReadOnlyList<PayrollAllowanceDashboardAllowanceBreakdownDto>>([]);
        }

        public Task<IReadOnlyList<PayrollAllowanceDashboardTrendPointDto>> GetTrendAsync(
            PayrollAllowanceDashboardFilter filter,
            CancellationToken cancellationToken = default)
        {
            TrendFilter = filter;
            return Task.FromResult<IReadOnlyList<PayrollAllowanceDashboardTrendPointDto>>([]);
        }

        public Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceComparisonDto>> GetAllowanceMonthlyComparisonAsync(
            PayrollAllowanceDashboardFilter filter,
            CancellationToken cancellationToken = default)
        {
            MonthlyComparisonFilter = filter;
            return Task.FromResult<IReadOnlyList<PayrollAllowanceDashboardAllowanceComparisonDto>>([]);
        }

        public Task<IReadOnlyList<PayrollAllowanceDashboardDepartmentTreeNodeDto>> GetDepartmentMonthlyComparisonAsync(
            PayrollAllowanceDashboardFilter filter,
            CancellationToken cancellationToken = default)
        {
            DepartmentComparisonFilter = filter;
            return Task.FromResult<IReadOnlyList<PayrollAllowanceDashboardDepartmentTreeNodeDto>>([]);
        }
    }
}
