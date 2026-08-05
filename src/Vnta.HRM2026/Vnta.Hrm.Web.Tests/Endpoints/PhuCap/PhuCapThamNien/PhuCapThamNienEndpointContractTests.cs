using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Vnta.Hrm.Application.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapThamNien;

public sealed class PhuCapThamNienEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BasePath = "/api/payroll/seniority-allowance";
    private readonly WebApplicationFactory<Program> factory;

    public PhuCapThamNienEndpointContractTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Port=5432;Database=vnta_security_test;Username=test;Password=test");

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
                    .AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>(
                        TestHeaderAuthenticationHandler.SchemeName,
                        _ => { });
            });
        });
    }

    [Theory]
    [InlineData("GET", BasePath)]
    [InlineData("GET", BasePath + "/search-page")]
    [InlineData("GET", BasePath + "/range-summaries")]
    [InlineData("POST", BasePath + "/prepare-period")]
    [InlineData("POST", BasePath + "/refresh")]
    [InlineData("POST", BasePath + "/manual-values")]
    [InlineData("POST", BasePath + "/lock-state")]
    [InlineData("POST", BasePath + "/lock-state/batch")]
    public async Task Feature_routes_require_payroll_administration(string method, string path)
    {
        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var employee = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        employee.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        using var anonymousResponse = await SendAsync(anonymous, method, path);
        using var employeeResponse = await SendAsync(employee, method, path);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, employeeResponse.StatusCode);
    }

    [Fact]
    public async Task Search_page_forwards_filter_paging_and_returns_filtered_summary()
    {
        var service = new CapturingReadService
        {
            PageResult = new PayrollEmployeeSeniorityAllowancePageDto([], 17, 2_550_000m)
        };
        using var client = CreatePayrollAdminClient(services => ReplaceReadService(services, service));

        using var response = await client.GetAsync(
            BasePath + "/search-page?year=2026&month=7&departmentName=Nhan%20su&searchText=NV001&isLocked=true&take=25&skip=50&seniorityRangeKey=10-13");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PayrollEmployeeSeniorityAllowancePageDto>();
        Assert.NotNull(page);
        Assert.Equal(17, page.TotalCount);
        Assert.Equal(2_550_000m, page.TotalAllowanceAmount);
        Assert.Equal(new PayrollEmployeeSeniorityAllowanceFilter(
            7, 2026, "Nhan su", "NV001", true, 25, 50, "10-13"), service.PageFilter);
    }

    [Fact]
    public async Task Full_period_export_data_query_uses_requested_take_without_paging_offset()
    {
        var service = new CapturingReadService { SearchResult = [] };
        using var client = CreatePayrollAdminClient(services => ReplaceReadService(services, service));

        using var response = await client.GetAsync(BasePath + "?year=2026&month=7&take=5000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new PayrollEmployeeSeniorityAllowanceFilter(7, 2026, Take: 5000, Skip: 0), service.SearchFilter);
    }

    [Fact]
    public async Task Range_summaries_forward_the_same_filter_scope_as_the_grid()
    {
        var service = new CapturingRangeSummaryService
        {
            Result = [new PayrollEmployeeSeniorityAllowanceRangeSummaryDto("6-10", 8)]
        };
        using var client = CreatePayrollAdminClient(services => ReplaceRangeSummaryService(services, service));

        using var response = await client.GetAsync(
            BasePath + "/range-summaries?year=2026&month=7&departmentName=Nhan%20su&searchText=An&isLocked=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summaries = await response.Content.ReadFromJsonAsync<List<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>>();
        var summary = Assert.Single(summaries!);
        Assert.Equal("6-10", summary.RangeKey);
        Assert.Equal(8, summary.Count);
        Assert.Equal(new PayrollEmployeeSeniorityAllowanceFilter(7, 2026, "Nhan su", "An", false), service.Filter);
    }

    [Theory]
    [InlineData("/refresh")]
    [InlineData("/manual-values")]
    [InlineData("/lock-state")]
    [InlineData("/lock-state/batch")]
    public async Task Null_command_payload_is_rejected_before_command_service_is_called(string route)
    {
        var refresh = new CapturingRefreshService();
        var manual = new CapturingManualAdjustmentService();
        var locks = new CapturingLockService();
        using var client = CreatePayrollAdminClient(services =>
        {
            ReplaceRefreshService(services, refresh);
            ReplaceManualService(services, manual);
            ReplaceLockService(services, locks);
        });

        using var response = await client.PostAsync(BasePath + route, JsonContent("null"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(refresh.WasCalled);
        Assert.False(manual.WasCalled);
        Assert.False(locks.WasCalled);
    }

    [Fact]
    public async Task Refresh_domain_validation_failure_returns_http_400_after_payroll_administrator_invokes_service()
    {
        var refresh = new CapturingRefreshService { ThrowInvalidOperation = true };
        using var client = CreatePayrollAdminClient(services => ReplaceRefreshService(services, refresh));

        using var response = await client.PostAsync(
            BasePath + "/refresh",
            JsonContent("{\"payrollYear\":2026,\"payrollMonth\":13}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(refresh.WasCalled);
        Assert.Equal(new RefreshPayrollEmployeeSeniorityAllowanceRequest(2026, 13), refresh.Request);
    }

    [Theory]
    [InlineData("/manual-values", "{\"payrollAllowanceSummaryRecordId\":\"11111111-1111-1111-1111-111111111111\",\"allowanceAmount\":150000}")]
    [InlineData("/lock-state", "{\"payrollAllowanceSummaryRecordId\":\"11111111-1111-1111-1111-111111111111\",\"isLocked\":true}")]
    public async Task Row_commands_require_original_version_and_return_http_409(string route, string payload)
    {
        using var client = CreatePayrollAdminClient();

        using var response = await client.PostAsync(BasePath + route, JsonContent(payload));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("/manual-values")]
    [InlineData("/lock-state")]
    public async Task Stale_row_command_is_mapped_to_http_409(string route)
    {
        using var client = CreatePayrollAdminClient(services =>
        {
            ReplaceManualService(services, new CapturingManualAdjustmentService { ThrowConflict = true });
            ReplaceLockService(services, new CapturingLockService { ThrowConflict = true });
        });
        var payload = route == "/manual-values"
            ? "{\"payrollAllowanceSummaryRecordId\":\"11111111-1111-1111-1111-111111111111\",\"allowanceAmount\":150000,\"originalUpdatedAtUtc\":\"2026-07-22T00:00:00Z\"}"
            : "{\"payrollAllowanceSummaryRecordId\":\"11111111-1111-1111-1111-111111111111\",\"isLocked\":true,\"originalUpdatedAtUtc\":\"2026-07-22T00:00:00Z\"}";

        using var response = await client.PostAsync(BasePath + route, JsonContent(payload));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Manual_adjustment_uses_authenticated_actor_for_audit_even_when_payload_spoofs_actor()
    {
        var auditScope = new RecordingAuditScope();
        using var client = CreatePayrollAdminClient(services =>
        {
            ReplaceManualService(services, new CapturingManualAdjustmentService());
            services.RemoveAll<IAuditScope>();
            services.AddSingleton<IAuditScope>(auditScope);
        });

        using var response = await client.PostAsync(
            BasePath + "/manual-values",
            JsonContent("{\"payrollAllowanceSummaryRecordId\":\"11111111-1111-1111-1111-111111111111\",\"allowanceAmount\":150000,\"originalUpdatedAtUtc\":\"2026-07-22T00:00:00Z\",\"actorId\":\"forged\",\"requestedBy\":\"forged\"}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var command = Assert.Single(auditScope.Commands);
        Assert.Equal(AuditActions.SeniorityAllowance.ManualValueUpdated, command.ActionIntent);
        Assert.Equal("security-boundary-test-user", command.Actor.ActorId);
    }

    [Fact]
    public async Task Whole_period_lock_forwards_null_row_ids_and_returns_command_result()
    {
        var locks = new CapturingLockService
        {
            BatchResult = new SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult(2026, 7, 12, 12)
        };
        using var client = CreatePayrollAdminClient(services => ReplaceLockService(services, locks));

        using var response = await client.PostAsync(
            BasePath + "/lock-state/batch",
            JsonContent("{\"payrollYear\":2026,\"payrollMonth\":7,\"isLocked\":true,\"payrollAllowanceSummaryRecordIds\":null}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult>();
        Assert.NotNull(result);
        Assert.Equal(12, result.TargetRowCount);
        Assert.Equal(12, result.UpdatedCount);
        Assert.NotNull(locks.BatchRequest);
        Assert.Null(locks.BatchRequest!.PayrollAllowanceSummaryRecordIds);
    }

    [Fact]
    public async Task Plural_seniority_allowance_route_is_not_mapped()
    {
        using var client = CreatePayrollAdminClient();

        using var response = await client.PostAsync(
            "/api/payroll/seniority-allowances/manual-values",
            JsonContent("{}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient CreatePayrollAdminClient(Action<IServiceCollection>? configure = null)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => configure?.Invoke(services)));
        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string path) =>
        method == "GET"
            ? await client.GetAsync(path)
            : await client.PostAsync(path, JsonContent("{}"));

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    private static void ReplaceReadService(IServiceCollection services, IPayrollEmployeeSeniorityAllowanceReadService service)
    {
        services.RemoveAll<IPayrollEmployeeSeniorityAllowanceReadService>();
        services.AddSingleton(service);
    }

    private static void ReplaceRangeSummaryService(IServiceCollection services, IPayrollEmployeeSeniorityAllowanceRangeSummaryService service)
    {
        services.RemoveAll<IPayrollEmployeeSeniorityAllowanceRangeSummaryService>();
        services.AddSingleton(service);
    }

    private static void ReplaceRefreshService(IServiceCollection services, IPayrollEmployeeSeniorityAllowanceRefreshService service)
    {
        services.RemoveAll<IPayrollEmployeeSeniorityAllowanceRefreshService>();
        services.AddSingleton(service);
    }

    private static void ReplaceManualService(IServiceCollection services, IPayrollEmployeeSeniorityAllowanceManualAdjustmentService service)
    {
        services.RemoveAll<IPayrollEmployeeSeniorityAllowanceManualAdjustmentService>();
        services.AddSingleton(service);
    }

    private static void ReplaceLockService(IServiceCollection services, IPayrollEmployeeSeniorityAllowanceLockService service)
    {
        services.RemoveAll<IPayrollEmployeeSeniorityAllowanceLockService>();
        services.AddSingleton(service);
    }

    private sealed class CapturingReadService : IPayrollEmployeeSeniorityAllowanceReadService
    {
        public PayrollEmployeeSeniorityAllowanceFilter? SearchFilter { get; private set; }
        public PayrollEmployeeSeniorityAllowanceFilter? PageFilter { get; private set; }
        public IReadOnlyList<PayrollEmployeeSeniorityAllowanceListItemDto> SearchResult { get; init; } = [];
        public PayrollEmployeeSeniorityAllowancePageDto PageResult { get; init; } = new([], 0, 0m);

        public Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceListItemDto>> SearchAsync(
            PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken = default)
        {
            SearchFilter = filter;
            return Task.FromResult(SearchResult);
        }

        public Task<PayrollEmployeeSeniorityAllowancePageDto> SearchPageAsync(
            PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken = default)
        {
            PageFilter = filter;
            return Task.FromResult(PageResult);
        }
    }

    private sealed class CapturingRangeSummaryService : IPayrollEmployeeSeniorityAllowanceRangeSummaryService
    {
        public PayrollEmployeeSeniorityAllowanceFilter? Filter { get; private set; }
        public IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto> Result { get; init; } = [];

        public Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>> GetRangeSummariesAsync(
            PayrollEmployeeSeniorityAllowanceFilter filter, CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return Task.FromResult(Result);
        }
    }

    private sealed class CapturingRefreshService : IPayrollEmployeeSeniorityAllowanceRefreshService
    {
        public bool WasCalled { get; private set; }
        public bool ThrowInvalidOperation { get; init; }
        public RefreshPayrollEmployeeSeniorityAllowanceRequest? Request { get; private set; }

        public Task<RefreshPayrollEmployeeSeniorityAllowanceResult> RefreshAsync(
            RefreshPayrollEmployeeSeniorityAllowanceRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            Request = request;
            if (ThrowInvalidOperation)
            {
                throw new InvalidOperationException("Invalid payroll period.");
            }

            return Task.FromResult(new RefreshPayrollEmployeeSeniorityAllowanceResult(request.PayrollYear, request.PayrollMonth, 0, 0, 0));
        }
    }

    private sealed class CapturingManualAdjustmentService : IPayrollEmployeeSeniorityAllowanceManualAdjustmentService
    {
        public bool ThrowConflict { get; init; }
        public bool WasCalled { get; private set; }

        public Task<PayrollEmployeeSeniorityAllowanceListItemDto> UpdateManualValuesAsync(
            UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            if (ThrowConflict)
            {
                throw new PayrollEmployeeSeniorityAllowanceConflictException("stale version");
            }

            return Task.FromResult(CreateRow(request.PayrollAllowanceSummaryRecordId, request.AllowanceAmount, false));
        }
    }

    private sealed class CapturingLockService : IPayrollEmployeeSeniorityAllowanceLockService
    {
        public bool ThrowConflict { get; init; }
        public bool WasCalled { get; private set; }
        public SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest? BatchRequest { get; private set; }
        public SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult BatchResult { get; init; } = new(2026, 7, 0, 0);

        public Task<PayrollEmployeeSeniorityAllowanceListItemDto> SetLockStateAsync(
            SetPayrollEmployeeSeniorityAllowanceLockStateRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            if (ThrowConflict)
            {
                throw new PayrollEmployeeSeniorityAllowanceConflictException("stale version");
            }

            return Task.FromResult(CreateRow(request.PayrollAllowanceSummaryRecordId, 0m, request.IsLocked));
        }

        public Task<SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
            SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            BatchRequest = request;
            return Task.FromResult(BatchResult);
        }
    }

    private sealed class RecordingAuditScope : IAuditScope
    {
        public List<AuditCommand> Commands { get; } = [];
        public AuditCommand? Current { get; private set; }

        public IDisposable Begin(AuditCommand command)
        {
            Commands.Add(command);
            var previous = Current;
            Current = command;
            return new ScopeLease(this, previous);
        }

        public void RefineAction(string finalAction) { }
        public void SetOperationOutcome(AuditOperationOutcome outcome) { }

        private sealed class ScopeLease(RecordingAuditScope owner, AuditCommand? previous) : IDisposable
        {
            public void Dispose() => owner.Current = previous;
        }
    }

    private static PayrollEmployeeSeniorityAllowanceListItemDto CreateRow(
        Guid summaryRecordId, decimal allowanceAmount, bool isLocked) => new(
        Guid.NewGuid(), summaryRecordId, Guid.NewGuid(), "NV001", "Test user", null, null,
        7, 2026, null, null, null, null, null, null, null, allowanceAmount, null,
        isLocked, null, null, DateTime.UtcNow);
}
