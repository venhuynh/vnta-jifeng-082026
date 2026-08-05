using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;
using Xunit;

#pragma warning disable CS0618 // This boundary test intentionally verifies legacy registrations.

namespace Vnta.Hrm.Web.Tests;

public sealed class LeaveHolidayAllowanceBoundaryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestConnectionString = "Host=localhost;Port=5432;Database=vnta_security_test;Username=test;Password=test";
    private readonly WebApplicationFactory<Program> factory;

    public LeaveHolidayAllowanceBoundaryTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", TestConnectionString);
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
    public async Task Search_endpoint_requires_authentication()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/search",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/payroll/leave-holiday-allowance/clear-manual-values")]
    [InlineData("/api/payroll/leave-holiday-allowance/sync-previous-month")]
    [InlineData("/api/payroll/leave-holiday-allowance/recalculate")]
    [InlineData("/api/payroll/leave-holiday-allowance/manual-values")]
    [InlineData("/api/payroll/leave-holiday-allowance/lock-state/batch")]
    public async Task Remaining_command_endpoints_require_authentication(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.PostAsync(path, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_endpoint_forbids_user_without_payroll_role()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/search",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/payroll/leave-holiday-allowance/clear-manual-values")]
    [InlineData("/api/payroll/leave-holiday-allowance/sync-previous-month")]
    [InlineData("/api/payroll/leave-holiday-allowance/recalculate")]
    [InlineData("/api/payroll/leave-holiday-allowance/manual-values")]
    [InlineData("/api/payroll/leave-holiday-allowance/lock-state/batch")]
    public async Task Remaining_command_endpoints_forbid_users_without_payroll_role(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        using var response = await client.PostAsync(path, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Prepare_period_endpoint_requires_authentication()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/prepare-period?year=2026&month=7",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Prepare_period_endpoint_forbids_user_without_payroll_role()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/prepare-period?year=2026&month=7",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Prepare_period_endpoint_is_mapped_and_executes_the_capability_service()
    {
        var service = new CapturingPeriodPreparationService();
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILeaveHolidayAllowancePeriodPreparationService>();
            services.AddSingleton<ILeaveHolidayAllowancePeriodPreparationService>(service);
        }));
        using var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/prepare-period?year=2026&month=7",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(2026, service.Request?.Year);
        Assert.Equal(7, service.Request?.Month);
    }

    [Fact]
    public async Task Lock_state_endpoint_requires_authentication()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/lock-state",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Lock_state_endpoint_forbids_user_without_payroll_role()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/lock-state",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public void Server_registers_the_narrow_read_and_command_contracts()
    {
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILeaveHolidayAllowanceReadService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILeaveHolidayAllowancePeriodPreparationService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILeaveHolidayAllowanceClearManualValuesService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILeaveHolidayAllowancePreviousMonthSyncService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILeaveHolidayAllowanceRecalculationService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILeaveHolidayAllowanceManualAdjustmentService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILeaveHolidayAllowanceLockService>());
    }

    [Fact]
    public async Task Manual_values_reject_invalid_payload_without_calling_handler()
    {
        var service = new CapturingLeaveHolidayAllowanceService();
        using var client = CreateClient(service);

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/manual-values",
            JsonContent($"{{\"payrollAllowanceSummaryRecordId\":\"{Guid.NewGuid()}\",\"dailyWageAmount\":-1,\"leaveDayCount\":0,\"holidayDayCount\":0}}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.ManualValuesRequest);
    }

    [Fact]
    public async Task Manual_values_reject_null_body_without_calling_handler()
    {
        var service = new CapturingLeaveHolidayAllowanceService();
        using var client = CreateClient(service);

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/manual-values",
            JsonContent("null"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.ManualValuesRequest);
    }

    [Fact]
    public async Task Manual_values_replace_client_actor_with_authenticated_principal()
    {
        var service = new CapturingLeaveHolidayAllowanceService();
        using var client = CreateClient(service);

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/manual-values",
            JsonContent($"{{\"payrollAllowanceSummaryRecordId\":\"{Guid.NewGuid()}\",\"dailyWageAmount\":100,\"leaveDayCount\":1,\"holidayDayCount\":0,\"actor\":\"forged-actor\"}}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.ManualValuesRequest?.Actor);
    }

    [Fact]
    public async Task Lock_state_maps_application_conflict_to_409()
    {
        var service = new CapturingLeaveHolidayAllowanceService { ThrowConflict = true };
        using var client = CreateClient(service);

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/lock-state",
            JsonContent($"{{\"payrollAllowanceSummaryRecordId\":\"{Guid.NewGuid()}\",\"isLocked\":true}}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Lock_state_replaces_client_actor_with_authenticated_principal()
    {
        var service = new CapturingLeaveHolidayAllowanceService();
        using var client = CreateClient(service);

        using var response = await client.PostAsync(
            "/api/payroll/leave-holiday-allowance/lock-state",
            JsonContent($"{{\"payrollAllowanceSummaryRecordId\":\"{Guid.NewGuid()}\",\"isLocked\":true,\"actor\":\"forged-actor\"}}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.LockStateRequest?.Actor);
    }

    private HttpClient CreateClient(CapturingLeaveHolidayAllowanceService service, string role = "PayrollAdmin")
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILeaveHolidayAllowanceManualAdjustmentService>();
            services.RemoveAll<ILeaveHolidayAllowanceLockService>();
            services.AddSingleton<ILeaveHolidayAllowanceManualAdjustmentService>(service);
            services.AddSingleton<ILeaveHolidayAllowanceLockService>(service);
        }));
        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        return client;
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    private sealed class CapturingPeriodPreparationService : ILeaveHolidayAllowancePeriodPreparationService
    {
        public (int Year, int Month)? Request { get; private set; }

        public Task PreparePeriodAsync(int payrollYear, int payrollMonth, CancellationToken cancellationToken = default)
        {
            Request = (payrollYear, payrollMonth);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingLeaveHolidayAllowanceService :
        ILeaveHolidayAllowanceManualAdjustmentService,
        ILeaveHolidayAllowanceLockService
    {
        public bool ThrowConflict { get; init; }
        public UpdateLeaveHolidayAllowanceManualValuesRequest? ManualValuesRequest { get; private set; }
        public SetLeaveHolidayAllowanceLockStateRequest? LockStateRequest { get; private set; }

        public Task<LeaveHolidayAllowanceListItemDto> UpdateManualValuesAsync(
            UpdateLeaveHolidayAllowanceManualValuesRequest request,
            CancellationToken cancellationToken = default)
        {
            ManualValuesRequest = request;
            if (ThrowConflict) throw new LeaveHolidayAllowanceConflictException("conflict");
            return Task.FromResult(CreateItem(request.PayrollAllowanceSummaryRecordId));
        }

        public Task<LeaveHolidayAllowanceListItemDto> SetLockStateAsync(
            SetLeaveHolidayAllowanceLockStateRequest request,
            CancellationToken cancellationToken = default)
        {
            LockStateRequest = request;
            if (ThrowConflict) throw new LeaveHolidayAllowanceConflictException("conflict");
            return Task.FromResult(CreateItem(request.PayrollAllowanceSummaryRecordId));
        }

        public Task<SetLeaveHolidayAllowanceBatchLockStateResult> SetLockStateBatchAsync(
            SetLeaveHolidayAllowanceBatchLockStateRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SetLeaveHolidayAllowanceBatchLockStateResult(
                request.PayrollYear, request.PayrollMonth, 0, 0, 0));

        private static LeaveHolidayAllowanceListItemDto CreateItem(Guid id) => new(
            id, Guid.NewGuid(), "E001", "Test", null, null, 7, 2026,
            100m, 1m, 0m, 100m, null, false, DateTime.UtcNow, "test", null, null, null);
    }
}
