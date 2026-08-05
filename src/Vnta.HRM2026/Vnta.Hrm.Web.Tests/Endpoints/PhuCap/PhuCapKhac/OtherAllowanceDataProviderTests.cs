using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapKhac;
using Xunit;

namespace Vnta.Hrm.Web.Tests;

public sealed class OtherAllowanceDataProviderTests
{
    [Fact]
    public async Task Create_maps_the_complete_command_snapshot_to_the_ui_list_item()
    {
        var commandResult = new OtherAllowanceCommandResult(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "NV001", "Test employee",
            "Payroll", "Specialist", 7, 2026, "Meal support", true, 1_250.75m,
            "Taxable", false, new DateTime(2026, 7, 31, 3, 0, 0, DateTimeKind.Utc),
            "creator", new DateTime(2026, 7, 31, 4, 0, 0, DateTimeKind.Utc), "editor");
        var createService = new CapturingCreateService(commandResult);
        var provider = new OtherAllowanceDataProvider(
            new EmptyReadService(),
            createService,
            new EmptyPreviousMonthSyncService(),
            new EmptyUpdateService(),
            new EmptyLockService(),
            new AllowAllPayrollAdministrationAuthorizer(),
            new PassThroughAuditScopeFactory(),
            new AuthenticatedStateProvider("ui-payroll-admin"),
            null!,
            null!);

        var row = await provider.CreateAsync(
            commandResult.PayrollAllowanceSummaryRecordId,
            "Meal support",
            isFixedAmount: true,
            allowanceAmount: 1_250.75m,
            note: "Taxable");

        Assert.Equal(commandResult.Id, row.Id);
        Assert.Equal(commandResult.PayrollAllowanceSummaryRecordId, row.PayrollAllowanceSummaryRecordId);
        Assert.Equal(commandResult.EmployeeId, row.EmployeeId);
        Assert.Equal(commandResult.EmployeeCode, row.EmployeeCode);
        Assert.Equal(commandResult.EmployeeName, row.EmployeeName);
        Assert.Equal(commandResult.DepartmentName, row.DepartmentName);
        Assert.Equal(commandResult.PositionName, row.PositionName);
        Assert.Equal(commandResult.PayrollMonth, row.PayrollMonth);
        Assert.Equal(commandResult.PayrollYear, row.PayrollYear);
        Assert.Equal(commandResult.AllowanceName, row.AllowanceName);
        Assert.Equal(commandResult.IsFixedAmount, row.IsFixedAmount);
        Assert.Equal(commandResult.AllowanceAmount, row.AllowanceAmount);
        Assert.Equal(commandResult.Note, row.Note);
        Assert.Equal(commandResult.IsLocked, row.IsLocked);
        Assert.Equal(commandResult.CreatedAtUtc, row.CreatedAtUtc);
        Assert.Equal(commandResult.CreatedBy, row.CreatedBy);
        Assert.Equal(commandResult.UpdatedAtUtc, row.UpdatedAtUtc);
        Assert.Equal(commandResult.UpdatedBy, row.UpdatedBy);
        Assert.Equal("ui-payroll-admin", createService.Request?.RequestedBy);
    }

    private sealed class CapturingCreateService(OtherAllowanceCommandResult result) : IOtherAllowanceCreateService
    {
        public CreateOtherAllowanceRequest? Request { get; private set; }

        public Task<OtherAllowanceCommandResult> CreateAsync(
            CreateOtherAllowanceRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(result);
        }
    }

    private sealed class EmptyReadService : IOtherAllowanceReadService
    {
        public Task<OtherAllowancePageDto> SearchPageAsync(OtherAllowanceFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OtherAllowancePageDto([], 0, 0m));
    }

    private sealed class EmptyUpdateService : IOtherAllowanceUpdateService
    {
        public Task<OtherAllowanceCommandResult> UpdateAsync(UpdateOtherAllowanceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class EmptyPreviousMonthSyncService : IOtherAllowancePreviousMonthSyncService
    {
        public Task<SyncOtherAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(
            SyncOtherAllowanceFromPreviousMonthRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class EmptyLockService : IOtherAllowanceLockService
    {
        public Task SetLockStateAsync(SetOtherAllowanceLockStateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SetOtherAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetOtherAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class AllowAllPayrollAdministrationAuthorizer : IPayrollAdministrationAuthorizer
    {
        public Task DemandAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PassThroughAuditScopeFactory : IInteractiveAuditCommandScopeFactory
    {
        public Task ExecuteAsync(
            string actionIntent,
            Func<CancellationToken, Task> command,
            AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) => command(cancellationToken);

        public Task<T> ExecuteAsync<T>(
            string actionIntent,
            Func<CancellationToken, Task<T>> command,
            AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) => command(cancellationToken);
    }

    private sealed class AuthenticatedStateProvider(string actor) : AuthenticationStateProvider
    {
        private readonly AuthenticationState state = new(new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, actor)], authenticationType: "Test")));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(state);
    }
}
