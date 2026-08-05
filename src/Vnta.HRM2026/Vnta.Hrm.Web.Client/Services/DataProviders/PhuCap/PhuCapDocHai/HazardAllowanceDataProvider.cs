using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class HazardAllowanceDataProvider(
    // Each dependency represents one use case; the provider never injects a feature-wide service.
    IHazardAllowanceReadService hazardAllowanceReadService,
    IHazardAllowanceExportService hazardAllowanceExportService,
    IHazardAllowanceRefreshService hazardAllowanceRefreshService,
    IHazardAllowanceManualAdjustmentService hazardAllowanceManualAdjustmentService,
    IHazardAllowanceEntitlementService hazardAllowanceEntitlementService,
    IHazardAllowanceLockService hazardAllowanceLockService,
    IHazardAllowanceExportJobService hazardAllowanceExportJobService,
    IPayrollAdministrationAuthorizer payrollAdministrationAuthorizer,
    IInteractiveAuditCommandScopeFactory auditCommandScopeFactory,
    // InteractiveServer dùng principal của circuit; WebAssembly vẫn có endpoint ghi đè actor lần cuối.
    AuthenticationStateProvider authenticationStateProvider)
{
    #region Luồng đọc

    private const int DatasetBatchSize = 5_000;

    /// <summary>Gọi contract danh sách cũ để giữ tương thích consumer chưa dùng paging.</summary>
    public Task<IReadOnlyList<HazardAllowanceListItemDto>> SearchAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        hazardAllowanceReadService.SearchAsync(filter, cancellationToken);

    /// <summary>Đọc một trang snapshot theo filter server-side.</summary>
    public Task<HazardAllowancePageDto> SearchPageAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        hazardAllowanceReadService.SearchPageAsync(filter, cancellationToken);

    /// <summary>
    /// Tải trọn tập dữ liệu theo filter bằng các batch server-side để UI có thể phân trang cục bộ
    /// mà không bị cắt theo giới hạn một request.
    /// </summary>
    public async Task<IReadOnlyList<HazardAllowanceListItemDto>> LoadAllAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<HazardAllowanceListItemDto>();
        var totalCount = 0;

        do
        {
            var page = await SearchPageAsync(
                filter with
                {
                    Take = DatasetBatchSize,
                    Skip = rows.Count,
                    IncludeTotalCount = true
                },
                cancellationToken);

            totalCount = page.TotalCount;
            if(page.Rows.Count == 0 && rows.Count < totalCount)
            {
                throw new InvalidOperationException("Không thể tải đầy đủ dữ liệu phụ cấp độc hại.");
            }

            rows.AddRange(page.Rows);
        }
        while(rows.Count < totalCount);

        return rows;
    }

    /// <summary>Đọc badge count độc lập với trang grid hiện hành.</summary>
    public Task<HazardAllowanceSummaryDto> GetSummaryAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        hazardAllowanceReadService.GetSummaryAsync(filter, cancellationToken);

    /// <summary>Đọc trọn tập dữ liệu export, không bị giới hạn bởi PageSize.</summary>
    public Task<IReadOnlyList<HazardAllowanceListItemDto>> ExportAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default) =>
        hazardAllowanceExportService.ExportAsync(filter, cancellationToken);

    public async Task<HazardAllowanceExportJobDto> QueueExportJobAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var actor = await ResolveTrustedActorAsync();
        return await hazardAllowanceExportJobService.QueueAsync(
            new CreateHazardAllowanceExportJobRequest(filter, actor),
            cancellationToken);
    }

    public async Task<HazardAllowanceExportJobDto?> GetExportJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveTrustedActorAsync();
        return await hazardAllowanceExportJobService.GetAsync(jobId, actor, cancellationToken);
    }

    #endregion

    #region Luồng command với actor tin cậy

    /// <summary>Tạo request refresh rỗng actor; helper bên dưới mới gắn actor từ principal.</summary>
    public async Task<RefreshHazardAllowanceResult> RefreshAsync(
        int payrollMonth,
        int payrollYear,
        Guid? payrollAllowanceSummaryRecordId = null,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.HazardAllowance.Refreshed,
            token => RefreshWithTrustedActorAsync(
                new RefreshHazardAllowanceRequest(
                    payrollMonth,
                    payrollYear,
                    RequestedBy: string.Empty,
                    payrollAllowanceSummaryRecordId),
                token),
            cancellationToken: cancellationToken);
    }

    public async Task<HazardAllowanceListItemDto> UpdateManualValuesAsync(
        UpdateHazardAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.HazardAllowance.ManualValuesUpdated,
            token => UpdateManualValuesWithTrustedActorAsync(request, token),
            cancellationToken: cancellationToken);
    }

    /// <summary>Cập nhật trạng thái hưởng chỉ cho các dòng đã chọn, kèm timestamp để phát hiện dữ liệu cũ.</summary>
    public async Task<SetHazardAllowanceEntitlementBatchResult> SetEntitlementBatchAsync(
        bool isEligibleForAllowance,
        IReadOnlyList<HazardAllowanceEntitlementTarget> targets,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.HazardAllowance.EntitlementBatchUpdated,
            token => SetEntitlementBatchWithTrustedActorAsync(
                new SetHazardAllowanceEntitlementBatchRequest(
                    isEligibleForAllowance,
                    targets,
                    RequestedBy: string.Empty),
                token),
            metadata: new Dictionary<string, string>
            {
                ["targetState"] = isEligibleForAllowance ? "eligible" : "excluded",
                ["targetCount"] = targets.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            cancellationToken: cancellationToken);
    }

    /// <summary>Khóa/mở khóa một hoặc nhiều dòng sau khi provider tự xác định actor.</summary>
    public async Task SetLockStateAsync(
        IReadOnlyCollection<Guid> payrollAllowanceSummaryRecordIds,
        bool isLocked,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.HazardAllowance.LockStateChanged,
            token => SetLockStateWithTrustedActorAsync(
                new SetHazardAllowanceLockStateRequest(
                    payrollAllowanceSummaryRecordIds,
                    isLocked,
                    RequestedBy: string.Empty),
                token),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Khóa/mở khóa tập dòng đã chọn; <paramref name="payrollAllowanceSummaryRecordIds"/> là <see langword="null"/>
    /// khi người dùng chọn toàn bộ kỳ lương.
    /// </summary>
    public async Task<SetHazardAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        int payrollYear,
        int payrollMonth,
        bool isLocked,
        IReadOnlyList<Guid>? payrollAllowanceSummaryRecordIds,
        CancellationToken cancellationToken = default)
    {
        await payrollAdministrationAuthorizer.DemandAsync(cancellationToken);
        var scope = payrollAllowanceSummaryRecordIds is null ? "whole-period" : "selected-rows";
        return await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.HazardAllowance.BatchLockStateChanged,
            token => SetLockStateBatchWithTrustedActorAsync(
                new SetHazardAllowanceBatchLockStateRequest(
                    payrollYear,
                    payrollMonth,
                    isLocked,
                    payrollAllowanceSummaryRecordIds,
                    RequestedBy: string.Empty),
                token),
            metadata: new Dictionary<string, string>
            {
                ["payrollYear"] = payrollYear.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["payrollMonth"] = payrollMonth.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["scope"] = scope,
                ["targetState"] = isLocked ? "locked" : "unlocked"
            },
            cancellationToken: cancellationToken);
    }

    private async Task<RefreshHazardAllowanceResult> RefreshWithTrustedActorAsync(
        RefreshHazardAllowanceRequest request,
        CancellationToken cancellationToken)
    {
        // Đọc actor mỗi command để không cache identity cũ sau khi circuit bị đổi principal.
        var actor = await ResolveTrustedActorAsync();
        return await hazardAllowanceRefreshService.RefreshAsync(request with { RequestedBy = actor }, cancellationToken);
    }

    private async Task<HazardAllowanceListItemDto> UpdateManualValuesWithTrustedActorAsync(
        UpdateHazardAllowanceManualValuesRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveTrustedActorAsync();
        return await hazardAllowanceManualAdjustmentService.UpdateManualValuesAsync(request with { RequestedBy = actor }, cancellationToken);
    }

    private async Task<SetHazardAllowanceEntitlementBatchResult> SetEntitlementBatchWithTrustedActorAsync(
        SetHazardAllowanceEntitlementBatchRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveTrustedActorAsync();
        return await hazardAllowanceEntitlementService.SetEntitlementBatchAsync(
            request with { RequestedBy = actor }, cancellationToken);
    }

    private async Task SetLockStateWithTrustedActorAsync(
        SetHazardAllowanceLockStateRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveTrustedActorAsync();
        await hazardAllowanceLockService.SetLockStateAsync(request with { RequestedBy = actor }, cancellationToken);
    }

    private async Task<SetHazardAllowanceBatchLockStateResult> SetLockStateBatchWithTrustedActorAsync(
        SetHazardAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await ResolveTrustedActorAsync();
        return await hazardAllowanceLockService.SetLockStateBatchAsync(
            request with { RequestedBy = actor },
            cancellationToken);
    }

    private async Task<string> ResolveTrustedActorAsync()
    {
        // AuthenticationState là boundary tin cậy của circuit, khác dữ liệu actor do UI tự tạo.
        var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        if(user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Không xác định được người dùng thực hiện thao tác.");
        }

        // Giữ thứ tự thống nhất với HTTP endpoint để audit không thay đổi giữa hai render mode.
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Không xác định được định danh người dùng thực hiện thao tác.");
    }

    #endregion
}
