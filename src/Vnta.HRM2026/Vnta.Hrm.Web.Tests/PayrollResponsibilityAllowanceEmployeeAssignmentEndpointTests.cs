using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class PayrollResponsibilityAllowanceEmployeeAssignmentEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PayrollResponsibilityAllowanceEmployeeAssignmentEndpointTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", "Host=localhost;Port=5432;Database=vnta_security_test;Username=test;Password=test");
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services => services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestHeaderAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestHeaderAuthenticationHandler.SchemeName;
                options.DefaultForbidScheme = TestHeaderAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>(TestHeaderAuthenticationHandler.SchemeName, _ => { }));
        });
    }

    [Fact]
    public async Task Search_forwards_server_paging_and_returns_page_contract()
    {
        var service = new CapturingAssignmentQueryService();
        using var client = CreateClient(service);
        var request = new PayrollResponsibilityAllowanceEmployeeAssignmentQuery(2026, 7, "NV001", "assigned", 50, 50);

        var response = await client.PostAsJsonAsync("/api/payroll/responsibility-allowance/employee-assignments/search", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(request, service.Query);
        var page = await response.Content.ReadFromJsonAsync<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto>();
        Assert.NotNull(page);
        Assert.Equal(101, page.TotalCount);
        Assert.Equal(67, page.Summary.AssignedCount);
        Assert.Single(page.Rows);
    }

    [Fact]
    public async Task View_forwards_sync_then_display_request_to_dedicated_workflow()
    {
        var service = new CapturingEmployeeAssignmentViewService();
        using var client = CreateClient(service);
        var request = new XemPhuCapTrachNhiemGanNhanVienRequest(2026, 7, "NV001", "assigned", 0, 50);

        var response = await client.PostAsJsonAsync(
            "/api/payroll/responsibility-allowance/employee-assignments/view",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(request, service.Request);
        var result = await response.Content.ReadFromJsonAsync<XemPhuCapTrachNhiemGanNhanVienResult>();
        Assert.NotNull(result);
        Assert.Equal(101, result.Page.TotalCount);
        Assert.Equal(101, result.Synchronization.TotalEmployees);
        Assert.True(Assert.Single(result.Page.Rows).IsAssignGradeFromPosition);
    }

    [Fact]
    public async Task Export_forwards_requested_format_and_returns_allowlisted_row()
    {
        var service = new CapturingAssignmentQueryService();
        using var client = CreateClient(service);
        var request = new PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest(2026, 7, "xlsx");

        var response = await client.PostAsJsonAsync("/api/payroll/responsibility-allowance/employee-assignments/export", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(request, service.ExportRequest);
        var rows = await response.Content.ReadFromJsonAsync<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>>();
        var row = Assert.Single(rows ?? []);
        Assert.Equal("NV001", row.EmployeeCode);
        Assert.Equal("Manual", row.AssignmentSource);
    }

    private HttpClient CreateClient(CapturingAssignmentQueryService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
                services.RemoveAll<IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService>();
                services.AddSingleton<IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService>(service);
                services.RemoveAll<IPayrollResponsibilityAllowanceEmployeeAssignmentExportService>();
                services.AddSingleton<IPayrollResponsibilityAllowanceEmployeeAssignmentExportService>(service);
        }));
        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private HttpClient CreateClient(CapturingEmployeeAssignmentViewService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPhuCapTrachNhiemGanNhanVienXemService>();
            services.AddSingleton<IPhuCapTrachNhiemGanNhanVienXemService>(service);
        }));
        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private sealed class CapturingEmployeeAssignmentViewService : IPhuCapTrachNhiemGanNhanVienXemService
    {
        public XemPhuCapTrachNhiemGanNhanVienRequest? Request { get; private set; }

        public Task<XemPhuCapTrachNhiemGanNhanVienResult> ExecuteAsync(
            XemPhuCapTrachNhiemGanNhanVienRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            var row = new PayrollResponsibilityAllowanceEmployeeAssignmentDto(
                Guid.NewGuid(), request.Year, request.Month, Guid.NewGuid(), "NV001", "Nhân viên kiểm thử",
                Guid.NewGuid(), "Kiểm thử viên", Guid.NewGuid(), "TN01", "Trách nhiệm 01",
                1_000_000m, true, "position-default", null, DateTime.UtcNow);
            var page = new PayrollResponsibilityAllowanceEmployeeAssignmentPageDto(
                [row], 101, new PayrollResponsibilityAllowanceEmployeeAssignmentSummaryDto(101, 67, 34), []);
            return Task.FromResult(new XemPhuCapTrachNhiemGanNhanVienResult(
                new PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult(request.Year, request.Month, 101, 67),
                page));
        }
    }

    private sealed class CapturingAssignmentQueryService : IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService, IPayrollResponsibilityAllowanceEmployeeAssignmentExportService
    {
        public PayrollResponsibilityAllowanceEmployeeAssignmentQuery? Query { get; private set; }
        public PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest? ExportRequest { get; private set; }

        public Task<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto> SearchEmployeeAssignmentsAsync(PayrollResponsibilityAllowanceEmployeeAssignmentQuery query, CancellationToken cancellationToken = default)
        {
            Query = query;
            var row = new PayrollResponsibilityAllowanceEmployeeAssignmentDto(Guid.NewGuid(), query.Year, query.Month, Guid.NewGuid(), "NV001", "Nhân viên kiểm thử", Guid.NewGuid(), "Kiểm thử viên", Guid.NewGuid(), "TN01", "Trách nhiệm 01", 1_000_000m, false, "Manual", null, DateTime.UtcNow);
            return Task.FromResult(new PayrollResponsibilityAllowanceEmployeeAssignmentPageDto([row], 101, new PayrollResponsibilityAllowanceEmployeeAssignmentSummaryDto(101, 67, 34), []));
        }

        public Task<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>> ExportEmployeeAssignmentsAsync(PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest request, CancellationToken cancellationToken = default)
        {
            ExportRequest = request;
            return Task.FromResult<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>>([new("NV001", "Nhân viên kiểm thử", "Kiểm thử viên", "TN01", "Trách nhiệm 01", 1_000_000m, "Manual")]);
        }
    }
}
