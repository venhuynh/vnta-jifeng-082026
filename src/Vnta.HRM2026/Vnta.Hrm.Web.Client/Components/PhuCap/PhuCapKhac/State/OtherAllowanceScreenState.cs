using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;
using Vnta.Hrm.Web.Client.Components.Shared.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.State;

/// <summary>Read-only state contract rendered by the period toolbar.</summary>
public sealed record OtherAllowanceToolbarState(
    int Month,
    int Year,
    int MinimumSupportedYear,
    int MaximumSupportedYear,
    IReadOnlyList<int> AvailableMonths,
    bool CanChangeFilters,
    bool CanView,
    bool CanCreate,
    bool CanInteract);

/// <summary>Read-only state contract rendered by the allowance grid and pager.</summary>
public sealed record OtherAllowanceGridState(
    IReadOnlyList<OtherAllowanceListItemDto> Rows,
    bool IsLoading,
    bool IsChangingLockState,
    string LoadingText,
    string? SearchText,
    int CurrentPageIndex,
    int PageSize,
    int TotalRecordCount,
    decimal TotalAllowanceAmount,
    int TotalPageCount,
    IReadOnlyList<int> PageSizeOptions,
    string PagerSummaryText,
    bool CanBrowsePages,
    string EmptyStateTitle,
    string EmptyStateMessage,
    string EmptyStateActionText);

/// <summary>Read-only state contract rendered by the create/edit dialog.</summary>
public sealed record OtherAllowanceEditDialogState(
    bool Visible,
    bool IsSaving,
    string Title,
    PhuCapKhacEditModel Model,
    bool IsCreateMode,
    IReadOnlyList<PhuCapKhacEmployeeOption> EmployeeOptions,
    string? ErrorMessage,
    bool CanEditFields,
    bool CanSave);

/// <summary>
/// The monthly-work row remains in its existing shared component namespace because it is
/// also consumed by other allowance screens.
/// </summary>
public sealed record OtherAllowanceMonthlyWorkDialogState(
    bool Visible,
    bool IsLoading,
    string Title,
    string Context,
    string? ErrorMessage,
    IReadOnlyList<MonthlyWorkdayPopupRow> Rows);

public sealed record OtherAllowanceRulesDialogState(bool Visible, string PeriodLabel);

/// <summary>Read-only state contract rendered by the lock-state scope dialog.</summary>
public sealed record OtherAllowanceLockActionDialogState(
    bool Visible,
    bool IsRefreshing,
    bool ShouldLock,
    string Title,
    string PromptText,
    string ContextText,
    string WholePeriodLabel,
    string SelectedScope,
    string SelectedRowsScope,
    string WholePeriodScope,
    string SelectedRowsDescription,
    string WholePeriodDescription,
    bool CanChooseSelectedRowsScope,
    bool CanConfirm);
