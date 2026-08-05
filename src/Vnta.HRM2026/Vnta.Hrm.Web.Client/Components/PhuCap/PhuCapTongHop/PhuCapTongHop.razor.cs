using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>
/// Route host for the allowance-summary screen. Workflow implementations live
/// in focused partials; this file owns composition and shared UI state only.
/// </summary>
public partial class PhuCapTongHop : IDisposable
{
    private const int MinimumSupportedYear = 2026;
    private const int MinimumSupportedMonth = 6;
    private const int MaximumSupportedYear = 2100;
    private const string SummaryAllKey = "all";
    private const string SummaryOpenKey = "open";
    private const string SummaryLockedKey = "locked";
    private const string LockScopeSelectedRows = "selected-rows";
    private const string LockScopeWholePeriod = "whole-period";
    private const string ResponsibilityAllowanceAmountTotalSummaryName = "ResponsibilityAllowanceAmountTotal";
    private const string ResponsibilityOtherAllowanceAmountTotalSummaryName = "ResponsibilityOtherAllowanceAmountTotal";
    private const string SeniorityAllowanceAmountTotalSummaryName = "SeniorityAllowanceAmountTotal";
    private const string AttendanceAllowanceAmountTotalSummaryName = "AttendanceAllowanceAmountTotal";
    private const string MealAllowanceAmountTotalSummaryName = "MealAllowanceAmountTotal";
    private const string HazardAllowanceAmountTotalSummaryName = "HazardAllowanceAmountTotal";
    private const string OtherAllowanceAmountTotalSummaryName = "OtherAllowanceAmountTotal";
    private const string LeaveHolidayAllowanceAmountTotalSummaryName = "LeaveHolidayAllowanceAmountTotal";
    private const string TotalAllowanceAmountTotalSummaryName = "TotalAllowanceAmountTotal";
    private const string DefaultLoadingText = "Đang tải dữ liệu tổng hợp phụ cấp...";

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly PayrollAllowanceSummaryOverviewDto EmptySummary = new(0, 0, 0, 0);
    private static readonly (int Month, int Year) DefaultPayrollPeriod = GetDefaultPayrollPeriod();
    private static readonly IReadOnlyList<MonthOption> AvailableMonthOptions = BuildMonthOptions();
    private static readonly IReadOnlyList<PageSizeOption> PageSizeOptions =
    [
        new(50, "50"),
        new(100, "100"),
        new(200, "200")
    ];

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private PhuCapTongHopReloadState ReloadState { get; } = new();
    private PhuCapTongHopSelectionState SelectionState { get; } = new();
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;

    [Inject] private IHrmDialogService DialogService { get; set; } = default!;
    [Inject] private IHrmToastService ToastService { get; set; } = default!;
    [Inject] private IPayrollAllowanceSummaryDataProvider DataProvider { get; set; } = default!;
    [Inject] private IMonthlyWorkSummaryDataProvider MonthlyWorkSummaryDataProvider { get; set; } = default!;
    [Inject] private IPhuCapTongHopFilterFactory FilterFactory { get; set; } = default!;

    private IGrid? Grid { get; set; }
    private IGrid? ExportGrid { get; set; }
    private IReadOnlyList<PayrollAllowanceSummaryRecord> Records { get; set; } = [];
    private IReadOnlyList<PayrollAllowanceSummaryExportRecord> ExportRecords { get; set; } = [];
    private IReadOnlyList<AllowanceSummaryBadge> SummaryBadges { get; set; } = BuildSummaryBadges(EmptySummary);
    private IReadOnlyList<object> SelectedDataItems
    {
        get => SelectionState.Items;
        set => SelectionState.Items = value;
    }

