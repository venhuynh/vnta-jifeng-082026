using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Models;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Export;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.State;
using Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop;

public partial class KhauTruTongKet : IDisposable
{
    #region Hằng số và cấu hình màn hình

    private const int MinimumSupportedMonth = 6;
    private const int MinimumSupportedYear = 2026;
    private const int MaximumSupportedYear = 2100;
    private const string PayrollTimeZoneId = "Asia/Ho_Chi_Minh";
    private const string PayrollTimeZoneWindowsId = "SE Asia Standard Time";
    private const string LockStatusAll = "all";
    private const string LockStatusOpen = "open";
    private const string LockStatusLocked = "locked";
    private const string LockScopeSelectedRows = "selected-rows";
    private const string LockScopeWholePeriod = "whole-period";
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly IReadOnlyList<MonthOption> MonthOptions = Enumerable.Range(1, 12)
        .Select(month => new MonthOption(month, $"Tháng {month:00}"))
        .ToArray();
    private static readonly IReadOnlyList<LockStatusFilter> LockStatusFilters =
    [
        new(LockStatusAll, "Tất cả"),
        new(LockStatusOpen, "Đang mở"),
        new(LockStatusLocked, "Đã khóa")
    ];

    #endregion

    #region Phụ thuộc được tiêm

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private readonly KhauTruTongKetReloadState reloadState = new();

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private PayrollDeductionSummaryDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private MonthlyWorkSummaryDataProvider MonthlyWorkSummaryDataProvider { get; set; } = default!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    #endregion

    #region Trạng thái màn hình

