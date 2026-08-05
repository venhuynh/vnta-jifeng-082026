using DevExpress.Blazor;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

/// <summary>
/// Explicit UI contracts and entry points for the other-responsibility-allowance
/// screen. Sections use these immutable snapshots and never access infrastructure.
/// </summary>
public sealed partial class OtherResponsibilityAllowanceCoordinator
{
    public OtherResponsibilityAllowanceToolbarState Toolbar => new(
        ToolbarMonth,
        ToolbarYear,
        MinimumSupportedYear,
        MaximumSupportedYear,
        AvailableMonthOptions,
        CanChangeFilters,
        CanView,
        CanOpenActionsMenu,
        CanUseAppliedPeriodActions,
        CanOpenRules,
        CanExport,
        CanExportSelected);

    public OtherResponsibilityAllowanceResultsGridState ResultsGrid => new(
        ShowLoadingPanel,
        LoadingText,
        PeriodHintCssClass,
        PeriodHintText,
        SearchText,
        CanSearchScreen,
        CanUseAppliedPeriodActions,
        VisibleRecords,
        PageSize,
        SelectedGridItems,
        EmptyStateTitle,
        EmptyStateMessage,
        EmptyStateActionText,
        CanEmptyStateAction);

    public OtherResponsibilityAllowanceLoadErrorState LoadError => new(
        HasLoadError,
        DataLoadErrorMessage,
        !ShowLoadingPanel);

    public OtherResponsibilityAllowanceLockActionDialogState LockActionDialog => new(
        IsLockActionPopupVisible,
        IsRunningScreenAction,
        PendingLockActionState,
        CanConfirmLockAction,
        CanChooseSelectedRowsScope,
        LockActionPopupTitle,
        LockActionPromptText,
        LockActionContextText,
        SelectedLockActionScope,
        LockScopeSelectedRows,
        LockScopeWholePeriod,
        PendingLockActionPeriodLabel,
        SelectedRowsScopeDescription,
        WholePeriodScopeDescription);

    public OtherResponsibilityAllowanceRecalculateDialogState RecalculateDialog => new(
        IsRecalculateConfirmPopupVisible,
        IsRunningScreenAction,
        CanConfirmRecalculate,
        AppliedPeriodLabel);

    public OtherResponsibilityAllowanceRulesDialogState RulesDialog => new(IsRulesPopupVisible);

    public void Initialize(Func<Task> renderRequested) => requestRenderAsync = renderRequested;

    public Task ChangeMonthAsync(int value) => OnSelectedMonthChangedAsync(value);
    public Task ChangeYearAsync(int value) => OnSelectedYearChangedAsync(value);
    public Task ViewAsync() => ExecuteExclusiveAsync(OnViewRequestedAsync);
    public Task RetryAsync() => ExecuteExclusiveAsync(OnRetryAsync);
    public Task SearchAsync(string? value) => OnSearchTextChanged(value);
    public Task ChangePageSizeAsync(int value) => OnPageSizeChanged(value);
    public Task UpdateSelectedItemsAsync(IReadOnlyList<object> items) => OnSelectedGridItemsChanged(items);
    public Task SetGridAsync(IGrid? grid) => OnGridChanged(grid);
    public Task RunEmptyStateActionAsync() => ExecuteExclusiveAsync(OnEmptyStateActionClick);
    public Task OpenRecalculateDialogAsync() => OnRecalculateClickAsync();
    public Task ConfirmRecalculateAsync() => ExecuteExclusiveAsync(ConfirmRecalculateCoreAsync);
    public Task OpenLockDialogAsync(bool shouldLock) => OpenLockActionPopupAsync(shouldLock);
    public Task ConfirmLockActionAsync() => ExecuteExclusiveAsync(ConfirmLockActionCoreAsync);
    public void CloseLockDialog() => CloseLockActionPopup();
    public void CloseRecalculateDialog() => CloseRecalculateConfirmPopup();
    public void SelectLockScope(string scope) => SelectLockActionScope(scope);
    public void OpenRulesDialog() => OpenRulesPopup();
    public void CloseRulesDialog() => IsRulesPopupVisible = false;
    public Task ShowColumnChooserAsync() => OnColumnChooserRequested();
    public Task ExportAllToExcelAsync() => ExportAllDataToExcelAsync();
    public Task ExportAllToPdfAsync() => ExportAllDataToPdfAsync();
    public Task ExportSelectedToExcelAsync() => ExportSelectedRowsToExcelAsync();
    public Task ExportSelectedToPdfAsync() => ExportSelectedRowsToPdfAsync();

    private async Task ExecuteExclusiveAsync(Func<Task> operation)
    {
        if (IsDisposalRequested || !await screenActionGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        try
        {
            await operation();
        }
        finally
        {
            screenActionGate.Release();
        }
    }
}

public sealed record OtherResponsibilityAllowanceToolbarState(
    int Month,
    int Year,
    int MinimumYear,
    int MaximumYear,
    IReadOnlyList<OtherResponsibilityAllowanceMonthOption> AvailableMonths,
    bool CanChangeFilters,
    bool CanView,
    bool CanOpenActionsMenu,
    bool CanUseAppliedPeriodActions,
    bool CanOpenRules,
    bool CanExport,
    bool CanExportSelected);

public sealed record OtherResponsibilityAllowanceResultsGridState(
    bool ShowLoadingPanel,
    string LoadingText,
    string PeriodHintCssClass,
    string PeriodHintText,
    string? SearchText,
    bool CanSearchScreen,
    bool CanUseAppliedPeriodActions,
    IReadOnlyList<OtherResponsibilityAllowanceRecord> VisibleRecords,
    int PageSize,
    IReadOnlyList<object> SelectedGridItems,
    string EmptyStateTitle,
    string EmptyStateMessage,
    string EmptyStateActionText,
    bool CanEmptyStateAction);

public sealed record OtherResponsibilityAllowanceLoadErrorState(
    bool Visible,
    string? Message,
    bool CanRetry);

public sealed record OtherResponsibilityAllowanceLockActionDialogState(
    bool Visible,
    bool IsBusy,
    bool ShouldLock,
    bool CanConfirm,
    bool CanChooseSelectedRowsScope,
    string Title,
    string PromptText,
    string ContextText,
    string SelectedScope,
    string SelectedRowsScope,
    string WholePeriodScope,
    string WholePeriodLabel,
    string SelectedRowsDescription,
    string WholePeriodDescription);

public sealed record OtherResponsibilityAllowanceRecalculateDialogState(
    bool Visible,
    bool IsBusy,
    bool CanConfirm,
    string PeriodLabel);

public sealed record OtherResponsibilityAllowanceRulesDialogState(bool Visible);