    private PayrollAllowanceSummaryOverviewDto Summary { get; set; } = EmptySummary;
    private AllowanceAmountTotals VisibleAllowanceTotals { get; set; } = AllowanceAmountTotals.Empty;
    private string ActiveSummaryBadgeKey { get; set; } = SummaryAllKey;
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string? ManualEditErrorMessage { get; set; }
    private string? CurrentActionLoadingText { get; set; }
    private int ToolbarMonth { get; set; } = DefaultPayrollPeriod.Month;
    private int ToolbarYear { get; set; } = DefaultPayrollPeriod.Year;
    private int AppliedMonth { get; set; } = DefaultPayrollPeriod.Month;
    private int AppliedYear { get; set; } = DefaultPayrollPeriod.Year;
    private int pageSize = PageSizeOptions[0].Value;
    private int currentPageIndex;
    private int totalRecordCount;
    private bool IsLoading { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool IsSyncConfirmPopupVisible { get; set; }
    private bool IsSyncingFromPreviousMonth { get; set; }
    private bool IsRefreshingAllowances { get; set; }
    private bool IsTogglingLock { get; set; }
    private bool IsLockActionPopupVisible { get; set; }
    private bool IsManualEditPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupLoading { get; set; }
    private bool IsSavingManualValues { get; set; }
    private bool IsExporting { get; set; }
    private bool HasRequestedData { get; set; }
    private bool IsAllowanceTotalsSyncPending { get; set; }
    private PhuCapTongHopManualEditModel? ManualEditModel { get; set; }
    private string? MonthlyWorkPopupErrorMessage { get; set; }
    private string MonthlyWorkPopupTitle { get; set; } = "Bảng công tháng";
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    private PayrollAllowanceSummaryRecord? MonthlyWorkPopupRecord { get; set; }
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    private bool IsRefreshConfirmPopupVisible { get; set; }
    private bool PendingLockActionState { get; set; } = true;
    private int PendingLockActionMonth { get; set; } = DefaultPayrollPeriod.Month;
    private int PendingLockActionYear { get; set; } = DefaultPayrollPeriod.Year;
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool HasPendingPeriodChange => HasRequestedData && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);
    private bool ShowLoadingPanel => IsLoading || IsChangingPageSize || IsSyncingFromPreviousMonth || IsRefreshingAllowances || IsTogglingLock || IsSavingManualValues || IsExporting;
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError && !IsSavingManualValues;
    private bool CanView => !ShowLoadingPanel;
    private bool CanOperateOnCurrentDataset => CanInteract && HasRequestedData && !HasPendingPeriodChange;
    private bool CanChangeFilters => !ShowLoadingPanel && !IsManualEditPopupVisible && !IsSyncConfirmPopupVisible && !IsRefreshConfirmPopupVisible && !IsLockActionPopupVisible;
    private bool CanSyncFromPreviousMonth => CanOperateOnCurrentDataset;
    private bool CanRefreshAllowances => CanOperateOnCurrentDataset;
    private bool CanExport => CanOperateOnCurrentDataset && !IsExporting;
    private bool CanOpenLockAction => CanOperateOnCurrentDataset;
    private bool CanOpenUnlockAction => CanOperateOnCurrentDataset;
    private bool CanChooseSelectedRowsScope => SelectedRowCount > 0;
    private bool CanConfirmLockAction => CanOperateOnCurrentDataset && (IsWholePeriodLockActionScope(SelectedLockActionScope) || CanChooseSelectedRowsScope);
    private bool CanSaveManualValues => CanOperateOnCurrentDataset && !IsSavingManualValues && ManualEditModel is not null;
    private int SelectedRowCount => GetSelectedRows().Count;
    private PayrollAllowanceSummaryRecord? SelectedRecord => GetSelectedRows().LastOrDefault();
    private int PageSize => pageSize;
    private int CurrentPageIndex => currentPageIndex;
    private int TotalRecordCount => totalRecordCount;
    private int TotalPageCount => TotalRecordCount <= 0 ? 1 : (int)Math.Ceiling(TotalRecordCount / (double)PageSize);
    private int CurrentPageStartRecord => TotalRecordCount == 0 ? 0 : CurrentPageIndex * PageSize + 1;
    private int CurrentPageEndRecord => TotalRecordCount == 0 ? 0 : Math.Min(TotalRecordCount, CurrentPageIndex * PageSize + Records.Count);
    private bool CanBrowsePages => CanOperateOnCurrentDataset && TotalRecordCount > 0;
    private string CurrentPayrollPeriodDisplay => $"{AppliedMonth:00}/{AppliedYear}";
    private string ExportTooltip => $"Xuất toàn bộ dữ liệu tổng hợp phụ cấp của kỳ {CurrentPayrollPeriodDisplay}";
    private string ExportExcelTooltip => $"{ExportTooltip} ra Excel";
    private string ExportPdfTooltip => $"{ExportTooltip} ra PDF";
    private string PendingLockActionPeriodDisplay => $"{PendingLockActionMonth:00}/{PendingLockActionYear}";
    private string LoadingText => IsChangingPageSize ? "Đang cập nhật số dòng hiển thị..." : CurrentActionLoadingText ?? DefaultLoadingText;
    private string PagerSummaryText => !HasRequestedData || HasLoadError || TotalRecordCount == 0 ? "Chưa có trang dữ liệu" : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalRecordCount:N0} dòng";

    private void AttachGrid(IGrid? grid) => Grid = grid;
    private void AttachExportGrid(IGrid? grid) => ExportGrid = grid;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if(IsExporting && ExportGrid is not null)
        {
            exportGridRenderCompletionSource?.TrySetResult(true);
        }

        if(IsAllowanceTotalsSyncPending)
        {
            IsAllowanceTotalsSyncPending = false;
            UpdateVisibleAllowanceTotalsFromGrid();
            return InvokeAsync(StateHasChanged);
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    public void Dispose()
    {
        CancelActiveReload();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    private readonly record struct AllowanceAmountTotals(decimal Total, decimal Responsibility, decimal ResponsibilityOther, decimal Seniority, decimal Attendance, decimal Meal, decimal Hazard, decimal Other, decimal LeaveHoliday)
    {
        public static readonly AllowanceAmountTotals Empty = new();
    }

    internal readonly record struct PayrollAllowanceSummaryReloadSnapshot(int PayrollMonth, int PayrollYear, string? SearchText, bool? IsLocked, int PageIndex, int PageSize);
}