    private KhauTruTongKetGridSection? GridSection { get; set; }
    private KhauTruTongKetExportGrid? ExportSection { get; set; }
    private static readonly int[] PageSizeOptions = [50, 100, 200];
    private IReadOnlyList<PayrollDeductionSummaryRecord> Records { get; set; } = [];
    private IReadOnlyList<PayrollDeductionSummaryExportRecord> ExportRecords { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string LoadingText { get; set; } = DefaultLoadingText;
    private decimal VisibleDeductionTotal { get; set; }
    private string SelectedLockStatusKey { get; set; } = LockStatusAll;
    private int ToolbarMonth { get; set; } = MinimumSupportedMonth;
    private int ToolbarYear { get; set; } = MinimumSupportedYear;
    private int AppliedMonth { get; set; } = MinimumSupportedMonth;
    private int AppliedYear { get; set; } = MinimumSupportedYear;
    private int PageSize { get; set; } = PageSizeOptions[0];
    private int CurrentPageIndex { get; set; }
    private int TotalRecordCount { get; set; }
    private PayrollDeductionSummaryLockStatusCounts LockStatusCounts { get; set; } = PayrollDeductionSummaryLockStatusCounts.Empty;
    private bool HasRequestedData { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool IsLoading { get; set; }
    private bool IsRefreshing { get; set; }
    private bool IsRefreshingRow { get; set; }
    private bool IsExporting { get; set; }
    private bool IsEditPopupVisible { get; set; }
    private bool IsSavingEdit { get; set; }
    private bool IsRulesPopupVisible { get; set; }
    private bool IsSyncFromPreviousMonthPopupVisible { get; set; }
    private bool IsSyncingPreviousMonth { get; set; }
    private bool IsRecalculateConfirmPopupVisible { get; set; }
    private string RulesPopupPeriodDisplay { get; set; } = $"{MinimumSupportedMonth:00}/{MinimumSupportedYear}";
    private string? DefaultPeriodWarningMessage { get; set; }
    private bool IsLockActionPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupLoading { get; set; }
    private bool CanReadMonthlyWork { get; } = true;
    private bool PendingLockActionState { get; set; } = true;
    private bool IsDeductionTotalSyncPending { get; set; }
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;
    private KhauTruTongKetEditModel EditModel { get; set; } = new();
    private string EditPopupTitle { get; set; } = "Điều chỉnh khoản khấu trừ khác";
    private string MonthlyWorkPopupTitle { get; set; } = "Bảng công tháng";
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    private string? MonthlyWorkPopupErrorMessage { get; set; }
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    private PayrollDeductionSummaryRecord? MonthlyWorkPopupRecord { get; set; }
    private bool isDisposed;

    private const string DefaultLoadingText = "Đang tải dữ liệu tổng kết khấu trừ...";

    #endregion

    #region Trạng thái suy diễn và quyền thao tác

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private IReadOnlyList<MonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : MonthOptions;
    private bool ShowLoadingPanel => IsLoading || IsChangingPageSize || IsSyncingPreviousMonth || IsRefreshing || IsRefreshingRow || IsExporting || IsSavingEdit;
    private bool HasPendingPeriodChange =>
        HasRequestedData
        && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    private bool CanView => !ShowLoadingPanel;
    private bool CanChangeFilters => !ShowLoadingPanel && !IsSyncFromPreviousMonthPopupVisible;
    private bool CanOperateOnCurrentDataset => CanInteract && HasRequestedData && !HasPendingPeriodChange;
    private bool CanSyncFromPreviousMonth => CanOperateOnCurrentDataset && !IsSyncFromPreviousMonthPopupVisible;
    private bool CanRecalculate => CanOperateOnCurrentDataset;
    private bool CanOpenLockAction => CanOperateOnCurrentDataset;
    private bool CanOpenUnlockAction => CanOperateOnCurrentDataset;
    private bool CanChooseSelectedRowsScope => GetSelectedRowCount() > 0;
    private bool CanConfirmLockAction => CanOperateOnCurrentDataset && !IsRefreshing;
    private bool CanExport => CanOperateOnCurrentDataset && TotalRecordCount > 0;
    private bool CanExportSelected => CanExport && GetSelectedRowCount() > 0;
    private bool CanEditFields => CanOperateOnCurrentDataset && !IsSavingEdit && !EditModel.IsLocked;
    private bool CanSaveEdit => CanEditFields && EditModel.Id != Guid.Empty;
    private string CurrentPayrollPeriodDisplay => $"{AppliedMonth:00}/{AppliedYear}";
    private string PreviousMonthSyncSourcePeriodLabel
    {
        get
        {
            var sourcePeriod = GetPreviousPeriod(AppliedMonth, AppliedYear);
            return $"{sourcePeriod.Month:00}/{sourcePeriod.Year}";
        }
    }
    private bool HasActiveDataFilter =>
        !string.IsNullOrWhiteSpace(SearchText)
        || !string.Equals(SelectedLockStatusKey, LockStatusAll, StringComparison.Ordinal);
    private string LockActionPopupTitle => PendingLockActionState ? "Chọn phạm vi khóa dữ liệu" : "Chọn phạm vi mở khóa dữ liệu";
    private string LockActionConfirmText => PendingLockActionState ? "Khóa" : "Mở khóa";
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa. Dòng đã khóa không thể điều chỉnh hoặc làm mới và sẽ bị bỏ qua khi đồng bộ từ tháng trước."
        : "Chọn phạm vi cần mở khóa để cho phép điều chỉnh, làm mới hoặc đồng bộ dữ liệu lại.";
    private string LockActionScopeContextText => $"Kỳ lương áp dụng: {CurrentPayrollPeriodDisplay}.";
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {GetSelectedRowCount():N0} dòng đang chọn."
        : "Chưa có dòng nào được chọn.";
    private string WholePeriodScopeDescription =>
        $"Áp dụng cho toàn bộ {TotalRecordCount:N0} dòng của kỳ {CurrentPayrollPeriodDisplay}.";
    private int TotalPageCount => TotalRecordCount <= 0 ? 1 : (int)Math.Ceiling(TotalRecordCount / (double)PageSize);
    private int CurrentPageStartRecord => TotalRecordCount == 0 ? 0 : CurrentPageIndex * PageSize + 1;
    private int CurrentPageEndRecord => TotalRecordCount == 0 ? 0 : Math.Min(TotalRecordCount, CurrentPageIndex * PageSize + Records.Count);
    private bool CanBrowsePages => CanOperateOnCurrentDataset && TotalRecordCount > 0;
    private string PagerSummaryText => !HasRequestedData || HasLoadError || TotalRecordCount == 0
        ? "Chưa có trang dữ liệu"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalRecordCount:N0} dòng";
    private string EmptyStateTitle => !HasRequestedData
        ? "Chưa tải dữ liệu tổng kết khấu trừ"
        : HasPendingPeriodChange
            ? "Kỳ lương đã thay đổi"
            : HasActiveDataFilter
                ? "Không tìm thấy kết quả phù hợp"
                : "Chưa có dữ liệu tổng kết khấu trừ";
    private string EmptyStateMessage => !HasRequestedData || HasPendingPeriodChange
        ? "Hãy chọn kỳ lương và bấm Xem để tải dữ liệu."
        : HasActiveDataFilter
            ? "Hãy thử điều kiện khác hoặc xóa tìm kiếm để xem toàn bộ dữ liệu."
            : "Bảng tổng kết khấu trừ sẽ hiển thị tại đây sau khi có dữ liệu cho kỳ lương đang chọn.";
    private string EmptyStateActionText => !HasRequestedData || HasPendingPeriodChange
        ? "Xem dữ liệu"
        : HasActiveDataFilter
            ? "Xóa bộ lọc"
            : "Tải lại";
    private KhauTruTongKetGridState GridState => new(
        Records,
        SelectedDataItems,
        SearchText,
        LockStatusCounts,
        SelectedLockStatusKey,
        VisibleDeductionTotal,
        CurrentPageIndex,
        PageSize,
        PageSizeOptions,
        TotalPageCount,
        TotalRecordCount,
        PagerSummaryText,
        EmptyStateTitle,
        EmptyStateMessage,
        EmptyStateActionText,
        CanChangeFilters,
        CanOperateOnCurrentDataset,
        CanBrowsePages,
        CanOperateOnCurrentDataset,
        CanOperateOnCurrentDataset && !IsRefreshingRow,
        CanOperateOnCurrentDataset && CanReadMonthlyWork && !IsMonthlyWorkPopupLoading);

    #endregion

    #region Vòng đời component

    protected override void OnInitialized()
    {
        var defaultPeriod = GetDefaultPayrollPeriod();
        ToolbarMonth = defaultPeriod.Month;
        ToolbarYear = defaultPeriod.Year;
        AppliedMonth = defaultPeriod.Month;
        AppliedYear = defaultPeriod.Year;
        RulesPopupPeriodDisplay = $"{defaultPeriod.Month:00}/{defaultPeriod.Year}";
        base.OnInitialized();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if(IsDeductionTotalSyncPending)
        {
            IsDeductionTotalSyncPending = false;
            UpdateVisibleDeductionTotalFromGrid();
            return InvokeAsync(StateHasChanged);
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    #endregion

    #region Luồng tải dữ liệu và thao tác nghiệp vụ

    private async Task OnViewRequestedAsync()
    {
        if(!CanView)
        {
            return;
        }

        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        AppliedMonth = normalizedPeriod.Month;
        AppliedYear = normalizedPeriod.Year;
        CurrentPageIndex = 0;
        HasRequestedData = true;
        await ReloadAsync();
    }

    private async Task OnRetryAsync()
    {
        if(HasRequestedData && !HasPendingPeriodChange)
        {
            await ReloadAsync();
            return;
        }

        await OnViewRequestedAsync();
    }

    private async Task ReloadAsync()
    {
        if(!HasRequestedData || isDisposed)
        {
            return;
        }

        var requestVersion = Interlocked.Increment(ref reloadState.RequestedVersion);
        CancelActiveReload();
        await reloadGate.WaitAsync(disposalTokenSource.Token);
        using var requestTokenSource = BeginReload();
        var cancellationToken = requestTokenSource.Token;

        try
        {
            LoadErrorMessage = null;
            LoadingText = DefaultLoadingText;
            IsLoading = true;

            var loadResult = await DataProvider.SearchAsync(
                new PayrollDeductionSummaryFilter(AppliedMonth, AppliedYear, SearchText, GetLockFilterValue(), CurrentPageIndex * PageSize, PageSize),
                cancellationToken);

            if(requestVersion != Volatile.Read(ref reloadState.RequestedVersion) || isDisposed)
            {
                return;
            }

            var maximumPageIndex = Math.Max(0, (int)Math.Ceiling(loadResult.TotalCount / (double)PageSize) - 1);
            if(loadResult.TotalCount > 0 && CurrentPageIndex > maximumPageIndex)
            {
                CurrentPageIndex = maximumPageIndex;
                loadResult = await DataProvider.SearchAsync(
                    new PayrollDeductionSummaryFilter(AppliedMonth, AppliedYear, SearchText, GetLockFilterValue(), CurrentPageIndex * PageSize, PageSize),
                    cancellationToken);
            }

            Records = loadResult.Rows;
            TotalRecordCount = loadResult.TotalCount;
            LockStatusCounts = loadResult.LockStatusCounts;
            ResetVisibleDeductionTotal();
            IsDeductionTotalSyncPending = true;
            await ClearSelectionAsync();
        }
        catch(OperationCanceledException) when(
            disposalTokenSource.IsCancellationRequested
            || requestVersion != Volatile.Read(ref reloadState.RequestedVersion))
        {
        }
        catch(Exception)
        {
            if(requestVersion != Volatile.Read(ref reloadState.RequestedVersion) || isDisposed)
            {
                return;
            }

            Records = [];
            TotalRecordCount = 0;
            LockStatusCounts = PayrollDeductionSummaryLockStatusCounts.Empty;
            ResetVisibleDeductionTotal();
            const string errorMessage = "Không thể tải dữ liệu tổng kết khấu trừ. Vui lòng thử lại.";
            LoadErrorMessage = errorMessage;
            ToastService.ShowError(errorMessage);
        }
        finally
        {
            if(requestVersion == Volatile.Read(ref reloadState.RequestedVersion) && !isDisposed)
            {
                IsLoading = false;
                LoadingText = DefaultLoadingText;
            }

            if(ReferenceEquals(reloadState.ActiveRequestTokenSource, requestTokenSource))
            {
                reloadState.ActiveRequestTokenSource = null;
            }
            reloadGate.Release();
        }
    }

    private CancellationTokenSource BeginReload()
    {
        var requestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        reloadState.ActiveRequestTokenSource = requestTokenSource;
        return requestTokenSource;
    }

    private void CancelActiveReload() => reloadState.ActiveRequestTokenSource?.Cancel();

    private Task OnToolbarMonthChangedAsync(int value)
    {
        (ToolbarMonth, ToolbarYear) = NormalizeSelectedPeriod(value, ToolbarYear);
        return Task.CompletedTask;
    }

    private Task OnToolbarYearChangedAsync(int value)
    {
        (ToolbarMonth, ToolbarYear) = NormalizeSelectedPeriod(ToolbarMonth, value);
        return Task.CompletedTask;
    }

    private async Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if(string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalizedValue;
        if(HasRequestedData && !HasPendingPeriodChange)
        {
            CurrentPageIndex = 0;
            await ReloadAsync();
        }
    }

    private async Task SelectLockStatusAsync(string lockStatusKey)
    {
        if(string.Equals(SelectedLockStatusKey, lockStatusKey, StringComparison.Ordinal))
        {
            return;
        }

        SelectedLockStatusKey = lockStatusKey;
        CurrentPageIndex = 0;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task OnPageSizeChanged(int value)
    {
        var normalizedValue = PageSizeOptions.Contains(value) ? value : PageSizeOptions[0];
        if(PageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;
        LoadingText = "Đang cập nhật số dòng hiển thị...";
        var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
        PageSize = normalizedValue;
        CurrentPageIndex = firstVisibleRecordIndex / PageSize;

        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            if(HasRequestedData && !HasPendingPeriodChange)
            {
                await ReloadAsync();
            }
        }
        finally
        {
            IsChangingPageSize = false;
            LoadingText = DefaultLoadingText;
        }
    }

    private async Task OnActivePageIndexChangedAsync(int value)
    {
        if(!CanBrowsePages)
        {
            return;
        }

        var normalizedValue = Math.Clamp(value, 0, TotalPageCount - 1);
        if(normalizedValue == CurrentPageIndex)
        {
            return;
        }

        CurrentPageIndex = normalizedValue;
        await ReloadAsync();
    }

    private Task OnColumnChooserRequested()
    {
        GridSection?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    private void OpenRulesPopup()
    {
        RulesPopupPeriodDisplay = $"{ToolbarMonth:00}/{ToolbarYear}";
        IsRulesPopupVisible = true;
    }

    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: true);

    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: false);

    private Task OnRecalculateClickAsync()
    {
        if(CanRecalculate)
        {
            IsRecalculateConfirmPopupVisible = true;
        }

        return Task.CompletedTask;
    }

    private void CloseRecalculateConfirmPopup()
    {
        if(!IsRefreshing)
        {
            IsRecalculateConfirmPopupVisible = false;
        }
    }

    private async Task ConfirmRecalculateAsync()
    {
        if(!CanRecalculate || !IsRecalculateConfirmPopupVisible || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            IsRecalculateConfirmPopupVisible = false;
            LoadingText = $"Đang làm mới tổng kết khấu trừ kỳ {CurrentPayrollPeriodDisplay}...";
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            await ClearSelectionAsync();

            var result = await DataProvider.RecalculatePeriodAsync(
                AppliedYear,
                AppliedMonth,
                disposalTokenSource.Token);
            await ReloadAsync();

            if(result.TargetRowCount == 0)
            {
                ToastService.ShowInfo($"Không có dòng tổng kết khấu trừ nào của kỳ {CurrentPayrollPeriodDisplay} để làm mới.");
                return;
            }

            var resultMessage = $"Kỳ {CurrentPayrollPeriodDisplay}: cập nhật {result.UpdatedCount:N0}, không đổi {result.UnchangedCount:N0}, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.";
            if(result.MissingSourceCount > 0)
            {
                ToastService.ShowWarning($"Đã làm mới tổng kết khấu trừ. {resultMessage} Thiếu {result.MissingSourceCount:N0} nguồn chi tiết; khoản tương ứng được đặt về 0.");
                return;
            }

            ToastService.ShowSuccess($"Đã làm mới tổng kết khấu trừ. {resultMessage}");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể làm mới tổng kết khấu trừ kỳ {CurrentPayrollPeriodDisplay}. Vui lòng tải lại và thử lại.");
        }
        finally
        {
            if(!isDisposed)
            {
                IsRefreshing = false;
                LoadingText = DefaultLoadingText;
            }
        }
    }

    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if(!CanOperateOnCurrentDataset)
        {
            return Task.CompletedTask;
        }

        PendingLockActionState = shouldLock;
        SelectedLockActionScope = CanChooseSelectedRowsScope
            ? LockScopeSelectedRows
            : LockScopeWholePeriod;
        IsLockActionPopupVisible = true;
        return Task.CompletedTask;
    }

    private Task OnGridFilterCriteriaChangedAsync(GridFilterCriteriaChangedEventArgs _)
    {
        IsDeductionTotalSyncPending = true;
        return InvokeAsync(StateHasChanged);
    }

    private void CloseLockActionPopup()
    {
        if(!IsRefreshing)
        {
            IsLockActionPopupVisible = false;
        }
    }

    private void SelectLockActionScope(string scope)
    {
        if(IsRefreshing)
        {
            return;
        }

        if(string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal) && CanChooseSelectedRowsScope)
        {
            SelectedLockActionScope = LockScopeSelectedRows;
        }
        else if(string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal))
        {
            SelectedLockActionScope = LockScopeWholePeriod;
        }
    }

