using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.KhauTru.KhauTruTongKet;

public sealed class KhauTruTongKetEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BasePath = "/api/payroll/deduction-summary";
    private readonly WebApplicationFactory<Program> factory;

    public KhauTruTongKetEndpointContractTests(WebApplicationFactory<Program> factory)
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

    [Theory]
    [InlineData("/search")]
    [InlineData("/export")]
    [InlineData("/sync-previous-month")]
    [InlineData("/refresh")]
    [InlineData("/recalculate")]
    [InlineData("/manual-other-deduction")]
    [InlineData("/lock-state")]
    [InlineData("/lock-state/batch")]
    public async Task Feature_routes_require_payroll_administration_at_the_http_boundary(string route)
    {
        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var employee = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        employee.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        using var anonymousResponse = await anonymous.PostAsync(BasePath + route, JsonContent("{}"));
        using var employeeResponse = await employee.PostAsync(BasePath + route, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, employeeResponse.StatusCode);
    }

    [Theory]
    [InlineData("/sync-previous-month")]
    [InlineData("/refresh")]
    [InlineData("/recalculate")]
    [InlineData("/manual-other-deduction")]
    [InlineData("/lock-state")]
    [InlineData("/lock-state/batch")]
    public async Task Null_command_payload_is_rejected_before_the_application_capability_is_called(string route)
    {
        var service = new CapturingDeductionSummaryService();
        using var client = CreatePayrollAdminClient(service);

        using var response = await client.PostAsync(BasePath + route, JsonContent("null"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(service.CommandWasCalled);
    }

    [Theory]
    [InlineData("/sync-previous-month", "{\"targetPayrollMonth\":0,\"targetPayrollYear\":2026}")]
    [InlineData("/refresh", "{\"summaryRecordId\":\"00000000-0000-0000-0000-000000000000\",\"payrollMonth\":7,\"payrollYear\":2026}")]
    [InlineData("/recalculate", "{\"payrollMonth\":13,\"payrollYear\":2026}")]
    [InlineData("/manual-other-deduction", "{\"id\":\"00000000-0000-0000-0000-000000000000\",\"otherDeductionAmount\":1,\"originalUpdatedAtUtc\":\"2026-07-22T00:00:00Z\"}")]
    [InlineData("/manual-other-deduction", "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"otherDeductionAmount\":-1,\"originalUpdatedAtUtc\":\"2026-07-22T00:00:00Z\"}")]
    [InlineData("/lock-state", "{\"id\":\"00000000-0000-0000-0000-000000000000\",\"isLocked\":true}")]
    [InlineData("/lock-state/batch", "{\"payrollYear\":2026,\"payrollMonth\":0,\"isLocked\":true}")]
    public async Task Invalid_command_payload_is_rejected_before_the_application_capability_is_called(string route, string payload)
    {
        var service = new CapturingDeductionSummaryService();
        using var client = CreatePayrollAdminClient(service);

        using var response = await client.PostAsync(BasePath + route, JsonContent(payload));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(service.CommandWasCalled);
    }

    [Fact]
    public async Task Manual_adjustment_uses_authenticated_actor_for_request_and_audit_when_payload_spoofs_actor()
    {
        var service = new CapturingDeductionSummaryService();
        var auditScope = new RecordingAuditScope();
        using var client = CreatePayrollAdminClient(service, auditScope);

        using var response = await client.PostAsync(
            BasePath + "/manual-other-deduction",
            JsonContent("{\"id\":\"11111111-1111-1111-1111-111111111111\",\"otherDeductionAmount\":150000,\"note\":\"test\",\"originalUpdatedAtUtc\":\"2026-07-22T00:00:00Z\",\"actor\":\"forged\",\"requestedBy\":\"forged\"}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.ManualAdjustmentRequest?.Actor);
        var command = Assert.Single(auditScope.Commands);
        Assert.Equal(AuditActions.DeductionSummary.ManualOtherDeductionUpdated, command.ActionIntent);
        Assert.Equal("security-boundary-test-user", command.Actor.ActorId);
        Assert.Equal(response.Headers.GetValues("X-Correlation-Id").Single(), command.CorrelationId);
    }

    [Theory]
    [InlineData("/manual-other-deduction", "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"otherDeductionAmount\":150000,\"originalUpdatedAtUtc\":\"2026-07-22T00:00:00Z\"}")]
    [InlineData("/lock-state", "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"isLocked\":true,\"originalUpdatedAtUtc\":\"2026-07-22T00:00:00Z\"}")]
    [InlineData("/lock-state/batch", "{\"payrollYear\":2026,\"payrollMonth\":7,\"isLocked\":true,\"items\":[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"originalUpdatedAtUtc\":\"2026-07-22T00:00:00Z\"}]}")]
    public async Task Concurrency_conflicts_are_mapped_to_http_409(string route, string payload)
    {
        var service = new CapturingDeductionSummaryService { ThrowConcurrencyConflict = true };
        using var client = CreatePayrollAdminClient(service);

        using var response = await client.PostAsync(BasePath + route, JsonContent(payload));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Export_forwards_the_authorized_period_and_format_and_records_the_export_operation()
    {
        var service = new CapturingDeductionSummaryService();
        var auditScope = new RecordingAuditScope();
        using var client = CreatePayrollAdminClient(service, auditScope);

        using var response = await client.PostAsync(
            BasePath + "/export", JsonContent("{\"payrollMonth\":7,\"payrollYear\":2026,\"format\":0}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((7, 2026, PayrollDeductionSummaryExportFormat.Excel), service.ExportRequest);
        Assert.Equal(AuditActions.DeductionSummary.Exported, Assert.Single(auditScope.Commands).ActionIntent);
    }

    private HttpClient CreatePayrollAdminClient(
        CapturingDeductionSummaryService service,
        RecordingAuditScope? auditScope = null)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPayrollDeductionSummaryReadService>();
            services.RemoveAll<IPayrollDeductionSummaryExportService>();
            services.RemoveAll<IPayrollDeductionSummarySyncService>();
            services.RemoveAll<IPayrollDeductionSummaryRefreshService>();
            services.RemoveAll<IPayrollDeductionSummaryManualAdjustmentService>();
            services.RemoveAll<IPayrollDeductionSummaryLockService>();
            if(auditScope is not null)
            {
                services.RemoveAll<IAuditScope>();
                services.AddSingleton<IAuditScope>(auditScope);
            }

            services.AddSingleton<IPayrollDeductionSummaryReadService>(service);
            services.AddSingleton<IPayrollDeductionSummaryExportService>(service);
            services.AddSingleton<IPayrollDeductionSummarySyncService>(service);
            services.AddSingleton<IPayrollDeductionSummaryRefreshService>(service);
            services.AddSingleton<IPayrollDeductionSummaryManualAdjustmentService>(service);
            services.AddSingleton<IPayrollDeductionSummaryLockService>(service);
        }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    private sealed class CapturingDeductionSummaryService :
        IPayrollDeductionSummaryReadService,
        IPayrollDeductionSummaryExportService,
        IPayrollDeductionSummarySyncService,
        IPayrollDeductionSummaryRefreshService,
        IPayrollDeductionSummaryManualAdjustmentService,
        IPayrollDeductionSummaryLockService
    {
        public bool ThrowConcurrencyConflict { get; init; }
        public bool CommandWasCalled { get; private set; }
        public UpdatePayrollDeductionSummaryManualOtherDeductionRequest? ManualAdjustmentRequest { get; private set; }
        public (int Month, int Year, PayrollDeductionSummaryExportFormat Format)? ExportRequest { get; private set; }

        public Task<PayrollDeductionSummaryPageDto> SearchAsync(
            PayrollDeductionSummaryFilter filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PayrollDeductionSummaryPageDto(
                [],
                0,
                PayrollDeductionSummaryAggregateDto.Empty,
                PayrollDeductionSummaryLockStatusCountsDto.Empty));

        public Task<IReadOnlyList<PayrollDeductionSummaryExportItemDto>> ExportPeriodAsync(
            int payrollMonth,
            int payrollYear,
            PayrollDeductionSummaryExportFormat format,
            CancellationToken cancellationToken = default)
        {
            ExportRequest = (payrollMonth, payrollYear, format);
            return Task.FromResult<IReadOnlyList<PayrollDeductionSummaryExportItemDto>>([]);
        }

        public Task<SyncPayrollDeductionSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
            SyncPayrollDeductionSummaryFromPreviousMonthRequest request,
            CancellationToken cancellationToken = default)
        {
            CommandWasCalled = true;
            return Task.FromResult(new SyncPayrollDeductionSummaryFromPreviousMonthResult(6, 2026, request.TargetPayrollMonth, request.TargetPayrollYear, 0, 0, 0, 0, 0, 0));
        }

        public Task<RefreshPayrollDeductionSummaryResult> RefreshAsync(
            RefreshPayrollDeductionSummaryRequest request,
            CancellationToken cancellationToken = default)
        {
            CommandWasCalled = true;
            return Task.FromResult(new RefreshPayrollDeductionSummaryResult(request.SummaryRecordId, request.PayrollYear, request.PayrollMonth, 0, 0, 0, 0));
        }

        public Task<RecalculatePayrollDeductionSummaryPeriodResult> RecalculatePeriodAsync(
            RecalculatePayrollDeductionSummaryPeriodRequest request,
            CancellationToken cancellationToken = default)
        {
            CommandWasCalled = true;
            return Task.FromResult(new RecalculatePayrollDeductionSummaryPeriodResult(request.PayrollYear, request.PayrollMonth, 0, 0, 0, 0, 0));
        }

        public Task<PayrollDeductionSummaryListItemDto> UpdateManualOtherDeductionAsync(
            UpdatePayrollDeductionSummaryManualOtherDeductionRequest request,
            CancellationToken cancellationToken = default)
        {
            CommandWasCalled = true;
            ManualAdjustmentRequest = request;
            ThrowIfConfigured();
            return Task.FromResult(CreateRow(request.Id, false));
        }

        public Task<PayrollDeductionSummaryListItemDto> SetLockStateAsync(
            SetPayrollDeductionSummaryLockStateRequest request,
            CancellationToken cancellationToken = default)
        {
            CommandWasCalled = true;
            ThrowIfConfigured();
            return Task.FromResult(CreateRow(request.Id, request.IsLocked));
        }

        public Task<SetPayrollDeductionSummaryBatchLockStateResult> SetLockStateBatchAsync(
            SetPayrollDeductionSummaryBatchLockStateRequest request,
            CancellationToken cancellationToken = default)
        {
            CommandWasCalled = true;
            ThrowIfConfigured();
            return Task.FromResult(new SetPayrollDeductionSummaryBatchLockStateResult(request.PayrollYear, request.PayrollMonth, 0, 0));
        }

        private void ThrowIfConfigured()
        {
            if(ThrowConcurrencyConflict)
            {
                throw new PayrollDeductionSummaryConcurrencyException("stale version");
            }
        }

        private static PayrollDeductionSummaryListItemDto CreateRow(Guid id, bool isLocked) => new(
            id, Guid.NewGuid(), "NV001", "Test user", null, null, 7, 2026,
            0m, 0m, 0m, 0m, 0m, isLocked, null, DateTime.UtcNow, "test", null, null);
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
