using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapTrachNhiem;

public sealed class ResponsibilityAllowanceEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string BasePath = "/api/payroll/responsibility-allowance";
    private readonly WebApplicationFactory<Program> factory;
    private readonly List<WebApplicationFactory<Program>> customizedFactories = [];

    public ResponsibilityAllowanceEndpointContractTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Port=5432;Database=vnta_security_test;Username=test;Password=test");

        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services => services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestHeaderAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestHeaderAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme = TestHeaderAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestHeaderAuthenticationHandler>(
                    TestHeaderAuthenticationHandler.SchemeName,
                    _ => { }));
        });
    }

    [Theory]
    [InlineData("/grade-config/grades")]
    [InlineData("/employee-assignments/recalculate")]
    [InlineData("/search")]
    [InlineData("/lock-state/batch")]
    public async Task Feature_routes_enforce_payroll_administration_at_the_http_boundary(string route)
    {
        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var employee = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        employee.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        using var anonymousResponse = await anonymous.PostAsync(BasePath + route, JsonContent("{}"));
        using var employeeResponse = await employee.PostAsync(BasePath + route, JsonContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, employeeResponse.StatusCode);
    }

    [Fact]
    public async Task Grade_command_rejects_a_null_body_before_calling_the_application_capability()
    {
        var service = new CapturingGradeConfigurationService();
        using var client = CreatePayrollAdminClient(service);

        using var response = await client.PostAsync(BasePath + "/grade-config/grades", JsonContent("null"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(service.SaveWasCalled);
    }

    [Fact]
    public async Task Grade_command_uses_authenticated_principal_for_audit_when_payload_spoofs_actor()
    {
        var service = new CapturingGradeConfigurationService();
        var auditScope = new RecordingAuditScope();
        using var client = CreatePayrollAdminClient(service, auditScope);

        using var response = await client.PostAsync(BasePath + "/grade-config/grades", JsonContent(ValidGradeJson("forged")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(service.SaveWasCalled);
        var audit = Assert.Single(auditScope.Commands);
        Assert.Equal("security-boundary-test-user", audit.Actor.ActorId);
        Assert.Equal(response.Headers.GetValues("X-Correlation-Id").Single(), audit.CorrelationId);
    }

    [Fact]
    public async Task Grade_command_maps_concurrency_conflict_to_409()
    {
        using var client = CreatePayrollAdminClient(new CapturingGradeConfigurationService { ThrowConflict = true });

        using var response = await client.PostAsync(BasePath + "/grade-config/grades", JsonContent(ValidGradeJson()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Grade_command_maps_application_validation_to_400()
    {
        using var client = CreatePayrollAdminClient(new CapturingGradeConfigurationService { ThrowValidation = true });

        using var response = await client.PostAsync(BasePath + "/grade-config/grades", JsonContent(ValidGradeJson()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreatePayrollAdminClient(
        CapturingGradeConfigurationService service,
        RecordingAuditScope? auditScope = null)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPayrollResponsibilityAllowanceGradeConfigurationWriteService>();
            services.AddSingleton<IPayrollResponsibilityAllowanceGradeConfigurationWriteService>(service);
            if (auditScope is not null)
            {
                services.RemoveAll<IAuditScope>();
                services.AddSingleton<IAuditScope>(auditScope);
            }
        }));
        customizedFactories.Add(customizedFactory);

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    public void Dispose()
    {
        foreach (var customizedFactory in customizedFactories)
        {
            customizedFactory.Dispose();
        }
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    private static string ValidGradeJson(string? actor = null) =>
        $$"""{"year":2026,"month":7,"code":"TN01","name":"Responsibility","standardResponsibilityAllowanceAmount":1000,"displayOrder":1,"isActive":true,"note":"test","actor":"{{actor}}","requestedBy":"{{actor}}"}""";

    private sealed class CapturingGradeConfigurationService : IPayrollResponsibilityAllowanceGradeConfigurationWriteService
    {
        public bool ThrowConflict { get; init; }
        public bool ThrowValidation { get; init; }
        public bool SaveWasCalled { get; private set; }

        public Task<PayrollResponsibilityAllowanceGradeDto> SaveGradeAsync(
            SavePayrollResponsibilityAllowanceGradeRequest request,
            CancellationToken cancellationToken = default)
        {
            SaveWasCalled = true;
            if (ThrowConflict)
            {
                throw new ResponsibilityAllowanceConflictException("stale version");
            }
            if (ThrowValidation)
            {
                throw new InvalidOperationException("invalid grade");
            }

            return Task.FromResult(new PayrollResponsibilityAllowanceGradeDto(
                request.Id ?? Guid.NewGuid(), request.Year, request.Month, request.Code, request.Name,
                request.StandardResponsibilityAllowanceAmount, request.DisplayOrder, request.IsActive, request.Note));
        }

        public Task<PayrollResponsibilityAllowanceGradePositionDto> SaveMappingAsync(
            SavePayrollResponsibilityAllowanceGradePositionRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PayrollResponsibilityAllowanceGradePositionDto> DeactivateMappingAsync(
            Guid id,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PayrollResponsibilityAllowanceConfigCopyResult> CopyFromPreviousMonthAsync(
            int year,
            int month,
            bool copyMappings,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingAuditScope : IAuditScope
    {
        public List<AuditCommand> Commands { get; } = [];
        public AuditCommand? Current { get; private set; }

        public IDisposable Begin(AuditCommand command)
        {
            var previous = Current;
            Current = command;
            Commands.Add(command);
            return new ScopeLease(this, previous);
        }

        public void RefineAction(string finalAction) { }
        public void SetOperationOutcome(AuditOperationOutcome outcome) { }

        private sealed class ScopeLease(RecordingAuditScope owner, AuditCommand? previous) : IDisposable
        {
            public void Dispose() => owner.Current = previous;
        }
    }
}
