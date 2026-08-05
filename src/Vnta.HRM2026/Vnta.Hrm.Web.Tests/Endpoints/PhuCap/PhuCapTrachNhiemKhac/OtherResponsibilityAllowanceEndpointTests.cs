using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Queries;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapTrachNhiemKhac;

public sealed class OtherResponsibilityAllowanceEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public OtherResponsibilityAllowanceEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Search_requires_payroll_administration_role()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsJsonAsync(
            "/api/payroll/other-responsibility-allowance/search",
            new OtherResponsibilityAllowanceFilter(6, 2026, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_forbids_authenticated_user_without_payroll_administration_role()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        var response = await client.PostAsJsonAsync(
            "/api/payroll/other-responsibility-allowance/search",
            new OtherResponsibilityAllowanceFilter(6, 2026, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Prepare_period_preserves_legacy_endpoint_and_server_actor()
    {
        var service = new CapturingOtherResponsibilityAllowancePeriodPreparationService();
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOtherResponsibilityAllowancePeriodPreparationService>();
                services.AddSingleton<IOtherResponsibilityAllowancePeriodPreparationService>(service);
            }));
        using var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");

        var response = await client.PostAsync(
            "/api/payroll/other-responsibility-allowance/prepare-period?year=2026&month=6",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal((2026, 6), (service.Year, service.Month));
        Assert.Equal("security-boundary-test-user", service.RequestedBy);
    }

    [Fact]
    public async Task Search_uses_june_2026_filter_from_view_request()
    {
        var service = new CapturingOtherResponsibilityAllowanceReadService();
        using var client = CreatePayrollAdminClient(service);
        var filter = new OtherResponsibilityAllowanceFilter(6, 2026, null);

        var response = await client.PostAsJsonAsync(
            "/api/payroll/other-responsibility-allowance/search",
            filter);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(filter, service.Filter);
        Assert.Empty(await response.Content.ReadFromJsonAsync<IReadOnlyList<OtherResponsibilityAllowanceListItemDto>>() ?? []);
    }

    [Fact]
    public async Task Search_delegates_invalid_period_to_the_feature_service()
    {
        var service = new CapturingOtherResponsibilityAllowanceReadService();
        using var client = CreatePayrollAdminClient(service);
        var filter = new OtherResponsibilityAllowanceFilter(5, 2026, null);

        var response = await client.PostAsJsonAsync(
            "/api/payroll/other-responsibility-allowance/search",
            filter);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(filter, service.Filter);
    }

    [Fact]
    public async Task Recalculate_uses_the_applied_payroll_period()
    {
        var recalculationService = new CapturingOtherResponsibilityAllowanceRecalculationService();
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOtherResponsibilityAllowanceRecalculationService>();
                services.AddSingleton<IOtherResponsibilityAllowanceRecalculationService>(recalculationService);
            }));
        using var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");

        var request = new RecalculateOtherResponsibilityAllowanceRequest(2026, 6);
        var response = await client.PostAsJsonAsync(
            "/api/payroll/other-responsibility-allowance/recalculate",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(request, recalculationService.Request);
        Assert.Equal("security-boundary-test-user", recalculationService.RequestedBy);
        Assert.Equal(
            new RecalculateOtherResponsibilityAllowanceResult(3, 2),
            await response.Content.ReadFromJsonAsync<RecalculateOtherResponsibilityAllowanceResult>());
    }

    [Fact]
    public async Task Recalculate_rejects_null_body_without_calling_service()
    {
        var recalculationService = new CapturingOtherResponsibilityAllowanceRecalculationService();
        using var client = CreatePayrollAdminClient(recalculationService);

        var response = await client.PostAsync(
            "/api/payroll/other-responsibility-allowance/recalculate",
            new StringContent("null", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(recalculationService.Request);
    }

    [Fact]
    public async Task Recalculate_delegates_invalid_period_to_the_feature_service()
    {
        var recalculationService = new CapturingOtherResponsibilityAllowanceRecalculationService();
        using var client = CreatePayrollAdminClient(recalculationService);
        var request = new RecalculateOtherResponsibilityAllowanceRequest(2026, 5);

        var response = await client.PostAsJsonAsync(
            "/api/payroll/other-responsibility-allowance/recalculate",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(request, recalculationService.Request);
    }

    [Fact]
    public async Task Recalculate_ignores_spoofed_actor_and_uses_authenticated_principal()
    {
        var recalculationService = new CapturingOtherResponsibilityAllowanceRecalculationService();
        using var client = CreatePayrollAdminClient(recalculationService);

        var response = await client.PostAsync(
            "/api/payroll/other-responsibility-allowance/recalculate",
            new StringContent(
                "{\"payrollYear\":2026,\"payrollMonth\":6,\"actor\":\"forged-actor\"}",
                Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", recalculationService.RequestedBy);
    }

    [Fact]
    public async Task Batch_lock_uses_server_actor_and_feature_lock_contract()
    {
        var lockService = new CapturingOtherResponsibilityAllowanceLockService();
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOtherResponsibilityAllowanceLockService>();
                services.AddSingleton<IOtherResponsibilityAllowanceLockService>(lockService);
            }));
        using var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        var id = Guid.NewGuid();
        var request = new SetOtherResponsibilityAllowanceBatchLockStateRequest(
            2026, 6, true, [id], [new OtherResponsibilityAllowanceLockStateConcurrencyToken(id, null)]);

        var response = await client.PostAsJsonAsync("/api/payroll/other-responsibility-allowance/lock-state/batch", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(lockService.Request);
        Assert.Equal(2026, lockService.Request.PayrollYear);
        Assert.Equal(6, lockService.Request.PayrollMonth);
        Assert.True(lockService.Request.IsLocked);
        Assert.Equal([id], lockService.Request.PayrollAllowanceSummaryRecordIds);
        Assert.Equal("security-boundary-test-user", lockService.RequestedBy);
    }

    [Fact]
    public async Task Batch_lock_maps_feature_concurrency_conflict_to_http_409()
    {
        var lockService = new CapturingOtherResponsibilityAllowanceLockService(
            new OtherResponsibilityAllowanceConcurrencyException("stale snapshot"));
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOtherResponsibilityAllowanceLockService>();
                services.AddSingleton<IOtherResponsibilityAllowanceLockService>(lockService);
            }));
        using var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        var id = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            "/api/payroll/other-responsibility-allowance/lock-state/batch",
            new SetOtherResponsibilityAllowanceBatchLockStateRequest(2026, 6, true, [id], [new OtherResponsibilityAllowanceLockStateConcurrencyToken(id, null)]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private HttpClient CreatePayrollAdminClient(CapturingOtherResponsibilityAllowanceReadService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOtherResponsibilityAllowanceReadService>();
                services.AddSingleton<IOtherResponsibilityAllowanceReadService>(service);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private HttpClient CreatePayrollAdminClient(CapturingOtherResponsibilityAllowanceRecalculationService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOtherResponsibilityAllowanceRecalculationService>();
                services.AddSingleton<IOtherResponsibilityAllowanceRecalculationService>(service);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private sealed class CapturingOtherResponsibilityAllowanceReadService : IOtherResponsibilityAllowanceReadService
    {
        public OtherResponsibilityAllowanceFilter? Filter { get; private set; }

        public Task<IReadOnlyList<OtherResponsibilityAllowanceListItemDto>> SearchAsync(
            OtherResponsibilityAllowanceFilter filter,
            CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return Task.FromResult<IReadOnlyList<OtherResponsibilityAllowanceListItemDto>>([]);
        }
    }

    private sealed class CapturingOtherResponsibilityAllowancePeriodPreparationService
        : IOtherResponsibilityAllowancePeriodPreparationService
    {
        public int Year { get; private set; }
        public int Month { get; private set; }
        public string? RequestedBy { get; private set; }

        public Task PreparePeriodAsync(int year, int month, string? requestedBy, CancellationToken cancellationToken = default)
        {
            Year = year;
            Month = month;
            RequestedBy = requestedBy;
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingOtherResponsibilityAllowanceLockService(Exception? exception = null)
        : IOtherResponsibilityAllowanceLockService
    {
        public SetOtherResponsibilityAllowanceBatchLockStateRequest? Request { get; private set; }
        public string? RequestedBy { get; private set; }

        public Task<SetOtherResponsibilityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
            SetOtherResponsibilityAllowanceBatchLockStateRequest request,
            string? requestedBy = null,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            RequestedBy = requestedBy;
            if(exception is not null) return Task.FromException<SetOtherResponsibilityAllowanceBatchLockStateResult>(exception);
            return Task.FromResult(new SetOtherResponsibilityAllowanceBatchLockStateResult(
                request.PayrollYear, request.PayrollMonth, request.PayrollAllowanceSummaryRecordIds?.Count ?? 0, 1));
        }
    }

    private sealed class CapturingOtherResponsibilityAllowanceRecalculationService
        : IOtherResponsibilityAllowanceRecalculationService
    {
        public RecalculateOtherResponsibilityAllowanceRequest? Request { get; private set; }
        public string? RequestedBy { get; private set; }

        public Task<RecalculateOtherResponsibilityAllowanceResult> RecalculateAsync(
            RecalculateOtherResponsibilityAllowanceRequest request,
            string? requestedBy = null,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            RequestedBy = requestedBy;
            return Task.FromResult(new RecalculateOtherResponsibilityAllowanceResult(3, 2));
        }
    }
}
