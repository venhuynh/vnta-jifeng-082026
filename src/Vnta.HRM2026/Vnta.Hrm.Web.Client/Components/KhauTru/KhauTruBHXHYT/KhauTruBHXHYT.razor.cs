using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruBHXHYT.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruBHXHYT;

public partial class KhauTruBHXHYT : IDisposable
{
    private const int MinimumSupportedMonth = 6;
    private const int MinimumSupportedYear = 2026;
    private const int ImportedInsurancePeriodMonth = 6;
    private const int ImportedInsurancePeriodYear = 2026;
    private const int MaximumSupportedYear = 2100;
    private const int SearchResultLimit = 2000;
    private const string DeductionAmountTotalSummaryName = "DeductionAmountTotal";
    private const string LockScopeSelectedRows = "selected-rows";
    private const string LockScopeWholePeriod = "whole-period";

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly IReadOnlyList<MonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, $"Tháng {month:00}"))
            .ToArray();
    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);

    [Inject]
    private IPayrollInsuranceDeductionDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    private IReadOnlyList<PayrollInsuranceDeductionRecord> Records { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private decimal VisibleDeductionTotal { get; set; }
    private bool IsDeductionTotalSyncPending { get; set; }
    private KhauTruBHXHYTEditModel EditModel { get; set; } = new();
    private EditContext? EditContext { get; set; }
    private string EditPopupTitle { get; set; } = "Điều chỉnh khấu trừ BHXH-YT";
    private string LoadingText { get; set; } = HrmUiDefaults.LoadingText;
    private int ToolbarMonth { get; set; } = MinimumSupportedMonth;
    private int ToolbarYear { get; set; } = MinimumSupportedYear;
    private int AppliedMonth { get; set; } = MinimumSupportedMonth;
    private int AppliedYear { get; set; } = MinimumSupportedYear;
    private int PageSize { get; set; } = 50;
    private bool IsLoading { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool IsSavingEdit { get; set; }
    private Guid? RefreshingRecordId { get; set; }
    private Guid? LockingRecordId { get; set; }
    private bool IsEditPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupLoading { get; set; }
    private bool IsRulesPopupVisible { get; set; }
    private bool IsRecalculateConfirmPopupVisible { get; set; }
    private bool IsLockActionPopupVisible { get; set; }
    private bool IsLockActionBusy { get; set; }
    private bool PendingLockActionState { get; set; } = true;
    private int PendingLockActionMonth { get; set; } = MinimumSupportedMonth;
    private int PendingLockActionYear { get; set; } = MinimumSupportedYear;
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;
    private bool HasRequestedData { get; set; }
    private string? MonthlyWorkPopupErrorMessage { get; set; }
    private string MonthlyWorkPopupTitle { get; set; } = "Bảng công tháng";
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    private IReadOnlyList<KhauTruBHXHYTMonthlyWorkdayRow> MonthlyWorkRows { get; set; } = [];
    private PayrollInsuranceDeductionRecord? MonthlyWorkPopupRecord { get; set; }
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;
    private bool ShouldShowInitialPeriodNormalizationWarning { get; set; }
    private int InitialToolbarMonth { get; set; } = MinimumSupportedMonth;
    private int InitialToolbarYear { get; set; } = MinimumSupportedYear;

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool IsRefreshingRow => RefreshingRecordId.HasValue;
    private bool IsLockingRow => LockingRecordId.HasValue;
    private bool ShowLoadingPanel => IsLoading || IsChangingPageSize || IsSavingEdit || IsRefreshingRow || IsLockingRow || IsLockActionBusy;
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    private bool HasPendingPeriodChange => HasRequestedData
        && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);
    private bool IsConfirmationPopupVisible => IsRecalculateConfirmPopupVisible
        || IsLockActionPopupVisible;
    private bool CanUseAppliedData => CanInteract
        && HasRequestedData
        && !HasPendingPeriodChange
        && !IsConfirmationPopupVisible;
    private bool CanRecalculate => CanUseAppliedData && !IsImportedInsurancePeriod(AppliedMonth, AppliedYear);
    private string RecalculateTooltip => IsImportedInsurancePeriod(AppliedMonth, AppliedYear)
        ? "Dữ liệu kỳ 06/2026 được nhập từ Excel nên không tính lại."
        : "Tính lại dữ liệu khấu trừ BHXH-YT của kỳ đang áp dụng.";
    private bool CanConfirmAppliedAction => CanInteract
        && HasRequestedData
        && !HasPendingPeriodChange;
    private bool CanOpenLockAction => CanUseAppliedData;
    private bool CanOpenUnlockAction => CanUseAppliedData;
    private bool CanChooseSelectedRowsScope => GetSelectedResultCount() > 0;
    private bool CanConfirmLockAction => IsLockActionPopupVisible
        && CanConfirmAppliedAction
        && (IsWholePeriodLockActionScope(SelectedLockActionScope) || CanChooseSelectedRowsScope);
    private bool CanReload => !ShowLoadingPanel;
    private bool CanChangeFilters => !ShowLoadingPanel && !IsConfirmationPopupVisible;
    private bool CanSearch => CanInteract && !HasPendingPeriodChange && !IsConfirmationPopupVisible;
    private bool CanOpenRules => CanInteract && !IsConfirmationPopupVisible;
    private bool CanExport => CanUseAppliedData && Records.Count > 0;
    private bool CanExportSelected => CanExport && GetSelectedResultCount() > 0;
    private bool CanEditFields => !IsSavingEdit && IsEditPopupVisible;
    private bool CanSaveEdit => CanUseAppliedData
        && !IsSavingEdit
        && EditContext is not null
        && EditModel.PayrollDeductionSummaryRecordId != Guid.Empty
        && EditModel.OriginalUpdatedAtUtc != default;
    private string AppliedPeriodLabel => FormatPayrollPeriod(AppliedMonth, AppliedYear);
    private string PendingLockActionPeriodLabel => FormatPayrollPeriod(PendingLockActionMonth, PendingLockActionYear);
    private string LockActionPopupTitle => PendingLockActionState
        ? "Khóa dữ liệu khấu trừ BHXH-YT"
        : "Mở khóa dữ liệu khấu trừ BHXH-YT";
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa dữ liệu khấu trừ BHXH-YT. Dòng đã khóa không thể điều chỉnh hoặc làm mới."
        : "Chọn phạm vi cần mở khóa dữ liệu khấu trừ BHXH-YT. Chỉ mở khóa detail BHXH-YT khi dòng tổng kết khấu trừ chưa khóa.";
    private string LockActionScopeContextText =>
        $"Kỳ lương áp dụng: {PendingLockActionPeriodLabel}. Toàn bộ kỳ bỏ qua bộ lọc tìm kiếm và phân trang hiện tại.";
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {GetSelectedResultCount():N0} dòng đang được chọn trong lưới."
        : "Chưa có dòng hợp lệ nào được chọn trong lưới hiện tại.";
    private string WholePeriodScopeDescription => PendingLockActionState
        ? $"Áp dụng cho toàn bộ dữ liệu BHXH-YT của kỳ {PendingLockActionPeriodLabel}."
        : $"Mở khóa toàn bộ dữ liệu BHXH-YT của kỳ {PendingLockActionPeriodLabel}.";
    private string RulesPeriodLabel => FormatPayrollPeriod(ToolbarMonth, ToolbarYear);
    private IReadOnlyList<MonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : MonthOptions;
    private bool CanResetFilters =>
        ToolbarMonth != InitialToolbarMonth
        || ToolbarYear != InitialToolbarYear
        || !string.IsNullOrWhiteSpace(SearchText);

    private string EmptyStateTitle => !HasRequestedData
        ? "Chưa tải dữ liệu khấu trừ BHXH-YT"
        : CanResetFilters
            ? "Không tìm thấy dòng khấu trừ BHXH-YT phù hợp"
            : "Chưa có dữ liệu khấu trừ BHXH-YT";

    private string EmptyStateMessage => !HasRequestedData
        ? "Chọn tháng, năm kỳ lương rồi nhấn Xem để tải dữ liệu khi bạn sẵn sàng."
        : CanResetFilters
            ? "Hãy nới điều kiện lọc hoặc xóa bộ lọc để xem thêm dữ liệu."
            : "Bảng khấu trừ BHXH-YT sẽ hiển thị tại đây sau khi có dữ liệu cho kỳ lương đang chọn.";

    private string EmptyStateActionText => !HasRequestedData
        ? "Xem dữ liệu"
        : CanResetFilters
            ? "Đặt lại bộ lọc"
            : "Tải lại";

    protected override Task OnInitializedAsync()
    {
        var defaultPeriod = GetDefaultPayrollPeriod();
        ToolbarMonth = defaultPeriod.Month;
        ToolbarYear = defaultPeriod.Year;
        AppliedMonth = defaultPeriod.Month;
        AppliedYear = defaultPeriod.Year;
        InitialToolbarMonth = defaultPeriod.Month;
        InitialToolbarYear = defaultPeriod.Year;
        ShouldShowInitialPeriodNormalizationWarning = defaultPeriod.IsNormalized;
        return base.OnInitializedAsync();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (ShouldShowInitialPeriodNormalizationWarning)
        {
            ShouldShowInitialPeriodNormalizationWarning = false;
            ToastService.ShowWarning(
                $"Kỳ hiện tại nằm ngoài phạm vi màn hỗ trợ; đã chọn kỳ {RulesPeriodLabel}.");
            return InvokeAsync(StateHasChanged);
        }

        if (IsDeductionTotalSyncPending)
        {
            IsDeductionTotalSyncPending = false;
            UpdateVisibleDeductionTotalFromGrid();
            return InvokeAsync(StateHasChanged);
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    private Task OnViewRequestedAsync() => ReloadAsync();

    private Task OnRetryAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref reloadRequestedVersion);
        if (!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        try
        {
            while (!disposalTokenSource.IsCancellationRequested
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
        var requestedPeriod = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        ToolbarMonth = requestedPeriod.Month;
        ToolbarYear = requestedPeriod.Year;
        var requestedMonth = requestedPeriod.Month;
        var requestedYear = requestedPeriod.Year;
        HasRequestedData = true;
        LoadErrorMessage = null;
        EditErrorMessage = null;
        LoadingText = HrmUiDefaults.LoadingText;
        IsLoading = true;

        try
        {
            await ClearSelectionAsync();
            var loadResult = await DataProvider.SearchAsync(
                BuildFilter(requestedMonth, requestedYear),
                disposalTokenSource.Token);
            Records = loadResult.Rows;
            ResetVisibleDeductionTotal();
            IsDeductionTotalSyncPending = true;
            AppliedMonth = requestedMonth;
            AppliedYear = requestedYear;
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            Records = [];
            ResetVisibleDeductionTotal();
            IsDeductionTotalSyncPending = false;
            LoadErrorMessage = "Có lỗi khi tải dữ liệu khấu trừ BHXH-YT. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách khấu trừ BHXH-YT.");
        }
        finally
        {
            IsLoading = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
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

    private void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

    private Task OnRecalculateClickAsync()
    {
        if (!CanRecalculate)
        {
            return Task.CompletedTask;
        }

        IsRecalculateConfirmPopupVisible = true;
        return Task.CompletedTask;
    }

    private Task OnRecalculateConfirmPopupVisibleChangedAsync(bool visible)
    {
        IsRecalculateConfirmPopupVisible = visible;
        return Task.CompletedTask;
    }

    private async Task ConfirmRecalculateAsync()
    {
        if (!IsRecalculateConfirmPopupVisible || !CanConfirmAppliedAction
            || IsImportedInsurancePeriod(AppliedMonth, AppliedYear))
        {
            return;
        }

        var payrollPeriod = AppliedPeriodLabel;
        IsRecalculateConfirmPopupVisible = false;

        try
        {
            LoadErrorMessage = null;
            EditErrorMessage = null;
            LoadingText = $"Đang tính lại dữ liệu BHXH-YT kỳ {payrollPeriod}...";
            IsLoading = true;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await DataProvider.RefreshAsync(
                AppliedMonth,
                AppliedYear,
                disposalTokenSource.Token);

            HasRequestedData = true;
            await ReloadAsync();
            ShowRefreshResultToast(result);
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể tính lại dữ liệu khấu trừ BHXH-YT.");
        }
        finally
        {
            IsLoading = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(true);

    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(false);

    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if (!CanUseAppliedData)
        {
            return Task.CompletedTask;
        }

        PendingLockActionState = shouldLock;
        PendingLockActionMonth = AppliedMonth;
        PendingLockActionYear = AppliedYear;
        SelectedLockActionScope = CanChooseSelectedRowsScope ? LockScopeSelectedRows : LockScopeWholePeriod;
        IsLockActionPopupVisible = true;
        return Task.CompletedTask;
    }

    private Task OnLockActionPopupVisibleChangedAsync(bool visible)
    {
        if (!IsLockActionBusy)
        {
            IsLockActionPopupVisible = visible;
        }

        return Task.CompletedTask;
    }

    private Task SelectLockActionScope(string scope)
    {
        if (!IsLockActionBusy
            && (string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal)
                || (string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal)
                    && CanChooseSelectedRowsScope)))
        {
            SelectedLockActionScope = scope;
        }

        return Task.CompletedTask;
    }

    private async Task ConfirmLockActionAsync()
    {
        if (!CanConfirmLockAction || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        var shouldLock = PendingLockActionState;
        var wholePeriod = IsWholePeriodLockActionScope(SelectedLockActionScope);
        Guid[]? targetIds = null;
        if (!wholePeriod)
        {
            targetIds = GetSelectedResults()
                .Select(row => row.PayrollDeductionSummaryRecordId ?? row.Id)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();
            if (targetIds.Length == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng hợp lệ hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
                return;
            }
        }

        if (shouldLock && IsEditPopupVisible
            && (wholePeriod || targetIds!.Contains(EditModel.PayrollDeductionSummaryRecordId)))
        {
            CloseEditPopupCore();
        }

        try
        {
            IsLockActionBusy = true;
            IsLockActionPopupVisible = false;
            LoadingText = wholePeriod
                ? $"Đang {(shouldLock ? "khóa" : "mở khóa")} dữ liệu BHXH-YT kỳ {PendingLockActionPeriodLabel}..."
                : $"Đang {(shouldLock ? "khóa" : "mở khóa")} {targetIds!.Length:N0} dòng BHXH-YT đã chọn...";
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await DataProvider.SetLockStateBatchAsync(
                new SetPayrollInsuranceDeductionBatchLockStateRequest(
                    PendingLockActionYear,
                    PendingLockActionMonth,
                    shouldLock,
                    targetIds),
                disposalTokenSource.Token);

            if (result.TargetRowCount == 0)
            {
                ToastService.ShowInfo($"Không có dòng BHXH-YT nào của kỳ {PendingLockActionPeriodLabel} để {(shouldLock ? "khóa" : "mở khóa")}.");
                return;
            }

            if (result.UpdatedCount == 0)
            {
                ToastService.ShowInfo($"{result.TargetRowCount:N0} dòng BHXH-YT đã ở trạng thái {(shouldLock ? "khóa" : "mở")}.");
                return;
            }

            await ReloadAsync();
            var unchangedCount = result.TargetRowCount - result.UpdatedCount;
            ToastService.ShowSuccess(
                unchangedCount > 0
                    ? $"Đã {(shouldLock ? "khóa" : "mở khóa")} {result.UpdatedCount:N0}/{result.TargetRowCount:N0} dòng BHXH-YT, giữ nguyên {unchangedCount:N0} dòng đã đúng trạng thái."
                    : $"Đã {(shouldLock ? "khóa" : "mở khóa")} {result.UpdatedCount:N0} dòng BHXH-YT.");
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            ToastService.ShowError($"Không thể {(shouldLock ? "khóa" : "mở khóa")} dữ liệu BHXH-YT.");
        }
        finally
        {
            IsLockActionBusy = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private async Task OnRulesClick()
    {
        IsRulesPopupVisible = true;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnEmptyStateActionClick()
    {
        if (!HasRequestedData)
        {
            await ReloadAsync();
            return;
        }

        if (CanResetFilters)
        {
            await ResetFiltersAsync();
            return;
        }

        await ReloadAsync();
    }

    private Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
        if (string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        SearchText = normalizedValue;
        if (!HasRequestedData || HasPendingPeriodChange)
        {
            return Task.CompletedTask;
        }

        return ReloadAsync();
    }

    private async Task OnPageSizeChanged(int value)
    {
        if (PageSize == value)
        {
            return;
        }

        IsChangingPageSize = true;
        LoadingText = "Đang cập nhật số dòng hiển thị...";
        PageSize = value;

        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }
        finally
        {
            IsChangingPageSize = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private async Task RefreshRowAsync(PayrollInsuranceDeductionRecord record)
    {
        if (!CanRefreshRow(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        try
        {
            LoadErrorMessage = null;
            EditErrorMessage = null;
            RefreshingRecordId = record.Id;
            LoadingText = $"Đang làm mới dữ liệu BHXH-YT của {record.EmployeeDisplay}...";

            var result = await DataProvider.RefreshRowAsync(
                AppliedMonth,
                AppliedYear,
                record.PayrollDeductionSummaryRecordId ?? record.Id,
                disposalTokenSource.Token);

            await ClearSelectionAsync();
            Records = (await DataProvider.SearchAsync(BuildAppliedFilter(), disposalTokenSource.Token)).Rows;
            ShowRefreshRowResultToast(record, result);
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể làm mới dòng khấu trừ BHXH-YT.");
        }
        finally
        {
            RefreshingRecordId = null;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private async Task ToggleLockStateAsync(PayrollInsuranceDeductionRecord record)
    {
        if (!CanToggleLock(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        var shouldLock = !record.IsLocked;
        try
        {
            LoadErrorMessage = null;
            EditErrorMessage = null;
            LockingRecordId = record.Id;
            LoadingText = $"Đang {(shouldLock ? "khóa" : "mở khóa")} dòng khấu trừ BHXH-YT của {record.EmployeeDisplay}...";

            var updatedRecord = await DataProvider.SetLockStateAsync(
                record.PayrollDeductionSummaryRecordId ?? record.Id,
                shouldLock,
                record.UpdatedAtUtc ?? record.CreatedAtUtc,
                disposalTokenSource.Token);

            ApplyUpdatedRecord(updatedRecord);
            ToastService.ShowSuccess(
                updatedRecord.IsLocked
                    ? $"Đã khóa dòng khấu trừ BHXH-YT của {updatedRecord.EmployeeDisplay}."
                    : $"Đã mở khóa dòng khấu trừ BHXH-YT của {updatedRecord.EmployeeDisplay}.");
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            ToastService.ShowError($"Không thể {(shouldLock ? "khóa" : "mở khóa")} dòng khấu trừ BHXH-YT.");
        }
        finally
        {
            LockingRecordId = null;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private async Task OpenMonthlyWorkPopupAsync(PayrollInsuranceDeductionRecord record)
    {
        if (!CanViewMonthlyWork(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupTitle = "Bảng công tháng";
        MonthlyWorkPopupContext =
            $"{record.EmployeeDisplay} - {record.DepartmentDisplay} - {record.PositionDisplay} - Tháng {AppliedPeriodLabel}";
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = record;
        IsMonthlyWorkPopupVisible = true;
        IsMonthlyWorkPopupLoading = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        await LoadMonthlyWorkPopupDataAsync(record);
    }

    private async Task RefreshMonthlyWorkPopupAsync()
    {
        if (MonthlyWorkPopupRecord is null
            || IsMonthlyWorkPopupLoading
            || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        await LoadMonthlyWorkPopupDataAsync(MonthlyWorkPopupRecord);
    }

    private async Task LoadMonthlyWorkPopupDataAsync(PayrollInsuranceDeductionRecord record)
    {
        IsMonthlyWorkPopupLoading = true;
        MonthlyWorkPopupErrorMessage = null;

        try
        {
            var monthlyWork = await DataProvider.LoadEmployeeMonthlyWorkAsync(
                record.Id,
                record.PayrollDeductionSummaryRecordId ?? Guid.Empty,
                record.EmployeeId ?? Guid.Empty,
                AppliedYear,
                AppliedMonth,
                disposalTokenSource.Token);

            MonthlyWorkRows = monthlyWork?.DayCellsByDate.Values
                .OrderBy(day => day.WorkDate)
                .Select(day => new KhauTruBHXHYTMonthlyWorkdayRow(
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
                    day.IsLocked))
                .ToArray()
                ?? [];
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            IsMonthlyWorkPopupVisible = false;
        }
        catch (UnauthorizedAccessException)
        {
            MonthlyWorkPopupErrorMessage = "Bạn không có quyền xem bảng công tháng của nhân viên.";
        }
        catch (Exception)
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
        if (IsMonthlyWorkPopupLoading)
        {
            return;
        }

        IsMonthlyWorkPopupVisible = false;
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = null;
        MonthlyWorkPopupContext = string.Empty;
    }

    private async Task OpenEditPopupAsync(PayrollInsuranceDeductionRecord record)
    {
        if (!CanEditRow(record) || disposalTokenSource.IsCancellationRequested)
        {
            if (IsImportedInsurancePeriod(record.PayrollMonth, record.PayrollYear))
            {
                ToastService.ShowWarning("Dữ liệu khấu trừ BHXH-YT kỳ 06/2026 được nhập từ Excel nên không thể điều chỉnh.");
            }
            else if (record.IsLocked)
            {
                ToastService.ShowWarning("Dòng khấu trừ BHXH-YT đã khóa nên không thể điều chỉnh.");
            }

            return;
        }

        EditErrorMessage = null;
        EditModel = CreateEditModel(record);
        EditContext = new EditContext(EditModel);
        EditPopupTitle = $"Điều chỉnh khấu trừ BHXH-YT - {record.EmployeeDisplay}";
        IsEditPopupVisible = true;
    }

    private Task OnEditPopupVisibleChangedAsync(bool visible)
    {
        if (visible)
        {
            IsEditPopupVisible = true;
            return Task.CompletedTask;
        }

        CloseEditPopup();
        return Task.CompletedTask;
    }

    private void CloseEditPopup()
    {
        if (IsSavingEdit)
        {
            return;
        }

        CloseEditPopupCore();
    }

    private void CloseEditPopupCore()
    {
        IsEditPopupVisible = false;
        EditErrorMessage = null;
        EditContext = null;
        EditModel = new();
        EditPopupTitle = "Điều chỉnh khấu trừ BHXH-YT";
    }

    private async Task SaveEditAsync()
    {
        if (!CanSaveEdit || EditContext is null || !EditContext.Validate())
        {
            return;
        }

        EditErrorMessage = null;
        try
        {
            IsSavingEdit = true;
            LoadingText = "Đang cập nhật dòng khấu trừ BHXH-YT...";
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var updatedRecord = await DataProvider.UpdateManualValuesAsync(
                new UpdatePayrollInsuranceDeductionManualValuesRequest(
                    EditModel.PayrollDeductionSummaryRecordId,
                    EditModel.InsuranceSalaryBaseAmount,
                    EditModel.SocialInsuranceRate,
                    EditModel.HealthInsuranceRate,
                    EditModel.UnemploymentInsuranceRate,
                    EditModel.IsParticipating,
                    EditModel.ParticipationChangeType,
                    EditModel.EffectiveDate,
                    EditModel.OriginalUpdatedAtUtc),
                disposalTokenSource.Token);

            CloseEditPopupCore();
            await ReloadAsync();
            ToastService.ShowSuccess($"Đã cập nhật dòng khấu trừ BHXH-YT của {updatedRecord.EmployeeDisplay}.");
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            EditErrorMessage = ex.Message;
            ToastService.ShowError("Không thể lưu khấu trừ BHXH-YT.");
        }
        catch (Exception)
        {
            EditErrorMessage = "Không thể lưu dữ liệu khấu trừ BHXH-YT. Vui lòng kiểm tra lại thông tin.";
            ToastService.ShowError("Không thể lưu khấu trừ BHXH-YT.");
        }
        finally
        {
            IsSavingEdit = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private Task ExportAllDataToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync("khau-tru-bhxh-yt"),
        "Đã bắt đầu xuất Excel khấu trừ BHXH-YT.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(
            "khau-tru-bhxh-yt-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất Excel cho các dòng đã chọn.");

    private Task ExportAllDataToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync("khau-tru-bhxh-yt"),
        "Đã bắt đầu xuất PDF khấu trừ BHXH-YT.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync(
            "khau-tru-bhxh-yt-selected",
            new GridPdfExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất PDF cho các dòng đã chọn.");

    private async Task ExportAsync(Func<Task> exportAction, string successMessage)
    {
        if (Grid is null)
        {
            ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất.");
            return;
        }

        try
        {
            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu khấu trừ BHXH-YT.");
        }
    }

    private async Task ResetFiltersAsync()
    {
        var defaultPeriod = GetDefaultPayrollPeriod();
        ToolbarMonth = defaultPeriod.Month;
        ToolbarYear = defaultPeriod.Year;
        InitialToolbarMonth = defaultPeriod.Month;
        InitialToolbarYear = defaultPeriod.Year;
        SearchText = null;
        await ReloadAsync();
    }

    private Task OnToolbarMonthChanged(int value)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(value, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    private Task OnToolbarYearChanged(int value)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, value);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];

        if (Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private List<PayrollInsuranceDeductionRecord> GetSelectedResults() =>
        SelectedDataItems
            .OfType<PayrollInsuranceDeductionRecord>()
            .Where(IsVisibleResult)
            .DistinctBy(result => result.Id)
            .ToList();

    private int GetSelectedResultCount() => GetSelectedResults().Count;

    private PayrollInsuranceDeductionFilter BuildFilter(int payrollMonth, int payrollYear) =>
        new(
            payrollMonth,
            payrollYear,
            SearchText,
            SearchResultLimit);

    private PayrollInsuranceDeductionFilter BuildAppliedFilter() =>
        BuildFilter(AppliedMonth, AppliedYear);

    private bool IsVisibleResult(PayrollInsuranceDeductionRecord result) =>
        Records.Any(row => row.Id == result.Id);

    private bool CanEditRow(PayrollInsuranceDeductionRecord record) =>
        CanUseAppliedData
        && IsVisibleResult(record)
        && !record.IsLocked
        && !IsImportedInsurancePeriod(record.PayrollMonth, record.PayrollYear);

    private static string GetEditActionTooltip(PayrollInsuranceDeductionRecord record) =>
        IsImportedInsurancePeriod(record.PayrollMonth, record.PayrollYear)
            ? "Dữ liệu kỳ 06/2026 được nhập từ Excel nên không thể điều chỉnh."
            : record.IsLocked
                ? "Dòng đã khóa nên không thể điều chỉnh."
                : "Điều chỉnh dữ liệu khấu trừ BHXH-YT của dòng này.";

    private bool CanRefreshRow(PayrollInsuranceDeductionRecord record) =>
        CanUseAppliedData
        && !record.IsLocked
        && IsVisibleResult(record);

    private bool CanToggleLock(PayrollInsuranceDeductionRecord record) =>
        CanUseAppliedData && IsVisibleResult(record);

    private static string GetLockActionTooltip(PayrollInsuranceDeductionRecord record) =>
        record.IsLocked
            ? "Mở khóa dòng khấu trừ BHXH-YT"
            : "Khóa dòng khấu trừ BHXH-YT";

    private void ApplyUpdatedRecord(PayrollInsuranceDeductionRecord updatedRecord)
    {
        Records = Records
            .Select(record => record.Id == updatedRecord.Id ? updatedRecord : record)
            .ToArray();

        if (updatedRecord.IsLocked
            && IsEditPopupVisible
            && EditModel.PayrollDeductionSummaryRecordId == updatedRecord.PayrollDeductionSummaryRecordId)
        {
            CloseEditPopupCore();
        }
    }

    private bool CanViewMonthlyWork(PayrollInsuranceDeductionRecord record) =>
        CanUseAppliedData
        && !IsMonthlyWorkPopupLoading
        && IsVisibleResult(record)
        && record.Id != Guid.Empty
        && record.PayrollDeductionSummaryRecordId is { } payrollDeductionSummaryRecordId
        && payrollDeductionSummaryRecordId != Guid.Empty
        && record.EmployeeId is { } employeeId
        && employeeId != Guid.Empty;

    private void ResetVisibleDeductionTotal()
    {
        VisibleDeductionTotal = Records.Sum(record => record.TotalDeductionAmount);
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

    private static KhauTruBHXHYTEditModel CreateEditModel(PayrollInsuranceDeductionRecord source)
    {
        return new KhauTruBHXHYTEditModel
        {
            PayrollDeductionSummaryRecordId = source.PayrollDeductionSummaryRecordId ?? source.Id,
            EmployeeDisplay = source.EmployeeDisplay,
            PayrollPeriodDisplay = source.PayrollPeriodDisplay,
            LockStatusText = source.LockStatusText,
            InsuranceSalaryBaseAmount = source.InsuranceSalaryBaseAmount,
            SocialInsuranceRate = source.SocialInsuranceRate,
            HealthInsuranceRate = source.HealthInsuranceRate,
            UnemploymentInsuranceRate = source.UnemploymentInsuranceRate,
            IsParticipating = source.IsParticipating,
            ParticipationChangeType = source.ParticipationChangeType,
            EffectiveDate = source.EffectiveDate,
            CurrentTotalInsuranceRate = source.TotalInsuranceRate,
            CurrentTotalDeductionAmount = source.TotalDeductionAmount,
            OriginalUpdatedAtUtc = source.UpdatedAtUtc ?? source.CreatedAtUtc
        };
    }

    private void ShowRefreshResultToast(RefreshPayrollInsuranceDeductionResult result)
    {
        var payrollPeriod = FormatPayrollPeriod(result.PayrollMonth, result.PayrollYear);
        var unchangedCount = result.MatchedRowCount - result.UpdatedCount - result.SkippedLockedCount;

        if (result.MatchedRowCount == 0)
        {
            ToastService.ShowInfo($"Kỳ {payrollPeriod} chưa có dòng BHXH-YT để tính lại.");
            return;
        }

        if (result.UpdatedCount == 0)
        {
            ToastService.ShowInfo(
                $"Dữ liệu BHXH-YT kỳ {payrollPeriod} đã đúng: giữ nguyên {unchangedCount:N0} dòng, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.");
            return;
        }

        ToastService.ShowSuccess(
            $"Đã tính lại dữ liệu BHXH-YT kỳ {payrollPeriod}: khớp {result.MatchedRowCount:N0} dòng, cập nhật {result.UpdatedCount:N0}, giữ nguyên {unchangedCount:N0}, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.");
    }

    private void ShowRefreshRowResultToast(
        PayrollInsuranceDeductionRecord record,
        RefreshPayrollInsuranceDeductionResult result)
    {
        if (result.SkippedLockedCount > 0)
        {
            ToastService.ShowWarning($"Không làm mới dòng BHXH-YT của {record.EmployeeDisplay} vì dữ liệu đã khóa.");
            return;
        }

        if (result.UpdatedCount == 0)
        {
            ToastService.ShowInfo($"Dòng BHXH-YT của {record.EmployeeDisplay} đã đúng, không có dữ liệu cần cập nhật.");
            return;
        }

        ToastService.ShowSuccess($"Đã làm mới dữ liệu BHXH-YT của {record.EmployeeDisplay}.");
    }

    private string FormatMoney(decimal value) =>
        value == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", value);

    private string FormatWorkday(decimal value) => value.ToString("0.##", DisplayCulture);

    private string FormatRate(decimal value) => value.ToString("P2", DisplayCulture);

    /// <summary>Định dạng ngày tùy chọn theo mẫu ngày/tháng/năm hoặc trả về giá trị thay thế.</summary>
    private static string FormatDate(DateOnly? value) =>
        value.HasValue ? value.Value.ToString("dd/MM/yyyy", DisplayCulture) : "--";

    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = string.IsNullOrWhiteSpace(value) ? "Chưa có" : value.Trim();
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var searchText = SearchText.Trim();
        if (searchText.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var startIndex = 0;
        var builder = new StringBuilder(displayText.Length + 32);
        while (true)
        {
            var matchIndex = displayText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                break;
            }

            builder.Append(WebUtility.HtmlEncode(displayText[startIndex..matchIndex]));
            builder.Append("<mark class=\"insurance-deduction-search-highlight\">");
            builder.Append(WebUtility.HtmlEncode(displayText.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            startIndex = matchIndex + searchText.Length;
        }

        if (builder.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        builder.Append(WebUtility.HtmlEncode(displayText[startIndex..]));
        return new MarkupString(builder.ToString());
    }

    private static string FormatPayrollPeriod(int payrollMonth, int payrollYear) => $"{payrollMonth:00}/{payrollYear}";

    private static bool IsWholePeriodLockActionScope(string scope) =>
        string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal);

    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        if (normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth)
        {
            return (MinimumSupportedMonth, MinimumSupportedYear);
        }

        return (normalizedMonth, normalizedYear);
    }

    private (int Month, int Year, bool IsNormalized) GetDefaultPayrollPeriod()
    {
        var localNow = TimeZoneInfo.ConvertTime(TimeProvider.GetUtcNow(), ResolvePayrollTimeZone());
        var normalizedPeriod = NormalizeSelectedPeriod(localNow.Month, localNow.Year);
        return (
            normalizedPeriod.Month,
            normalizedPeriod.Year,
            normalizedPeriod.Month != localNow.Month || normalizedPeriod.Year != localNow.Year);
    }

    private static bool IsImportedInsurancePeriod(int payrollMonth, int payrollYear) =>
        payrollMonth == ImportedInsurancePeriodMonth && payrollYear == ImportedInsurancePeriodYear;

    private static TimeZoneInfo ResolvePayrollTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private static string GetLockBadgeCssClass(bool isLocked) => isLocked
        ? "yes-no-status yes-no-status-no hrm-grid-status"
        : "yes-no-status yes-no-status-yes hrm-grid-status";

    private sealed record MonthOption(int Value, string Text);

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }
}
