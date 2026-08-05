using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class EmployeeTaxDependentEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public EmployeeTaxDependentEndpointContractTests(WebApplicationFactory<Program> factory)
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
    public async Task Save_requires_payroll_administration(string? role, HttpStatusCode expectedStatusCode)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, role);
        }

        using var response = await client.PostAsync("/api/payroll/tax-dependents", JsonContent("{}"));

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task Save_uses_the_authenticated_actor_instead_of_client_payload()
    {
        var service = new CapturingService();
        using var client = CreatePayrollAdminClient(service);

        using var response = await client.PostAsync("/api/payroll/tax-dependents", JsonContent(RequestJson(actor: "forged-user")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("security-boundary-test-user", service.ReceivedRequest?.Actor);
    }

    [Fact]
    public async Task Save_maps_concurrency_conflict_to_http_409()
    {
        using var client = CreatePayrollAdminClient(new CapturingService { ThrowConcurrencyConflict = true });

        using var response = await client.PostAsync("/api/payroll/tax-dependents", JsonContent(RequestJson()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private HttpClient CreatePayrollAdminClient(CapturingService service)
    {
        var customizedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmployeeTaxDependentService>();
                services.AddSingleton<IEmployeeTaxDependentService>(service);
            }));

        var client = customizedFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestHeaderAuthenticationHandler.RoleHeaderName, "PayrollAdmin");
        return client;
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    private static string RequestJson(string? actor = null) => $$"""
        {
          "id":"11111111-1111-1111-1111-111111111111",
          "employeeId":"22222222-2222-2222-2222-222222222222",
          "dependentFullName":"Nguyễn Văn B",
          "isFamilyDeductionRegistered":true,
          "originalUpdatedAtUtc":"2026-07-27T00:00:00Z",
          "actor":{{(actor is null ? "null" : $"\"{actor}\"")}}
        }
        """;

    private sealed class CapturingService : IEmployeeTaxDependentService
    {
        public bool ThrowConcurrencyConflict { get; init; }
        public SaveEmployeeTaxDependentRequest? ReceivedRequest { get; private set; }

        public Task<IReadOnlyList<EmployeeTaxDependentDto>> GetByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmployeeTaxDependentDto>>([]);

        public Task<EmployeeTaxDependentPageDto> SearchAsync(EmployeeTaxDependentFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(new EmployeeTaxDependentPageDto([], 0));

        public Task<EmployeeTaxDependentDto> SaveAsync(SaveEmployeeTaxDependentRequest request, CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            if (ThrowConcurrencyConflict)
            {
                throw new DbUpdateConcurrencyException("conflict");
            }

            return Task.FromResult(new EmployeeTaxDependentDto(
                request.Id,
                request.EmployeeId,
                request.EmployeeTaxCode,
                request.RegistrationDate,
                request.DependentFullName,
                request.DependentGender,
                request.DependentBirthDate,
                request.DependentIdentityNumber,
                request.DependentTaxCode,
                request.DependentNationality,
                request.EmployeeIdentityNumber,
                request.RelationshipToEmployee,
                request.IsFamilyDeductionRegistered,
                request.RegistrationBookNumber,
                request.RegistrationPageNumber,
                request.CountryName,
                request.OldWardCode,
                request.OldWardName,
                request.OldDistrictCode,
                request.OldDistrictName,
                request.OldProvinceCode,
                request.OldProvinceName,
                request.NewWardCode,
                request.NewWardName,
                request.NewDistrictCode,
                request.NewDistrictName,
                request.NewProvinceCode,
                request.NewProvinceName,
                request.DeductionFromMonth,
                request.DeductionToMonth,
                request.GhiChu,
                DateTime.UtcNow,
                DateTime.UtcNow));
        }
    }
}
