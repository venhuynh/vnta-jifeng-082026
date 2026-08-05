using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTongHop;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapKhac;

/// <summary>
/// Adapter giữa contract phụ cấp khác và màn hình vận hành Interactive Server.
/// Quy tắc nghiệp vụ vẫn thuộc service phía máy chủ.
/// </summary>
public sealed class OtherAllowanceDataProvider(
    IOtherAllowanceReadService readService,
    IOtherAllowanceCreateService createService,
    IOtherAllowancePreviousMonthSyncService previousMonthSyncService,
    IOtherAllowanceUpdateService updateService,
    IOtherAllowanceLockService lockService,
    IPayrollAdministrationAuthorizer payrollAdministrationAuthorizer,
    IInteractiveAuditCommandScopeFactory auditCommandScopeFactory,
    AuthenticationStateProvider authenticationStateProvider,
    IPayrollAllowanceSummaryDataProvider payrollAllowanceSummaryDataProvider,
    MonthlyWorkSummaryDataProvider monthlyWorkSummaryDataProvider)
    : IOtherAllowanceReadDataProvider,
      IOtherAllowanceCreateDataProvider,
      IOtherAllowancePreviousMonthSyncDataProvider,
      IOtherAllowanceUpdateDataProvider,
      IOtherAllowanceLockDataProvider,
      IOtherAllowanceMonthlyWorkDataProvider
{
    public Task<OtherAllowancePageDto> SearchPageAsync(
        OtherAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        readService.SearchPageAsync(filter, cancellationToken);

    /// <summary>Loads editable payroll-summary records used only by this feature's create dialog.</summary>
    public Task<PayrollAllowanceSummaryLoadResult> SearchCreateEmployeesAsync(
        int payrollMonth,
        int payrollYear,
        int take,
        CancellationToken cancellationToken = default) =>
        payrollAllowanceSummaryDataProvider.SearchAsync(
            new PayrollAllowanceSummaryFilter(
                payrollMonth,
                payrollYear,
                SearchText: null,
                IsLocked: false,
                Take: take),
            cancellationToken);

    /// <summary>Loads read-only monthly attendance data for an Other Allowance row.</summary>
    public Task<MonthlyWorkSummaryGridRowRecord?> LoadEmployeeMonthlyWorkAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        monthlyWorkSummaryDataProvider.LoadEmployeeMonthAsync(fromDate, toDate, employeeId, cancellationToken);

    public async Task<OtherAllowanceListItemDto> CreateAsync(
        Guid payrollAllowanceSummaryRecordId,
        string allowanceName,
        bool isFixedAmount,
        decimal allowanceAmount,
        string? note,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var actor = await ResolveTrustedActorAsync(cancellationToken);

        var result = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.OtherAllowance.Created,
            token => createService.CreateAsync(
                new CreateOtherAllowanceRequest(
                    payrollAllowanceSummaryRecordId,
                    allowanceName,
                    isFixedAmount,
                    allowanceAmount,
                    note,
                    actor),
                token),
            cancellationToken: cancellationToken);
        return ToListItem(result);
    }

    public async Task<SyncOtherAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var actor = await ResolveTrustedActorAsync(cancellationToken);
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.OtherAllowance.SyncedFromPreviousMonth,
            token => previousMonthSyncService.SyncFromPreviousMonthAsync(
                new SyncOtherAllowanceFromPreviousMonthRequest(targetPayrollMonth, targetPayrollYear, actor),
                token),
            cancellationToken: cancellationToken);
    }

    public async Task<OtherAllowanceListItemDto> UpdateAsync(
        Guid id,
        string allowanceName,
        bool isFixedAmount,
        decimal allowanceAmount,
        string? note,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var actor = await ResolveTrustedActorAsync(cancellationToken);

        var result = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.OtherAllowance.Updated,
            token => updateService.UpdateAsync(
                new UpdateOtherAllowanceRequest(
                    id,
                    allowanceName,
                    isFixedAmount,
                    allowanceAmount,
                    note,
                    originalUpdatedAtUtc,
                    actor),
                token),
            cancellationToken: cancellationToken);
        return ToListItem(result);
    }

    public async Task SetLockStateAsync(
        Guid id,
        bool isLocked,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var actor = await ResolveTrustedActorAsync(cancellationToken);

        await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.OtherAllowance.LockStateChanged,
            token => lockService.SetLockStateAsync(
                new SetOtherAllowanceLockStateRequest(
                    id,
                    isLocked,
                    originalUpdatedAtUtc,
                    actor),
                token),
            cancellationToken: cancellationToken);
    }

    public async Task<SetOtherAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        int payrollMonth,
        int payrollYear,
        bool isLocked,
        IEnumerable<OtherAllowanceListItemDto>? rows = null,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var actor = await ResolveTrustedActorAsync(cancellationToken);
        var targetRows = rows?
            .Where(row => row.Id != Guid.Empty)
            .DistinctBy(row => row.Id)
            .ToArray();
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.OtherAllowance.LockStateChanged,
            token => lockService.SetLockStateBatchAsync(
                new SetOtherAllowanceBatchLockStateRequest(
                    payrollMonth,
                    payrollYear,
                    isLocked,
                    targetRows?.Select(row => row.Id).ToArray(),
                    actor,
                    targetRows?.Select(row => new OtherAllowanceLockItem(
                        row.Id,
                        row.UpdatedAtUtc ?? row.CreatedAtUtc)).ToArray()),
                token),
            cancellationToken: cancellationToken);
    }

    private static IReadOnlyDictionary<string, string> CreateSelfApprovalMetadata(string actor) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["approval.mode"] = "self",
            ["approval.approved_by"] = actor
        };

    private static OtherAllowanceListItemDto ToListItem(OtherAllowanceCommandResult result) => new(
        result.Id, result.PayrollAllowanceSummaryRecordId, result.EmployeeId, result.EmployeeCode,
        result.EmployeeName, result.DepartmentName, result.PositionName, result.PayrollMonth,
        result.PayrollYear, result.AllowanceName, result.IsFixedAmount, result.AllowanceAmount,
        result.Note, result.IsLocked, result.CreatedAtUtc, result.CreatedBy, result.UpdatedAtUtc,
        result.UpdatedBy);

    private async Task<string> ResolveTrustedActorAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        if(user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Không xác định được người dùng thực hiện thao tác.");
        }

        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Không xác định được định danh người dùng thực hiện thao tác.");
    }
}
