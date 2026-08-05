using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

/// <summary>Locks down the unchanged public contract at the hazard allowance HTTP boundary.</summary>
public sealed class HazardAllowanceEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public HazardAllowanceEndpointContractTests(WebApplicationFactory<Program> factory)
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
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Employee", HttpStatusCode.Forbidden)]
    public async Task Hazard_allowance_commands_require_payroll_administration(
        string? role,
        HttpStatusCode expectedStatusCode)
    {
        using var client = CreateClient(new CapturingHazardAllowanceService(), role);

        using var response = await client.PostAsync(
            "/api/payroll/hazard-allowance/refresh",
            JsonContent("{}"));

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/payroll/hazard-allowance/refresh")]
    [InlineData("/api/payroll/hazard-allowance/manual-values")]
    [InlineData("/api/payroll/hazard-allowance/lock-state")]
    [InlineData("/api/payroll/hazard-allowance/lock-state/batch")]
    public async Task Hazard_allowance_commands_reject_null_body_with_400(string path)
    {
        var service = new CapturingHazardAllowanceService();
        using var client = CreateClient(service, "PayrollAdmin");

        using var response = await client.PostAsync(path, JsonContent("null"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.RefreshRequest);
        Assert.Null(service.ManualValuesRequest);
        Assert.Null(service.LockStateRequest);
        Assert.Null(service.BatchLockStateRequest);
    }

    [Fact]
    public async Task Refresh_rejects_invalid_period_with_400_without_calling_service()
    {
        var service = new CapturingHazardAllowanceService();
        using var client = CreateClient(service, "PayrollAdmin");

        using var response = await client.PostAsync(
            "/api/payroll/hazard-allowance/refresh",
            JsonContent("{\"payrollMonth\":13,\"payrollYear\":2026,\"requestedBy\":\"forged\"}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.RefreshRequest);
    }

    [Fact]
    public async Task Refresh_overwrites_client_actor_with_authenticated_principal()
    {
        var service = new CapturingHazardAllowanceService();
        using var client = CreateClient(service, "PayrollAdmin");

        using var response = await client.PostAsync(
            "/api/payroll/hazard-allowance/refresh",
            JsonContent("{\"payrollMonth\":6,\"payrollYear\":2026,\"requestedBy\":\"forged-actor\"}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.RefreshRequest?.RequestedBy);
    }

    [Fact]
    public async Task Manual_adjustment_overwrites_client_actor_with_authenticated_principal()
    {
        var service = new CapturingHazardAllowanceService();
        using var client = CreateClient(service, "PayrollAdmin");

        using var response = await client.PostAsync(
            "/api/payroll/hazard-allowance/manual-values",
            JsonContent($"{{\"payrollAllowanceSummaryRecordId\":\"{Guid.NewGuid()}\",\"qualifiedWorkdayCount\":2,\"lateEarlyDeductionDays\":0,\"hazardAllowancePerDay\":100,\"hazardAllowanceAmount\":200,\"isEligibleDepartment\":true,\"originalDetailUpdatedAtUtc\":\"2026-06-01T00:00:00Z\",\"originalSummaryUpdatedAtUtc\":\"2026-06-01T00:00:00Z\",\"requestedBy\":\"forged-actor\"}}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.ManualValuesRequest?.RequestedBy);
    }

    [Fact]
    public async Task Refresh_maps_conflict_to_http_409()
    {
        using var client = CreateClient(new CapturingHazardAllowanceService { ThrowConflict = true }, "PayrollAdmin");

        using var response = await client.PostAsync(
            "/api/payroll/hazard-allowance/refresh",
            JsonContent("{\"payrollMonth\":6,\"payrollYear\":2026,\"requestedBy\":\"forged\"}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/payroll/hazard-allowance/search")]
    [InlineData("/api/payroll/hazard-allowance/search-page")]
    [InlineData("/api/payroll/hazard-allowance/summary")]
    [InlineData("/api/payroll/hazard-allowance/export")]
    public async Task Hazard_allowance_read_and_export_endpoints_forbid_authenticated_non_administrators(string path)
    {
        using var client = CreateClient(new CapturingHazardAllowanceService(), "Employee");

        using var response = await client.PostAsync(path, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Employee", HttpStatusCode.Forbidden)]
    public async Task Export_job_endpoints_require_payroll_administration(string? role, HttpStatusCode expectedStatusCode)
    {
        var jobId = Guid.NewGuid();
        using var client = CreateClient(new CapturingHazardAllowanceService(), role);

        using var queueResponse = await client.PostAsync(
            "/api/payroll/hazard-allowance/export-jobs",
            JsonContent("{}"));
        using var statusResponse = await client.GetAsync(
            $"/api/payroll/hazard-allowance/export-jobs/{jobId:D}");
        using var downloadResponse = await client.GetAsync(
            $"/api/payroll/hazard-allowance/export-jobs/{jobId:D}/download");

        Assert.Equal(expectedStatusCode, queueResponse.StatusCode);
        Assert.Equal(expectedStatusCode, statusResponse.StatusCode);
        Assert.Equal(expectedStatusCode, downloadResponse.StatusCode);
    }

    [Fact]
    public async Task Queue_export_job_returns_accepted_and_uses_the_authenticated_actor()
    {
        var exportJobs = new CapturingExportJobService();
        using var client = CreateClient(new CapturingHazardAllowanceService(), "PayrollAdmin", exportJobs);

        using var response = await client.PostAsync(
            "/api/payroll/hazard-allowance/export-jobs",
            JsonContent("{\"payrollMonth\":6,\"payrollYear\":2026}"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(exportJobs.QueueRequest);
        Assert.Equal("security-boundary-test-user", exportJobs.QueueRequest!.RequestedBy);
        Assert.Equal(6, exportJobs.QueueRequest.Filter.PayrollMonth);
        Assert.Equal(2026, exportJobs.QueueRequest.Filter.PayrollYear);
        Assert.Equal($"/api/payroll/hazard-allowance/export-jobs/{exportJobs.JobId:D}",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Manual_adjustment_rejects_an_empty_summary_id_without_calling_service()
    {
        var service = new CapturingHazardAllowanceService();
        using var client = CreateClient(service, "PayrollAdmin");

        using var response = await client.PostAsync(
            "/api/payroll/hazard-allowance/manual-values",
            JsonContent("{\"payrollAllowanceSummaryRecordId\":\"00000000-0000-0000-0000-000000000000\"}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.ManualValuesRequest);
    }

    [Fact]
    public async Task Batch_lock_rejects_an_invalid_period_without_calling_service()
    {
        var service = new CapturingHazardAllowanceService();
        using var client = CreateClient(service, "PayrollAdmin");

        using var response = await client.PostAsync(
            "/api/payroll/hazard-allowance/lock-state/batch",
            JsonContent("{\"payrollYear\":2026,\"payrollMonth\":13,\"isLocked\":true}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.BatchLockStateRequest);
    }

    [Theory]
    [InlineData("/api/payroll/hazard-allowance/manual-values")]
    [InlineData("/api/payroll/hazard-allowance/lock-state")]
    [InlineData("/api/payroll/hazard-allowance/lock-state/batch")]
    public async Task Hazard_allowance_mutating_endpoints_map_domain_conflict_to_http_409(string path)
    {
        using var client = CreateClient(new CapturingHazardAllowanceService { ThrowConflict = true }, "PayrollAdmin");
        var payload = path.EndsWith("manual-values", StringComparison.Ordinal)
            ? $$"""{"payrollAllowanceSummaryRecordId":"{{Guid.NewGuid()}}","qualifiedWorkdayCount":1,"isEligibleDepartment":true}"""
            : path.EndsWith("batch", StringComparison.Ordinal)
                ? "{\"payrollYear\":2026,\"payrollMonth\":6,\"isLocked\":true}"
                : $$"""{"payrollAllowanceSummaryRecordIds":["{{Guid.NewGuid()}}"],"isLocked":true}""";

        using var response = await client.PostAsync(path, JsonContent(payload));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Lock_commands_overwrite_a_client_supplied_actor_with_authenticated_principal()
    {
        var service = new CapturingHazardAllowanceService();
        using var client = CreateClient(service, "PayrollAdmin");

        using var singleResponse = await client.PostAsync(
            "/api/payroll/hazard-allowance/lock-state",
            JsonContent($$"""{"payrollAllowanceSummaryRecordIds":["{{Guid.NewGuid()}}"],"isLocked":true,"requestedBy":"forged"}"""));
        using var batchResponse = await client.PostAsync(
            "/api/payroll/hazard-allowance/lock-state/batch",
            JsonContent("{\"payrollYear\":2026,\"payrollMonth\":6,\"isLocked\":true,\"requestedBy\":\"forged\"}"));

        Assert.Equal(HttpStatusCode.NoContent, singleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, batchResponse.StatusCode);
        Assert.Equal("security-boundary-test-user", service.LockStateRequest?.RequestedBy);
        Assert.Equal("security-boundary-test-user", service.BatchLockStateRequest?.RequestedBy);
    }

    private HttpClient CreateClient(
        CapturingHazardAllowanceService service,
        string? role,
        IHazardAllowanceExportJobService? exportJobs = null)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHazardAllowanceRefreshService>();
                services.RemoveAll<IHazardAllowanceManualAdjustmentService>();
                services.RemoveAll<IHazardAllowanceLockService>();
                services.AddSingleton<IHazardAllowanceRefreshService>(service);
                services.AddSingleton<IHazardAllowanceManualAdjustmentService>(service);
                services.AddSingleton<IHazardAllowanceLockService>(service);
                if(exportJobs is not null)
                {
                    services.RemoveAll<IHazardAllowanceExportJobService>();
                    services.AddSingleton(exportJobs);
                }
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        if(role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        return client;
    }

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private sealed class CapturingHazardAllowanceService :
        IHazardAllowanceRefreshService,
        IHazardAllowanceManualAdjustmentService,
        IHazardAllowanceLockService
    {
        public bool ThrowConflict { get; init; }
        public RefreshHazardAllowanceRequest? RefreshRequest { get; private set; }
        public UpdateHazardAllowanceManualValuesRequest? ManualValuesRequest { get; private set; }
        public SetHazardAllowanceLockStateRequest? LockStateRequest { get; private set; }
        public SetHazardAllowanceBatchLockStateRequest? BatchLockStateRequest { get; private set; }

        public Task<RefreshHazardAllowanceResult> RefreshAsync(
            RefreshHazardAllowanceRequest request,
            CancellationToken cancellationToken = default)
        {
            RefreshRequest = request;
            ThrowIfConflict();
            return Task.FromResult(new RefreshHazardAllowanceResult(6, 2026, 0, 0, 0, 0, 0, 0));
        }

        public Task<HazardAllowanceListItemDto> UpdateManualValuesAsync(
            UpdateHazardAllowanceManualValuesRequest request,
            CancellationToken cancellationToken = default)
        {
            ManualValuesRequest = request;
            ThrowIfConflict();
            return Task.FromResult(new HazardAllowanceListItemDto(
                request.PayrollAllowanceSummaryRecordId, Guid.NewGuid(), "NV001", "Test employee", 6, 2026,
                request.QualifiedWorkdayCount, request.LateEarlyDeductionDays, request.QualifiedWorkdayCount,
                request.HazardAllowancePerDay, request.HazardAllowanceAmount, request.IsEligibleDepartment,
                request.ExclusionReason, false, DateTime.UtcNow, request.RequestedBy, null, null, null));
        }

        public Task SetLockStateAsync(
            SetHazardAllowanceLockStateRequest request,
            CancellationToken cancellationToken = default)
        {
            LockStateRequest = request;
            ThrowIfConflict();
            return Task.CompletedTask;
        }

        public Task<SetHazardAllowanceBatchLockStateResult> SetLockStateBatchAsync(
            SetHazardAllowanceBatchLockStateRequest request,
            CancellationToken cancellationToken = default)
        {
            BatchLockStateRequest = request;
            ThrowIfConflict();
            return Task.FromResult(new SetHazardAllowanceBatchLockStateResult(
                request.PayrollYear, request.PayrollMonth, 0, 0));
        }

        private void ThrowIfConflict()
        {
            if(ThrowConflict)
            {
                throw new HazardAllowanceConflictException("conflict");
            }
        }
    }

    private sealed class CapturingExportJobService : IHazardAllowanceExportJobService
    {
        public Guid JobId { get; } = Guid.NewGuid();
        public CreateHazardAllowanceExportJobRequest? QueueRequest { get; private set; }

        public Task<HazardAllowanceExportJobDto> QueueAsync(
            CreateHazardAllowanceExportJobRequest request,
            CancellationToken cancellationToken = default)
        {
            QueueRequest = request;
            return Task.FromResult(new HazardAllowanceExportJobDto(
                JobId,
                HazardAllowanceExportJobStatus.Queued,
                DateTime.UnixEpoch,
                null,
                null,
                null,
                null));
        }

        public Task<HazardAllowanceExportJobDto?> GetAsync(
            Guid jobId,
            string requestedBy,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<HazardAllowanceExportJobDto?>(null);

        public Task<HazardAllowanceExportJobFileDto?> OpenCompletedFileAsync(
            Guid jobId,
            string requestedBy,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<HazardAllowanceExportJobFileDto?>(null);
    }
}
