using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class OtherAllowanceEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly List<WebApplicationFactory<Program>> customizedFactories = [];

    public OtherAllowanceEndpointContractTests(WebApplicationFactory<Program> factory)
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
    [InlineData("/api/payroll/other-allowances/search")]
    [InlineData("/api/payroll/other-allowances")]
    [InlineData("/api/payroll/other-allowances/lock-state")]
    public async Task Other_allowance_endpoints_require_payroll_administration_role(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync(path, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Other_allowance_endpoints_forbid_authenticated_users_without_payroll_administration_role()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        var response = await client.PostAsync("/api/payroll/other-allowances/search", JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("PUT", "/api/payroll/other-allowances")]
    [InlineData("DELETE", "/api/payroll/other-allowances/11111111-1111-1111-1111-111111111111")]
    public async Task Update_and_delete_require_payroll_administration_role(string method, string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "PUT" ? JsonContent(ValidUpdateJson()) : null
        };
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_returns_bad_request_for_a_null_body()
    {
        using var client = CreateClient(new CapturingOtherAllowanceService());

        var response = await client.PostAsync("/api/payroll/other-allowances", JsonContent("null"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_maps_application_validation_failure_to_http_400()
    {
        using var client = CreateClient(new CapturingOtherAllowanceService { ThrowValidationOnSearch = true });

        var response = await client.PostAsync(
            "/api/payroll/other-allowances/search",
            JsonContent("{\"payrollMonth\":0,\"payrollYear\":2026}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_forwards_period_filter_paging_and_lock_state_to_the_read_service()
    {
        var service = new CapturingOtherAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/other-allowances/search",
            JsonContent("{\"payrollMonth\":7,\"payrollYear\":2026,\"searchText\":\"meal\",\"isLocked\":false,\"take\":25,\"skip\":50}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new OtherAllowanceFilter(7, 2026, "meal", false, 25, 50), service.SearchFilter);
    }

    [Fact]
    public async Task Search_with_null_filter_uses_the_current_payroll_period()
    {
        var service = new CapturingOtherAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync("/api/payroll/other-allowances/search", JsonContent("null"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new OtherAllowanceFilter(DateTime.Today.Month, DateTime.Today.Year), service.SearchFilter);
    }

    [Fact]
    public async Task Create_overwrites_client_supplied_actor_with_authenticated_principal()
    {
        var service = new CapturingOtherAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/other-allowances",
            JsonContent($"{{\"payrollAllowanceSummaryRecordId\":\"{Guid.NewGuid()}\",\"allowanceName\":\"Hỗ trợ\",\"isFixedAmount\":true,\"allowanceAmount\":1000,\"requestedBy\":\"forged\"}}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.CreateRequest?.RequestedBy);
    }

    [Fact]
    public async Task Update_maps_conflict_to_http_409()
    {
        using var client = CreateClient(new CapturingOtherAllowanceService { ThrowConflictOnUpdate = true });

        var response = await client.PutAsync(
            "/api/payroll/other-allowances",
            JsonContent($"{{\"id\":\"{Guid.NewGuid()}\",\"allowanceName\":\"Hỗ trợ\",\"isFixedAmount\":true,\"allowanceAmount\":1000}}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/payroll/other-allowances")]
    [InlineData("PUT", "/api/payroll/other-allowances")]
    [InlineData("POST", "/api/payroll/other-allowances/lock-state")]
    [InlineData("DELETE", "/api/payroll/other-allowances/11111111-1111-1111-1111-111111111111")]
    public async Task Commands_map_domain_concurrency_conflicts_to_http_409(string method, string path)
    {
        using var client = CreateClient(new CapturingOtherAllowanceService { ThrowConflictOnAnyCommand = true });

        var response = await SendValidCommandAsync(client, method, path);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Lock_state_maps_database_concurrency_conflict_to_http_409()
    {
        using var client = CreateClient(new CapturingOtherAllowanceService { ThrowDbConcurrencyOnAnyCommand = true });

        var response = await client.PostAsync(
            "/api/payroll/other-allowances/lock-state",
            JsonContent(ValidLockJson()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/payroll/other-allowances")]
    [InlineData("PUT", "/api/payroll/other-allowances")]
    [InlineData("POST", "/api/payroll/other-allowances/lock-state")]
    [InlineData("DELETE", "/api/payroll/other-allowances/11111111-1111-1111-1111-111111111111")]
    public async Task Commands_map_business_validation_failures_to_http_400(string method, string path)
    {
        using var client = CreateClient(new CapturingOtherAllowanceService { ThrowValidationOnAnyCommand = true });

        var response = await SendValidCommandAsync(client, method, path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_maps_a_missing_allowance_to_http_404()
    {
        using var client = CreateClient(new CapturingOtherAllowanceService { ThrowNotFoundOnAnyCommand = true });

        var response = await client.DeleteAsync("/api/payroll/other-allowances/11111111-1111-1111-1111-111111111111");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Lock_state_overwrites_client_supplied_actor_with_authenticated_principal()
    {
        var service = new CapturingOtherAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/other-allowances/lock-state",
            JsonContent($"{{\"id\":\"{Guid.NewGuid()}\",\"isLocked\":true,\"requestedBy\":\"forged\"}}"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.LockRequest?.RequestedBy);
    }

    [Fact]
    public async Task Update_overwrites_client_supplied_actor_with_authenticated_principal()
    {
        var service = new CapturingOtherAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PutAsync(
            "/api/payroll/other-allowances",
            JsonContent($"{{\"id\":\"{Guid.NewGuid()}\",\"allowanceName\":\"Há»— trá»£\",\"isFixedAmount\":true,\"allowanceAmount\":1000,\"requestedBy\":\"forged\"}}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.UpdateRequest?.RequestedBy);
    }

    [Fact]
    public async Task Previous_month_sync_overwrites_client_supplied_actor_with_authenticated_principal()
    {
        var service = new CapturingOtherAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/other-allowances/sync-previous-month",
            JsonContent("{\"targetPayrollMonth\":7,\"targetPayrollYear\":2026,\"requestedBy\":\"forged\"}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.SyncRequest?.RequestedBy);
    }

    [Fact]
    public async Task Delete_overwrites_client_supplied_actor_with_authenticated_principal()
    {
        var service = new CapturingOtherAllowanceService();
        using var client = CreateClient(service);
        var id = Guid.NewGuid();

        var response = await client.DeleteAsync(
            $"/api/payroll/other-allowances/{id}?originalUpdatedAtUtc={Uri.EscapeDataString(DateTime.UtcNow.ToString("O"))}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(id, service.DeleteRequest?.Id);
        Assert.Equal("security-boundary-test-user", service.DeleteRequest?.RequestedBy);
    }

    [Fact]
    public async Task Delete_records_self_approval_with_the_authenticated_principal()
    {
        var service = new CapturingOtherAllowanceService();
        var auditScope = new RecordingAuditScope();
        using var client = CreateClient(service, auditScope);

        var response = await client.DeleteAsync($"/api/payroll/other-allowances/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var command = Assert.Single(auditScope.Commands);
        Assert.Equal(AuditActions.OtherAllowance.Deleted, command.ActionIntent);
        Assert.Equal("security-boundary-test-user", command.Actor.ActorId);
        Assert.Equal(response.Headers.GetValues("X-Correlation-Id").Single(), command.CorrelationId);
        Assert.Equal("self", command.Metadata?["approval.mode"]);
        Assert.Equal("security-boundary-test-user", command.Metadata?["approval.approved_by"]);
    }

    private HttpClient CreateClient(CapturingOtherAllowanceService service, RecordingAuditScope? auditScope = null)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IOtherAllowanceReadService>();
            services.RemoveAll<IOtherAllowanceCreateService>();
            services.RemoveAll<IOtherAllowancePreviousMonthSyncService>();
            services.RemoveAll<IOtherAllowanceUpdateService>();
            services.RemoveAll<IOtherAllowanceLockService>();
            services.RemoveAll<IOtherAllowanceDeleteService>();
            if(auditScope is not null)
            {
                services.RemoveAll<IAuditScope>();
                services.AddSingleton<IAuditScope>(auditScope);
            }
            services.AddSingleton<IOtherAllowanceReadService>(service);
            services.AddSingleton<IOtherAllowanceCreateService>(service);
            services.AddSingleton<IOtherAllowancePreviousMonthSyncService>(service);
            services.AddSingleton<IOtherAllowanceUpdateService>(service);
            services.AddSingleton<IOtherAllowanceLockService>(service);
            services.AddSingleton<IOtherAllowanceDeleteService>(service);
        }));
        customizedFactories.Add(customizedFactory);
        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    public void Dispose()
    {
        foreach(var customizedFactory in customizedFactories)
        {
            customizedFactory.Dispose();
        }
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<HttpResponseMessage> SendValidCommandAsync(HttpClient client, string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method switch
            {
                "POST" when path.EndsWith("lock-state", StringComparison.Ordinal) => JsonContent(ValidLockJson()),
                "POST" => JsonContent(ValidCreateJson()),
                "PUT" => JsonContent(ValidUpdateJson()),
                _ => null
            }
        };
        return await client.SendAsync(request);
    }

    private static string ValidCreateJson() =>
        "{\"payrollAllowanceSummaryRecordId\":\"11111111-1111-1111-1111-111111111111\",\"allowanceName\":\"Meal support\",\"isFixedAmount\":true,\"allowanceAmount\":1000,\"requestedBy\":\"forged\"}";

    private static string ValidUpdateJson() =>
        "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"allowanceName\":\"Meal support\",\"isFixedAmount\":true,\"allowanceAmount\":1000,\"requestedBy\":\"forged\"}";

    private static string ValidLockJson() =>
        "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"isLocked\":true,\"requestedBy\":\"forged\"}";

    private sealed class CapturingOtherAllowanceService :
        IOtherAllowanceReadService,
        IOtherAllowanceCreateService,
        IOtherAllowancePreviousMonthSyncService,
        IOtherAllowanceUpdateService,
        IOtherAllowanceLockService,
        IOtherAllowanceDeleteService
    {
        public bool ThrowConflictOnUpdate { get; init; }
        public bool ThrowValidationOnSearch { get; init; }
        public bool ThrowConflictOnAnyCommand { get; init; }
        public bool ThrowDbConcurrencyOnAnyCommand { get; init; }
        public bool ThrowValidationOnAnyCommand { get; init; }
        public bool ThrowNotFoundOnAnyCommand { get; init; }
        public OtherAllowanceFilter? SearchFilter { get; private set; }
        public CreateOtherAllowanceRequest? CreateRequest { get; private set; }
        public SyncOtherAllowanceFromPreviousMonthRequest? SyncRequest { get; private set; }
        public UpdateOtherAllowanceRequest? UpdateRequest { get; private set; }
        public SetOtherAllowanceLockStateRequest? LockRequest { get; private set; }
        public DeleteOtherAllowanceRequest? DeleteRequest { get; private set; }

        public Task<OtherAllowancePageDto> SearchPageAsync(OtherAllowanceFilter filter, CancellationToken cancellationToken = default)
        {
            SearchFilter = filter;
            if (ThrowValidationOnSearch)
            {
                throw new InvalidOperationException("Kỳ lương không hợp lệ.");
            }

            return Task.FromResult(new OtherAllowancePageDto([], 0, 0m));
        }

        public Task<OtherAllowanceCommandResult> CreateAsync(CreateOtherAllowanceRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequest = request;
            ThrowCommandFailure();
            return Task.FromResult(CreateRow(request.PayrollAllowanceSummaryRecordId));
        }

        public Task<OtherAllowanceCommandResult> UpdateAsync(UpdateOtherAllowanceRequest request, CancellationToken cancellationToken = default)
        {
            UpdateRequest = request;
            if(ThrowConflictOnUpdate)
            {
                throw new OtherAllowanceConflictException("conflict");
            }

            ThrowCommandFailure();

            return Task.FromResult(CreateRow(Guid.NewGuid()));
        }

        public Task<SyncOtherAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(
            SyncOtherAllowanceFromPreviousMonthRequest request,
            CancellationToken cancellationToken = default)
        {
            SyncRequest = request;
            ThrowCommandFailure();
            return Task.FromResult(new SyncOtherAllowanceFromPreviousMonthResult(
                6, 2026, request.TargetPayrollMonth, request.TargetPayrollYear, 0, 0, 0, 0, 0, 0, 0));
        }

        public Task SetLockStateAsync(SetOtherAllowanceLockStateRequest request, CancellationToken cancellationToken = default)
        {
            LockRequest = request;
            ThrowCommandFailure();
            return Task.CompletedTask;
        }

        public Task<SetOtherAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetOtherAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default)
        {
            ThrowCommandFailure();
            return Task.FromResult(new SetOtherAllowanceBatchLockStateResult(0, 0));
        }

        public Task DeleteAsync(DeleteOtherAllowanceRequest request, CancellationToken cancellationToken = default)
        {
            DeleteRequest = request;
            ThrowCommandFailure();
            return Task.CompletedTask;
        }

        private void ThrowCommandFailure()
        {
            if (ThrowConflictOnAnyCommand)
            {
                throw new OtherAllowanceConflictException("conflict");
            }

            if (ThrowDbConcurrencyOnAnyCommand)
            {
                throw new DbUpdateConcurrencyException("conflict");
            }

            if (ThrowValidationOnAnyCommand)
            {
                throw new InvalidOperationException("invalid allowance");
            }

            if (ThrowNotFoundOnAnyCommand)
            {
                throw new KeyNotFoundException("not found");
            }
        }

        private static OtherAllowanceCommandResult CreateRow(Guid summaryId) => new(
            Guid.NewGuid(), summaryId, Guid.NewGuid(), "NV001", "Test user", null, null, 7, 2026,
            "Hỗ trợ", true, 1_000m, null, false, DateTime.UtcNow, "test", null, null);
    }

    private sealed class RecordingAuditScope : IAuditScope
    {
        public List<AuditCommand> Commands { get; } = [];

        public AuditCommand? Current { get; private set; }

        public IDisposable Begin(AuditCommand command)
        {
            var previous = Current;
            Current = command;
            Commands.Add(command);
            return new ScopeLease(this, previous);
        }

        public void RefineAction(string finalAction) { }

        public void SetOperationOutcome(AuditOperationOutcome outcome) { }

        private sealed class ScopeLease(RecordingAuditScope owner, AuditCommand? previous) : IDisposable
        {
            public void Dispose() => owner.Current = previous;
        }
    }
}
