using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Workspace trình bày danh sách ABC; state và I/O vẫn thuộc page coordinator.</summary>
public partial class PhuCapTrachNhiemGridSection
{
    private IGrid? grid;
    [Parameter, EditorRequired] public IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto> Records { get; set; } = [];
    [Parameter, EditorRequired] public IReadOnlyList<ResponsibilitySummaryBadge> SummaryBadges { get; set; } = [];
    [Parameter, EditorRequired] public IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    [Parameter, EditorRequired] public IReadOnlyList<int> PageSizeOptions { get; set; } = [];
    [Parameter] public int CurrentPageIndex { get; set; }
    [Parameter] public int PageSize { get; set; }
    [Parameter] public int TotalPageCount { get; set; }
    [Parameter] public decimal TotalActualAllowanceAmount { get; set; }
    [Parameter] public string ActiveSummaryBadgeKey { get; set; } = string.Empty;
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public string PagerSummaryText { get; set; } = string.Empty;
    [Parameter] public string EmptyStateTitle { get; set; } = string.Empty;
    [Parameter] public string EmptyStateMessage { get; set; } = string.Empty;
    [Parameter] public string EmptyStateActionText { get; set; } = string.Empty;
    [Parameter] public string LoadingText { get; set; } = string.Empty;
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public bool CanOperate { get; set; }
    [Parameter] public bool CanChangeFilters { get; set; }
    [Parameter] public bool CanBrowsePages { get; set; }
    [Parameter, EditorRequired] public Func<PayrollResponsibilityAllowanceAbcItemDto, bool> CanAdjust { get; set; } = default!;
    [Parameter, EditorRequired] public Func<PayrollResponsibilityAllowanceAbcItemDto, bool> CanViewCalculation { get; set; } = default!;
    [Parameter, EditorRequired] public Func<PayrollResponsibilityAllowanceAbcItemDto, bool> CanRefresh { get; set; } = default!;
    [Parameter, EditorRequired] public Func<PayrollResponsibilityAllowanceAbcItemDto, bool> CanToggleLock { get; set; } = default!;
    [Parameter, EditorRequired] public Func<PayrollResponsibilityAllowanceAbcItemDto, bool> CanViewMonthlyWork { get; set; } = default!;
    [Parameter, EditorRequired] public Func<string?, string> FormatOptional { get; set; } = default!;
    [Parameter, EditorRequired] public Func<decimal, string> FormatCurrency { get; set; } = default!;
    [Parameter, EditorRequired] public Func<decimal, string> FormatNumber { get; set; } = default!;
    [Parameter, EditorRequired] public Func<decimal, string> FormatWorkday { get; set; } = default!;
    [Parameter, EditorRequired] public Func<decimal, string> FormatPercentage { get; set; } = default!;
    [Parameter, EditorRequired] public Func<string?, string> GetAbcStatusCssClass { get; set; } = default!;
    [Parameter, EditorRequired] public Func<string?, string> GetAbcStatusText { get; set; } = default!;
    [Parameter, EditorRequired] public Func<bool, string> GetYesNoStatusCssClass { get; set; } = default!;
    [Parameter, EditorRequired] public Func<bool, string> GetPerformanceBonusStatusText { get; set; } = default!;
    [Parameter, EditorRequired] public Func<bool, string> GetLockStatusText { get; set; } = default!;
    [Parameter] public EventCallback<string> SummaryBadgeSelected { get; set; }
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<object>> SelectedDataItemsChanged { get; set; }
    [Parameter] public EventCallback<int> ActivePageIndexChanged { get; set; }
    [Parameter] public EventCallback<int> PageSizeChanged { get; set; }
    [Parameter] public EventCallback EmptyStateActionRequested { get; set; }
    [Parameter] public EventCallback<PayrollResponsibilityAllowanceAbcItemDto> AdjustmentRequested { get; set; }
    [Parameter] public EventCallback<PayrollResponsibilityAllowanceAbcItemDto> CalculationRequested { get; set; }
    [Parameter] public EventCallback<PayrollResponsibilityAllowanceAbcItemDto> RefreshRequested { get; set; }
    [Parameter] public EventCallback<PayrollResponsibilityAllowanceAbcItemDto> LockToggleRequested { get; set; }
    [Parameter] public EventCallback<PayrollResponsibilityAllowanceAbcItemDto> MonthlyWorkRequested { get; set; }
    public void ShowColumnChooser() => grid?.ShowColumnChooser();
    public async Task ClearSelectionAsync()
    {
        if (grid is null)
        {
            return;
        }

        await grid.DeselectAllAsync();
        grid.SetFocusedRowIndex(-1);
    }
}
