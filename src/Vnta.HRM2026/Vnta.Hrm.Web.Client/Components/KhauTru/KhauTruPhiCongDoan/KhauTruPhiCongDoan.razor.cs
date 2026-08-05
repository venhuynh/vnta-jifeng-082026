using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.Api;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruPhiCongDoan;

public partial class KhauTruPhiCongDoan : IDisposable
{
    private const int MinimumSupportedMonth = 6;
    private const int MinimumSupportedYear = 2026;
    private const int MaximumSupportedYear = 2100;
    private const int PageSize = 50;
    private const int ExportPageSize = 200;
    private const decimal MaximumManualDeductionAmount = 9_999_999_999_999_999.99m;
    private const string LockScopeSelectedRows = "selected-rows";
    private const string LockScopeWholePeriod = "whole-period";
    private static readonly IReadOnlyList<MonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, $"Tháng {month:00}"))
            .ToArray();
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);

    [Inject] private PayrollUnionFeeDeductionDataProvider DataProvider { get; set; } = default!;
    [Inject] private MonthlyWorkSummaryDataProvider MonthlyWorkSummaryDataProvider { get; set; } = default!;
    [Inject] private IHrmToastService ToastService { get; set; } = default!;
    [Inject] private ILogger<KhauTruPhiCongDoan> Logger { get; set; } = default!;

    private IReadOnlyList<PayrollUnionFeeDeductionRecord> Records { get; set; } = [];
    private IReadOnlyList<PayrollUnionFeeDeductionRecord> ExportRecords { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private IGrid? ExportGrid { get; set; }
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string LoadingText { get; set; } = "Đang tải dữ liệu khấu trừ phí công đoàn...";
    private int ToolbarMonth { get; set; }
    private int ToolbarYear { get; set; }
    private int AppliedMonth { get; set; }
    private int AppliedYear { get; set; }
    private int CurrentPageIndex { get; set; }
    private int TotalRecordCount { get; set; }
    private bool HasRequestedData { get; set; }
    private bool IsPreparingPeriod { get; set; }
    private bool IsLoading { get; set; }
    private bool IsRefreshing { get; set; }
    private bool IsUpdatingLock { get; set; }
    private bool IsExporting { get; set; }
    private bool IsEditPopupVisible { get; set; }
    private bool IsSavingEdit { get; set; }
    private KhauTruPhiCongDoanEditModel EditModel { get; set; } = new();
    private string EditPopupTitle { get; set; } = "Điều chỉnh phí công đoàn";
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;

    private bool IsMonthlyWorkPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupLoading { get; set; }
    private string? MonthlyWorkPopupErrorMessage { get; set; }
    private string MonthlyWorkPopupTitle { get; set; } = "Đối chiếu bảng công tháng";
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    private PayrollUnionFeeDeductionRecord? MonthlyWorkPopupRecord { get; set; }
    private bool IsRulesPopupVisible { get; set; }
    private bool IsRecalculateConfirmPopupVisible { get; set; }
    private bool IsLockActionPopupVisible { get; set; }
    private bool PendingLockActionState { get; set; } = true;
    private int PendingLockActionMonth { get; set; }
    private int PendingLockActionYear { get; set; }
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool HasPendingPeriodChange => ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear;
    private bool ShowLoadingPanel => IsPreparingPeriod || IsLoading || IsRefreshing || IsUpdatingLock || IsExporting || IsSavingEdit;
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    private bool CanView => !ShowLoadingPanel;
    private bool CanChangeFilters => !ShowLoadingPanel;
    private bool CanOperateOnCurrentDataset => CanInteract && HasRequestedData && !HasPendingPeriodChange;
    private bool CanRecalculate => CanOperateOnCurrentDataset;
    private bool CanEditFields => CanOperateOnCurrentDataset && !IsSavingEdit && !EditModel.IsLocked;
    private bool CanSaveEdit =>
        CanEditFields
        && EditModel.PayrollDeductionSummaryRecordId != Guid.Empty
        && EditModel.OriginalVersionAtUtc != default
        && EditModel.DeductionAmount is >= 0m and <= MaximumManualDeductionAmount
        && decimal.Round(EditModel.DeductionAmount, 2, MidpointRounding.AwayFromZero) == EditModel.DeductionAmount;
    private IReadOnlyList<MonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : MonthOptions;
    private bool CanExport => CanOperateOnCurrentDataset && TotalRecordCount > 0;
    private int SelectedRecordCount => GetSelectedRecords().Count;
    private bool CanOpenLockAction => CanOperateOnCurrentDataset;
    private bool CanOpenUnlockAction => CanOperateOnCurrentDataset;
    private bool CanChooseSelectedRowsScope => SelectedRecordCount > 0;
    private bool CanConfirmLockAction =>
        CanOperateOnCurrentDataset
        && (string.Equals(SelectedLockActionScope, LockScopeWholePeriod, StringComparison.Ordinal) || CanChooseSelectedRowsScope);
    private bool CanGoToPreviousPage => CanOperateOnCurrentDataset && CurrentPageIndex > 0;
    private bool CanGoToNextPage => CanOperateOnCurrentDataset && CurrentPageEndRecord < TotalRecordCount;
    private int CurrentPageStartRecord => TotalRecordCount == 0 ? 0 : CurrentPageIndex * PageSize + 1;
    private int CurrentPageEndRecord => TotalRecordCount == 0 ? 0 : CurrentPageStartRecord + Records.Count - 1;
    private decimal CurrentPageDeductionTotal => Records.Sum(record => record.DeductionAmount);
    private string AppliedPeriodLabel => $"{AppliedMonth:00}/{AppliedYear}";
    private string PendingLockActionPeriodLabel => $"{PendingLockActionMonth:00}/{PendingLockActionYear}";
    private string LockActionPopupTitle => PendingLockActionState
        ? "Khóa dữ liệu khấu trừ phí công đoàn"
        : "Mở khóa dữ liệu khấu trừ phí công đoàn";
    private string LockActionConfirmText => PendingLockActionState ? "Khóa" : "Mở khóa";
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa dữ liệu khấu trừ phí công đoàn."
        : "Chọn phạm vi cần mở khóa dữ liệu khấu trừ phí công đoàn.";
    private string LockActionScopeContextText =>
        $"Kỳ lương áp dụng: {PendingLockActionPeriodLabel}. Lựa chọn toàn kỳ sẽ bỏ qua bộ lọc tìm kiếm hiện tại.";
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {SelectedRecordCount:N0} dòng đang được chọn trong lưới."
        : "Chưa có dòng nào được chọn trong lưới hiện tại.";
    private string WholePeriodScopeDescription => PendingLockActionState
        ? $"Áp dụng cho toàn bộ dữ liệu khấu trừ phí công đoàn của kỳ {PendingLockActionPeriodLabel}."
        : $"Mở khóa toàn bộ dữ liệu khấu trừ phí công đoàn của kỳ {PendingLockActionPeriodLabel}.";
    private string PagerSummaryText => TotalRecordCount == 0
        ? "Không có bản ghi"
        : $"Trang {CurrentPageIndex + 1:N0} · {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} trên {TotalRecordCount:N0} bản ghi";
    private string EmptyStateTitle => !HasRequestedData
        ? "Chưa tải dữ liệu khấu trừ phí công đoàn"
        : !string.IsNullOrWhiteSpace(SearchText)
            ? "Không tìm thấy dòng khấu trừ phí công đoàn phù hợp"
            : "Chưa có dữ liệu khấu trừ phí công đoàn";
    private string EmptyStateMessage => !HasRequestedData
        ? "Chọn kỳ lương rồi bấm Xem để tải dữ liệu."
        : !string.IsNullOrWhiteSpace(SearchText)
            ? "Hãy thử từ khóa khác hoặc xóa tìm kiếm để xem thêm dữ liệu."
            : $"Chưa có dữ liệu phí công đoàn cho kỳ {AppliedPeriodLabel}.";
    private string EmptyStateActionText => !string.IsNullOrWhiteSpace(SearchText) ? "Xóa tìm kiếm" : "Tải lại";

    protected override Task OnInitializedAsync()
    {
        var defaultPeriod = GetDefaultPayrollPeriod();
        ToolbarMonth = AppliedMonth = defaultPeriod.Month;
        ToolbarYear = AppliedYear = defaultPeriod.Year;
        return Task.CompletedTask;
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        return base.OnAfterRenderAsync(firstRender);
    }

    private async Task OnViewRequestedAsync()
    {
        if(!CanView)
        {
            return;
        }

        (ToolbarMonth, ToolbarYear) = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        LoadErrorMessage = null;
        CurrentPageIndex = 0;

        try
        {
            IsPreparingPeriod = true;
            LoadingText = $"Đang chuẩn bị dữ liệu khấu trừ phí công đoàn kỳ {ToolbarMonth:00}/{ToolbarYear}...";
            await ClearSelectionAsync();
            await DataProvider.PreparePeriodAsync(ToolbarYear, ToolbarMonth, disposalTokenSource.Token);

            AppliedMonth = ToolbarMonth;
            AppliedYear = ToolbarYear;
            HasRequestedData = true;
            await ReloadAsync();
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã bị hủy; không thay đổi trạng thái UI nữa.
        }
        catch(Exception ex)
        {
            SetLoadError(ex, "Không thể chuẩn bị dữ liệu khấu trừ phí công đoàn. Vui lòng thử lại.");
        }
        finally
        {
            IsPreparingPeriod = false;
            LoadingText = "Đang tải dữ liệu khấu trừ phí công đoàn...";
        }
    }

    private Task OnRetryAsync() => !HasRequestedData || HasPendingPeriodChange
        ? OnViewRequestedAsync()
        : ReloadAsync();

    private Task OnRecalculateClickAsync()
    {
        if(!CanRecalculate)
        {
            return Task.CompletedTask;
        }

        IsRecalculateConfirmPopupVisible = true;
        return Task.CompletedTask;
    }

    private Task OnSelectedMonthChangedAsync(int month)
    {
        (ToolbarMonth, ToolbarYear) = NormalizeSelectedPeriod(month, ToolbarYear);
        return Task.CompletedTask;
    }

    private Task OnSelectedYearChangedAsync(int year)
    {
        (ToolbarMonth, ToolbarYear) = NormalizeSelectedPeriod(ToolbarMonth, year);
        return Task.CompletedTask;
    }

    private async Task OnSearchTextChangedAsync(string? value)
    {
        var normalizedValue = NormalizeOptional(value);
        if(string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalizedValue;
        CurrentPageIndex = 0;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task PreviousPageAsync()
    {
        if(!CanGoToPreviousPage)
        {
            return;
        }

        CurrentPageIndex--;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task NextPageAsync()
    {
        if(!CanGoToNextPage)
        {
            return;
        }

        CurrentPageIndex++;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task OnEmptyStateActionClickAsync()
    {
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            await OnViewRequestedAsync();
            return;
        }

        if(!string.IsNullOrWhiteSpace(SearchText))
        {
            SearchText = null;
            CurrentPageIndex = 0;
            await ClearSelectionAsync();
        }

        await ReloadAsync();
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
        IsLoading = true;
        LoadErrorMessage = null;
        LoadingText = "Đang tải dữ liệu khấu trừ phí công đoàn...";

        try
        {
            var result = await DataProvider.SearchAsync(BuildFilter(), disposalTokenSource.Token);
            Records = result.Rows;
            TotalRecordCount = result.TotalCount;
            HasRequestedData = true;

            if(TotalRecordCount > 0 && Records.Count == 0 && CurrentPageIndex > 0)
            {
                CurrentPageIndex--;
                await ReloadCoreAsync();
            }
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã bị hủy; không thay đổi trạng thái UI nữa.
        }
        catch(Exception ex)
        {
            Records = [];
            TotalRecordCount = 0;
            SetLoadError(ex, "Không thể tải danh sách khấu trừ phí công đoàn. Vui lòng thử lại.");
        }
        finally
        {
            IsLoading = false;
            LoadingText = "Đang tải dữ liệu khấu trừ phí công đoàn...";
        }
    }

    private async Task ToggleLockStateAsync(PayrollUnionFeeDeductionRecord record)
    {
        if(!CanToggleLock(record))
        {
            return;
        }

        var shouldLock = !record.IsLocked;
        IsUpdatingLock = true;
        LoadingText = shouldLock
            ? $"Đang khóa dòng phí công đoàn của {record.EmployeeDisplay}..."
            : $"Đang mở khóa dòng phí công đoàn của {record.EmployeeDisplay}...";

        try
        {
            await DataProvider.SetLockStateAsync(record, shouldLock, disposalTokenSource.Token);
            await ClearSelectionAsync();
            await ReloadAsync();
            ToastService.ShowSuccess(shouldLock
                ? $"Đã khóa dòng phí công đoàn của {record.EmployeeDisplay}."
                : $"Đã mở khóa dòng phí công đoàn của {record.EmployeeDisplay}.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Không hiển thị lỗi khi circuit hoặc component đã kết thúc.
        }
        catch(Exception)
        {
            ToastService.ShowError(shouldLock
                ? "Không thể khóa dòng phí công đoàn. Vui lòng tải lại dữ liệu rồi thử lại."
                : "Không thể mở khóa dòng phí công đoàn. Vui lòng tải lại dữ liệu rồi thử lại.");
        }
        finally
        {
            IsUpdatingLock = false;
            LoadingText = "Đang tải dữ liệu khấu trừ phí công đoàn...";
        }
    }

    private void OpenEditPopup(PayrollUnionFeeDeductionRecord record)
    {
        if(!CanEditRow(record))
        {
            if(record.IsSummaryLocked || record.IsLocked)
            {
                ToastService.ShowWarning("Dòng phí công đoàn đã khóa nên không thể điều chỉnh.");
            }

            return;
        }

        EditModel = new KhauTruPhiCongDoanEditModel
        {
            PayrollDeductionSummaryRecordId = record.Id,
            EmployeeDisplay = record.EmployeeDisplay,
            DepartmentDisplay = record.DepartmentDisplay,
            PositionDisplay = record.PositionDisplay,
            PayrollPeriodDisplay = record.PayrollPeriodDisplay,
            DeductionAmount = record.DeductionAmount,
            IsLocked = record.IsLocked || record.IsSummaryLocked,
            OriginalVersionAtUtc = record.UpdatedAtUtc ?? record.CreatedAtUtc
        };
        EditPopupTitle = $"Điều chỉnh phí công đoàn - {record.EmployeeDisplay}";
        IsEditPopupVisible = true;
    }

    private void CloseEditPopup()
    {
        if(!IsSavingEdit)
        {
            CloseEditPopupCore();
        }
    }

    private void CloseEditPopupCore()
    {
        IsEditPopupVisible = false;
        EditModel = new();
        EditPopupTitle = "Điều chỉnh phí công đoàn";
    }

    private async Task SaveEditAsync()
    {
        if(!CanSaveEdit)
        {
            ToastService.ShowWarning("Số tiền phí công đoàn phải là số không âm và có tối đa 2 chữ số thập phân.");
            return;
        }

        try
        {
            IsSavingEdit = true;
            LoadingText = $"Đang cập nhật phí công đoàn của {EditModel.EmployeeDisplay}...";
            var updatedRecord = await DataProvider.UpdateManualValueAsync(
                EditModel.PayrollDeductionSummaryRecordId,
                EditModel.DeductionAmount,
                EditModel.OriginalVersionAtUtc,
                disposalTokenSource.Token);

            CloseEditPopupCore();
            await ClearSelectionAsync();
            await ReloadAsync();
            ToastService.ShowSuccess($"Đã cập nhật phí công đoàn của {updatedRecord.EmployeeDisplay}.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã bị hủy; không hiển thị lỗi cho người dùng.
        }
        catch(HrmApiException apiException) when(apiException.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            ToastService.ShowWarning("Dòng phí công đoàn đã thay đổi hoặc bị khóa. Vui lòng tải lại dữ liệu trước khi lưu tiếp.");
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể cập nhật phí công đoàn của {EditModel.EmployeeDisplay}.");
        }
        finally
        {
            IsSavingEdit = false;
            LoadingText = "Đang tải dữ liệu khấu trừ phí công đoàn...";
        }
    }

    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: true);

    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: false);

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
            targetRecordIds = GetSelectedRecords()
                .Select(record => record.Id)
                .Distinct()
                .ToArray();
            if(targetRecordIds.Length == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
                return;
            }

            targetRowCount = targetRecordIds.Length;
        }

        if(shouldLock
           && IsEditPopupVisible
           && (IsWholePeriodLockActionScope(actionScope)
               || targetRecordIds?.Contains(EditModel.PayrollDeductionSummaryRecordId) == true))
        {
            CloseEditPopupCore();
        }

        try
        {
            IsRefreshing = true;
            IsLockActionPopupVisible = false;
            LoadingText = BuildLockActionPendingLoadingMessage(shouldLock, actionScope, targetRowCount > 0 ? targetRowCount : null);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await DataProvider.SetLockStateBatchAsync(
                new SetPayrollUnionFeeDeductionBatchLockStateRequest(
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

            await ClearSelectionAsync();
            await ReloadAsync();
            ToastService.ShowSuccess(BuildLockActionSuccessMessage(shouldLock, actionScope, result.TargetRowCount, result.UpdatedCount));
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã bị hủy; không cần hiển thị lỗi.
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể {LockActionConfirmText.ToLowerInvariant()} dữ liệu khấu trừ phí công đoàn của kỳ {AppliedPeriodLabel}.");
        }
        finally
        {
            IsRefreshing = false;
            LoadingText = "Đang tải dữ liệu khấu trừ phí công đoàn...";
        }
    }

    private async Task OpenMonthlyWorkPopupAsync(PayrollUnionFeeDeductionRecord record)
    {
        if(!CanViewMonthlyWork(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupTitle = "Đối chiếu bảng công tháng";
        MonthlyWorkPopupContext =
            $"{record.EmployeeDisplay} - {record.DepartmentDisplay} - {record.PositionDisplay} - Tháng {record.PayrollPeriodDisplay}";
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = record;
        IsMonthlyWorkPopupVisible = true;
        await LoadMonthlyWorkPopupDataAsync(record);
    }

    private async Task RefreshMonthlyWorkPopupAsync()
    {
        if(MonthlyWorkPopupRecord is null || IsMonthlyWorkPopupLoading)
        {
            return;
        }

        await LoadMonthlyWorkPopupDataAsync(MonthlyWorkPopupRecord);
    }

    private async Task LoadMonthlyWorkPopupDataAsync(PayrollUnionFeeDeductionRecord record)
    {
        IsMonthlyWorkPopupLoading = true;
        MonthlyWorkPopupErrorMessage = null;

        try
        {
            var fromDate = new DateOnly(record.PayrollYear, record.PayrollMonth, 1);
            var toDate = new DateOnly(record.PayrollYear, record.PayrollMonth, DateTime.DaysInMonth(record.PayrollYear, record.PayrollMonth));
            var monthlyWork = await MonthlyWorkSummaryDataProvider.LoadEmployeeMonthAsync(
                fromDate,
                toDate,
                record.EmployeeId,
                disposalTokenSource.Token);

            MonthlyWorkRows = monthlyWork?.DayCellsByDate.Values
                .OrderBy(day => day.WorkDate)
                .Select(day => new MonthlyWorkdayPopupRow(
                    day.Id,
                    day.WorkDate,
                    day.DayTypeDisplay,
                    string.IsNullOrWhiteSpace(day.ShiftShortName) ? "--" : day.ShiftShortName.Trim(),
                    day.ShiftColorHex,
                    day.CheckInDisplay,
                    day.CheckOutDisplay,
                    string.IsNullOrWhiteSpace(day.Status) ? string.Empty : day.Status,
                    day.LateMinutes,
                    day.EarlyLeaveMinutes,
                    day.OvertimeMinutes,
                    day.OvertimeMinutes15,
                    day.OvertimeMinutes20,
                    day.OvertimeMinutes30,
                    day.IsLocked ? "Đã khóa" : "Mở",
                    day.IsLocked))
                .ToArray()
                ?? [];
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            IsMonthlyWorkPopupVisible = false;
        }
        catch(Exception)
        {
            MonthlyWorkPopupErrorMessage = "Không thể tải bảng công tháng của nhân viên.";
        }
        finally
        {
            IsMonthlyWorkPopupLoading = false;
        }
    }

    private void CloseMonthlyWorkPopup()
    {
        if(IsMonthlyWorkPopupLoading)
        {
            return;
        }

        IsMonthlyWorkPopupVisible = false;
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkPopupContext = string.Empty;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = null;
    }

    private void OnColumnChooserRequested() => Grid?.ShowColumnChooser();

    private void OpenRulesPopup() => IsRulesPopupVisible = true;

    private void CloseRecalculateConfirmPopup()
    {
        if(IsRefreshing)
        {
            return;
        }

        IsRecalculateConfirmPopupVisible = false;
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
            IsRefreshing = true;
            LoadingText = $"Đang tính lại dữ liệu khấu trừ phí công đoàn kỳ {AppliedPeriodLabel}...";
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            await ClearSelectionAsync();

            var result = await DataProvider.RefreshAsync(
                new RefreshPayrollUnionFeeDeductionRequest(AppliedYear, AppliedMonth),
                disposalTokenSource.Token);

            await ReloadAsync();
            ToastService.ShowSuccess(
                $"Đã tính lại {result.UpdatedCount:N0} dòng phí công đoàn của kỳ {AppliedPeriodLabel}, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.");
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
            ToastService.ShowError($"Không thể tính lại phí công đoàn của kỳ {AppliedPeriodLabel}.");
        }
        finally
        {
            IsRefreshing = false;
            LoadingText = "Đang tải dữ liệu khấu trừ phí công đoàn...";
        }
    }

    private Task ExportAllDataToExcelAsync() => ExportAllForAppliedPeriodAsync(
        () => ExportGrid!.ExportToXlsxAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu khấu trừ phí công đoàn kỳ {AppliedPeriodLabel} ra Excel.");

    private Task ExportAllDataToPdfAsync() => ExportAllForAppliedPeriodAsync(
        () => ExportGrid!.ExportToPdfAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu khấu trừ phí công đoàn kỳ {AppliedPeriodLabel} ra PDF.");

    private async Task ExportAllForAppliedPeriodAsync(Func<Task> exportAction, string successMessage)
    {
        if(!CanExport || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsExporting = true;
        LoadingText = $"Đang chuẩn bị toàn bộ dữ liệu khấu trừ phí công đoàn kỳ {AppliedPeriodLabel} để xuất file...";
        try
        {
            ExportRecords = await LoadAllForAppliedPeriodExportAsync();
            if(ExportRecords.Count == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu khấu trừ phí công đoàn kỳ {AppliedPeriodLabel} để xuất file.");
                return;
            }

            exportGridRenderCompletionSource = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            await InvokeAsync(StateHasChanged);
            await exportGridRenderCompletionSource.Task.WaitAsync(disposalTokenSource.Token);

            if(ExportGrid is null)
            {
                throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
            }

            await exportAction();
            ToastService.ShowSuccess(successMessage);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã bị hủy; không cần hiển thị lỗi.
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể xuất dữ liệu khấu trừ phí công đoàn kỳ {AppliedPeriodLabel}.");
        }
        finally
        {
            ExportRecords = [];
            exportGridRenderCompletionSource = null;
            IsExporting = false;
            LoadingText = "Đang tải dữ liệu khấu trừ phí công đoàn...";

            if(!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task<IReadOnlyList<PayrollUnionFeeDeductionRecord>> LoadAllForAppliedPeriodExportAsync()
    {
        var firstPage = await DataProvider.SearchAsync(
            new PayrollUnionFeeDeductionFilter(AppliedMonth, AppliedYear, null, 0, ExportPageSize),
            disposalTokenSource.Token);
        var allRecords = new List<PayrollUnionFeeDeductionRecord>(firstPage.TotalCount);
        allRecords.AddRange(firstPage.Rows);

        while(allRecords.Count < firstPage.TotalCount)
        {
            var page = await DataProvider.SearchAsync(
                new PayrollUnionFeeDeductionFilter(AppliedMonth, AppliedYear, null, allRecords.Count, ExportPageSize),
                disposalTokenSource.Token);
            if(page.Rows.Count == 0)
            {
                break;
            }

            allRecords.AddRange(page.Rows);
        }

        return allRecords;
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];
        var grid = Grid;
        if(grid is null || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await grid.DeselectAllAsync();
            grid.SetFocusedRowIndex(-1);
        }
        catch(ObjectDisposedException)
        {
            // Grid có thể vừa bị DevExpress dispose khi circuit/render bị thay thế.
            // Selection cục bộ đã được xóa nên không cần làm hỏng thao tác Xem.
            if(ReferenceEquals(Grid, grid))
            {
                Grid = null;
            }
        }
    }

    private PayrollUnionFeeDeductionFilter BuildFilter() => new(
        AppliedMonth,
        AppliedYear,
        SearchText,
        CurrentPageIndex * PageSize,
        PageSize);

    private IReadOnlyList<PayrollUnionFeeDeductionRecord> GetSelectedRecords() =>
        SelectedDataItems
            .OfType<PayrollUnionFeeDeductionRecord>()
            .DistinctBy(record => record.Id)
            .ToArray();

    private bool CanEditRow(PayrollUnionFeeDeductionRecord record) =>
        CanOperateOnCurrentDataset && !record.IsSummaryLocked && !record.IsLocked;

    private bool CanToggleLock(PayrollUnionFeeDeductionRecord record) =>
        CanOperateOnCurrentDataset && !record.IsSummaryLocked;

    private static string GetLockActionTooltip(PayrollUnionFeeDeductionRecord record) => record.IsSummaryLocked
        ? "Kỳ lương khấu trừ đã khóa nên không thể thay đổi trạng thái dòng."
        : record.IsLocked
            ? "Mở khóa dòng khấu trừ phí công đoàn"
            : "Khóa dòng khấu trừ phí công đoàn";

    private bool CanViewMonthlyWork(PayrollUnionFeeDeductionRecord record) =>
        CanOperateOnCurrentDataset
        && record.EmployeeId != Guid.Empty
        && record.PayrollMonth is >= 1 and <= 12
        && record.PayrollYear is >= MinimumSupportedYear and <= MaximumSupportedYear;

    private static string GetLockStatusCssClass(PayrollUnionFeeDeductionRecord record) => string.Join(
        ' ',
        "yes-no-status",
        record.IsSummaryLocked || record.IsLocked ? "yes-no-status-no hrm-grid-status" : "yes-no-status-yes hrm-grid-status");

    private static string FormatCurrency(decimal value) =>
        value == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", value);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsWholePeriodLockActionScope(string scope) =>
        string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal);

    private string BuildLockActionNoDataMessage(bool shouldLock, string scope) =>
        IsWholePeriodLockActionScope(scope)
            ? $"Không có dữ liệu phí công đoàn của kỳ {PendingLockActionPeriodLabel} để {(shouldLock ? "khóa" : "mở khóa")}."
            : "Không còn dòng phí công đoàn hợp lệ trong phạm vi đang chọn để xử lý.";

    private string BuildLockActionAlreadyAppliedMessage(bool shouldLock, string scope, int targetRowCount)
    {
        var stateText = shouldLock ? "khóa" : "mở";
        return IsWholePeriodLockActionScope(scope)
            ? $"Không có dòng nào cần {(shouldLock ? "khóa" : "mở khóa")}. {targetRowCount:N0} dòng của kỳ {PendingLockActionPeriodLabel} đã ở trạng thái {stateText}."
            : $"Không có dòng nào cần {(shouldLock ? "khóa" : "mở khóa")}. {targetRowCount:N0} dòng đã chọn đã ở trạng thái {stateText}.";
    }

    private string BuildLockActionPendingLoadingMessage(bool shouldLock, string scope, int? affectedCount = null)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        if(!IsWholePeriodLockActionScope(scope) && affectedCount.HasValue)
        {
            return $"Đang xử lý {actionText} {affectedCount.Value:N0} dòng phí công đoàn đã chọn...";
        }

        return IsWholePeriodLockActionScope(scope)
            ? $"Đang xử lý {actionText} dữ liệu phí công đoàn của kỳ {PendingLockActionPeriodLabel}..."
            : $"Đang xử lý {actionText} các dòng phí công đoàn đã chọn...";
    }

    private string BuildLockActionSuccessMessage(bool shouldLock, string scope, int targetRowCount, int updatedCount)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        var unchangedCount = Math.Max(0, targetRowCount - updatedCount);
        var scopeText = IsWholePeriodLockActionScope(scope)
            ? $"dòng phí công đoàn của kỳ {PendingLockActionPeriodLabel}"
            : "dòng phí công đoàn đã chọn";

        return unchangedCount > 0
            ? $"Đã {actionText} {updatedCount:N0}/{targetRowCount:N0} {scopeText}, giữ nguyên {unchangedCount:N0} dòng đã đúng trạng thái."
            : $"Đã {actionText} {updatedCount:N0} {scopeText}.";
    }

    private void SetLoadError(Exception exception, string message)
    {
        var traceId = exception is HrmApiException apiException ? apiException.TraceId : null;
        Logger.LogError(
            exception,
            "Không thể xử lý dữ liệu phí công đoàn. TraceId: {TraceId}",
            traceId ?? "(không có)");
        LoadErrorMessage = string.IsNullOrWhiteSpace(traceId)
            ? message
            : $"{message} Mã theo dõi: {traceId}.";
        ToastService.ShowError(message);
    }

    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        return normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth
            ? (MinimumSupportedMonth, MinimumSupportedYear)
            : (normalizedMonth, normalizedYear);
    }

    private string BuildExportFileName() =>
        $"khau-tru-phi-cong-doan-{AppliedYear}-{AppliedMonth:00}";

    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        return NormalizeSelectedPeriod(localNow.Month, localNow.Year);
    }

    private sealed record MonthOption(int Value, string Text);

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        reloadGate.Dispose();
        disposalTokenSource.Dispose();
    }
}
