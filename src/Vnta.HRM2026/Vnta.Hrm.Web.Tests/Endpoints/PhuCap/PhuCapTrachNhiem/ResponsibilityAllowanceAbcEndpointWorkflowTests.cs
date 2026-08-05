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

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapTrachNhiem;

/// <summary>HTTP contracts for the ABC workflow, independent of a production database.</summary>
public sealed class ResponsibilityAllowanceAbcEndpointWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BasePath = "/api/payroll/responsibility-allowance";
    private readonly WebApplicationFactory<Program> factory;

    public ResponsibilityAllowanceAbcEndpointWorkflowTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Port=5432;Database=vnta_security_test;Username=test;Password=test");
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services => services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestHeaderAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestHeaderAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme = TestHeaderAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>(
                    TestHeaderAuthenticationHandler.SchemeName,
                    _ => { }));
        });
    }

    [Fact]
    public async Task Search_forwards_filter_and_server_paging_and_returns_unfiltered_period_summary()
    {
        var service = new CapturingAbcQueryService();
        var (customizedFactory, client) = CreatePayrollAdminClient(services =>
        {
            services.RemoveAll<IPayrollResponsibilityAllowanceMonthlyAbcQueryService>();
            services.AddSingleton<IPayrollResponsibilityAllowanceMonthlyAbcQueryService>(service);
        });
        using (customizedFactory)
        using (client)
        {
            var request = new PayrollResponsibilityAllowanceAbcQuery(2026, 7, "NV001", "locked", 50, 25);

            var response = await client.PostAsJsonAsync(BasePath + "/search", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(request, service.SearchRequest);
            var page = await response.Content.ReadFromJsonAsync<PayrollResponsibilityAllowanceAbcPageDto>();
            Assert.NotNull(page);
            Assert.Equal(101, page.TotalCount);
            Assert.Equal(4, page.Summary.LockedCount);
            Assert.Single(page.Rows);
        }
    }

    [Theory]
    [InlineData("xlsx")]
    [InlineData("pdf")]
    public async Task Export_forwards_requested_format_and_returns_allowlisted_abc_fields(string format)
    {
        var service = new CapturingAbcExportService();
        var (customizedFactory, client) = CreatePayrollAdminClient(services =>
        {
            services.RemoveAll<IPayrollResponsibilityAllowanceMonthlyAbcExportService>();
            services.AddSingleton<IPayrollResponsibilityAllowanceMonthlyAbcExportService>(service);
        });
        using (customizedFactory)
        using (client)
        {
            var request = new PayrollResponsibilityAllowanceAbcExportRequest(2026, 7, format);

            var response = await client.PostAsJsonAsync(BasePath + "/export", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(request, service.Request);
            var row = Assert.Single(await response.Content.ReadFromJsonAsync<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>>() ?? []);
            Assert.Equal("NV001", row.EmployeeCode);
            Assert.Equal(1_250_000m, row.ActualResponsibilityAllowanceAmount);
            Assert.True(row.IsLocked);
        }
    }

    [Fact]
    public async Task Refresh_and_recalculate_forward_the_same_employee_concurrency_request_to_their_separate_use_cases()
    {
        var refreshService = new CapturingAbcRefreshService();
        var recalculateService = new CapturingRecalculationService();
        var (customizedFactory, client) = CreatePayrollAdminClient(services =>
        {
            services.RemoveAll<IPayrollResponsibilityAllowanceMonthlyAbcRefreshService>();
            services.AddSingleton<IPayrollResponsibilityAllowanceMonthlyAbcRefreshService>(refreshService);
            services.RemoveAll<IPayrollResponsibilityAllowanceRecalculationService>();
            services.AddSingleton<IPayrollResponsibilityAllowanceRecalculationService>(recalculateService);
        });
        using (customizedFactory)
        using (client)
        {
            var request = new RefreshPayrollResponsibilityAllowanceAbcRequest(2026, 7, Guid.NewGuid(), DateTime.UtcNow);

            var refreshResponse = await client.PostAsJsonAsync(BasePath + "/refresh", request);
            var recalculateResponse = await client.PostAsJsonAsync(BasePath + "/recalculate", request);

            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, recalculateResponse.StatusCode);
            Assert.Equal(request, refreshService.RefreshRequest);
            Assert.Equal(request, recalculateService.Request);
        }
    }

    [Fact]
    public async Task Batch_lock_for_whole_period_forwards_null_targets_and_all_concurrency_tokens()
    {
        var service = new CapturingAbcLockService();
        var (customizedFactory, client) = CreatePayrollAdminClient(services =>
        {
            services.RemoveAll<IPayrollResponsibilityAllowanceMonthlyAbcLockService>();
            services.AddSingleton<IPayrollResponsibilityAllowanceMonthlyAbcLockService>(service);
        });
        using (customizedFactory)
        using (client)
        {
            var request = new SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest(
                2026,
                7,
                true,
                EmployeeIds: null,
                ConcurrencyTokens:
                [
                    new PayrollResponsibilityAllowanceAbcConcurrencyToken(Guid.NewGuid(), DateTime.UtcNow),
                    new PayrollResponsibilityAllowanceAbcConcurrencyToken(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1))
                ]);

            var response = await client.PostAsJsonAsync(BasePath + "/lock-state/batch", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(service.BatchRequest);
            Assert.Equal((2026, 7, true), (service.BatchRequest.Year, service.BatchRequest.Month, service.BatchRequest.IsLocked));
            Assert.Null(service.BatchRequest.EmployeeIds);
            Assert.Equal(request.ConcurrencyTokens, service.BatchRequest.ConcurrencyTokens);
        }
    }

    [Fact]
    public async Task Manual_adjustment_maps_stale_write_conflict_to_409()
    {
        var service = new CapturingAdjustmentService { ThrowConflict = true };
        var (customizedFactory, client) = CreatePayrollAdminClient(services =>
        {
            services.RemoveAll<IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService>();
            services.AddSingleton<IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService>(service);
        });
        using (customizedFactory)
        using (client)
        {
            var request = new SavePayrollResponsibilityAllowanceAdjustmentRequest(
                null, 2026, 7, Guid.NewGuid(), Guid.NewGuid(), true, "manual", 0.9m, false, DateTime.UtcNow);

            var response = await client.PostAsJsonAsync(BasePath + "/adjustments", request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(request, service.Request);
        }
    }

    private (WebApplicationFactory<Program> Factory, HttpClient Client) CreatePayrollAdminClient(Action<IServiceCollection> configureServices)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(configureServices));
        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return (customizedFactory, client);
    }

    private sealed class CapturingAbcQueryService : IPayrollResponsibilityAllowanceMonthlyAbcQueryService
    {
        public PayrollResponsibilityAllowanceAbcQuery? SearchRequest { get; private set; }

        public Task<PayrollResponsibilityAllowanceAbcPageDto> SearchAbcAsync(PayrollResponsibilityAllowanceAbcQuery query, CancellationToken cancellationToken = default)
        {
            SearchRequest = query;
            return Task.FromResult(new PayrollResponsibilityAllowanceAbcPageDto(
                [CreateAbcItem()], 101, new PayrollResponsibilityAllowanceAbcSummaryDto(101, 90, 40, 30, 20, 11, 97, 4)));
        }

        public Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto>> GetAbcAsync(PayrollResponsibilityAllowanceAbcFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto>>([CreateAbcItem()]);

        public Task<PayrollResponsibilityAllowanceUpdateContextDto> GetUpdateContextAsync(Guid employeeId, int year, int month, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class CapturingAbcExportService : IPayrollResponsibilityAllowanceMonthlyAbcExportService
    {
        public PayrollResponsibilityAllowanceAbcExportRequest? Request { get; private set; }

        public Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>> ExportAsync(PayrollResponsibilityAllowanceAbcExportRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>>(
                [new("NV001", "Employee", "Department", "Position", "TN01", "Grade", 25m, 26m, "A", 1m, false, 1_250_000m, 1_250_000m, true, DateTime.UtcNow)]);
        }
    }

    private sealed class CapturingAbcRefreshService : IPayrollResponsibilityAllowanceMonthlyAbcRefreshService
    {
        public RefreshPayrollResponsibilityAllowanceAbcRequest? RefreshRequest { get; private set; }

        public Task<RefreshPayrollResponsibilityAllowanceAbcResult> RefreshAbcAsync(RefreshPayrollResponsibilityAllowanceAbcRequest request, CancellationToken cancellationToken = default)
        {
            RefreshRequest = request;
            return Task.FromResult(new RefreshPayrollResponsibilityAllowanceAbcResult(request.Year, request.Month, 1, 0, 1, 0));
        }

        public Task<CalculatePayrollResponsibilityAllowanceAbcResult> CalculateAbcAsync(RefreshPayrollResponsibilityAllowanceAbcRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CalculatePayrollResponsibilityAllowanceAbcResult(request.Year, request.Month, 1, 1, 0, 1, 0, 0, 0));
    }

    private sealed class CapturingRecalculationService : IPayrollResponsibilityAllowanceRecalculationService
    {
        public RefreshPayrollResponsibilityAllowanceAbcRequest? Request { get; private set; }

        public Task<RecalculatePayrollResponsibilityAllowanceAbcResult> RecalculateAbcAsync(RefreshPayrollResponsibilityAllowanceAbcRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new RecalculatePayrollResponsibilityAllowanceAbcResult(
                request.Year,
                request.Month,
                new RefreshPayrollResponsibilityAllowanceAbcResult(request.Year, request.Month, 1, 0, 1, 0),
                new CalculatePayrollResponsibilityAllowanceAbcResult(request.Year, request.Month, 1, 1, 0, 1, 0, 0, 0)));
        }
    }

    private sealed class CapturingAbcLockService : IPayrollResponsibilityAllowanceMonthlyAbcLockService
    {
        public SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest? BatchRequest { get; private set; }

        public Task<PayrollResponsibilityAllowanceAbcItemDto> SetLockStateAsync(Guid employeeId, int year, int month, bool isLocked, DateTime? originalUpdatedAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateAbcItem(isLocked));

        public Task<SetPayrollResponsibilityAllowanceAbcBatchLockStateResult> SetLockStateBatchAsync(SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest request, CancellationToken cancellationToken = default)
        {
            BatchRequest = request;
            return Task.FromResult(new SetPayrollResponsibilityAllowanceAbcBatchLockStateResult(request.Year, request.Month, 2, 2));
        }
    }

    private sealed class CapturingAdjustmentService : IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService
    {
        public bool ThrowConflict { get; init; }
        public SavePayrollResponsibilityAllowanceAdjustmentRequest? Request { get; private set; }

        public Task<PayrollResponsibilityAllowanceAbcItemDto> SaveAdjustmentAsync(SavePayrollResponsibilityAllowanceAdjustmentRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            if (ThrowConflict)
            {
                throw new ResponsibilityAllowanceConflictException("stale adjustment");
            }

            return Task.FromResult(CreateAbcItem());
        }
    }

    private static PayrollResponsibilityAllowanceAbcItemDto CreateAbcItem(bool isLocked = true) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "NV001", "Employee", "Department", Guid.NewGuid(), "Position", Guid.NewGuid(), "TN01", "Grade", 2026, 7, 25m, 26m, "A", 1m, false, 1_250_000m, 1_250_000m, isLocked, DateTime.UtcNow, "tester", DateTime.UtcNow, "tester", isLocked ? DateTime.UtcNow : null, isLocked ? "tester" : null, null, DateTime.UtcNow);
}
