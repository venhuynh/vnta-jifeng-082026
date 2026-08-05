using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class PayrollResponsibilityPerformanceBonusEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PayrollResponsibilityPerformanceBonusEndpointTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task Update_performance_bonus_for_period_passes_concurrency_tokens_to_workflow_service()
    {
        var service = CreateCapturingWorkflowService();
        using var client = CreateClient(service);
        var employeeId = Guid.NewGuid();
        var originalUpdatedAtUtc = DateTime.UtcNow;

        var response = await client.PostAsJsonAsync(
            "/api/payroll/responsibility-allowance/2026/7/performance-bonus",
            new
            {
                monthlyPerformanceBonusAmount = 0.9m,
                concurrencyTokens = new[]
                {
                    new PayrollResponsibilityAllowanceAbcConcurrencyToken(employeeId, originalUpdatedAtUtc)
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2026, service.Year);
        Assert.Equal(7, service.Month);
        Assert.Equal(0.9m, service.MonthlyPerformanceBonusAmount);
        var token = Assert.Single(service.ConcurrencyTokens!);
        Assert.Equal(employeeId, token.EmployeeId);
        Assert.Equal(originalUpdatedAtUtc, token.OriginalUpdatedAtUtc);
    }

    private HttpClient CreateClient(CapturingWorkflowService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPayrollResponsibilityAllowanceMonthlyAbcCommandService>();
                services.AddSingleton<IPayrollResponsibilityAllowanceMonthlyAbcCommandService>(
                    service.WorkflowService);
                services.RemoveAll<IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService>();
                services.AddSingleton<IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService>(
                    (IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService)service.WorkflowService);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private static CapturingWorkflowService CreateCapturingWorkflowService()
    {
        var workflowService = DispatchProxy.Create<
            IPayrollResponsibilityAllowanceMonthlyAbcCommandService,
            CapturingWorkflowService>();
        var recorder = (CapturingWorkflowService)(object)workflowService;
        recorder.WorkflowService = workflowService;
        return recorder;
    }

    private class CapturingWorkflowService : DispatchProxy
    {
        public IPayrollResponsibilityAllowanceMonthlyAbcCommandService WorkflowService { get; set; } = default!;
        public int Year { get; private set; }
        public int Month { get; private set; }
        public decimal MonthlyPerformanceBonusAmount { get; private set; }
        public IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? ConcurrencyTokens { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IPayrollResponsibilityAllowanceMonthlyAbcCommandService.UpdatePerformanceBonusForPeriodAsync)
                || args is null)
            {
                throw new NotSupportedException($"Unexpected workflow call: {targetMethod?.Name}.");
            }

            Year = (int)args[0]!;
            Month = (int)args[1]!;
            MonthlyPerformanceBonusAmount = (decimal)args[2]!;
            ConcurrencyTokens = (IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>?)args[3];

            return Task.FromResult(new UpdatePayrollResponsibilityPerformanceBonusForPeriodResult(
                Year,
                Month,
                ConcurrencyTokens?.Count ?? 0,
                ConcurrencyTokens?.Count ?? 0,
                0,
                0));
        }
    }
}
