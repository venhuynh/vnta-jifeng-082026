using System.Net;
using System.Net.Http.Json;
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

public sealed class ResponsibilityPositionAssignmentEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BasePath = "/api/payroll/responsibility-position-assignments";
    private readonly WebApplicationFactory<Program> factory;

    public ResponsibilityPositionAssignmentEndpointTests(WebApplicationFactory<Program> factory)
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
    [InlineData("")]
    [InlineData("/search")]
    public async Task Position_assignment_routes_require_payroll_administration(string route)
    {
        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var employee = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        employee.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "Employee");

        using var anonymousResponse = await anonymous.PostAsJsonAsync(BasePath + route, new { });
        using var employeeResponse = await employee.PostAsJsonAsync(BasePath + route, new { });

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, employeeResponse.StatusCode);
    }

    [Fact]
    public async Task Save_rejects_missing_payload_without_invoking_the_command()
    {
        var service = new CapturingCommandService();
        var (customizedFactory, client) = CreatePayrollAdminClient(services =>
        {
            services.RemoveAll<IResponsibilityPositionAssignmentCommandService>();
            services.AddSingleton<IResponsibilityPositionAssignmentCommandService>(service);
        });
        using (customizedFactory)
        using (client)
        {
            using var response = await client.PostAsJsonAsync<object?>(BasePath, null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(service.SaveWasCalled);
        }
    }

    [Fact]
    public async Task Search_forwards_filter_and_server_paging_to_the_feature_query()
    {
        var service = new CapturingReadService();
        var (customizedFactory, client) = CreatePayrollAdminClient(services =>
        {
            services.RemoveAll<IResponsibilityPositionAssignmentReadService>();
            services.AddSingleton<IResponsibilityPositionAssignmentReadService>(service);
        });
        using (customizedFactory)
        using (client)
        {
            var query = new ResponsibilityPositionAssignmentQuery(2026, 7, "Trưởng phòng", 20, 10);

            using var response = await client.PostAsJsonAsync(BasePath + "/search", query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(query, service.SearchRequest);
            var page = await response.Content.ReadFromJsonAsync<ResponsibilityPositionAssignmentPageDto>();
            Assert.NotNull(page);
            Assert.Equal(31, page.TotalCount);
            Assert.Single(page.Rows);
        }
    }

    [Fact]
    public async Task Save_uses_authenticated_actor_not_a_spoofed_body_field_and_maps_stale_write_to_409()
    {
        var service = new CapturingCommandService { ThrowConflict = true };
        var auditScope = new RecordingAuditScope();
        var (customizedFactory, client) = CreatePayrollAdminClient(services =>
        {
            services.RemoveAll<IResponsibilityPositionAssignmentCommandService>();
            services.AddSingleton<IResponsibilityPositionAssignmentCommandService>(service);
            services.RemoveAll<IAuditScope>();
            services.AddSingleton<IAuditScope>(auditScope);
        });
        using (customizedFactory)
        using (client)
        {
            var request = new SaveResponsibilityPositionAssignmentRequest(
                null, 2026, 7, Guid.NewGuid(), Guid.NewGuid(), true, "mapping", DateTime.UtcNow);
            using var message = new HttpRequestMessage(HttpMethod.Post, BasePath)
            {
                Content = JsonContent.Create(new
                {
                    request.Id,
                    request.Year,
                    request.Month,
                    request.GradeId,
                    request.PositionId,
                    request.IsActive,
                    request.Note,
                    request.OriginalUpdatedAtUtc,
                    actor = "forged-client-actor"
                })
            };

            using var response = await client.SendAsync(message);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(request, service.SaveRequest);
            var audit = Assert.Single(auditScope.Commands);
            Assert.Equal("security-boundary-test-user", audit.Actor.ActorId);
        }
    }

    private (WebApplicationFactory<Program> Factory, HttpClient Client) CreatePayrollAdminClient(Action<IServiceCollection> configureServices)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(configureServices));
        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return (customizedFactory, client);
    }

    private sealed class CapturingReadService : IResponsibilityPositionAssignmentReadService
    {
        public ResponsibilityPositionAssignmentQuery? SearchRequest { get; private set; }

        public Task<ResponsibilityPositionAssignmentPageDto> SearchPageAsync(
            ResponsibilityPositionAssignmentQuery query,
            CancellationToken cancellationToken = default)
        {
            SearchRequest = query;
            return Task.FromResult(new ResponsibilityPositionAssignmentPageDto(
                [CreateItem()], 31));
        }

        public Task<IReadOnlyList<ResponsibilityPositionAssignmentGradeOptionDto>> GetGradeOptionsAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ResponsibilityPositionAssignmentGradeOptionDto>>([]);
    }

    private sealed class CapturingCommandService : IResponsibilityPositionAssignmentCommandService
    {
        public bool ThrowConflict { get; init; }
        public bool SaveWasCalled { get; private set; }
        public SaveResponsibilityPositionAssignmentRequest? SaveRequest { get; private set; }

        public Task<ResponsibilityPositionAssignmentItemDto> SaveAsync(
            SaveResponsibilityPositionAssignmentRequest request,
            CancellationToken cancellationToken = default)
        {
            SaveWasCalled = true;
            SaveRequest = request;
            if (ThrowConflict)
            {
                throw new ResponsibilityPositionAssignmentConflictException("stale mapping");
            }

            return Task.FromResult(CreateItem());
        }

        public Task<ResponsibilityPositionAssignmentItemDto> DeactivateAsync(
            DeactivateResponsibilityPositionAssignmentRequest request,
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

    private static ResponsibilityPositionAssignmentItemDto CreateItem() => new(
        Guid.NewGuid(), 2026, 7, Guid.NewGuid(), "TN01", "Responsibility", Guid.NewGuid(), "TP", "Manager", true,
        "mapping", DateTime.UtcNow, DateTime.UtcNow);
}
