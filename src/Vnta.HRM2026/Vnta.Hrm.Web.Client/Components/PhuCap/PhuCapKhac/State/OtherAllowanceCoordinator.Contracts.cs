using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.State;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

/// <summary>Screen-facing contract: the route host depends on workflow capabilities, not a concrete coordinator.</summary>
public interface IOtherAllowanceScreenController : IDisposable
{
    OtherAllowanceToolbarState Toolbar { get; }
    OtherAllowanceGridState Grid { get; }
    OtherAllowanceEditDialogState EditDialog { get; }
    OtherAllowanceMonthlyWorkDialogState MonthlyWorkDialog { get; }
    OtherAllowanceRulesDialogState RulesDialog { get; }
    OtherAllowanceLockActionDialogState LockActionDialog { get; }
    IReadOnlyList<object> SelectedItems { get; }
    bool CanOperate { get; }
    bool CanSyncFromPreviousMonth { get; }
    bool CanLockSelectedRows { get; }
    bool CanUnlockSelectedRows { get; }
    bool HasLoadError { get; }
    string? LoadErrorMessage { get; }
    void Initialize();
    Task ChangeMonthAsync(int month);
    Task ChangeYearAsync(int year);
    Task ViewAsync();
    Task SearchAsync(string? searchText);
    Task ChangePageSizeAsync(int value);
    Task ChangePageAsync(int value);
    Task SelectRowsAsync(IReadOnlyList<object> items);
    Task SyncFromPreviousMonthAsync();
    Task OpenLockActionAsync(bool shouldLock);
    Task ConfirmLockActionAsync();
    void SelectLockActionScope(string scope);
    void CloseLockActionDialog();
    Task RunEmptyStateActionAsync();
    Task OpenCreateDialogAsync();
    void OpenEditDialog(OtherAllowanceListItemDto row);
    Task SaveEditAsync(PhuCapKhacEditModel draft);
    void CloseEditDialog();
    Task ToggleLockAsync(OtherAllowanceListItemDto row);
    Task OpenMonthlyWorkDialogAsync(OtherAllowanceListItemDto row);
    Task RefreshMonthlyWorkDialogAsync();
    void CloseMonthlyWorkDialog();
    void OpenRulesDialog();
    void CloseRulesDialog();
    bool CanEdit(OtherAllowanceListItemDto row);
    bool CanToggleLock(OtherAllowanceListItemDto row);
    bool CanViewMonthlyWork(OtherAllowanceListItemDto row);
}

public sealed partial class OtherAllowanceCoordinator
{
    public OtherAllowanceToolbarState Toolbar => new(
        ToolbarMonth,
        ToolbarYear,
        MinimumSupportedYear,
        MaximumSupportedYear,
        AvailableMonthOptions,
        CanChangeFilters,
        CanView,
        CanCreate,
        CanInteract);

    public OtherAllowanceGridState Grid => new(
        Rows,
        IsLoading || IsSyncingFromPreviousMonth,
        IsChangingLockState,
        LoadingText,
        SearchText,
        CurrentPageIndex,
        PageSize,
        ServerTotalRecordCount,
        TotalAllowanceAmount,
        TotalPageCount,
        PageSizeOptions,
        PagerSummaryText,
        CanBrowsePages,
        EmptyStateTitle,
        EmptyStateMessage,
        EmptyStateActionText);

    public OtherAllowanceEditDialogState EditDialog => new(
        IsEditPopupVisible,
        IsSavingEdit,
        EditPopupTitle,
        EditModel,
        IsCreateMode,
        CreateEmployeeOptions,
        EditErrorMessage,
        CanEditFields,
        CanSaveEdit);

    public OtherAllowanceMonthlyWorkDialogState MonthlyWorkDialog => new(
        IsMonthlyWorkPopupVisible,
        IsMonthlyWorkPopupLoading,
        MonthlyWorkPopupTitle,
        MonthlyWorkPopupContext,
        MonthlyWorkPopupErrorMessage,
        MonthlyWorkRows);

    public OtherAllowanceRulesDialogState RulesDialog => new(IsRulesPopupVisible, CurrentPeriodLabel);

    public OtherAllowanceLockActionDialogState LockActionDialog => new(
        IsLockActionPopupVisible,
        IsChangingLockState,
        PendingLockActionState,
        LockActionPopupTitle,
        LockActionPromptText,
        LockActionScopeContextText,
        PendingLockActionPeriodLabel,
        SelectedLockActionScope,
        LockScopeSelectedRows,
        LockScopeWholePeriod,
        SelectedRowsScopeDescription,
        WholePeriodScopeDescription,
        CanChooseSelectedRowsScope,
        CanConfirmLockAction);

    public string? LoadErrorMessage => ErrorMessage;

    public Task ChangeMonthAsync(int month) => OnSelectedMonthChangedAsync(month);
    public Task ChangeYearAsync(int year) => OnSelectedYearChangedAsync(year);
    public Task ViewAsync() => ExecuteExclusiveAsync(OnViewRequestedAsync);
    public Task SearchAsync(string? searchText) => OnSearchTextChangedAsync(searchText);
    public Task ChangePageSizeAsync(int value) => OnPageSizeChangedAsync(value);
    public Task ChangePageAsync(int value) => OnActivePageIndexChangedAsync(value);
    public Task SelectRowsAsync(IReadOnlyList<object> items)
    {
        selectedItems = items;
        return Task.CompletedTask;
    }
    public Task SyncFromPreviousMonthAsync() => ExecuteExclusiveAsync(SyncFromPreviousMonthCoreAsync);
    public Task OpenLockActionAsync(bool shouldLock) => ExecuteExclusiveAsync(() => OpenLockActionPopupAsync(shouldLock));
    public Task ConfirmLockActionAsync() => ExecuteExclusiveAsync(ConfirmLockActionCoreAsync);
    public void SelectLockActionScope(string scope) => SelectLockActionScopeCore(scope);
    public void CloseLockActionDialog() => CloseLockActionPopup();
    public Task RunEmptyStateActionAsync() => ExecuteExclusiveAsync(OnEmptyStateActionRequestedAsync);
    public Task OpenCreateDialogAsync() => ExecuteExclusiveAsync(OpenCreatePopupAsync);
    public void OpenEditDialog(OtherAllowanceListItemDto row) => OpenEditPopup(row);
    public Task SaveEditAsync(PhuCapKhacEditModel draft) => ExecuteExclusiveAsync(() => SaveEditCoreAsync(draft));
    public void CloseEditDialog() => CloseEditPopup();
    public Task ToggleLockAsync(OtherAllowanceListItemDto row) => ExecuteExclusiveAsync(() => ToggleLockStateAsync(row));
    public Task OpenMonthlyWorkDialogAsync(OtherAllowanceListItemDto row) => ExecuteExclusiveAsync(() => OpenMonthlyWorkPopupAsync(row));
    public Task RefreshMonthlyWorkDialogAsync() => ExecuteExclusiveAsync(RefreshMonthlyWorkPopupAsync);
    public void CloseMonthlyWorkDialog() => CloseMonthlyWorkPopup();
    public void OpenRulesDialog() => OpenRulesPopup();
    public void CloseRulesDialog() => IsRulesPopupVisible = false;

    public bool CanEdit(OtherAllowanceListItemDto row) => CanEditRow(row);
    public bool CanToggleLock(OtherAllowanceListItemDto row) => CanToggleLockRow(row);
    public bool CanViewMonthlyWork(OtherAllowanceListItemDto row) => CanViewMonthlyWorkRow(row);
}
