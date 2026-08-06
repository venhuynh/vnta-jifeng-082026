using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vnta.Hrm.Web.Security;
using Vnta.Hrm.Web.Client.Services.DataProviders.NhanSu.NhanVien;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapChuyenCan;
using Vnta.Hrm.Application.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class SecurityBoundaryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestConnectionString = "Host=localhost;Port=5432;Database=vnta_security_test;Username=test;Password=test";
    private const string TestGatewayKey = "test-hmac-key";
    private readonly WebApplicationFactory<Program> factory;

    public SecurityBoundaryTests(WebApplicationFactory<Program> factory)
    {
        ConfigureTestEnvironment(requireMutualTls: false);
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
    [InlineData("/api/payroll/basic-salaries")]
    [InlineData("/api/attendance/devices")]
    [InlineData("/api/adms/device-commands/lookup-options")]
    public async Task Sensitive_api_requires_authentication(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Adms_monitor_hub_requires_authentication()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsync(
            "/hubs/adms-monitor/negotiate?negotiateVersion=1",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Nhan_vien_api_requires_authentication()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsync(
            "/api/nhan-su/nhan-vien/search-page",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Nhan_vien_api_forbids_attendance_administrator_without_hr_role()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "AttendanceAdmin");

        var response = await client.PostAsync(
            "/api/nhan-su/nhan-vien/search-page",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("AttendanceAdmin", HttpStatusCode.Forbidden)]
    public async Task Nhan_su_workbook_preview_requires_human_resources_administration(
        string? role,
        HttpStatusCode expectedStatusCode)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        using var response = await client.PostAsync(
            "/api/nhan-su/nhan-vien/nhansu-workbook-preview",
            new ByteArrayContent([0x50, 0x4B]));

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/nhan-su/chi-tiet-nhan-vien/11111111-1111-1111-1111-111111111111/contact-profile")]
    [InlineData("PUT", "/api/nhan-su/chi-tiet-nhan-vien/11111111-1111-1111-1111-111111111111/contact-profile")]
    [InlineData("GET", "/api/nhan-su/chi-tiet-nhan-vien/11111111-1111-1111-1111-111111111111/citizen-identity")]
    [InlineData("PUT", "/api/nhan-su/chi-tiet-nhan-vien/11111111-1111-1111-1111-111111111111/citizen-identity")]
    public async Task Employee_personal_information_endpoints_require_authentication(string method, string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/nhan-su/chi-tiet-nhan-vien/11111111-1111-1111-1111-111111111111/contact-profile")]
    [InlineData("/api/nhan-su/chi-tiet-nhan-vien/11111111-1111-1111-1111-111111111111/citizen-identity")]
    public async Task Employee_personal_information_endpoints_forbid_user_without_hr_role(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public void Employee_personal_information_rows_are_excluded_from_audit_payloads()
    {
        var auditPolicy = new AuditPolicy();

        Assert.Null(auditPolicy.GetPolicy(typeof(EmployeeContactProfileRow)));
        Assert.Null(auditPolicy.GetPolicy(typeof(CitizenIdentityRow)));
    }

    [Fact]
    public void Nhan_vien_data_provider_is_registered_for_interactive_server()
    {
        using var scope = factory.Services.CreateScope();

        var provider = scope.ServiceProvider.GetRequiredService<NhanVienDataProvider>();

        Assert.NotNull(provider);
    }

    [Fact]
    public void Nhan_su_workbook_preview_service_is_registered()
    {
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<INhanSuWorkbookPreviewService>());
    }

    [Fact]
    public void Attendance_allowance_feature_provider_is_registered_for_interactive_server()
    {
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AttendanceAllowanceResultDataProvider>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAttendanceAllowanceResultDataProvider>());
    }

    [Fact]
    public void Attendance_status_code_provider_is_registered_for_interactive_server()
    {
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AttendanceStatusCodeDataProvider>());
    }

    [Theory]
    [InlineData("/api/payroll/basic-salaries")]
    [InlineData("/api/attendance/devices")]
    [InlineData("/api/adms/device-commands/lookup-options")]
    public async Task Sensitive_api_forbids_an_authenticated_user_without_required_role(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Employee", HttpStatusCode.Forbidden)]
    public async Task Attendance_allowance_actual_workday_endpoint_requires_payroll_administration(
        string? role,
        HttpStatusCode expectedStatusCode)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        if(role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        using var response = await client.PostAsync(
            "/api/payroll/attendance-allowance/actual-workday",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Employee", HttpStatusCode.Forbidden)]
    public async Task Attendance_allowance_refresh_endpoint_requires_payroll_administration(
        string? role,
        HttpStatusCode expectedStatusCode)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        if(role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        using var response = await client.PostAsync(
            "/api/payroll/attendance-allowance/refresh",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Employee", HttpStatusCode.Forbidden)]
    public async Task Attendance_allowance_rule_endpoint_requires_payroll_administration(
        string? role,
        HttpStatusCode expectedStatusCode)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        if(role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        using var response = await client.GetAsync("/api/payroll/attendance-allowance/rule");

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task Adms_monitor_hub_allows_a_device_administrator_to_negotiate()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "AttendanceAdmin");

        var response = await client.PostAsync(
            "/hubs/adms-monitor/negotiate?negotiateVersion=1",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Gateway_inbound_requires_hmac_signature()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsync(
            "/api/integration/adms/realtime/events",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Gateway_inbound_requires_a_client_certificate_when_mutual_tls_is_enabled()
    {
        ConfigureTestEnvironment(requireMutualTls: true);
        try
        {
            using var mutualTlsFactory = factory.WithWebHostBuilder(_ => { });
            using var client = mutualTlsFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.PostAsync(
                "/api/integration/adms/realtime/events",
                content: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            ConfigureTestEnvironment(requireMutualTls: false);
        }
    }

    [Fact]
    public async Task Gateway_inbound_rejects_a_replayed_signed_request()
    {
        var gatewaySecurity = factory.Services
            .GetRequiredService<IOptions<GatewayInboundSecurityOptions>>()
            .Value;
        Assert.False(gatewaySecurity.RequireMutualTls);
        Assert.Equal(TestGatewayKey, gatewaySecurity.Keys["gateway-test"]);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var body = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Id = "event-1",
            FlowId = "flow-1",
            ConnectionId = "connection-1",
            Sn = "device-1",
            DeviceName = "Test device",
            RequestMethod = "POST",
            RequestUrl = "/iclock/cdata",
            Direction = "inbound",
            EventType = "test-event",
            RawBody = "test",
            LogStatus = "accepted",
            RejectionReason = (string?)null,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
            SummaryText = "test",
            IsSemantic = false
        });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");

        using var firstResponse = await client.SendAsync(CreateSignedGatewayRequest(body, timestamp, nonce));
        using var secondResponse = await client.SendAsync(CreateSignedGatewayRequest(body, timestamp, nonce));

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
    }

    private static HttpRequestMessage CreateSignedGatewayRequest(
        byte[] body,
        string timestamp,
        string nonce)
    {
        const string path = "/api/integration/adms/realtime/events";
        var bodyHash = Convert.ToHexString(SHA256.HashData(body));
        var canonicalRequest = string.Join('\n', "POST", path, timestamp, nonce, bodyHash);
        var signature = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("test-hmac-key"),
            Encoding.UTF8.GetBytes(canonicalRequest)));
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new ByteArrayContent(body)
        };

        request.Content.Headers.ContentType = new("application/json");
        request.Headers.Add("X-VNTA-Key-Id", "gateway-test");
        request.Headers.Add("X-VNTA-Timestamp", timestamp);
        request.Headers.Add("X-VNTA-Nonce", nonce);
        request.Headers.Add("X-VNTA-Signature", signature);

        return request;
    }

    private static void ConfigureTestEnvironment(bool requireMutualTls)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", TestConnectionString);
        Environment.SetEnvironmentVariable("IntegrationSecurity__GatewayInbound__Keys__gateway-test", TestGatewayKey);
        Environment.SetEnvironmentVariable(
            "IntegrationSecurity__GatewayInbound__RequireMutualTls",
            requireMutualTls ? "true" : "false");
    }
}
