using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.ChamCong.DashboardBangChamCong;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class AttendanceTimesheetDashboardEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AttendanceTimesheetDashboardEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Dashboard_requires_attendance_administration_role()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync(
            "/api/attendance/timesheet-dashboard",
            new AttendanceTimesheetDashboardFilter(7, 2026));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_uses_attendance_contract_for_selected_period()
    {
        var service = new CapturingDashboardService();
        using var client = CreateClient(service);
        var filter = new AttendanceTimesheetDashboardFilter(7, 2026);

        var response = await client.PostAsJsonAsync("/api/attendance/timesheet-dashboard", filter);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(filter, service.Filter);
        var result = await response.Content.ReadFromJsonAsync<AttendanceTimesheetDashboardDto>();
        Assert.NotNull(result);
        Assert.Equal(7, result.WorkMonth);
        Assert.Equal(12, result.Overview.OvertimeMinutes);
        Assert.Equal("Đi làm", result.StatusBreakdown.Single().Status);
        Assert.Equal("NV001", result.Exceptions.Single().EmployeeCode);
    }

    private HttpClient CreateClient(CapturingDashboardService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAttendanceTimesheetDashboardService>();
                services.AddSingleton<IAttendanceTimesheetDashboardService>(service);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "AttendanceAdmin");
        return client;
    }

    private sealed class CapturingDashboardService : IAttendanceTimesheetDashboardService
    {
        public AttendanceTimesheetDashboardFilter? Filter { get; private set; }

        public Task<AttendanceTimesheetDashboardDto> GetDashboardAsync(
            AttendanceTimesheetDashboardFilter filter,
            CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return Task.FromResult(new AttendanceTimesheetDashboardDto(
                filter.WorkMonth,
                filter.WorkYear,
                new AttendanceTimesheetDashboardOverviewDto(4, 8, 12, 30),
                [new AttendanceTimesheetDashboardDailyTrendPointDto(new DateOnly(2026, 7, 1), 4, 12, 30)],
                [new AttendanceTimesheetDashboardStatusBreakdownDto("Đi làm", 8)],
                [new AttendanceTimesheetDashboardDepartmentDto("Khối sản xuất", 4, 8, 12, 30)],
                [new AttendanceTimesheetDashboardExceptionDto("NV001", "Nguyễn Văn A", "Khối sản xuất", 30, 12, true)]));
        }
    }
}
