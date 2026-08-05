using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapDocHai;

public sealed class HazardAllowanceDataProviderWorkflowTests
{
    [Fact]
    public async Task Load_all_reads_successive_server_pages_until_the_reported_total_is_reached()
    {
        var readService = new PagingReadService();
        var provider = new HazardAllowanceDataProvider(
            readService, null!, null!, null!, null!, null!, new RecordingAuthorizer(), new PassThroughAuditScope(),
            AuthenticatedAs("payroll-admin@example.test"));

        var rows = await provider.LoadAllAsync(new HazardAllowanceFilter(7, 2026, HazardAllowanceLockState.All, null));

        Assert.Equal(2, rows.Count);
        Assert.Equal([0, 1], readService.ReceivedFilters.Select(filter => filter.Skip));
        Assert.All(readService.ReceivedFilters, filter =>
        {
            Assert.Equal(5_000, filter.Take);
            Assert.True(filter.IncludeTotalCount);
        });
    }

    [Fact]
    public async Task Manual_adjustment_demands_payroll_access_and_replaces_a_forged_actor_with_the_circuit_principal()
    {
        var authorizer = new RecordingAuthorizer();
        var auditScope = new PassThroughAuditScope();
        var manualService = new CapturingManualAdjustmentService();
        var provider = new HazardAllowanceDataProvider(
            null!, null!, null!, manualService, null!, null!, authorizer, auditScope,
            AuthenticatedAs("payroll-admin@example.test"));
        var request = new UpdateHazardAllowanceManualValuesRequest(
            Guid.NewGuid(), 2m, 0m, 0m, 600_000m, true, null,
            DateTime.UnixEpoch, DateTime.UnixEpoch, "forged-client-actor");

        await provider.UpdateManualValuesAsync(request);

        Assert.True(authorizer.WasDemanded);
        Assert.Equal(AuditActions.HazardAllowance.ManualValuesUpdated, Assert.Single(auditScope.Actions));
        Assert.Equal("payroll-admin@example.test", manualService.Request?.RequestedBy);
    }

    private static AuthenticationStateProvider AuthenticatedAs(string email) =>
        new StaticAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, email)], "test")));

    private sealed class PagingReadService : IHazardAllowanceReadService
    {
        public List<HazardAllowanceFilter> ReceivedFilters { get; } = [];

        public Task<IReadOnlyList<HazardAllowanceListItemDto>> SearchAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<HazardAllowancePageDto> SearchPageAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken = default)
        {
            ReceivedFilters.Add(filter);
            var rows = filter.Skip switch
            {
                0 => new[] { Row("NV001") },
                1 => new[] { Row("NV002") },
                _ => []
            };
            return Task.FromResult(new HazardAllowancePageDto(rows, 2));
        }

        public Task<HazardAllowanceSummaryDto> GetSummaryAsync(HazardAllowanceFilter filter, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class CapturingManualAdjustmentService : IHazardAllowanceManualAdjustmentService
    {
        public UpdateHazardAllowanceManualValuesRequest? Request { get; private set; }

        public Task<HazardAllowanceListItemDto> UpdateManualValuesAsync(
            UpdateHazardAllowanceManualValuesRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(Row("NV001"));
        }
    }

    private sealed class RecordingAuthorizer : IPayrollAdministrationAuthorizer
    {
        public bool WasDemanded { get; private set; }

        public Task DemandAsync(CancellationToken cancellationToken = default)
        {
            WasDemanded = true;
            return Task.CompletedTask;
        }
    }

    private sealed class PassThroughAuditScope : IInteractiveAuditCommandScopeFactory
    {
        public List<string> Actions { get; } = [];

        public async Task ExecuteAsync(string actionIntent, Func<CancellationToken, Task> command,
            AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            Actions.Add(actionIntent);
            await command(cancellationToken);
        }

        public async Task<T> ExecuteAsync<T>(string actionIntent, Func<CancellationToken, Task<T>> command,
            AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            Actions.Add(actionIntent);
            return await command(cancellationToken);
        }
    }

    private sealed class StaticAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(principal));
    }

    private static HazardAllowanceListItemDto Row(string employeeCode) => new(
        Guid.NewGuid(), Guid.NewGuid(), employeeCode, "Test employee", 7, 2026,
        2m, 0m, 2m, 0m, 600_000m, true, null, false,
        DateTime.UnixEpoch, "seed", null, null, null);
}
