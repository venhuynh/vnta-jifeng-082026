using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN.Models;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN.Export;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN.Dialogs;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruThueTNCN;

public partial class KhauTruThueTNCN : IDisposable
{
    #region Cấu hình màn hình

    private const int MinimumSupportedMonth = 6;
    private const int MinimumSupportedYear = 2026;
    private const int MaximumSupportedYear = 2100;
    private const string PayrollTimeZoneId = "Asia/Ho_Chi_Minh";
    private const string PayrollTimeZoneWindowsId = "SE Asia Standard Time";
    private const string DeductionAmountTotalSummaryName = "DeductionAmountTotal";
    private const string LockScopeSelectedRows = "selected-rows";
    private const string LockScopeWholePeriod = "whole-period";

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly IReadOnlyList<MonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, $"Tháng {month:00}"))
            .ToArray();

    #endregion

    #region Phụ thuộc và vòng đời

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);

    [Inject]
    private PayrollPersonalIncomeTaxDeductionDataProvider PersonalIncomeTaxDataProvider { get; set; } = default!;

    private PayrollPersonalIncomeTaxDeductionDataProvider DataProvider => PersonalIncomeTaxDataProvider;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    #endregion

    #region Trạng thái màn hình

    private IReadOnlyList<PayrollPersonalIncomeTaxDeductionRecord> AllRecords { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private IGrid? ExportGrid { get; set; }
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;
    private IReadOnlyList<PayrollPersonalIncomeTaxDeductionExportRow> ExportRecords { get; set; } = [];
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private string CurrentLoadingText { get; set; } = HrmUiDefaults.LoadingText;
    private int ToolbarMonth { get; set; } = MinimumSupportedMonth;
    private int ToolbarYear { get; set; } = MinimumSupportedYear;
    private int AppliedMonth { get; set; } = MinimumSupportedMonth;
    private int AppliedYear { get; set; } = MinimumSupportedYear;
    private int PageSize { get; set; } = 50;
    private decimal VisibleDeductionTotal { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsChangingPageSize { get; set; }
    private bool IsEditPopupVisible { get; set; }
    private bool IsSavingEdit { get; set; }
    private bool IsRefreshingRow { get; set; }
    private KhauTruThueTNCNEditModel EditModel { get; set; } = new();
    private string EditPopupTitle { get; set; } = "Điều chỉnh Thuế TNCN";
    private bool IsRulesPopupVisible { get; set; }
    private bool IsLockActionPopupVisible { get; set; }
    private bool PendingLockActionState { get; set; } = true;
    private int PendingLockActionMonth { get; set; } = MinimumSupportedMonth;
    private int PendingLockActionYear { get; set; } = MinimumSupportedYear;
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;
    private bool HasRequestedData { get; set; }
    private bool IsExporting { get; set; }
    private bool IsDeductionTotalSyncPending { get; set; }
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;

    #endregion

    #region Trạng thái suy diễn

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool ShowLoadingPanel => IsLoading || IsChangingPageSize || IsExporting || IsRefreshingRow;
    private bool HasPendingPeriodChange =>
        HasRequestedData
        && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    private bool CanOperateOnCurrentDataset => CanInteract && HasRequestedData && !HasPendingPeriodChange;
    private bool CanCreate => false;
    private bool CanOpenLockAction => CanOperateOnCurrentDataset;
    private bool CanOpenUnlockAction => CanOperateOnCurrentDataset;
    private int SelectedRecordCount => GetSelectedResults().Count;
    private bool CanChooseSelectedRowsScope => SelectedRecordCount > 0;
    private bool CanConfirmLockAction => CanOperateOnCurrentDataset
        && (IsWholePeriodLockActionScope(SelectedLockActionScope) || CanChooseSelectedRowsScope);
    private bool CanView => !ShowLoadingPanel;
    private bool CanChangeFilters => CanView;
    private bool CanExport => false;
    private bool CanSaveEdit => !IsSavingEdit
        && CanOperateOnCurrentDataset
        && !EditModel.IsLocked
        && EditModel.PayrollDeductionSummaryRecordId != Guid.Empty
        && EditModel.DeductionAmount >= 0m
        && decimal.Round(EditModel.DeductionAmount, 2, MidpointRounding.AwayFromZero) == EditModel.DeductionAmount;
    private bool CanResetFilters => !string.IsNullOrWhiteSpace(SearchText);
    private string LoadingText => CurrentLoadingText;
    private string AppliedPeriodLabel => $"{AppliedMonth:00}/{AppliedYear}";
    private string PendingLockActionPeriodLabel => $"{PendingLockActionMonth:00}/{PendingLockActionYear}";
    private string LockActionPopupTitle => PendingLockActionState
        ? "Khóa dữ liệu Thuế TNCN"
        : "Mở khóa dữ liệu Thuế TNCN";
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa dữ liệu Thuế TNCN."
        : "Chọn phạm vi cần mở khóa dữ liệu Thuế TNCN.";
    private string LockActionScopeContextText =>
        $"Kỳ lương áp dụng: {PendingLockActionPeriodLabel}. Lựa chọn toàn kỳ sẽ bỏ qua bộ lọc tìm kiếm hiện tại.";
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {SelectedRecordCount:N0} dòng đang được chọn trong lưới."
        : "Chưa có dòng nào được chọn trong lưới hiện tại.";
    private string WholePeriodScopeDescription => PendingLockActionState
        ? $"Áp dụng cho toàn bộ dữ liệu Thuế TNCN của kỳ {PendingLockActionPeriodLabel}."
        : $"Mở khóa toàn bộ dữ liệu Thuế TNCN của kỳ {PendingLockActionPeriodLabel}.";
    private string ExportTooltip => HasRequestedData && !HasPendingPeriodChange
        ? $"Xuất toàn bộ dữ liệu thuế TNCN của kỳ {AppliedPeriodLabel}"
        : "Tải dữ liệu kỳ lương trước khi xuất file";
    private string EmptyStateTitle => !HasRequestedData
        ? "Chưa tải dữ liệu thuế TNCN"
        : HasPendingPeriodChange
            ? "Kỳ lương đã thay đổi"
        : CanResetFilters
            ? "Không tìm thấy dòng thuế TNCN phù hợp"
            : "Chưa có dữ liệu thuế TNCN";
    private string EmptyStateMessage => !HasRequestedData
        ? "Chọn tháng, năm kỳ lương rồi nhấn Xem để tải dữ liệu khi bạn sẵn sàng."
        : HasPendingPeriodChange
            ? $"Bạn đã đổi kỳ lương. Nhấn Xem để tải dữ liệu của kỳ {ToolbarMonth:00}/{ToolbarYear}."
        : CanResetFilters
            ? "Hãy nới điều kiện lọc hoặc xóa bộ lọc để xem thêm dữ liệu."
            : "Bảng thuế TNCN sẽ hiển thị tại đây sau khi có dữ liệu cho kỳ lương đang chọn.";
    private string EmptyStateActionText => !HasRequestedData
        ? "Xem dữ liệu"
        : HasPendingPeriodChange
            ? "Xem dữ liệu"
        : CanResetFilters
            ? "Đặt lại bộ lọc"
            : CanCreate
                ? "Thêm dòng thuế TNCN"
                : "Tải lại";
    private IReadOnlyList<MonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : MonthOptions;

    #endregion

    #region Lifecycle và tải dữ liệu

    protected override void OnInitialized()
    {
        var defaultPeriod = GetDefaultPayrollPeriod();
        ToolbarMonth = defaultPeriod.Month;
        ToolbarYear = defaultPeriod.Year;
        AppliedMonth = defaultPeriod.Month;
        AppliedYear = defaultPeriod.Year;
        base.OnInitialized();
    }

    protected override async Task OnInitializedAsync()
    {
        IsLoading = false;
        await base.OnInitializedAsync();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        exportGridRenderCompletionSource?.TrySetResult(true);

        if (IsDeductionTotalSyncPending)
        {
            IsDeductionTotalSyncPending = false;
            UpdateVisibleActualAllowanceTotalFromGrid();
            return InvokeAsync(StateHasChanged);
        }

        return base.OnAfterRenderAsync(firstRender);
    }

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
        LoadErrorMessage = null;
        EditErrorMessage = null;
        IsLoading = true;
        CurrentLoadingText = "Đang tải dữ liệu thuế TNCN...";

        try
        {
            await ClearSelectionAsync();
            AllRecords = await PersonalIncomeTaxDataProvider.SearchAsync(
                requestedMonth,
                requestedYear,
                SearchText,
                disposalTokenSource.Token);
            ResetVisibleActualAllowanceTotal();
            IsDeductionTotalSyncPending = true;
            AppliedMonth = requestedMonth;
            AppliedYear = requestedYear;
            HasRequestedData = true;
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
            AllRecords = [];
            ResetVisibleActualAllowanceTotal();
            LoadErrorMessage = "Có lỗi khi tải dữ liệu thuế TNCN. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách thuế TNCN.");
        }
        finally
        {
            IsLoading = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private Task OnColumnChooserRequested()
    {
        Grid?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    private Task OnGridFilterCriteriaChangedAsync(GridFilterCriteriaChangedEventArgs _)
    {
        IsDeductionTotalSyncPending = true;
        return InvokeAsync(StateHasChanged);
    }

    private Task OnViewRequestedAsync() => ReloadAsync();

    private Task OnSelectedMonthChangedAsync(int month)
    {
        ToolbarMonth = NormalizeSelectedPeriod(month, ToolbarYear).Month;
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
        var normalizedValue = NormalizeNullable(value);
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
        CurrentLoadingText = "Đang cập nhật số dòng hiển thị...";
        PageSize = value;
        IsDeductionTotalSyncPending = true;

        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }
        finally
        {
            IsChangingPageSize = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: true);

    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: false);

    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if (!CanOperateOnCurrentDataset)
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
        if (!IsLoading)
        {
            IsLockActionPopupVisible = false;
        }
    }

    private void SelectLockActionScope(string scope)
    {
        if (IsLoading)
        {
            return;
        }

        if (string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal)
            && CanChooseSelectedRowsScope)
        {
            SelectedLockActionScope = LockScopeSelectedRows;
        }
        else if (string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal))
        {
            SelectedLockActionScope = LockScopeWholePeriod;
        }
    }

    private async Task ConfirmLockActionAsync()
    {
        if (!CanConfirmLockAction)
        {
            return;
        }

        var shouldLock = PendingLockActionState;
        var scope = SelectedLockActionScope;
        Guid[]? targetIds = null;
        var selectedCount = 0;
        if (!IsWholePeriodLockActionScope(scope))
        {
            targetIds = GetSelectedResults()
                .Select(record => record.Id)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();
            selectedCount = targetIds.Length;
            if (selectedCount == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
                return;
            }
        }

        try
        {
            IsLoading = true;
            IsLockActionPopupVisible = false;
            CurrentLoadingText = BuildLockActionLoadingText(shouldLock, scope, selectedCount);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await PersonalIncomeTaxDataProvider.SetLockStateBatchAsync(
                PendingLockActionYear,
                PendingLockActionMonth,
                shouldLock,
                IsWholePeriodLockActionScope(scope)
                    ? PayrollPersonalIncomeTaxDeductionLockActionScope.WholePeriod
                    : PayrollPersonalIncomeTaxDeductionLockActionScope.SelectedRows,
                targetIds,
                disposalTokenSource.Token);

            if (result.TargetRowCount == 0)
            {
                ToastService.ShowInfo(BuildLockActionNoDataMessage(shouldLock, scope));
                return;
            }

            await ReloadAsync();
            ToastService.ShowSuccess(BuildLockActionSuccessMessage(
                shouldLock,
                scope,
                result.TargetRowCount,
                result.UpdatedCount));
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
            ToastService.ShowError($"Không thể {(shouldLock ? "khóa" : "mở khóa")} dữ liệu Thuế TNCN của kỳ {PendingLockActionPeriodLabel}.");
        }
        finally
        {
            IsLoading = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private void OpenRulesPopup()
    {
        IsRulesPopupVisible = true;
    }

    private async Task OnEmptyStateActionClick()
    {
        if (!HasRequestedData || HasPendingPeriodChange)
        {
            await ReloadAsync();
            return;
        }

        if (CanResetFilters)
        {
            await ResetFiltersAsync();
            return;
        }

        if (!CanCreate)
        {
            await ReloadAsync();
            return;
        }

        await ReloadAsync();
    }

    private void OpenEditPopup(PayrollPersonalIncomeTaxDeductionRecord record)
    {
        if (!CanOperateOnCurrentDataset)
        {
            return;
        }

        if (record.Id == Guid.Empty)
        {
            ToastService.ShowWarning("Không xác định được dòng Thuế TNCN để điều chỉnh.");
            return;
        }

        if (record.IsLocked)
        {
            ToastService.ShowWarning("Dòng Thuế TNCN hoặc kỳ tổng hợp đã khóa nên không thể điều chỉnh.");
            return;
        }

        EditErrorMessage = null;
        EditModel = new KhauTruThueTNCNEditModel
        {
            PayrollDeductionSummaryRecordId = record.Id,
            EmployeeDisplay = record.EmployeeDisplay,
            PayrollPeriodDisplay = record.PayrollPeriodDisplay,
            DeductionAmount = record.DeductionAmount,
            IsLocked = record.IsLocked,
            OriginalUpdatedAtUtc = record.UpdatedAtUtc
        };
        EditPopupTitle = $"Điều chỉnh Thuế TNCN - {record.EmployeeDisplay}";
        IsEditPopupVisible = true;
    }

    private void CloseEditPopup()
    {
        if (IsSavingEdit)
        {
            return;
        }

        IsEditPopupVisible = false;
        EditErrorMessage = null;
        EditModel = new();
        EditPopupTitle = "Điều chỉnh Thuế TNCN";
    }

    private async Task SaveEditAsync()
    {
        if (!CanSaveEdit)
        {
            return;
        }

        EditErrorMessage = null;
        try
        {
            IsSavingEdit = true;
            CurrentLoadingText = $"Đang cập nhật Thuế TNCN của {EditModel.EmployeeDisplay}...";
            var updated = await PersonalIncomeTaxDataProvider.UpdateManualValueAsync(
                EditModel.PayrollDeductionSummaryRecordId,
                EditModel.DeductionAmount,
                EditModel.OriginalUpdatedAtUtc,
                disposalTokenSource.Token);

            CloseEditPopupCore();
            await ReloadAsync();
            ToastService.ShowSuccess($"Đã cập nhật Thuế TNCN của {updated.EmployeeDisplay}.");
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
        }
        catch (Exception)
        {
            EditErrorMessage = "Không thể cập nhật Thuế TNCN. Vui lòng kiểm tra lại thông tin và thử lại.";
            ToastService.ShowError("Không thể cập nhật Thuế TNCN.");
        }
        finally
        {
            IsSavingEdit = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private void CloseEditPopupCore()
    {
        IsEditPopupVisible = false;
        EditErrorMessage = null;
        EditModel = new();
        EditPopupTitle = "Điều chỉnh Thuế TNCN";
    }

    private bool CanEditRow(PayrollPersonalIncomeTaxDeductionRecord record) =>
        CanOperateOnCurrentDataset && !record.IsLocked && record.Id != Guid.Empty;

    private bool CanRefreshRow(PayrollPersonalIncomeTaxDeductionRecord record) =>
        CanOperateOnCurrentDataset && !record.IsLocked && record.Id != Guid.Empty;

    private async Task RefreshRowAsync(PayrollPersonalIncomeTaxDeductionRecord record)
    {
        if (!CanRefreshRow(record))
        {
            return;
        }

        try
        {
            IsRefreshingRow = true;
            CurrentLoadingText = $"Đang làm mới Thuế TNCN của {record.EmployeeDisplay}...";

            var result = await PersonalIncomeTaxDataProvider.RefreshAsync(
                record.Id,
                record.PayrollMonth,
                record.PayrollYear,
                disposalTokenSource.Token);

            await ReloadAsync();
            if (result.SkippedLockedCount > 0)
            {
                ToastService.ShowWarning("Dòng Thuế TNCN đã khóa nên không được làm mới.");
            }
            else if (result.UpdatedCount > 0)
            {
                ToastService.ShowSuccess("Đã làm mới dòng Thuế TNCN.");
            }
            else
            {
                ToastService.ShowInfo("Dòng Thuế TNCN đã đồng bộ, không có thay đổi.");
            }
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
            ToastService.ShowError($"Không thể làm mới Thuế TNCN của {record.EmployeeDisplay}.");
        }
        finally
        {
            IsRefreshingRow = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private Task ExportAllDataToExcelAsync() => ExportAllForAppliedPeriodAsync(
        PayrollPersonalIncomeTaxDeductionExportFormat.Excel,
        () => ExportGrid!.ExportToXlsxAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu thuế TNCN kỳ {AppliedPeriodLabel} ra Excel.");

    private Task ExportAllDataToPdfAsync() => ExportAllForAppliedPeriodAsync(
        PayrollPersonalIncomeTaxDeductionExportFormat.Pdf,
        () => ExportGrid!.ExportToPdfAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu thuế TNCN kỳ {AppliedPeriodLabel} ra PDF.");

    private async Task ExportAllForAppliedPeriodAsync(
        PayrollPersonalIncomeTaxDeductionExportFormat format,
        Func<Task> exportAction,
        string successMessage)
    {
        if (!CanExport || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsExporting = true;
        CurrentLoadingText = $"Đang chuẩn bị toàn bộ dữ liệu thuế TNCN kỳ {AppliedPeriodLabel} để xuất file...";
        try
        {
            ExportRecords = await DataProvider.ExportAsync(
                AppliedYear,
                AppliedMonth,
                format,
                disposalTokenSource.Token);
            if (ExportRecords.Count == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu thuế TNCN của kỳ {AppliedPeriodLabel} để xuất file.");
                return;
            }

            exportGridRenderCompletionSource = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            await InvokeAsync(StateHasChanged);
            await exportGridRenderCompletionSource.Task.WaitAsync(disposalTokenSource.Token);

            if (ExportGrid is null)
            {
                throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
            }

            await exportAction();
            ToastService.ShowSuccess(successMessage);
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu thuế TNCN.");
        }
        finally
        {
            ExportRecords = [];
            exportGridRenderCompletionSource = null;
            IsExporting = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;

            if (!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    #endregion

    #region Tương tác toolbar, lưới và thao tác nghiệp vụ

    private async Task ResetFiltersAsync()
    {
        SearchText = null;

        if (HasRequestedData)
        {
            ToolbarMonth = AppliedMonth;
            ToolbarYear = AppliedYear;
        }

        await ReloadAsync();
    }

    private void ResetVisibleActualAllowanceTotal()
    {
        VisibleDeductionTotal = AllRecords.Sum(record => record.DeductionAmount);
    }

    private void UpdateVisibleActualAllowanceTotalFromGrid()
    {
        var grid = Grid;
        if (grid is null)
        {
            return;
        }

        var summaryItem = grid.GetTotalSummaryItems()
            .FirstOrDefault(item => string.Equals(
                item.Name,
                DeductionAmountTotalSummaryName,
                StringComparison.Ordinal));
        var summaryValue = summaryItem is null ? null : grid.GetTotalSummaryValue(summaryItem);
        VisibleDeductionTotal = summaryValue switch
        {
            decimal value => value,
            null => 0m,
            IConvertible value => Convert.ToDecimal(value, DisplayCulture),
            _ => 0m
        };
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

    private List<PayrollPersonalIncomeTaxDeductionRecord> GetSelectedResults() =>
        SelectedDataItems
            .OfType<PayrollPersonalIncomeTaxDeductionRecord>()
            .Where(IsVisibleResult)
            .DistinctBy(result => result.Id)
            .ToList();

    private int GetSelectedResultCount() => GetSelectedResults().Count;

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
        return NormalizeSelectedPeriod(localNow.Month, localNow.Year);
    }

    private static TimeZoneInfo ResolvePayrollTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(PayrollTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(PayrollTimeZoneWindowsId);
        }
    }

    private string BuildExportFileName() =>
        $"khau-tru-thue-tncn-{AppliedYear:D4}-{AppliedMonth:D2}";

    private bool IsVisibleResult(PayrollPersonalIncomeTaxDeductionRecord result) =>
        AllRecords.Any(row => row.Id == result.Id);

    private string FormatMoney(decimal value) =>
        value == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", value);

    private string FormatWorkday(decimal value) => value.ToString("0.##", DisplayCulture);

    private string FormatRate(decimal value) => value.ToString("P2", DisplayCulture);

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
            builder.Append("<mark class=\"personal-income-tax-search-highlight\">");
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

    private string BuildLockActionNoDataMessage(bool shouldLock, string scope) =>
        IsWholePeriodLockActionScope(scope)
            ? $"Không có dữ liệu Thuế TNCN của kỳ {PendingLockActionPeriodLabel} để {(shouldLock ? "khóa" : "mở khóa")}."
            : "Không còn dòng Thuế TNCN hợp lệ trong phạm vi đang chọn để xử lý.";

    private string BuildLockActionLoadingText(bool shouldLock, string scope, int selectedCount)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        return IsWholePeriodLockActionScope(scope)
            ? $"Đang xử lý {actionText} dữ liệu Thuế TNCN của kỳ {PendingLockActionPeriodLabel}..."
            : $"Đang xử lý {actionText} {selectedCount:N0} dòng Thuế TNCN đã chọn...";
    }

    private string BuildLockActionSuccessMessage(bool shouldLock, string scope, int targetRowCount, int updatedCount)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        var unchangedCount = Math.Max(0, targetRowCount - updatedCount);
        var scopeText = IsWholePeriodLockActionScope(scope)
            ? $"dòng Thuế TNCN của kỳ {PendingLockActionPeriodLabel}"
            : "dòng đã chọn";

        return unchangedCount > 0
            ? $"Đã {actionText} {updatedCount:N0}/{targetRowCount:N0} {scopeText}, giữ nguyên {unchangedCount:N0} dòng đã đúng trạng thái."
            : $"Đã {actionText} {updatedCount:N0} {scopeText}.";
    }

    private static string GetLockBadgeCssClass(bool isLocked) => isLocked
        ? "yes-no-status yes-no-status-no hrm-grid-status"
        : "yes-no-status yes-no-status-yes hrm-grid-status";

    private sealed record MonthOption(int Value, string Text);

    public void Dispose()
    {
        exportGridRenderCompletionSource?.TrySetCanceled();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    #endregion
}
