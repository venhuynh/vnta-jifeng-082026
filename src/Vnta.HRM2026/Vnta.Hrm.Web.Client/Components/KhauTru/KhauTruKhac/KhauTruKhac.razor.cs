using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Services.Api;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruKhac;

public partial class KhauTruKhac : IDisposable
{
    #region Hằng số và cấu hình màn hình

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly (int Month, int Year) DefaultPayrollPeriod = GetDefaultPayrollPeriod();
    private static readonly IReadOnlyList<MonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, $"Tháng {month:00}"))
            .ToArray();

    private const int MinimumSupportedMonth = 6;
    private const int MinimumSupportedYear = 2026;
    private const int MaximumSupportedYear = 2100;
    private const string LockScopeSelectedRows = "selected-rows";
    private const string LockScopeWholePeriod = "whole-period";
    private const string DeductionAmountTotalSummaryName = "DeductionAmountTotal";
    private const string DefaultLoadingText = "Đang tải dữ liệu khấu trừ khác...";

    #endregion

    #region Phụ thuộc được tiêm

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);

    [Inject]
    private PayrollEmployeeOtherDeductionAllowanceDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private MonthlyWorkSummaryDataProvider MonthlyWorkSummaryDataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    #endregion

    #region Trạng thái màn hình

    private IReadOnlyList<KhauTruKhacRecord> AllRecords { get; set; } = [];
    private IReadOnlyList<KhauTruKhacRecord> ExportRecords { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private IGrid? ExportGrid { get; set; }
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private int ToolbarMonth { get; set; } = DefaultPayrollPeriod.Month;
    private int ToolbarYear { get; set; } = DefaultPayrollPeriod.Year;
    private int AppliedMonth { get; set; } = DefaultPayrollPeriod.Month;
    private int AppliedYear { get; set; } = DefaultPayrollPeriod.Year;
    private int PageSize { get; set; } = 50;
    private bool HasRequestedData { get; set; }
    private bool IsPreparingPeriod { get; set; }
    private bool IsLoading { get; set; }
    private bool IsRefreshing { get; set; }
    private bool IsRefreshingRow { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool IsExporting { get; set; }
    private bool IsRulesPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupLoading { get; set; }
    private bool IsEditPopupVisible { get; set; }
    private bool IsRecalculateConfirmPopupVisible { get; set; }
    private bool IsLockActionPopupVisible { get; set; }
    private bool IsSavingEdit { get; set; }
    private bool PendingLockActionState { get; set; } = true;
    private int PendingLockActionMonth { get; set; } = DefaultPayrollPeriod.Month;
    private int PendingLockActionYear { get; set; } = DefaultPayrollPeriod.Year;
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;
    private KhauTruKhacEditModel EditModel { get; set; } = new();
    private string EditPopupTitle { get; set; } = "Sửa khấu trừ khác";
    private string? MonthlyWorkPopupErrorMessage { get; set; }
    private string MonthlyWorkPopupTitle { get; set; } = "Chi tiết khấu trừ khác";
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    private KhauTruKhacRecord? MonthlyWorkPopupRecord { get; set; }
    private decimal MonthlyWorkPopupSalaryWorkDays { get; set; }
    private string LoadingText { get; set; } = DefaultLoadingText;
    private decimal VisibleDeductionTotal { get; set; }
    private bool IsDeductionTotalSyncPending { get; set; }
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;

    #endregion

    #region Trạng thái suy diễn và quyền thao tác

    private IReadOnlyList<KhauTruKhacRecord> VisibleRecords => AllRecords;

    private IReadOnlyList<MonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : MonthOptions;

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool HasPendingPeriodChange =>
        HasRequestedData
        && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);

    private bool ShowLoadingPanel =>
        IsPreparingPeriod
        || IsLoading
        || IsRefreshing
        || IsRefreshingRow
        || IsChangingPageSize
        || IsExporting
        || IsSavingEdit;

    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    private bool CanView => !ShowLoadingPanel;
    private bool CanOperateOnCurrentDataset => CanInteract && HasRequestedData && !HasPendingPeriodChange;
    private bool CanRecalculate => CanOperateOnCurrentDataset;
    private int SelectedRecordCount => GetSelectedRecords().Count;
    private bool CanOpenLockAction => CanOperateOnCurrentDataset;
    private bool CanOpenUnlockAction => CanOperateOnCurrentDataset;
    private bool CanChooseSelectedRowsScope => SelectedRecordCount > 0;
    private bool CanConfirmLockAction =>
        CanOperateOnCurrentDataset
        && (string.Equals(SelectedLockActionScope, LockScopeWholePeriod, StringComparison.Ordinal) || CanChooseSelectedRowsScope);
    private bool CanChangeFilters => !ShowLoadingPanel;
    private bool CanExport => CanOperateOnCurrentDataset;
    private bool CanSaveEdit =>
        !IsSavingEdit
        && !HasPendingPeriodChange
        && EditModel.PayrollDeductionSummaryRecordId != Guid.Empty
        && EditModel.OriginalUpdatedAtUtc.HasValue
        && !EditModel.IsLocked;

    private string CurrentPeriodLabel => $"{ToolbarMonth:00}/{ToolbarYear}";
    private string AppliedPeriodLabel => $"{AppliedMonth:00}/{AppliedYear}";
    private string PendingLockActionPeriodLabel => $"{PendingLockActionMonth:00}/{PendingLockActionYear}";
    private string LockActionPopupTitle => PendingLockActionState
        ? "Khóa dữ liệu khấu trừ khác"
        : "Mở khóa dữ liệu khấu trừ khác";
    private string LockActionConfirmText => PendingLockActionState ? "Khóa" : "Mở khóa";
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa dữ liệu khấu trừ khác."
        : "Chọn phạm vi cần mở khóa dữ liệu khấu trừ khác.";
    private string LockActionScopeContextText =>
        $"Kỳ lương áp dụng: {PendingLockActionPeriodLabel}. Lựa chọn toàn kỳ sẽ bỏ qua bộ lọc tìm kiếm hiện tại.";
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {SelectedRecordCount:N0} dòng đang được chọn trong lưới."
        : "Chưa có dòng nào được chọn trong lưới hiện tại.";
    private string WholePeriodScopeDescription => PendingLockActionState
        ? $"Áp dụng cho toàn bộ dữ liệu khấu trừ khác của kỳ {PendingLockActionPeriodLabel}."
        : $"Mở khóa toàn bộ dữ liệu khấu trừ khác của kỳ {PendingLockActionPeriodLabel}.";

    private string EmptyStateTitle => !HasRequestedData
        ? "Chưa tải dữ liệu khấu trừ khác"
        : HasPendingPeriodChange
            ? "Kỳ lương đã thay đổi"
            : !string.IsNullOrWhiteSpace(SearchText)
                ? "Không tìm thấy dòng khấu trừ khác phù hợp"
                : "Chưa có dữ liệu khấu trừ khác";

    private string EmptyStateMessage => !HasRequestedData
        ? "Chọn tháng, năm kỳ lương rồi nhấn Xem để tải dữ liệu khi bạn sẵn sàng."
        : HasPendingPeriodChange
            ? $"Bạn đã đổi kỳ lương sang {CurrentPeriodLabel}. Nhấn Xem để tải dữ liệu của kỳ này."
            : !string.IsNullOrWhiteSpace(SearchText)
                ? "Hãy thử từ khóa khác để xem thêm dữ liệu."
                : $"Dữ liệu khấu trừ khác của kỳ {AppliedPeriodLabel} sẽ hiển thị tại đây sau khi hệ thống tạo snapshot cho kỳ lương này.";

    private string EmptyStateActionText => !HasRequestedData || HasPendingPeriodChange
        ? "Xem dữ liệu"
        : "Tải lại";

    #endregion

    #region Điểm vào của giao diện

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        if(IsDeductionTotalSyncPending)
        {
            IsDeductionTotalSyncPending = false;
            UpdateVisibleDeductionTotalFromGrid();
            return InvokeAsync(StateHasChanged);
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    #endregion

    #region Chuẩn bị và tải dữ liệu

    private async Task OnViewRequestedAsync()
    {
        if(!CanView)
        {
            return;
        }

        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        LoadErrorMessage = null;

        try
        {
            IsPreparingPeriod = true;
            SetLoadingText($"Đang chuẩn bị dữ liệu khấu trừ khác kỳ {CurrentPeriodLabel}...");
            await ClearSelectionAsync();
            await DataProvider.PreparePeriodAsync(ToolbarYear, ToolbarMonth, disposalTokenSource.Token);

            AppliedMonth = ToolbarMonth;
            AppliedYear = ToolbarYear;
            HasRequestedData = true;

            await ReloadAsync();
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(HrmApiException ex)
        {
            LoadErrorMessage = ex.UserMessage;
            ToastService.ShowError(ex.UserMessage);
        }
        catch(Exception)
        {
            const string errorMessage = "Không thể chuẩn bị dữ liệu khấu trừ khác. Vui lòng thử lại.";
            LoadErrorMessage = errorMessage;
            ToastService.ShowError(errorMessage);
        }
        finally
        {
            IsPreparingPeriod = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    private Task OnRetryAsync()
    {
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            return OnViewRequestedAsync();
        }

        return ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if(!HasRequestedData || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref reloadRequestedVersion);
        if(!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        try
        {
            while(!disposalTokenSource.IsCancellationRequested
                  && reloadProcessedVersion < Volatile.Read(ref reloadRequestedVersion))
            {
                reloadProcessedVersion = Volatile.Read(ref reloadRequestedVersion);
                await ReloadCoreAsync();
            }
        }
        finally
        {
            reloadGate.Release();
        }
    }

    private async Task ReloadCoreAsync()
    {
        LoadErrorMessage = null;
        IsLoading = true;
        SetLoadingText(DefaultLoadingText);

        try
        {
            await ClearSelectionAsync();
            AllRecords = await DataProvider.SearchAsync(BuildFilter(), disposalTokenSource.Token);
            ResetVisibleDeductionTotal();
            IsDeductionTotalSyncPending = true;
            await PruneSelectionToVisibleRecordsAsync();
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            AllRecords = [];
            ResetVisibleDeductionTotal();
            const string errorMessage = "Không thể tải dữ liệu khấu trừ khác. Vui lòng thử lại.";
            LoadErrorMessage = errorMessage;
            ToastService.ShowError(errorMessage);
        }
        finally
        {
            IsLoading = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    #endregion

    #region Thao tác trên thanh công cụ và màn hình

    private Task OnSelectedMonthChangedAsync(int month)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(month, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    private Task OnSelectedYearChangedAsync(int year)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, year);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    private Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = NormalizeOptional(value);
        if(string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        SearchText = normalizedValue;
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            return Task.CompletedTask;
        }

        return ReloadAsync();
    }

    private async Task OnPageSizeChanged(int value)
    {
        if(PageSize == value)
        {
            return;
        }

        IsChangingPageSize = true;
        PageSize = value;
        SetLoadingText("Đang cập nhật số dòng hiển thị...");

        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }
        finally
        {
            IsChangingPageSize = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    private async Task OnEmptyStateActionClick()
    {
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            await OnViewRequestedAsync();
            return;
        }

        await ReloadAsync();
    }

    private void OpenRulesPopup()
    {
        IsRulesPopupVisible = true;
    }

    private Task OnRecalculateClickAsync()
    {
        if(!CanRecalculate)
        {
            return Task.CompletedTask;
        }

        IsRecalculateConfirmPopupVisible = true;
        return Task.CompletedTask;
    }

    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: true);

    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: false);

    private void CloseRecalculateConfirmPopup()
    {
        IsRecalculateConfirmPopupVisible = false;
    }

    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if(!CanOperateOnCurrentDataset)
        {
            return Task.CompletedTask;
        }

        PendingLockActionState = shouldLock;
        PendingLockActionMonth = AppliedMonth;
        PendingLockActionYear = AppliedYear;
        SelectedLockActionScope = CanChooseSelectedRowsScope
            ? LockScopeSelectedRows
            : LockScopeWholePeriod;
        IsLockActionPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseLockActionPopup()
    {
        if(IsRefreshing)
        {
            return;
        }

        IsLockActionPopupVisible = false;
    }

    private void SelectLockActionScope(string scope)
    {
        if(IsRefreshing)
        {
            return;
        }

        if(string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal))
        {
            if(!CanChooseSelectedRowsScope)
            {
                return;
            }

            SelectedLockActionScope = LockScopeSelectedRows;
            return;
        }

        if(string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal))
        {
            SelectedLockActionScope = LockScopeWholePeriod;
        }
    }

    private async Task ConfirmRecalculateAsync()
    {
        CloseRecalculateConfirmPopup();

        if(!CanRecalculate)
        {
            return;
        }

        try
        {
            // Bộ lọc có thể đã đổi sau lần tải gần nhất; chỉ kỳ đã áp dụng mới xác định snapshot được phép tính lại.
            IsRefreshing = true;
            SetLoadingText($"Đang tính lại dữ liệu khấu trừ khác kỳ {AppliedPeriodLabel}...");
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            await ClearSelectionAsync();

            var result = await DataProvider.RefreshAsync(
                new RefreshPayrollEmployeeOtherDeductionAllowanceRequest(
                    AppliedYear,
                    AppliedMonth),
                disposalTokenSource.Token);

            await ReloadAsync();
            ToastService.ShowSuccess(
                $"Đã tính lại {result.UpdatedCount:N0} dòng khấu trừ khác của kỳ {AppliedPeriodLabel}, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể tính lại khấu trừ khác của kỳ {AppliedPeriodLabel}.");
        }
        finally
        {
            IsRefreshing = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    private void CloseRulesPopup()
    {
        IsRulesPopupVisible = false;
    }

    private void OpenEditPopup(KhauTruKhacRecord record)
    {
        if(!CanOperateOnCurrentDataset)
        {
            return;
        }

        if(record.IsLocked)
        {
            ToastService.ShowWarning("Dòng khấu trừ khác đã khóa nên không thể chỉnh sửa thủ công.");
            return;
        }

        EditModel = new KhauTruKhacEditModel
        {
            PayrollDeductionSummaryRecordId = record.PayrollDeductionSummaryRecordId,
            EmployeeDisplay = record.EmployeeDisplay,
            Description = record.DescriptionDisplay,
            DeductionAmount = record.DeductionAmount,
            Note = record.Note,
            IsLocked = record.IsLocked,
            OriginalUpdatedAtUtc = record.VersionAtUtc
        };
        EditPopupTitle = $"Sửa khấu trừ khác - {record.EmployeeDisplay}";
        IsEditPopupVisible = true;
    }

    private void CloseEditPopup()
    {
        if(IsSavingEdit)
        {
            return;
        }

        CloseEditPopupCore();
    }

    private void CloseEditPopupCore()
    {
        IsEditPopupVisible = false;
        EditModel = new();
        EditPopupTitle = "Sửa khấu trừ khác";
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
            SetLoadingText($"Đang cập nhật khấu trừ khác của {EditModel.EmployeeDisplay}...");

            var updatedRecord = await DataProvider.UpdateManualValuesAsync(
                EditModel.PayrollDeductionSummaryRecordId,
                EditModel.DeductionAmount,
                EditModel.Note,
                EditModel.OriginalUpdatedAtUtc,
                disposalTokenSource.Token);

            CloseEditPopupCore();
            await ReloadAsync();
            ToastService.ShowSuccess($"Đã cập nhật khấu trừ khác của {updatedRecord.EmployeeDisplay}.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể cập nhật khấu trừ khác của {EditModel.EmployeeDisplay}.");
        }
        finally
        {
            IsSavingEdit = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    private async Task RefreshRowAsync(KhauTruKhacRecord record)
    {
        if(!CanRefreshRow(record))
        {
            return;
        }

        try
        {
            IsRefreshingRow = true;
            SetLoadingText($"Đang làm mới khấu trừ khác của {record.EmployeeDisplay}...");

            var result = await DataProvider.RefreshAsync(
                new RefreshPayrollEmployeeOtherDeductionAllowanceRequest(
                    record.PayrollYear,
                    record.PayrollMonth,
                    record.PayrollDeductionSummaryRecordId),
                disposalTokenSource.Token);

            await ReloadAsync();
            ToastService.ShowSuccess(
                $"Đã làm mới {result.UpdatedCount:N0} dòng khấu trừ khác, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể làm mới khấu trừ khác của {record.EmployeeDisplay}.");
        }
        finally
        {
            IsRefreshingRow = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    private async Task ToggleLockStateAsync(KhauTruKhacRecord record)
    {
        if(!CanToggleLock(record))
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            SetLoadingText($"Đang {(record.IsLocked ? "mở khóa" : "khóa")} dòng khấu trừ khác của {record.EmployeeDisplay}...");
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var updatedRecord = await DataProvider.SetLockStateAsync(
                record.PayrollDeductionSummaryRecordId,
                !record.IsLocked,
                disposalTokenSource.Token);

            await ReloadAsync();
            if(HasLoadError)
            {
                return;
            }

            ToastService.ShowSuccess(
                updatedRecord.IsLocked
                    ? $"Đã khóa dòng khấu trừ khác của {updatedRecord.EmployeeDisplay}."
                    : $"Đã mở khóa dòng khấu trừ khác của {updatedRecord.EmployeeDisplay}.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể cập nhật trạng thái khóa của {record.EmployeeDisplay}.");
        }
        finally
        {
            IsRefreshing = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    private async Task ConfirmLockActionAsync()
    {
        var shouldLock = PendingLockActionState;
        var actionScope = SelectedLockActionScope;

        if(!CanOperateOnCurrentDataset)
        {
            return;
        }

        Guid[]? targetRecordIds = null;
        var targetRowCount = 0;
        if(!IsWholePeriodLockActionScope(actionScope))
        {
            var selectedRecords = GetSelectedRecords()
                .DistinctBy(record => record.PayrollDeductionSummaryRecordId)
                .ToArray();
            if(selectedRecords.Length == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
                return;
            }

            targetRecordIds = selectedRecords
                .Select(record => record.PayrollDeductionSummaryRecordId)
                .ToArray();
            targetRowCount = targetRecordIds.Length;

            if(shouldLock
               && IsEditPopupVisible
               && selectedRecords.Any(record => record.PayrollDeductionSummaryRecordId == EditModel.PayrollDeductionSummaryRecordId))
            {
                CloseEditPopupCore();
            }
        }
        else if(shouldLock && IsEditPopupVisible)
        {
            CloseEditPopupCore();
        }

        try
        {
            IsRefreshing = true;
            IsLockActionPopupVisible = false;
            SetLoadingText(BuildLockActionPendingLoadingMessage(shouldLock, actionScope, targetRowCount > 0 ? targetRowCount : null));
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await DataProvider.SetLockStateBatchAsync(
                new SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateRequest(
                    PendingLockActionYear,
                    PendingLockActionMonth,
                    shouldLock,
                    targetRecordIds),
                disposalTokenSource.Token);

            if(result.TargetRowCount == 0)
            {
                ToastService.ShowInfo(BuildLockActionNoDataMessage(shouldLock, actionScope));
                return;
            }

            if(result.UpdatedCount == 0)
            {
                ToastService.ShowInfo(BuildLockActionAlreadyAppliedMessage(shouldLock, actionScope, result.TargetRowCount));
                return;
            }

            await ReloadAsync();
            ToastService.ShowSuccess(BuildLockActionSuccessMessage(shouldLock, actionScope, result.TargetRowCount, result.UpdatedCount));
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể {LockActionConfirmText.ToLowerInvariant()} dữ liệu khấu trừ khác của kỳ {AppliedPeriodLabel}.");
        }
        finally
        {
            IsRefreshing = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    private Task OnColumnChooserRequested()
    {
        Grid?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private Task OnGridFilterCriteriaChangedAsync(GridFilterCriteriaChangedEventArgs _)
    {
        IsDeductionTotalSyncPending = true;
        return InvokeAsync(StateHasChanged);
    }

    private void ResetVisibleDeductionTotal()
    {
        VisibleDeductionTotal = VisibleRecords.Sum(record => record.DeductionAmount);
    }

    private void UpdateVisibleDeductionTotalFromGrid()
    {
        var summaryItem = Grid?.GetTotalSummaryItems()
            .FirstOrDefault(item => string.Equals(item.Name, DeductionAmountTotalSummaryName, StringComparison.Ordinal));
        var summaryValue = summaryItem is null ? null : Grid!.GetTotalSummaryValue(summaryItem);
        VisibleDeductionTotal = summaryValue switch
        {
            decimal value => value,
            null => 0m,
            IConvertible value => Convert.ToDecimal(value, DisplayCulture),
            _ => 0m
        };
    }

    #endregion

    #region Hỗ trợ chọn dòng và lọc dữ liệu

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];

        if(Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private async Task PruneSelectionToVisibleRecordsAsync()
    {
        if(SelectedDataItems.Count == 0)
        {
            return;
        }

        var visibleIds = VisibleRecords
            .Select(record => record.Id)
            .ToHashSet();
        var visibleSelection = SelectedDataItems
            .OfType<KhauTruKhacRecord>()
            .Where(record => visibleIds.Contains(record.Id))
            .DistinctBy(record => record.Id)
            .Cast<object>()
            .ToArray();

        if(visibleSelection.Length == SelectedDataItems.Count)
        {
            return;
        }

        SelectedDataItems = visibleSelection;
        if(visibleSelection.Length == 0)
        {
            Grid?.SetFocusedRowIndex(-1);
        }

        await InvokeAsync(StateHasChanged);
    }

    private List<KhauTruKhacRecord> GetSelectedRecords()
    {
        var selectedIds = SelectedDataItems
            .OfType<KhauTruKhacRecord>()
            .Select(record => record.Id)
            .ToHashSet();

        return VisibleRecords
            .Where(record => selectedIds.Contains(record.Id))
            .DistinctBy(record => record.Id)
            .ToList();
    }

    private PayrollEmployeeOtherDeductionAllowanceFilter BuildFilter() =>
        new(
            HasRequestedData ? AppliedMonth : ToolbarMonth,
            HasRequestedData ? AppliedYear : ToolbarYear,
            null,
            SearchText);

    private bool IsVisibleRecord(KhauTruKhacRecord record) =>
        VisibleRecords.Any(row => row.Id == record.Id);

    private bool CanEditRow(KhauTruKhacRecord record) =>
        CanOperateOnCurrentDataset && !record.IsLocked;

    private bool CanRefreshRow(KhauTruKhacRecord record) =>
        CanOperateOnCurrentDataset && !record.IsLocked;

    private bool CanToggleLock(KhauTruKhacRecord record) =>
        CanOperateOnCurrentDataset;

    private bool CanViewMonthlyWork(KhauTruKhacRecord record) =>
        CanOperateOnCurrentDataset && record.EmployeeId != Guid.Empty;

    private static bool IsWholePeriodLockActionScope(string scope) =>
        string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal);

    private string BuildExportFileName() =>
        $"khau-tru-khac-{AppliedYear}-{AppliedMonth:00}";

    private string BuildLockActionNoDataMessage(bool shouldLock, string scope)
    {
        if(IsWholePeriodLockActionScope(scope))
        {
            return $"Không có dữ liệu khấu trừ khác của kỳ {PendingLockActionPeriodLabel} để {(shouldLock ? "khóa" : "mở khóa")}.";
        }

        return "Không còn dòng khấu trừ khác hợp lệ trong phạm vi đang chọn để xử lý.";
    }

    private string BuildLockActionAlreadyAppliedMessage(bool shouldLock, string scope, int targetRowCount)
    {
        var stateText = shouldLock ? "khóa" : "mở";
        if(IsWholePeriodLockActionScope(scope))
        {
            return $"Không có dòng nào cần {(shouldLock ? "khóa" : "mở khóa")}. {targetRowCount:N0} dòng của kỳ {PendingLockActionPeriodLabel} đã ở trạng thái {stateText}.";
        }

        return $"Không có dòng nào cần {(shouldLock ? "khóa" : "mở khóa")}. {targetRowCount:N0} dòng đã chọn đã ở trạng thái {stateText}.";
    }

    private string BuildLockActionPendingLoadingMessage(bool shouldLock, string scope, int? affectedCount = null)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        if(!IsWholePeriodLockActionScope(scope) && affectedCount.HasValue)
        {
            return $"Đang xử lý {actionText} {affectedCount.Value:N0} dòng khấu trừ khác đã chọn...";
        }

        return IsWholePeriodLockActionScope(scope)
            ? $"Đang xử lý {actionText} dữ liệu khấu trừ khác của kỳ {PendingLockActionPeriodLabel}..."
            : $"Đang xử lý {actionText} các dòng khấu trừ khác đã chọn...";
    }

    private string BuildLockActionSuccessMessage(bool shouldLock, string scope, int targetRowCount, int updatedCount)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        var unchangedCount = Math.Max(0, targetRowCount - updatedCount);

        return IsWholePeriodLockActionScope(scope)
            ? unchangedCount > 0
                ? $"Đã {actionText} {updatedCount:N0}/{targetRowCount:N0} dòng khấu trừ khác của kỳ {PendingLockActionPeriodLabel}, giữ nguyên {unchangedCount:N0} dòng đã đúng trạng thái."
                : $"Đã {actionText} {updatedCount:N0} dòng khấu trừ khác của kỳ {PendingLockActionPeriodLabel}."
            : unchangedCount > 0
                ? $"Đã {actionText} {updatedCount:N0}/{targetRowCount:N0} dòng đã chọn, giữ nguyên {unchangedCount:N0} dòng đã đúng trạng thái."
                : $"Đã {actionText} {updatedCount:N0} dòng khấu trừ khác đã chọn.";
    }

    private void ApplyUpdatedRecord(KhauTruKhacRecord updatedRecord)
    {
        AllRecords = AllRecords
            .Select(item => item.PayrollDeductionSummaryRecordId == updatedRecord.PayrollDeductionSummaryRecordId
                ? updatedRecord
                : item)
            .ToArray();

        if(updatedRecord.IsLocked
           && IsEditPopupVisible
           && EditModel.PayrollDeductionSummaryRecordId == updatedRecord.PayrollDeductionSummaryRecordId)
        {
            CloseEditPopupCore();
        }
    }

    #endregion

    #region Hỗ trợ hiển thị

    private string FormatCurrency(decimal value) =>
        value == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", value);

    private static string FormatDate(DateTime? value) =>
        value.HasValue ? value.Value.ToString("dd/MM/yyyy", DisplayCulture) : "--";

    private static string FormatWorkDays(decimal? value) =>
        value.HasValue ? value.Value.ToString("0.0", DisplayCulture) : "--";

    private static string GetDisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();

    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = GetDisplayValue(value);
        if(string.IsNullOrWhiteSpace(SearchText))
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var searchText = SearchText.Trim();
        if(searchText.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var startIndex = 0;
        var builder = new StringBuilder(displayText.Length + 32);
        while(true)
        {
            var matchIndex = displayText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if(matchIndex < 0)
            {
                break;
            }

            builder.Append(WebUtility.HtmlEncode(displayText[startIndex..matchIndex]));
            builder.Append("<mark class=\"other-deduction-search-highlight\">");
            builder.Append(WebUtility.HtmlEncode(displayText.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            startIndex = matchIndex + searchText.Length;
        }

        if(builder.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        builder.Append(WebUtility.HtmlEncode(displayText[startIndex..]));
        return new MarkupString(builder.ToString());
    }

    private static string GetLockStatusCssClass(bool isLocked) => string.Join(
        ' ',
        "yes-no-status",
        isLocked ? "yes-no-status-no hrm-grid-status" : "yes-no-status-yes hrm-grid-status");

    private static string GetRuleStatusCssClass(string? ruleKey) => string.Join(
        ' ',
        "kind-status",
        ResolveRuleStatusCssClass(ruleKey));

    private static string ResolveRuleStatusCssClass(string? _) => "kind-status-neutral";

    private void SetLoadingText(string value)
    {
        LoadingText = value;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        if(normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth)
        {
            return (MinimumSupportedMonth, MinimumSupportedYear);
        }

        return (normalizedMonth, normalizedYear);
    }

    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        return NormalizeSelectedPeriod(localNow.Month, localNow.Year);
    }

    #endregion

    #region Giải phóng tài nguyên và kiểu nội bộ

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    private sealed record MonthOption(int Value, string Text);

    #endregion
}