    private Task OnSyncFromPreviousMonthClick()
    {
        if(!CanSyncFromPreviousMonth)
        {
            return Task.CompletedTask;
        }

        IsSyncFromPreviousMonthPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseSyncFromPreviousMonthPopup(bool _) =>
        IsSyncFromPreviousMonthPopupVisible = false;

    private async Task ConfirmSyncFromPreviousMonthAsync()
    {
        if(!CanOperateOnCurrentDataset || !IsSyncFromPreviousMonthPopupVisible)
        {
            return;
        }

        var sourcePeriodDisplay = PreviousMonthSyncSourcePeriodLabel;
        var targetPeriodDisplay = CurrentPayrollPeriodDisplay;

        LoadingText = $"Đang lấy dữ liệu từ kỳ {sourcePeriodDisplay} sang {targetPeriodDisplay}...";
        IsSyncingPreviousMonth = true;
        IsLoading = true;

        try
        {
            var result = await DataProvider.SyncFromPreviousMonthAsync(AppliedMonth, AppliedYear, disposalTokenSource.Token);
            await ReloadAsync();

            if(result.SourceRecordCount == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu kỳ {sourcePeriodDisplay} để đồng bộ sang kỳ {targetPeriodDisplay}.");
                return;
            }

            ToastService.ShowSuccess($"Đã đồng bộ dữ liệu tổng kết khấu trừ từ kỳ {sourcePeriodDisplay} sang {targetPeriodDisplay}: tạo mới {result.CreatedCount:N0}, cập nhật {result.UpdatedCount:N0}, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(Exception)
        {
            ToastService.ShowError("Không thể lấy dữ liệu tổng kết khấu trừ từ tháng trước. Vui lòng thử lại.");
        }
        finally
        {
            if(!isDisposed)
            {
                IsSyncingPreviousMonth = false;
                IsSyncFromPreviousMonthPopupVisible = false;
                IsLoading = false;
                LoadingText = DefaultLoadingText;
            }
        }
    }

    private async Task ToggleLockStateAsync(PayrollDeductionSummaryRecord row)
    {
        if(!CanToggleLock(row))
        {
            return;
        }

        var nextLockedState = !row.IsLocked;
        LoadingText = row.IsLocked ? $"Đang mở khóa dữ liệu của {row.EmployeeDisplay}..." : $"Đang khóa dữ liệu của {row.EmployeeDisplay}...";
        IsLoading = true;

        try
        {
            var updatedRecord = await DataProvider.SetLockStateAsync(
                row.Id,
                nextLockedState,
                row.UpdatedAtUtc ?? row.CreatedAtUtc,
                disposalTokenSource.Token);

            ApplyUpdatedRecord(updatedRecord);
            ToastService.ShowSuccess(
                updatedRecord.IsLocked
                    ? $"Đã khóa dòng tổng kết khấu trừ của {updatedRecord.EmployeeDisplay}."
                    : $"Đã mở khóa dòng tổng kết khấu trừ của {updatedRecord.EmployeeDisplay}.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(Exception)
        {
            ToastService.ShowError("Không thể cập nhật trạng thái khóa dữ liệu tổng kết khấu trừ. Vui lòng thử lại.");
        }
        finally
        {
            if(!isDisposed)
            {
                IsLoading = false;
                LoadingText = DefaultLoadingText;
            }
        }
    }

    private async Task ConfirmLockActionAsync()
    {
        if(!CanConfirmLockAction)
        {
            return;
        }

        var targetIds = Array.Empty<Guid>();
        IReadOnlyList<PayrollDeductionSummaryLockItem>? lockItems = null;
        if(string.Equals(SelectedLockActionScope, LockScopeSelectedRows, StringComparison.Ordinal))
        {
            targetIds = GetSelectedRows().Select(row => row.Id).ToArray();
            lockItems = GetSelectedRows().Select(row => new PayrollDeductionSummaryLockItem(row.Id, row.UpdatedAtUtc ?? row.CreatedAtUtc)).ToArray();
            if(targetIds.Length == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
                return;
            }
        }

        var shouldLock = PendingLockActionState;
        try
        {
            IsRefreshing = true;
            IsLockActionPopupVisible = false;
            if(shouldLock
               && IsEditPopupVisible
               && (string.Equals(SelectedLockActionScope, LockScopeWholePeriod, StringComparison.Ordinal)
                   || targetIds.Contains(EditModel.Id)))
            {
                CloseEditPopup();
            }
            LoadingText = shouldLock
                ? $"Đang khóa dữ liệu tổng kết khấu trừ kỳ {CurrentPayrollPeriodDisplay}..."
                : $"Đang mở khóa dữ liệu tổng kết khấu trừ kỳ {CurrentPayrollPeriodDisplay}...";
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await DataProvider.SetLockStateBatchAsync(
                new SetPayrollDeductionSummaryBatchLockStateRequest(
                    AppliedYear,
                    AppliedMonth,
                    shouldLock,
                    targetIds.Length == 0 ? null : targetIds,
                    Items: lockItems),
                disposalTokenSource.Token);

            if(result.TargetRowCount == 0)
            {
                ToastService.ShowInfo($"Không có dòng tổng kết khấu trừ nào của kỳ {CurrentPayrollPeriodDisplay} trong phạm vi đã chọn.");
                return;
            }

            if(result.UpdatedCount == 0)
            {
                ToastService.ShowInfo($"{result.TargetRowCount:N0} dòng trong phạm vi đã ở trạng thái {LockActionConfirmText.ToLowerInvariant()}.");
                return;
            }

            await ReloadAsync();
            ToastService.ShowSuccess(
                $"Đã {LockActionConfirmText.ToLowerInvariant()} {result.UpdatedCount:N0}/{result.TargetRowCount:N0} dòng tổng kết khấu trừ kỳ {CurrentPayrollPeriodDisplay}.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể {LockActionConfirmText.ToLowerInvariant()} dữ liệu tổng kết khấu trừ kỳ {CurrentPayrollPeriodDisplay}.");
        }
        finally
        {
            if(!isDisposed)
            {
                IsRefreshing = false;
                LoadingText = DefaultLoadingText;
            }
        }
    }

    private void OpenEditPopup(PayrollDeductionSummaryRecord row)
    {
        if(!CanEditRow(row))
        {
            if(row.IsLocked)
            {
                ToastService.ShowWarning("Dòng tổng kết khấu trừ đã khóa nên không thể điều chỉnh.");
            }

            return;
        }

        EditModel = new KhauTruTongKetEditModel
        {
            Id = row.Id,
            EmployeeDisplay = row.EmployeeDisplay,
            PayrollPeriodDisplay = row.PayrollPeriodDisplay,
            OtherDeductionAmount = row.OtherDeductionAmount,
            Note = row.Note,
            OriginalUpdatedAtUtc = row.UpdatedAtUtc ?? row.CreatedAtUtc,
            IsLocked = row.IsLocked
        };
        EditPopupTitle = $"Điều chỉnh khoản khấu trừ khác - {row.EmployeeDisplay}";
        IsEditPopupVisible = true;
    }

    private void CloseEditPopup()
    {
        if(IsSavingEdit)
        {
            return;
        }

        IsEditPopupVisible = false;
        EditModel = new();
        EditPopupTitle = "Điều chỉnh khoản khấu trừ khác";
    }

    private async Task SaveEditAsync()
    {
        if(!CanSaveEdit)
        {
            return;
        }

        try
        {
            IsSavingEdit = true;
            LoadingText = $"Đang cập nhật khoản khấu trừ khác của {EditModel.EmployeeDisplay}...";

            await DataProvider.UpdateManualOtherDeductionAsync(
                EditModel.Id,
                EditModel.OtherDeductionAmount,
                EditModel.Note,
                EditModel.OriginalUpdatedAtUtc,
                disposalTokenSource.Token);

            var employeeDisplay = EditModel.EmployeeDisplay;
            IsEditPopupVisible = false;
            EditModel = new();
            EditPopupTitle = "Điều chỉnh khoản khấu trừ khác";
            await ReloadAsync();
            ToastService.ShowSuccess($"Đã cập nhật khoản khấu trừ khác của {employeeDisplay}.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(Exception)
        {
            ToastService.ShowError("Không thể cập nhật khoản khấu trừ khác. Vui lòng kiểm tra dữ liệu và thử lại.");
        }
        finally
        {
            if(!isDisposed)
            {
                IsSavingEdit = false;
                LoadingText = DefaultLoadingText;
            }
        }
    }

    private async Task RefreshRowAsync(PayrollDeductionSummaryRecord row)
    {
        if(!CanRefreshRow(row) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        try
        {
            IsRefreshingRow = true;
            LoadingText = $"Đang làm mới dữ liệu tổng kết khấu trừ của {row.EmployeeDisplay}...";
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            await ClearSelectionAsync();

            var result = await DataProvider.RefreshAsync(
                new RefreshPayrollDeductionSummaryRequest(
                    row.Id,
                    row.PayrollYear,
                    row.PayrollMonth,
                    row.UpdatedAtUtc ?? row.CreatedAtUtc),
                disposalTokenSource.Token);

            if(result.SkippedLockedCount > 0)
            {
                ToastService.ShowInfo($"Dòng tổng kết khấu trừ của {row.EmployeeDisplay} đã khóa nên không được làm mới.");
                return;
            }

            await ReloadAsync();
            if(result.UpdatedCount > 0)
            {
                var missingSourceMessage = result.MissingSourceCount > 0
                    ? $" Không tìm thấy {result.MissingSourceCount:N0} nguồn chi tiết nên khoản tương ứng đã được đặt về 0."
                    : string.Empty;
                ToastService.ShowSuccess($"Đã làm mới dữ liệu tổng kết khấu trừ của {row.EmployeeDisplay}.{missingSourceMessage}");
                return;
            }

            ToastService.ShowInfo($"Dữ liệu tổng kết khấu trừ của {row.EmployeeDisplay} đã khớp với nguồn chi tiết.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể làm mới dữ liệu tổng kết khấu trừ của {row.EmployeeDisplay}. Vui lòng tải lại và thử lại.");
        }
        finally
        {
            if(!isDisposed)
            {
                IsRefreshingRow = false;
                LoadingText = DefaultLoadingText;
            }
        }
    }

    private async Task OnEmptyStateActionClick()
    {
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            await OnViewRequestedAsync();
            return;
        }

        if(HasActiveDataFilter)
        {
            SearchText = null;
            SelectedLockStatusKey = LockStatusAll;
        }

        await ReloadAsync();
    }

    private int GetLockStatusCount(string lockStatusKey) => lockStatusKey switch
    {
        LockStatusOpen => LockStatusCounts.Open,
        LockStatusLocked => LockStatusCounts.Locked,
        _ => LockStatusCounts.All
    };

    private bool? GetLockFilterValue() => SelectedLockStatusKey switch
    {
        LockStatusOpen => false,
        LockStatusLocked => true,
        _ => null
    };

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];
        if(GridSection is null)
        {
            return;
        }

        await GridSection.ClearSelectionAsync();
    }

    private List<PayrollDeductionSummaryRecord> GetSelectedRows() =>
        SelectedDataItems.OfType<PayrollDeductionSummaryRecord>().DistinctBy(row => row.Id).ToList();

    private int GetSelectedRowCount() => GetSelectedRows().Count;

    private bool CanToggleLock(PayrollDeductionSummaryRecord _) => CanOperateOnCurrentDataset;

    private bool CanEditRow(PayrollDeductionSummaryRecord record) =>
        CanOperateOnCurrentDataset && !record.IsLocked;

    private bool CanRefreshRow(PayrollDeductionSummaryRecord record) =>
        CanOperateOnCurrentDataset && !record.IsLocked && !IsRefreshingRow;

    private void ApplyUpdatedRecord(PayrollDeductionSummaryRecord updatedRecord)
    {
        Records = Records
            .Select(record => record.Id == updatedRecord.Id ? updatedRecord : record)
            .ToArray();
        ResetVisibleDeductionTotal();
        IsDeductionTotalSyncPending = true;

        if(updatedRecord.IsLocked
           && IsEditPopupVisible
           && EditModel.Id == updatedRecord.Id)
        {
            CloseEditPopup();
        }
    }

    private string GetLockStatusFilterCssClass(string lockStatusKey)
    {
        var activeClass = string.Equals(lockStatusKey, SelectedLockStatusKey, StringComparison.Ordinal)
            ? " is-active"
            : string.Empty;
        return $"deduction-summary-summary-button deduction-summary-summary-button-{lockStatusKey}{activeClass}";
    }

    private void ResetVisibleDeductionTotal()
    {
        VisibleDeductionTotal = Records.Sum(record => record.TotalDeductionAmount);
    }

    private void UpdateVisibleDeductionTotalFromGrid()
    {
        VisibleDeductionTotal = GridSection?.GetVisibleDeductionTotal()
            ?? Records.Sum(record => record.TotalDeductionAmount);
    }

    private static string GetLockBadgeCssClass(bool isLocked) =>
        isLocked
            ? "yes-no-status yes-no-status-no hrm-grid-status"
            : "yes-no-status yes-no-status-yes hrm-grid-status";

    private static string GetNotePreview(string? note) => note ?? string.Empty;

    private string BuildExportFileName() =>
        $"payroll-deduction-summary-{AppliedYear:D4}-{AppliedMonth:D2}";

    private static string FormatMoney(decimal value) =>
        value == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", value);

    private static string FormatAuditDate(DateTime? value) =>
        value.HasValue ? value.Value.ToString("dd/MM/yyyy HH:mm", DisplayCulture) : "Chưa cập nhật";

    private MarkupString HighlightSearchText(string? value)
    {
        var source = value ?? string.Empty;
        if(string.IsNullOrWhiteSpace(SearchText) || string.IsNullOrWhiteSpace(source))
        {
            return new MarkupString(HtmlEncoder.Default.Encode(source));
        }

        var searchText = SearchText.Trim();
        var builder = new StringBuilder();
        var startIndex = 0;
        while(startIndex < source.Length)
        {
            var matchIndex = source.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if(matchIndex < 0)
            {
                builder.Append(HtmlEncoder.Default.Encode(source[startIndex..]));
                break;
            }

            builder.Append(HtmlEncoder.Default.Encode(source[startIndex..matchIndex]));
            builder.Append("<mark class=\"deduction-summary-search-highlight\">");
            builder.Append(HtmlEncoder.Default.Encode(source.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            startIndex = matchIndex + searchText.Length;
        }

        return new MarkupString(builder.ToString());
    }

    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        return normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth
            ? (MinimumSupportedMonth, MinimumSupportedYear)
            : (normalizedMonth, normalizedYear);
    }

    private (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = TimeZoneInfo.ConvertTime(TimeProvider.GetUtcNow(), ResolvePayrollTimeZone());
        var normalizedPeriod = NormalizeSelectedPeriod(localNow.Month, localNow.Year);
        if(normalizedPeriod.Month != localNow.Month || normalizedPeriod.Year != localNow.Year)
        {
            DefaultPeriodWarningMessage =
                $"Kỳ hiện tại {localNow.Month:00}/{localNow.Year} nằm ngoài phạm vi hỗ trợ; hệ thống chọn {normalizedPeriod.Month:00}/{normalizedPeriod.Year}.";
        }

        return normalizedPeriod;
    }

    private static TimeZoneInfo ResolvePayrollTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(PayrollTimeZoneId);
        }
        catch(TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(PayrollTimeZoneWindowsId);
        }
    }

    private static (int Month, int Year) GetPreviousPeriod(int month, int year) =>
        month == 1 ? (12, year - 1) : (month - 1, year);

    #endregion

    #region Giải phóng và kiểu nội bộ

    public void Dispose()
    {
        isDisposed = true;
        CancelActiveReload();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    private sealed record MonthOption(int Value, string Text);

    private sealed record LockStatusFilter(string Key, string Label);

    #endregion
}
