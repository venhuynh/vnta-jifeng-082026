using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
namespace Vnta.Hrm.Web.Client.Services.Api;

public sealed class HttpHazardAllowanceService(NavigationManager navigationManager)
    : IHazardAllowanceReadService,
      IHazardAllowanceExportService,
      IHazardAllowanceExportJobService,
      IHazardAllowanceRefreshService,
      IHazardAllowanceManualAdjustmentService,
      IHazardAllowanceEntitlementService,
      IHazardAllowanceLockService
{
    // Base URI của browser hiện tại; chỉ adapter WebAssembly sử dụng HTTP, InteractiveServer được DI host thay bằng Infrastructure.
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    /// <summary>Gọi endpoint danh sách cũ để tương thích consumer chưa chuyển paging.</summary>
    public async Task<IReadOnlyList<HazardAllowanceListItemDto>> SearchAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/hazard-allowance/search",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<HazardAllowanceListItemDto>>(cancellationToken);
    }

    /// <summary>Gọi read endpoint phân trang; payload filter được serialize nguyên trạng sang server.</summary>
    public async Task<HazardAllowancePageDto> SearchPageAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/hazard-allowance/search-page",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<HazardAllowancePageDto>(cancellationToken);
    }

    /// <summary>Gọi endpoint đếm badge để UI không suy luận count từ một trang dữ liệu.</summary>
    public async Task<HazardAllowanceSummaryDto> GetSummaryAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/hazard-allowance/summary",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<HazardAllowanceSummaryDto>(cancellationToken);
    }

    /// <summary>Gọi endpoint export để nhận toàn bộ tập filter thay vì snapshot grid hiện tại.</summary>
    public async Task<IReadOnlyList<HazardAllowanceListItemDto>> ExportAsync(
        HazardAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/hazard-allowance/export",
            filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<HazardAllowanceListItemDto>>(cancellationToken);
    }

    /// <summary>Queues a durable CSV export job for browser consumers.</summary>
    public async Task<HazardAllowanceExportJobDto> QueueAsync(
        CreateHazardAllowanceExportJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/hazard-allowance/export-jobs",
            request.Filter,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<HazardAllowanceExportJobDto>(cancellationToken);
    }

    /// <summary>Gets the caller-owned background export job; the server derives ownership from the principal.</summary>
    public async Task<HazardAllowanceExportJobDto?> GetAsync(
        Guid jobId,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/payroll/hazard-allowance/export-jobs/{jobId:D}",
            cancellationToken);
        return response.StatusCode == System.Net.HttpStatusCode.NotFound
            ? null
            : await response.ReadRequiredFromJsonAsync<HazardAllowanceExportJobDto>(cancellationToken);
    }

    /// <summary>Opens a completed CSV for callers that need its content instead of browser navigation.</summary>
    public async Task<HazardAllowanceExportJobFileDto?> OpenCompletedFileAsync(
        Guid jobId,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/payroll/hazard-allowance/export-jobs/{jobId:D}/download",
            cancellationToken);
        if(response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await response.EnsureSuccessAsync(cancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? $"hazard-allowance-{jobId:D}.csv";
        return new HazardAllowanceExportJobFileDto(
            new MemoryStream(content, writable: false),
            fileName.Trim('"'),
            response.Content.Headers.ContentType?.ToString() ?? "text/csv; charset=utf-8");
    }

    /// <summary>Gửi command tính lại; endpoint sẽ ghi đè actor từ principal đã xác thực.</summary>
    public async Task<RefreshHazardAllowanceResult> RefreshAsync(
        RefreshHazardAllowanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/hazard-allowance/refresh",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RefreshHazardAllowanceResult>(cancellationToken);
    }

    /// <summary>Gửi điều chỉnh tay cùng concurrency snapshot do UI vừa tải.</summary>
    public async Task<HazardAllowanceListItemDto> UpdateManualValuesAsync(
        UpdateHazardAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/hazard-allowance/manual-values",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<HazardAllowanceListItemDto>(cancellationToken);
    }

    public async Task<SetHazardAllowanceEntitlementBatchResult> SetEntitlementBatchAsync(
        SetHazardAllowanceEntitlementBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/hazard-allowance/entitlement/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetHazardAllowanceEntitlementBatchResult>(cancellationToken);
    }

    /// <summary>Gửi command khóa/mở khóa; response thành công là 204 No Content.</summary>
    public async Task SetLockStateAsync(
        SetHazardAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/hazard-allowance/lock-state",
            request,
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }

    /// <summary>Gửi command khóa/mở khóa theo dòng chọn hoặc toàn bộ kỳ lương.</summary>
    public async Task<SetHazardAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetHazardAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/hazard-allowance/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetHazardAllowanceBatchLockStateResult>(cancellationToken);
    }
}
