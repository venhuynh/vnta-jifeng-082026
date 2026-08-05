using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Exceptions;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Endpoints;
using Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapChuyenCan;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class AttendanceAllowanceEndpointExecutionTests
{
    [Fact]
    public async Task Execute_async_uses_authenticated_http_actor_and_request_correlation()
    {
        var auditScope = new AsyncLocalAuditScope();
        var correlationScope = new AsyncLocalAuditCorrelationScope();
        using var correlationLease = correlationScope.Begin("attendance-api-correlation");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "attendance-api-user"),
                new Claim(ClaimTypes.Name, "Người dùng kiểm thử")
            ],
            authenticationType: "Test"))
        };

        var command = await PayrollEndpointExecution.ExecuteAsync(
            httpContext,
            auditScope,
            correlationScope,
            "payroll.attendance-allowance.refresh",
            _ => Task.FromResult(auditScope.Current!),
            CancellationToken.None);

        Assert.Equal("attendance-api-user", command.Actor.ActorId);
        Assert.Equal(AuditActorKind.User, command.Actor.Kind);
        Assert.Equal(AuditSource.Api, command.Actor.Source);
        Assert.Equal("attendance-api-correlation", command.CorrelationId);
        Assert.Null(auditScope.Current);
    }

    [Theory]
    [InlineData(AttendanceAllowanceCommandFailure.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(AttendanceAllowanceCommandFailure.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(AttendanceAllowanceCommandFailure.Locked, StatusCodes.Status409Conflict)]
    [InlineData(AttendanceAllowanceCommandFailure.Concurrency, StatusCodes.Status409Conflict)]
    public async Task Map_command_exception_returns_the_contract_status_code(
        AttendanceAllowanceCommandFailure failure,
        int expectedStatusCode)
    {
        var result = AttendanceAllowanceEndpointExecution.MapCommandException(
            new AttendanceAllowanceCommandException(failure, "Lỗi kiểm thử"));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.Configure<JsonOptions>(_ => { });
        using var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        await result.ExecuteAsync(httpContext);

        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
    }
}
