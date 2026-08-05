using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class PayrollInsuranceDeductionEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PayrollInsuranceDeductionEndpointContractTests(WebApplicationFactory<Program> factory)
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
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Employee", HttpStatusCode.Forbidden)]
    public async Task Manual_adjustment_requires_payroll_administration(string? role, HttpStatusCode expectedStatusCode)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        using var response = await client.PostAsync(
            "/api/payroll/social-health-insurance-deductions/manual-values",
            JsonContent("{}"));

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task Manual_adjustment_maps_concurrency_conflict_to_http_409()
    {
        using var client = CreatePayrollAdminClient(new CapturingService { ThrowConcurrencyConflict = true });

        using var response = await client.PostAsync(
            "/api/payroll/social-health-insurance-deductions/manual-values",
            JsonContent("""
                {
                  "payrollDeductionSummaryRecordId":"11111111-1111-1111-1111-111111111111",
                  "insuranceSalaryBaseAmount":10000000,
                  "socialInsuranceRate":0.08,
                  "healthInsuranceRate":0.015,
                  "unemploymentInsuranceRate":0.01,
                  "isParticipating":true,
                  "participationChangeType":0,
                  "originalUpdatedAtUtc":"2026-07-22T00:00:00"
                }
                """));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Employee", HttpStatusCode.Forbidden)]
    public async Task Lock_state_change_requires_payroll_administration(string? role, HttpStatusCode expectedStatusCode)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        using var response = await client.PostAsync(
            "/api/payroll/social-health-insurance-deductions/lock-state",
            JsonContent("{}"));

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task Lock_state_change_maps_concurrency_conflict_to_http_409()
    {
        using var client = CreatePayrollAdminClient(new CapturingService { ThrowLockConcurrencyConflict = true });

        using var response = await client.PostAsync(
            "/api/payroll/social-health-insurance-deductions/lock-state",
            JsonContent("""
                {
                  "payrollDeductionSummaryRecordId":"11111111-1111-1111-1111-111111111111",
                  "isLocked":true,
                  "originalUpdatedAtUtc":"2026-07-22T00:00:00"
                }
                """));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Employee", HttpStatusCode.Forbidden)]
    public async Task Batch_lock_state_change_requires_payroll_administration(string? role, HttpStatusCode expectedStatusCode)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        using var response = await client.PostAsync(
            "/api/payroll/social-health-insurance-deductions/lock-state/batch",
            JsonContent("{}"));

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task Batch_lock_state_change_maps_concurrency_conflict_to_http_409()
    {
        using var client = CreatePayrollAdminClient(new CapturingService { ThrowBatchLockConcurrencyConflict = true });

        using var response = await client.PostAsync(
            "/api/payroll/social-health-insurance-deductions/lock-state/batch",
            JsonContent("""
                {
                  "payrollYear":2026,
                  "payrollMonth":7,
                  "isLocked":true,
                  "payrollDeductionSummaryRecordIds":null
                }
                """));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Search_returns_page_with_total_count_and_forwards_paging_filter()
    {
        var service = new CapturingService
        {
            SearchResult = new PayrollInsuranceDeductionPageDto([], 123)
        };
        using var client = CreatePayrollAdminClient(service);

        using var response = await client.PostAsync(
            "/api/payroll/social-health-insurance-deductions/search",
            JsonContent("""
                {
                  "payrollMonth":7,
                  "payrollYear":2026,
                  "searchText":"NV001",
                  "skip":100,
                  "take":50
                }
                """));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PayrollInsuranceDeductionPageDto>();
        Assert.NotNull(page);
        Assert.Equal(123, page.TotalCount);
        Assert.Equal(100, service.ReceivedSearchFilter?.Skip);
        Assert.Equal(50, service.ReceivedSearchFilter?.Take);
    }

    private HttpClient CreatePayrollAdminClient(CapturingService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPayrollInsuranceDeductionReadService>();
                services.RemoveAll<IPayrollInsuranceDeductionRefreshService>();
                services.RemoveAll<IPayrollInsuranceDeductionPreviousMonthSyncService>();
                services.RemoveAll<IPayrollInsuranceDeductionManualAdjustmentService>();
                services.RemoveAll<IPayrollInsuranceDeductionLockService>();
                services.RemoveAll<IPayrollInsuranceDeductionLegacyWriteService>();
                services.AddSingleton<IPayrollInsuranceDeductionReadService>(service);
                services.AddSingleton<IPayrollInsuranceDeductionRefreshService>(service);
                services.AddSingleton<IPayrollInsuranceDeductionPreviousMonthSyncService>(service);
                services.AddSingleton<IPayrollInsuranceDeductionManualAdjustmentService>(service);
                services.AddSingleton<IPayrollInsuranceDeductionLockService>(service);
                services.AddSingleton<IPayrollInsuranceDeductionLegacyWriteService>(service);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    private sealed class CapturingService :
        IPayrollInsuranceDeductionReadService,
        IPayrollInsuranceDeductionRefreshService,
        IPayrollInsuranceDeductionPreviousMonthSyncService,
        IPayrollInsuranceDeductionManualAdjustmentService,
        IPayrollInsuranceDeductionLockService,
        IPayrollInsuranceDeductionLegacyWriteService
    {
        public bool ThrowConcurrencyConflict { get; init; }
        public bool ThrowLockConcurrencyConflict { get; init; }
        public bool ThrowBatchLockConcurrencyConflict { get; init; }
        public PayrollInsuranceDeductionFilter? ReceivedSearchFilter { get; private set; }
        public PayrollInsuranceDeductionPageDto SearchResult { get; init; } = new([], 0);

        public Task<PayrollInsuranceDeductionPageDto> SearchAsync(PayrollInsuranceDeductionFilter filter, CancellationToken cancellationToken = default)
        {
            ReceivedSearchFilter = filter;
            return Task.FromResult(SearchResult);
        }

        public Task<RefreshPayrollInsuranceDeductionResult> RefreshAsync(RefreshPayrollInsuranceDeductionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SyncPayrollInsuranceDeductionFromPreviousMonthResult> SyncFromPreviousMonthAsync(SyncPayrollInsuranceDeductionFromPreviousMonthRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PayrollInsuranceDeductionListItemDto> UpdateManualValuesAsync(UpdatePayrollInsuranceDeductionManualValuesRequest request, CancellationToken cancellationToken = default)
        {
            if (ThrowConcurrencyConflict)
            {
                throw new PayrollInsuranceDeductionConcurrencyException("conflict");
            }

            throw new NotSupportedException();
        }

        public Task<PayrollInsuranceDeductionListItemDto> SetLockStateAsync(SetPayrollInsuranceDeductionLockStateRequest request, CancellationToken cancellationToken = default)
        {
            if (ThrowLockConcurrencyConflict)
            {
                throw new PayrollInsuranceDeductionConcurrencyException("conflict");
            }

            throw new NotSupportedException();
        }

        public Task<SetPayrollInsuranceDeductionBatchLockStateResult> SetLockStateBatchAsync(SetPayrollInsuranceDeductionBatchLockStateRequest request, CancellationToken cancellationToken = default)
        {
            if (ThrowBatchLockConcurrencyConflict)
            {
                throw new PayrollInsuranceDeductionConcurrencyException("conflict");
            }

            throw new NotSupportedException();
        }

        public Task<string?> ValidateAsync(UpsertPayrollInsuranceDeductionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PayrollInsuranceDeductionListItemDto> SaveAsync(UpsertPayrollInsuranceDeductionRequest request, bool isNew, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
