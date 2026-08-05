using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Application.PhuCap.Common;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class AttendanceAllowanceEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AttendanceAllowanceEndpointContractTests(WebApplicationFactory<Program> factory)
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
    [InlineData("/api/payroll/attendance-allowance/refresh")]
    [InlineData("/api/payroll/attendance-allowance/actual-workday")]
    [InlineData("/api/payroll/attendance-allowance/standard-workday")]
    [InlineData("/api/payroll/attendance-allowance/lock-state")]
    [InlineData("/api/payroll/attendance-allowance/lock-state/batch")]
    [InlineData("/api/payroll/attendance-allowance/search")]
    [InlineData("/api/payroll/attendance-allowance/export")]
    public async Task Attendance_allowance_endpoints_require_management_authorization(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync(path, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Commands_forbid_authenticated_users_without_payroll_permission()
    {
        using var client = CreateClient(new CapturingRefreshService(), "Employee");

        var response = await client.PostAsync(
            "/api/payroll/attendance-allowance/refresh",
            JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rejects_null_or_invalid_payload_with_400_without_calling_service()
    {
        var service = new CapturingRefreshService();
        using var client = CreateClient(service);

        var nullResponse = await client.PostAsync(
            "/api/payroll/attendance-allowance/refresh",
            JsonContent("null"));
        var invalidResponse = await client.PostAsync(
            "/api/payroll/attendance-allowance/refresh",
            JsonContent("{\"targetPayrollMonth\":13,\"targetPayrollYear\":2026}"));

        Assert.Equal(HttpStatusCode.BadRequest, nullResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Null(service.Request);
    }

    [Fact]
    public async Task Refresh_audit_uses_authenticated_actor_and_server_correlation_not_client_body()
    {
        var service = new CapturingRefreshService();
        var auditScope = new RecordingAuditScope();
        using var client = CreateClient(service, auditScope: auditScope);

        var response = await client.PostAsync(
            "/api/payroll/attendance-allowance/refresh",
            JsonContent("{\"targetPayrollMonth\":6,\"targetPayrollYear\":2026,\"actor\":\"forged-actor\",\"correlationId\":\"forged-correlation\"}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var audit = Assert.Single(auditScope.Commands);
        Assert.Equal(AuditActions.AttendanceAllowance.Refresh, audit.ActionIntent);
        Assert.Equal("security-boundary-test-user", audit.Actor.ActorId);
        Assert.Equal(response.Headers.GetValues("X-Correlation-Id").Single(), audit.CorrelationId);
    }

    [Fact]
    public async Task Refresh_maps_concurrency_conflict_to_http_409()
    {
        using var client = CreateClient(new CapturingRefreshService { ThrowConflict = true });

        var response = await client.PostAsync(
            "/api/payroll/attendance-allowance/refresh",
            JsonContent("{\"targetPayrollMonth\":6,\"targetPayrollYear\":2026}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_maps_missing_target_to_http_404()
    {
        using var client = CreateClient(new CapturingRefreshService { ThrowNotFound = true });

        var response = await client.PostAsync(
            "/api/payroll/attendance-allowance/refresh",
            JsonContent("{\"targetPayrollMonth\":6,\"targetPayrollYear\":2026}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Manual_workday_command_rejects_invalid_input_before_calling_the_service()
    {
        var service = new CapturingManualAdjustmentService();
        using var client = CreateClient(manualAdjustmentService: service);

        var response = await client.PostAsync(
            "/api/payroll/attendance-allowance/standard-workday",
            JsonContent("{\"id\":\"00000000-0000-0000-0000-000000000000\",\"standardWorkdayCount\":0}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.StandardRequest);
    }

    [Fact]
    public async Task Manual_workday_command_maps_a_stale_version_to_http_409()
    {
        var service = new CapturingManualAdjustmentService { ThrowConflict = true };
        using var client = CreateClient(manualAdjustmentService: service);
        var id = Guid.NewGuid();

        var response = await client.PostAsync(
            "/api/payroll/attendance-allowance/actual-workday",
            JsonContent($"{{\"id\":\"{id}\",\"actualWorkdayCount\":20,\"actor\":\"forged-actor\"}}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(service.ActualRequest);
        Assert.Equal(id, service.ActualRequest!.Id);
    }

    [Fact]
    public async Task Batch_lock_command_maps_concurrency_conflict_to_http_409()
    {
        using var client = CreateClient(lockService: new CapturingLockService { ThrowConflict = true });

        var response = await client.PostAsync(
            "/api/payroll/attendance-allowance/lock-state/batch",
            JsonContent("{\"payrollYear\":2026,\"payrollMonth\":7,\"isLocked\":true,\"actor\":\"forged-actor\"}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private HttpClient CreateClient(
        CapturingRefreshService? service = null,
        string role = "PayrollAdmin",
        RecordingAuditScope? auditScope = null,
        CapturingManualAdjustmentService? manualAdjustmentService = null,
        CapturingLockService? lockService = null)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            if(service is not null)
            {
                services.RemoveAll<IAttendanceAllowanceRefreshService>();
                services.AddSingleton<IAttendanceAllowanceRefreshService>(service);
            }
            if(manualAdjustmentService is not null)
            {
                services.RemoveAll<IAttendanceAllowanceManualAdjustmentService>();
                services.AddSingleton<IAttendanceAllowanceManualAdjustmentService>(manualAdjustmentService);
            }
            if(lockService is not null)
            {
                services.RemoveAll<IAttendanceAllowanceLockService>();
                services.AddSingleton<IAttendanceAllowanceLockService>(lockService);
            }
            if(auditScope is not null)
            {
                services.RemoveAll<IAuditScope>();
                services.AddSingleton<IAuditScope>(auditScope);
            }
        }));
        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        return client;
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    private sealed class CapturingRefreshService : IAttendanceAllowanceRefreshService
    {
        public bool ThrowConflict { get; init; }
        public bool ThrowNotFound { get; init; }
        public RefreshAttendanceAllowanceRequest? Request { get; private set; }

        public Task<RefreshAttendanceAllowanceResult> RefreshAsync(
            RefreshAttendanceAllowanceRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            if(ThrowConflict)
                throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "conflict");
            if(ThrowNotFound)
                throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.NotFound, "not found");

            return Task.FromResult(new RefreshAttendanceAllowanceResult(
                request.TargetPayrollMonth,
                request.TargetPayrollYear,
                0,
                0,
                0,
                request.PayrollAllowanceSummaryRecordId));
        }
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

    private sealed class CapturingManualAdjustmentService : IAttendanceAllowanceManualAdjustmentService
    {
        public bool ThrowConflict { get; init; }
        public UpdateAttendanceAllowanceActualWorkdayRequest? ActualRequest { get; private set; }
        public UpdateAttendanceAllowanceStandardWorkdayRequest? StandardRequest { get; private set; }

        public Task<AttendanceAllowanceResultListItemDto> UpdateActualWorkdayAsync(UpdateAttendanceAllowanceActualWorkdayRequest request, CancellationToken cancellationToken = default)
        {
            ActualRequest = request;
            return CompleteAsync(request.Id);
        }

        public Task<AttendanceAllowanceResultListItemDto> UpdateStandardWorkdayAsync(UpdateAttendanceAllowanceStandardWorkdayRequest request, CancellationToken cancellationToken = default)
        {
            StandardRequest = request;
            return CompleteAsync(request.Id);
        }

        private Task<AttendanceAllowanceResultListItemDto> CompleteAsync(Guid id)
        {
            if(ThrowConflict) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "conflict");
            return Task.FromResult(CreateRow(id));
        }
    }

    private sealed class CapturingLockService : IAttendanceAllowanceLockService
    {
        public bool ThrowConflict { get; init; }

        public Task<AttendanceAllowanceResultListItemDto> SetLockStateAsync(SetAttendanceAllowanceLockStateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateRow(request.Id));

        public Task<SetAttendanceAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetAttendanceAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default)
        {
            if(ThrowConflict) throw new AttendanceAllowanceCommandException(AttendanceAllowanceCommandFailure.Concurrency, "conflict");
            return Task.FromResult(new SetAttendanceAllowanceBatchLockStateResult(request.PayrollYear, request.PayrollMonth, 0, 0, IsLocked: request.IsLocked, IsWholePeriod: request.Items is null && request.AttendanceAllowanceRecordIds is null));
        }
    }

    private static AttendanceAllowanceResultListItemDto CreateRow(Guid id) => new(
        id,
        PayrollAllowanceKind.Attendance,
        Guid.NewGuid(),
        "NV001",
        "Test user",
        null,
        null,
        7,
        2026,
        600_000m,
        26m,
        20m,
        0.7692m,
        300_000m,
        false,
        DateTime.UnixEpoch,
        DateTime.UnixEpoch);
}
