using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Queries;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapTongHop;

public sealed class PayrollAllowanceSummaryCommandEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PayrollAllowanceSummaryCommandEndpointTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Port=5432;Database=vnta_security_test;Username=test;Password=test");
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestHeaderAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestHeaderAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme = TestHeaderAuthenticationHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>(
                    TestHeaderAuthenticationHandler.SchemeName,
                    _ => { }));
        });
    }

    [Theory]
    [InlineData("/api/payroll/allowance-summary/refresh")]
    [InlineData("/api/payroll/allowance-summary/sync-previous-month")]
    [InlineData("/api/payroll/allowance-summary/manual-adjustment")]
    [InlineData("/api/payroll/allowance-summary/delete")]
    [InlineData("/api/payroll/allowance-summary/lock-state")]
    [InlineData("/api/payroll/allowance-summary/lock-state/batch")]
    public async Task Command_endpoints_require_payroll_administration_authorization(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync(path, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Commands_forbid_an_authenticated_user_without_the_payroll_administration_role()
    {
        using var client = CreateClient(new CapturingRefreshService(), role: "Employee");

        var response = await client.PostAsync(
            "/api/payroll/allowance-summary/refresh",
            JsonContent("{\"targetPayrollMonth\":7,\"targetPayrollYear\":2026}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_replaces_a_client_supplied_actor_with_the_authenticated_actor()
    {
        var service = new CapturingRefreshService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/allowance-summary/refresh",
            JsonContent("{\"targetPayrollMonth\":7,\"targetPayrollYear\":2026,\"actor\":\"forged-admin\"}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(service.Request);
        Assert.Equal("security-boundary-test-user", service.Request!.Actor);
    }

    [Fact]
    public async Task Manual_adjustment_maps_business_validation_failure_to_bad_request()
    {
        using var client = CreateClient(manualService: new CapturingManualService { RejectRequest = true });

        var response = await client.PostAsync(
            "/api/payroll/allowance-summary/manual-adjustment",
            JsonContent($"{{\"id\":\"{Guid.NewGuid()}\",\"note\":\"{new string('a', 1001)}\"}}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Manual_adjustment_maps_editable_values_and_omits_the_derived_attendance_projection()
    {
        var service = new CapturingManualService();
        var id = Guid.NewGuid();
        using var client = CreateClient(manualService: service);

        var response = await client.PostAsync(
            "/api/payroll/allowance-summary/manual-adjustment",
            JsonContent($"{{\"id\":\"{id}\",\"responsibilityAllowanceAmount\":100000,\"responsibilityOtherAllowanceAmount\":150000,\"seniorityAllowanceAmount\":200000,\"mealAllowanceAmount\":400000,\"hazardAllowanceAmount\":500000,\"otherAllowanceAmount\":600000,\"leaveHolidayAllowanceAmount\":700000,\"isLocked\":true,\"note\":\"manual adjustment\",\"actor\":\"forged-admin\"}}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(service.Request);
        Assert.Equal(id, service.Request!.Id);
        Assert.Equal(100000m, service.Request.ResponsibilityAllowanceAmount);
        Assert.Equal(150000m, service.Request.ResponsibilityOtherAllowanceAmount);
        Assert.Equal(700000m, service.Request.LeaveHolidayAllowanceAmount);
        Assert.Null(service.Request.AttendanceAllowanceAmount);
        Assert.True(service.Request.IsLocked);
        Assert.Equal("manual adjustment", service.Request.Note);
        Assert.Equal("security-boundary-test-user", service.Request.Actor);
    }

    [Fact]
    public async Task Manual_adjustment_forwards_a_legacy_attendance_value_for_server_side_ownership_validation()
    {
        var service = new CapturingManualService();
        var id = Guid.NewGuid();
        using var client = CreateClient(manualService: service);

        var response = await client.PostAsync(
            "/api/payroll/allowance-summary/manual-adjustment",
            JsonContent($"{{\"id\":\"{id}\",\"attendanceAllowanceAmount\":300000}}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(service.Request);
        Assert.Equal(300000m, service.Request!.AttendanceAllowanceAmount);
    }

    [Fact]
    public async Task Batch_lock_maps_an_optimistic_concurrency_conflict_to_http_409()
    {
        using var client = CreateClient(lockService: new CapturingLockService { ThrowConcurrencyConflict = true });

        var response = await client.PostAsync(
            "/api/payroll/allowance-summary/lock-state/batch",
            JsonContent("{\"payrollYear\":2026,\"payrollMonth\":7,\"isLocked\":true}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private HttpClient CreateClient(
        CapturingRefreshService? refreshService = null,
        CapturingManualService? manualService = null,
        CapturingLockService? lockService = null,
        string role = "PayrollAdmin")
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            if(refreshService is not null)
            {
                services.RemoveAll<IPayrollAllowanceSummaryRefreshService>();
                services.AddSingleton<IPayrollAllowanceSummaryRefreshService>(refreshService);
            }
            if(manualService is not null)
            {
                services.RemoveAll<IPayrollAllowanceSummaryManualAdjustmentService>();
                services.AddSingleton<IPayrollAllowanceSummaryManualAdjustmentService>(manualService);
            }
            if(lockService is not null)
            {
                services.RemoveAll<IPayrollAllowanceSummaryLockService>();
                services.AddSingleton<IPayrollAllowanceSummaryLockService>(lockService);
            }
        }));
        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        return client;
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    private sealed class CapturingRefreshService : IPayrollAllowanceSummaryRefreshService
    {
        public RefreshPayrollAllowanceSummaryRequest? Request { get; private set; }

        public Task<RefreshPayrollAllowanceSummaryResult> RefreshAsync(RefreshPayrollAllowanceSummaryRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new RefreshPayrollAllowanceSummaryResult(
                request.TargetPayrollMonth, request.TargetPayrollYear, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        }
    }

    private sealed class CapturingManualService : IPayrollAllowanceSummaryManualAdjustmentService
    {
        public bool RejectRequest { get; init; }
        public UpdatePayrollAllowanceSummaryManualValuesRequest? Request { get; private set; }

        public Task<PayrollAllowanceSummaryListItemDto> UpdateManualValuesAsync(UpdatePayrollAllowanceSummaryManualValuesRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;

            if(RejectRequest)
                throw new PayrollAllowanceSummaryValidationException("Ghi chú không được vượt quá 1000 ký tự.");

            return Task.FromResult(CreateRow(request.Id));
        }
    }

    private sealed class CapturingLockService : IPayrollAllowanceSummaryLockService
    {
        public bool ThrowConcurrencyConflict { get; init; }

        public Task<PayrollAllowanceSummaryListItemDto> SetLockStateAsync(SetPayrollAllowanceSummaryLockStateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateRow(request.Id));

        public Task<SetPayrollAllowanceSummaryBatchLockStateResult> SetLockStateBatchAsync(SetPayrollAllowanceSummaryBatchLockStateRequest request, CancellationToken cancellationToken = default)
        {
            if(ThrowConcurrencyConflict)
                throw new DbUpdateConcurrencyException("stale allowance-summary row");

            return Task.FromResult(new SetPayrollAllowanceSummaryBatchLockStateResult(request.PayrollYear, request.PayrollMonth, 0, 0));
        }
    }

    private static PayrollAllowanceSummaryListItemDto CreateRow(Guid id) => new(
        id, Guid.NewGuid(), "NV001", "Nhân viên", "Nhân sự", "Chuyên viên", 7, 2026,
        0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, false, null,
        new DateTime(2026, 7, 1), "tester", null, null);
}
