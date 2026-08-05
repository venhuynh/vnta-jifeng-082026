using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapCom.Queries;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapCom;

public sealed class MealAllowanceEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public MealAllowanceEndpointContractTests(WebApplicationFactory<Program> factory)
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
    [InlineData("/api/payroll/meal-allowance/refresh")]
    [InlineData("/api/payroll/meal-allowance/manual-values")]
    [InlineData("/api/payroll/meal-allowance/lock-state/batch")]
    public async Task Meal_allowance_commands_require_payroll_administration_role(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsync(path, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/payroll/meal-allowance/refresh")]
    [InlineData("/api/payroll/meal-allowance/manual-values")]
    [InlineData("/api/payroll/meal-allowance/lock-state/batch")]
    public async Task Meal_allowance_commands_forbid_authenticated_users_without_payroll_administration_role(string path)
    {
        using var client = CreateClient(new CapturingMealAllowanceService(), "Employee");

        var response = await client.PostAsync(path, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/payroll/meal-allowance/refresh")]
    [InlineData("/api/payroll/meal-allowance/manual-values")]
    [InlineData("/api/payroll/meal-allowance/lock-state/batch")]
    public async Task Meal_allowance_commands_reject_null_body_with_400(string path)
    {
        var service = new CapturingMealAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(path, JsonContent("null"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.RefreshRequest);
        Assert.Null(service.ManualValuesRequest);
        Assert.Null(service.LockStateRequest);
    }

    [Fact]
    public async Task Refresh_rejects_invalid_period_with_400_without_calling_command_service()
    {
        var service = new CapturingMealAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/meal-allowance/refresh",
            JsonContent("{\"targetPayrollMonth\":13,\"targetPayrollYear\":2026}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.RefreshRequest);
    }

    [Theory]
    [InlineData("{\"id\":\"00000000-0000-0000-0000-000000000000\",\"qualifiedMealDays\":1}")]
    [InlineData("{\"id\":\"00000000-0000-0000-0000-000000000001\",\"qualifiedMealDays\":-1}")]
    public async Task Manual_adjustment_rejects_invalid_values_without_calling_command_service(string payload)
    {
        var service = new CapturingMealAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync("/api/payroll/meal-allowance/manual-values", JsonContent(payload));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.ManualValuesRequest);
    }

    [Fact]
    public async Task Lock_batch_rejects_an_invalid_scope_without_calling_command_service()
    {
        var service = new CapturingMealAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/meal-allowance/lock-state/batch",
            JsonContent("{\"payrollYear\":2026,\"payrollMonth\":7,\"isLocked\":true,\"scope\":1,\"recordIds\":[]}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.LockStateRequest);
    }

    [Fact]
    public async Task Manual_adjustment_returns_404_when_target_record_no_longer_exists()
    {
        var service = new CapturingMealAllowanceService { ThrowNotFound = true };
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/meal-allowance/manual-values",
            JsonContent($"{{\"id\":\"{Guid.NewGuid()}\",\"qualifiedMealDays\":2}}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_overwrites_client_supplied_actor_with_authenticated_principal()
    {
        var service = new CapturingMealAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/meal-allowance/refresh",
            JsonContent("{\"targetPayrollMonth\":6,\"targetPayrollYear\":2026,\"actor\":\"forged-actor\"}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.RefreshRequest?.Actor);
    }

    [Fact]
    public async Task Manual_adjustment_overwrites_client_supplied_actor_with_authenticated_principal()
    {
        var service = new CapturingMealAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/meal-allowance/manual-values",
            JsonContent($"{{\"id\":\"{Guid.NewGuid()}\",\"qualifiedMealDays\":2,\"actor\":\"forged-actor\"}}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.ManualValuesRequest?.Actor);
        Assert.Equal(2, service.ManualValuesRequest?.QualifiedMealDays);
    }

    [Fact]
    public async Task Lock_batch_overwrites_client_supplied_actor_with_authenticated_principal()
    {
        var service = new CapturingMealAllowanceService();
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/meal-allowance/lock-state/batch",
            JsonContent($"{{\"payrollYear\":2026,\"payrollMonth\":6,\"isLocked\":true,\"scope\":1,\"recordIds\":[\"{Guid.NewGuid()}\"],\"actor\":\"forged-actor\"}}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.LockStateRequest?.Actor);
    }

    [Theory]
    [InlineData("/api/payroll/meal-allowance/refresh")]
    public async Task Meal_allowance_command_maps_conflict_to_http_409(string path)
    {
        var service = new CapturingMealAllowanceService { ThrowConflict = true };
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            path,
            JsonContent("{\"targetPayrollMonth\":6,\"targetPayrollYear\":2026}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Manual_adjustment_maps_conflict_to_http_409()
    {
        var service = new CapturingMealAllowanceService { ThrowConflict = true };
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/meal-allowance/manual-values",
            JsonContent($"{{\"id\":\"{Guid.NewGuid()}\",\"qualifiedMealDays\":2}}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Lock_batch_maps_conflict_to_http_409()
    {
        var service = new CapturingMealAllowanceService { ThrowConflict = true };
        using var client = CreateClient(service);

        var response = await client.PostAsync(
            "/api/payroll/meal-allowance/lock-state/batch",
            JsonContent($"{{\"payrollYear\":2026,\"payrollMonth\":6,\"isLocked\":true,\"scope\":1,\"recordIds\":[\"{Guid.NewGuid()}\"]}}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private HttpClient CreateClient(CapturingMealAllowanceService service, string role = "PayrollAdmin")
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMealAllowanceRefreshService>();
                services.RemoveAll<IMealAllowanceLockService>();
                services.RemoveAll<IMealAllowanceManualAdjustmentService>();
                services.AddSingleton<IMealAllowanceRefreshService>(service);
                services.AddSingleton<IMealAllowanceLockService>(service);
                services.AddSingleton<IMealAllowanceManualAdjustmentService>(service);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        return client;
    }

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private sealed class CapturingMealAllowanceService :
        IMealAllowanceRefreshService,
        IMealAllowanceManualAdjustmentService,
        IMealAllowanceLockService
    {
        public bool ThrowConflict { get; init; }
        public bool ThrowNotFound { get; init; }
        public RefreshMealAllowanceRequest? RefreshRequest { get; private set; }
        public UpdateMealAllowanceManualValuesRequest? ManualValuesRequest { get; private set; }
        public SetMealAllowanceLockStateBatchRequest? LockStateRequest { get; private set; }

        public Task<RefreshMealAllowanceResult> RefreshAsync(RefreshMealAllowanceRequest request, CancellationToken cancellationToken = default)
        {
            RefreshRequest = request;
            if(ThrowConflict)
            {
                throw new MealAllowanceConflictException("conflict");
            }

            return Task.FromResult(new RefreshMealAllowanceResult(6, 2026, 0, 0, 0, 0, 0, 0));
        }

        public Task<MealAllowanceListItemDto> UpdateManualValuesAsync(UpdateMealAllowanceManualValuesRequest request, CancellationToken cancellationToken = default)
        {
            ManualValuesRequest = request;
            if(ThrowConflict)
            {
                throw new MealAllowanceConflictException("conflict");
            }
            if(ThrowNotFound)
            {
                throw new KeyNotFoundException("not found");
            }

            return Task.FromResult(new MealAllowanceListItemDto(
                request.Id,
                Guid.NewGuid(),
                "NV001",
                "Nhân viên kiểm thử",
                null,
                null,
                6,
                2026,
                request.QualifiedMealDays,
                0,
                MealAllowancePolicy.DefaultMealAllowancePerQualifiedDay,
                MealAllowancePolicy.CalculateAllowanceAmount(new MealAllowanceAmountInput(
                    request.QualifiedMealDays,
                    MealAllowancePolicy.DefaultMealAllowancePerQualifiedDay)),
                MealAllowancePolicy.ManualAdjustmentRuleCode,
                MealAllowancePolicy.ManualAdjustmentRuleVersion,
                request.Note,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                DateTime.UtcNow));
        }

        public Task<SetMealAllowanceLockStateBatchResult> SetLockStateBatchAsync(SetMealAllowanceLockStateBatchRequest request, CancellationToken cancellationToken = default)
        {
            LockStateRequest = request;
            if(ThrowConflict)
            {
                throw new MealAllowanceConflictException("conflict");
            }

            return Task.FromResult(new SetMealAllowanceLockStateBatchResult(
                request.PayrollYear,
                request.PayrollMonth,
                request.RecordIds?.Count ?? 0,
                request.RecordIds?.Count ?? 0));
        }

    }
}
