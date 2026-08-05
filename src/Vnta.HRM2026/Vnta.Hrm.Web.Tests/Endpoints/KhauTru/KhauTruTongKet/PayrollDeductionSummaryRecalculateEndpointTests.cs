using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class PayrollDeductionSummaryRecalculateEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PayrollDeductionSummaryRecalculateEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Recalculate_requires_payroll_administration_role(string? role, HttpStatusCode expectedStatusCode)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if(role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        using var response = await client.PostAsJsonAsync(
            "/api/payroll/deduction-summary/recalculate",
            new RecalculatePayrollDeductionSummaryPeriodRequest(2026, 7));

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task Recalculate_uses_applied_period_and_authenticated_actor()
    {
        var service = new CapturingDeductionSummaryService();
        using var client = CreatePayrollAdminClient(service);

        using var response = await client.PostAsJsonAsync(
            "/api/payroll/deduction-summary/recalculate",
            new RecalculatePayrollDeductionSummaryPeriodRequest(2026, 7, "untrusted-body-actor"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            new RecalculatePayrollDeductionSummaryPeriodRequest(2026, 7, "security-boundary-test-user"),
            service.RecalculateRequest);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Employee", HttpStatusCode.Forbidden)]
    public async Task Export_requires_payroll_administration_role(string? role, HttpStatusCode expectedStatusCode)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if(role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        using var response = await client.PostAsJsonAsync(
            "/api/payroll/deduction-summary/export",
            new PayrollDeductionSummaryExportRequest(2026, 7, PayrollDeductionSummaryExportFormat.Excel));

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task Export_uses_requested_period_and_format()
    {
        var service = new CapturingDeductionSummaryService();
        using var client = CreatePayrollAdminClient(service);

        using var response = await client.PostAsJsonAsync(
            "/api/payroll/deduction-summary/export",
            new PayrollDeductionSummaryExportRequest(2026, 7, PayrollDeductionSummaryExportFormat.Pdf));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((7, 2026, PayrollDeductionSummaryExportFormat.Pdf), service.ExportRequest);
    }

    [Fact]
    public async Task Sync_from_previous_month_uses_authenticated_actor_and_returns_attendance_metrics()
    {
        var service = new CapturingDeductionSummaryService();
        using var client = CreatePayrollAdminClient(service);

        using var response = await client.PostAsJsonAsync(
            "/api/payroll/deduction-summary/sync-previous-month",
            new SyncPayrollDeductionSummaryFromPreviousMonthRequest(7, 2026, "untrusted-body-actor"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            new SyncPayrollDeductionSummaryFromPreviousMonthRequest(7, 2026, "security-boundary-test-user"),
            service.SyncRequest);
        var result = await response.Content.ReadFromJsonAsync<SyncPayrollDeductionSummaryFromPreviousMonthResult>();
        Assert.NotNull(result);
        Assert.Equal(4, result.AttendanceEmployeeCount);
        Assert.Equal(1, result.RemovedCount);
    }

    private HttpClient CreatePayrollAdminClient(CapturingDeductionSummaryService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPayrollDeductionSummaryReadService>();
                services.RemoveAll<IPayrollDeductionSummaryExportService>();
                services.RemoveAll<IPayrollDeductionSummarySyncService>();
                services.RemoveAll<IPayrollDeductionSummaryRefreshService>();
                services.RemoveAll<IPayrollDeductionSummaryManualAdjustmentService>();
                services.RemoveAll<IPayrollDeductionSummaryLockService>();
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

    private sealed class CapturingDeductionSummaryService :
        IPayrollDeductionSummaryReadService,
        IPayrollDeductionSummaryExportService,
        IPayrollDeductionSummarySyncService,
        IPayrollDeductionSummaryRefreshService,
        IPayrollDeductionSummaryManualAdjustmentService,
        IPayrollDeductionSummaryLockService
    {
        public RecalculatePayrollDeductionSummaryPeriodRequest? RecalculateRequest { get; private set; }
        public SyncPayrollDeductionSummaryFromPreviousMonthRequest? SyncRequest { get; private set; }
        public (int PayrollMonth, int PayrollYear, PayrollDeductionSummaryExportFormat Format)? ExportRequest { get; private set; }

        public Task<PayrollDeductionSummaryPageDto> SearchAsync(
            PayrollDeductionSummaryFilter filter,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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
            SyncRequest = request;
            return Task.FromResult(new SyncPayrollDeductionSummaryFromPreviousMonthResult(
                SourcePayrollMonth: 6,
                SourcePayrollYear: 2026,
                TargetPayrollMonth: request.TargetPayrollMonth,
                TargetPayrollYear: request.TargetPayrollYear,
                SourceRecordCount: 2,
                CreatedCount: 2,
                UpdatedCount: 1,
                SkippedLockedCount: 1,
                AttendanceEmployeeCount: 4,
                RemovedCount: 1));
        }

        public Task<RefreshPayrollDeductionSummaryResult> RefreshAsync(
            RefreshPayrollDeductionSummaryRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<RecalculatePayrollDeductionSummaryPeriodResult> RecalculatePeriodAsync(
            RecalculatePayrollDeductionSummaryPeriodRequest request,
            CancellationToken cancellationToken = default)
        {
            RecalculateRequest = request;
            return Task.FromResult(new RecalculatePayrollDeductionSummaryPeriodResult(
                request.PayrollYear,
                request.PayrollMonth,
                TargetRowCount: 0,
                UpdatedCount: 0,
                UnchangedCount: 0,
                SkippedLockedCount: 0,
                MissingSourceCount: 0));
        }

        public Task<PayrollDeductionSummaryListItemDto> SetLockStateAsync(
            SetPayrollDeductionSummaryLockStateRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SetPayrollDeductionSummaryBatchLockStateResult> SetLockStateBatchAsync(
            SetPayrollDeductionSummaryBatchLockStateRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PayrollDeductionSummaryListItemDto> UpdateManualOtherDeductionAsync(
            UpdatePayrollDeductionSummaryManualOtherDeductionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
