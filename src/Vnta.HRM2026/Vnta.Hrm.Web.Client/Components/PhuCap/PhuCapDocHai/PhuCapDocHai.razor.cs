using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Đại diện kiểu <c>PhuCapDocHai</c> phục vụ màn hình phụ cấp độc hại.</summary>
public partial class PhuCapDocHai : IDisposable
{
    #region Dependencies

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp độc hại.</summary>
    private readonly CancellationTokenSource disposalTokenSource = new();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp độc hại.</summary>
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp độc hại.</summary>
    private readonly SemaphoreSlim dataOperationGate = new(1, 1);

    [Inject]
    /// <summary>Giá trị <c>DataProvider</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private HazardAllowanceDataProvider DataProvider { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>MonthlyWorkSummaryDataProvider</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private MonthlyWorkSummaryDataProvider MonthlyWorkSummaryDataProvider { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>DialogService</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private IHrmDialogService DialogService { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>ToastService</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>Logger</c> được sử dụng bởi màn hình phụ cấp độc hại.</summary>
    private ILogger<PhuCapDocHai> Logger { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    #endregion
    #region Lifecycle

    /// <summary>Xử lý sự kiện cho luồng <c>OnInitializedAsync</c>.</summary>
    protected override async Task OnInitializedAsync()
    {
        var defaultPeriod = GetDefaultPayrollPeriod();
        ToolbarMonth = defaultPeriod.Month;
        ToolbarYear = defaultPeriod.Year;
        AppliedMonth = defaultPeriod.Month;
        AppliedYear = defaultPeriod.Year;
        await base.OnInitializedAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnAfterRenderAsync</c>.</summary>
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if(IsAllowanceTotalSyncPending)
        {
            IsAllowanceTotalSyncPending = false;
            UpdateVisibleAllowanceTotalFromGrid();
            return InvokeAsync(StateHasChanged);
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>Hoàn tất đồng bộ khi lưới xuất dữ liệu ẩn đã render.</summary>
    private Task OnExportGridRendered()
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    /// <summary>Giải phóng tài nguyên cho luồng <c>Dispose</c>.</summary>
    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
        dataOperationGate.Dispose();
    }

    #endregion
}
