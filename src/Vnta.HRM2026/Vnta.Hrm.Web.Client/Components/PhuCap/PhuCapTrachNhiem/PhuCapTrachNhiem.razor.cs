using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Employees;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.Api;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

public partial class PhuCapTrachNhiem : IDisposable
{
    #region Hằng số giao diện và kỳ lương

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly ResponsibilityAllowancePeriodKey MinimumSupportedPeriod = new(2026, 6);
    private static readonly IReadOnlyList<MonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, $"Tháng {month:00}"))
            .ToArray();
    private static readonly int[] PageSizeOptions = [50, 100, 200];

    private const int MaximumSupportedYear = 2100;
    private const string PayrollTimeZoneId = "Asia/Ho_Chi_Minh";
    private const string PayrollTimeZoneWindowsId = "SE Asia Standard Time";
    private const int ResignedEmployeeStatus = 5;
    private const int ConfigGradesTabIndex = 0;
    private const int ConfigMappingsTabIndex = 1;
    private const string FocusPositionAssignments = "position-assignments";
    private const string FocusEmployeeAssignments = "employee-assignments";
    private const string SummaryAllKey = "all";
    private const string SummaryActiveKey = "active";
    private const string SummaryOpenKey = "open";
    private const string SummaryLockedKey = "locked";
    private const string SummaryAbcAKey = "abc-a";
    private const string SummaryAbcBKey = "abc-b";
    private const string SummaryAbcCKey = "abc-c";
    private const string SummaryAbcDKey = "abc-d";
    private const string LockScopeSelectedRows = "selected-rows";
    private const string LockScopeWholePeriod = "whole-period";

    #endregion

    #region Dependency được inject

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private PhuCapTrachNhiemReloadState ReloadState { get; } = new();
    private PhuCapTrachNhiemSelectionState SelectionState { get; } = new();

    [Inject]
    private PhuCapTrachNhiemAbcQueryDataProvider AbcQueryProvider { get; set; } = default!;

    [Inject]
    private PhuCapTrachNhiemAbcCommandDataProvider AbcCommandProvider { get; set; } = default!;

    [Inject]
    private PhuCapTrachNhiemConfigurationDataProvider ConfigurationProvider { get; set; } = default!;

    [Inject]
    private IPhuCapTrachNhiemQueryFactory QueryFactory { get; set; } = default!;

    [Inject]
    private EmployeeDataProvider EmployeeDataProvider { get; set; } = default!;

    [Inject]
    private AttendancePositionDataProvider PositionDataProvider { get; set; } = default!;

    [Inject]
    private MonthlyWorkSummaryDataProvider MonthlyWorkSummaryDataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "focus")]
    public string? Focus { get; set; }

    #endregion

    #region Trạng thái màn hình

    private IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto> AbcRows { get; set; } = [];
    private PayrollResponsibilityAllowanceAbcSummaryDto AbcSummary { get; set; } =
        new(0, 0, 0, 0, 0, 0, 0, 0);
    private int AbcTotalCount { get; set; }
    private IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> GradeRows { get; set; } = [];
    private IReadOnlyList<PayrollResponsibilityAllowanceGradePositionDto> MappingRows { get; set; } = [];
    private IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentDto> EmployeeAssignmentRows { get; set; } = [];
    private IReadOnlyList<EmployeeRecord> EmployeeRows { get; set; } = [];
    private IReadOnlyList<AttendancePositionRecord> PositionRows { get; set; } = [];
    private IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto> ExportRows { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems
    {
        get => SelectionState.Items;
        set => SelectionState.Items = value;
    }
    private PhuCapTrachNhiemGridSection? GridSection { get; set; }
    private PhuCapTrachNhiemExportGrid? ExportGrid { get; set; }
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;
    private int toolbarMonth = MinimumSupportedPeriod.Month;
    private int toolbarYear = MinimumSupportedPeriod.Year;

    private int AppliedMonth { get; set; } = MinimumSupportedPeriod.Month;
    private int AppliedYear { get; set; } = MinimumSupportedPeriod.Year;
    private int ToolbarMonth
    {
        get => toolbarMonth;
        set => toolbarMonth = Math.Clamp(value, GetMinimumSupportedMonth(ToolbarYear), 12);
    }

    private int ToolbarYear
    {
        get => toolbarYear;
        set
        {
            toolbarYear = Math.Clamp(value, MinimumSupportedYear, MaximumSupportedYear);
            toolbarMonth = Math.Clamp(toolbarMonth, GetMinimumSupportedMonth(toolbarYear), 12);
        }
    }

    private int pageSize = PageSizeOptions[0];
    private int currentPageIndex;
    private string ActiveSummaryBadgeKey { get; set; } = SummaryAllKey;
    private string? SearchText { get; set; }

    private string? LoadErrorMessage { get; set; }
    private string? DefaultPeriodWarningMessage { get; set; }
    private bool IsLoading { get; set; }
    private bool IsExecutingCommand { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool HasRequestedData { get; set; }
    private string CurrentCommandLoadingText { get; set; } = HrmUiDefaults.LoadingText;
    private int ConfigPopupActiveTabIndex { get; set; }
    private string? handledFocus;

    private bool IsConfigPopupVisible { get; set; }
    private bool IsRulesPopupVisible { get; set; }
    private bool IsAssignmentsPopupVisible { get; set; }
    private bool IsAdjustmentPopupVisible { get; set; }
    private bool IsCalculationPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupVisible { get; set; }
    private bool IsMonthlyWorkPopupLoading { get; set; }
    private bool IsRecalculateConfirmPopupVisible { get; set; }
    private bool IsPerformanceBonusPopupVisible { get; set; }
    private bool IsLockActionPopupVisible { get; set; }
    private bool PendingLockActionState { get; set; } = true;
    private int PendingLockActionMonth { get; set; } = MinimumSupportedPeriod.Month;
    private int PendingLockActionYear { get; set; } = MinimumSupportedPeriod.Year;
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;

    private string? ConfigPopupErrorMessage { get; set; }
    private string? AssignmentsPopupErrorMessage { get; set; }
    private string? AdjustmentPopupErrorMessage { get; set; }
    private string? PerformanceBonusErrorMessage { get; set; }
    private string? MonthlyWorkPopupErrorMessage { get; set; }
    private string MonthlyWorkPopupTitle { get; set; } = "Đối chiếu bảng công chi tiết";
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    private PayrollResponsibilityAllowanceAbcItemDto? MonthlyWorkPopupRecord { get; set; }

    private ResponsibilityAllowancePeriodKey ConfigPopupPeriod { get; set; } = MinimumSupportedPeriod;
    private ResponsibilityAllowancePeriodKey AssignmentsPopupPeriod { get; set; } = MinimumSupportedPeriod;
    private ResponsibilityAllowancePeriodKey? LoadedConfigPeriod { get; set; }

    private GradeFormModel GradeForm { get; set; } = GradeFormModel.CreateDefault();
    private Guid? EditingGradeId { get; set; }

    private MappingFormModel MappingForm { get; set; } = MappingFormModel.CreateDefault();
    private Guid? EditingMappingId { get; set; }

    private string AssignmentSearchText { get; set; } = string.Empty;
    private List<EmployeeAssignmentEditorRow> AssignmentEditorRows { get; set; } = [];

    private PayrollResponsibilityAllowanceAbcItemDto? AdjustmentTargetRow { get; set; }
    private PayrollResponsibilityAllowanceUpdateContextDto? AdjustmentContext { get; set; }
    private AdjustmentFormModel AdjustmentForm { get; set; } = AdjustmentFormModel.CreateDefault();
    private bool IsLoadingAdjustmentContext { get; set; }

    private PayrollResponsibilityAllowanceAbcItemDto? CalculationPopupRecord { get; set; }

    #endregion

    #region Vòng đời component

    protected override void OnInitialized()
    {
        var defaultPeriod = GetDefaultPayrollPeriod();
        ToolbarYear = defaultPeriod.Year;
        ToolbarMonth = defaultPeriod.Month;
        AppliedYear = defaultPeriod.Year;
        AppliedMonth = defaultPeriod.Month;
        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await HandleRouteFocusAsync();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    private Task OnExportGridRendered()
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    #endregion

    #region Trạng thái suy diễn phục vụ giao diện

    private int MinimumSupportedYear => MinimumSupportedPeriod.Year;
    private string CurrentPeriodLabel => $"{AppliedMonth:00}/{AppliedYear}";
    private string AdjustmentPopupTitle => AdjustmentTargetRow is null
        ? "Điều chỉnh trách nhiệm"
        : $"Điều chỉnh trách nhiệm - {AdjustmentTargetRow.EmployeeCode} - {AdjustmentTargetRow.EmployeeName}";
    private string CalculationPopupTitle => CalculationPopupRecord is null
        ? "Chi tiết tính phụ cấp trách nhiệm"
        : $"Chi tiết tính phụ cấp trách nhiệm - {CalculationPopupRecord.EmployeeCode} - {CalculationPopupRecord.EmployeeName}";
    private string ConfigPopupPeriodLabel => FormatPeriodLabel(ConfigPopupPeriod.Year, ConfigPopupPeriod.Month);
    private string AssignmentsPopupPeriodLabel => FormatPeriodLabel(AssignmentsPopupPeriod.Year, AssignmentsPopupPeriod.Month);
    private string RequestedPeriodLabel => FormatPeriodLabel(ToolbarYear, ToolbarMonth);
    // Toolbar là kỳ nháp; Applied chỉ đại diện cho snapshot đã được tải bằng thao tác Xem.
    private bool HasPendingPeriodChange => ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear;
    private bool HasOpenPopup =>
        IsConfigPopupVisible
        || IsRulesPopupVisible
        || IsAssignmentsPopupVisible
        || IsAdjustmentPopupVisible
        || IsCalculationPopupVisible
        || IsMonthlyWorkPopupVisible
        || IsRecalculateConfirmPopupVisible
        || IsPerformanceBonusPopupVisible
        || IsLockActionPopupVisible;
    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool ShowLoadingPanel => IsLoading || IsExecutingCommand || IsChangingPageSize;
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    private bool CanExecuteScreenActions => !ShowLoadingPanel && !HasLoadError && !HasOpenPopup;
    private bool CanUseLoadedDataActions => CanExecuteScreenActions && HasRequestedData && !HasPendingPeriodChange;
    private bool CanReload => !ShowLoadingPanel && !HasOpenPopup;
    private bool CanView => CanReload;
    private bool CanOperateOnCurrentDataset => CanUseLoadedDataActions;
    private bool CanRecalculate => CanUseLoadedDataActions;
    private bool CanSavePerformanceBonus =>
        !ShowLoadingPanel && !HasLoadError && HasRequestedData && !HasPendingPeriodChange;
    private bool CanOpenLockAction => CanOperateOnCurrentDataset;
    private bool CanOpenUnlockAction => CanOperateOnCurrentDataset;
    private bool CanOperateLockAction =>
        !ShowLoadingPanel && !HasLoadError && HasRequestedData && !HasPendingPeriodChange;
    private bool CanChooseSelectedRowsScope => GetSelectedAbcRows().Count > 0;
    private bool CanConfirmLockAction =>
        CanOperateLockAction
        && (IsWholePeriodLockActionScope(SelectedLockActionScope) || CanChooseSelectedRowsScope);
    private bool CanChangeFilters => CanReload;
    private bool CanExport => CanUseLoadedDataActions && AbcSummary.TotalCount > 0;
    private bool CanViewMonthlyWork(PayrollResponsibilityAllowanceAbcItemDto row) =>
        CanUseLoadedDataActions && row.EmployeeId != Guid.Empty;
    private bool CanToggleLockRow(PayrollResponsibilityAllowanceAbcItemDto row) =>
        CanUseLoadedDataActions && row.Id != Guid.Empty;
    private string PendingLockActionPeriodLabel => $"{PendingLockActionMonth:00}/{PendingLockActionYear}";
    private string LockActionPopupTitle => PendingLockActionState
        ? "Khóa phụ cấp trách nhiệm"
        : "Mở khóa phụ cấp trách nhiệm";
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa phụ cấp trách nhiệm."
        : "Chọn phạm vi cần mở khóa phụ cấp trách nhiệm.";
    private string LockActionScopeContextText =>
        $"Kỳ áp dụng: {PendingLockActionPeriodLabel}. Khi chọn toàn bộ kỳ, thao tác áp dụng cho tất cả nhân viên có phụ cấp trách nhiệm trong kỳ này, không phụ thuộc kết quả tìm kiếm hoặc bộ lọc đang hiển thị.";
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {GetSelectedAbcRows().Count:N0} nhân viên đang được chọn trong danh sách."
        : "Chưa có nhân viên nào được chọn trong danh sách hiện tại.";
    private string WholePeriodScopeDescription => PendingLockActionState
        ? $"Khóa phụ cấp trách nhiệm của tất cả nhân viên trong kỳ {PendingLockActionPeriodLabel}."
        : $"Mở khóa phụ cấp trách nhiệm của tất cả nhân viên trong kỳ {PendingLockActionPeriodLabel}.";
    private string LoadingText => IsChangingPageSize
        ? "Đang cập nhật số dòng hiển thị..."
        : IsExecutingCommand
            ? CurrentCommandLoadingText
            : "Đang tải dữ liệu phụ cấp trách nhiệm...";
    private IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto> PagedAbcRows => AbcRows;
    private int PageSize => pageSize;
    private int CurrentPageIndex => currentPageIndex;
    private int TotalRecordCount => AbcTotalCount;
    private int TotalPageCount => TotalRecordCount <= 0
        ? 1
        : (int)Math.Ceiling(TotalRecordCount / (double)PageSize);
    private int CurrentPageStartRecord => TotalRecordCount == 0
        ? 0
        : CurrentPageIndex * PageSize + 1;
    private int CurrentPageEndRecord => TotalRecordCount == 0
        ? 0
        : Math.Min(TotalRecordCount, CurrentPageIndex * PageSize + AbcRows.Count);
    private bool CanBrowsePages => CanOperateOnCurrentDataset && TotalRecordCount > 0;
    private string PagerSummaryText => !HasRequestedData || HasLoadError || TotalRecordCount == 0
        ? "Chưa có trang dữ liệu"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalRecordCount:N0} dòng";
    private IReadOnlyList<ResponsibilitySummaryBadge> SummaryBadges =>
    [
        new(SummaryAllKey, "Tất cả dòng ABC", "TC", AbcSummary.TotalCount),
        new(SummaryActiveKey, "Đang hưởng phụ cấp trách nhiệm", "Hưởng", AbcSummary.ActiveCount),
        new(SummaryAbcAKey, "Xếp loại ABC: A", "A", AbcSummary.AbcACount),
        new(SummaryAbcBKey, "Xếp loại ABC: B", "B", AbcSummary.AbcBCount),
        new(SummaryAbcCKey, "Xếp loại ABC: C", "C", AbcSummary.AbcCCount),
        new(SummaryAbcDKey, "Xếp loại ABC: D", "D", AbcSummary.AbcDCount),
        new(SummaryOpenKey, "Đang mở", "Mở", AbcSummary.OpenCount),
        new(SummaryLockedKey, "Đã khóa", "Khóa", AbcSummary.LockedCount)
    ];
    private IReadOnlyList<MonthOption> AvailableMonthOptions =>
        MonthOptions
            .Where(option => option.Value >= GetMinimumSupportedMonth(ToolbarYear))
            .ToArray();
    private bool HasActiveSearch => !string.IsNullOrWhiteSpace(SearchText);
    private bool HasActiveSummaryBadge =>
        !string.Equals(ActiveSummaryBadgeKey, SummaryAllKey, StringComparison.Ordinal);
    private string ActiveSummaryBadgeLabel =>
        SummaryBadges.FirstOrDefault(badge =>
            string.Equals(badge.Key, ActiveSummaryBadgeKey, StringComparison.Ordinal))?.Label
        ?? "Tất cả";
    private decimal TotalActualAllowanceAmount => PagedAbcRows.Sum(static row => row.ActualResponsibilityAllowanceAmount);
    private IReadOnlyList<EmployeeAssignmentEditorRow> FilteredAssignmentEditorRows =>
        AssignmentEditorRows
            .Where(MatchesAssignmentFilter)
            .OrderBy(row => row.Employee.EmployeeCode)
            .ThenBy(row => row.Employee.LastName)
            .ThenBy(row => row.Employee.FirstName)
            .ToArray();
    private string EmptyStateTitle => !HasRequestedData
        ? "Chưa tải dữ liệu phụ cấp trách nhiệm"
        : HasPendingPeriodChange
            ? "Kỳ lương đã thay đổi"
            : HasActiveSearch
                ? "Không tìm thấy phụ cấp trách nhiệm phù hợp"
                : HasActiveSummaryBadge
                    ? $"Không có dòng phụ cấp trách nhiệm thuộc nhóm {ActiveSummaryBadgeLabel}"
                    : $"Chưa có dòng trách nhiệm cho kỳ {CurrentPeriodLabel}";
    private string EmptyStateMessage => !HasRequestedData
        ? $"Chọn tháng, năm kỳ lương rồi nhấn Xem để tải workflow phụ cấp trách nhiệm kỳ {RequestedPeriodLabel} khi bạn sẵn sàng."
        : HasPendingPeriodChange
            ? $"Kỳ {RequestedPeriodLabel} chưa được tải. Nhấn Xem để áp dụng bộ lọc tháng, năm mới."
            : HasActiveSearch
                ? "Hãy đổi từ khóa tìm kiếm hoặc xóa bộ lọc để xem toàn bộ dữ liệu của kỳ lương đang tải."
                : HasActiveSummaryBadge
                    ? "Hãy chọn nhóm badge khác hoặc chọn Tất cả để xem toàn bộ dữ liệu của kỳ lương đang tải."
                    : "Hãy cấu hình cấp bậc, gán nhân viên rồi bấm Tính lại để tạo hoặc làm mới snapshot tháng.";
    private string EmptyStateActionText => !HasRequestedData
        ? "Xem dữ liệu"
        : HasPendingPeriodChange
            ? "Xem kỳ đã chọn"
            : HasActiveSearch
                ? "Xem tất cả"
                : HasActiveSummaryBadge
                    ? "Xem tất cả"
                    : "Làm mới snapshot";
    #endregion

    #region Tải và đồng bộ dữ liệu
    private async Task EnsureLookupDataAsync(bool includeEmployees, bool includePositions)
    {
        var employeesTask = includeEmployees && EmployeeRows.Count == 0
            ? LoadEmployeesAsync()
            : null;
        var positionsTask = includePositions && PositionRows.Count == 0
            ? LoadPositionsAsync()
            : null;

        if (employeesTask is not null)
        {
            EmployeeRows = await employeesTask;
        }

        if (positionsTask is not null)
        {
            PositionRows = await positionsTask;
        }
    }

    private async Task<IReadOnlyList<EmployeeRecord>> LoadEmployeesAsync() =>
        (await EmployeeDataProvider.GetAsync(disposalTokenSource.Token))
        .Where(row => row.Status != ResignedEmployeeStatus)
        .OrderBy(row => row.EmployeeCode)
        .ThenBy(row => row.FullName)
        .ToArray();

    private async Task<IReadOnlyList<AttendancePositionRecord>> LoadPositionsAsync() =>
        (await PositionDataProvider.GetAsync(disposalTokenSource.Token))
        .OrderBy(row => row.Name)
        .ToArray();

    private void SyncPopupTargetsAfterReload()
    {
        if (AdjustmentTargetRow is not null)
        {
            AdjustmentTargetRow = AbcRows.FirstOrDefault(row =>
                row.EmployeeId == AdjustmentTargetRow.EmployeeId
                && row.Year == AdjustmentTargetRow.Year
                && row.Month == AdjustmentTargetRow.Month);
        }

        if (CalculationPopupRecord is not null)
        {
            CalculationPopupRecord = AbcRows.FirstOrDefault(row =>
                row.EmployeeId == CalculationPopupRecord.EmployeeId
                && row.Year == CalculationPopupRecord.Year
                && row.Month == CalculationPopupRecord.Month);
        }
    }

    #endregion

    #region Thao tác thanh công cụ và bộ lọc

    private async Task OnRetryAsync()
    {
        ApplyToolbarPeriod();
        await ReloadAsync();
    }

    private async Task OnViewClick()
    {
        ApplyToolbarPeriod();
        currentPageIndex = 0;
        await ReloadAsync();
    }

    private Task OnViewRequestedAsync() => OnViewClick();

    private Task OnSelectedMonthChangedAsync(int month)
    {
        if (ToolbarMonth == month)
        {
            return Task.CompletedTask;
        }

        ToolbarMonth = month;
        InvalidateReloadForPendingPeriodChange();
        return Task.CompletedTask;
    }

    private Task OnSelectedYearChangedAsync(int year)
    {
        if (ToolbarYear == year)
        {
            return Task.CompletedTask;
        }

        ToolbarYear = year;
        InvalidateReloadForPendingPeriodChange();
        return Task.CompletedTask;
    }

    private Task OnAssignmentSearchTextChanged(string? value)
    {
        AssignmentSearchText = value ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnConfigPopupActiveTabIndexChanged(int value)
    {
        ConfigPopupActiveTabIndex = value;
        return Task.CompletedTask;
    }

    private Task OpenRulesPopupAsync()
    {
        if (!CanInteract || disposalTokenSource.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        IsRulesPopupVisible = true;
        return Task.CompletedTask;
    }

    private void OnColumnChooserRequested() => GridSection?.ShowColumnChooser();

    private void ApplyToolbarPeriod()
    {
        ToolbarYear = ToolbarYear;
        ToolbarMonth = ToolbarMonth;
        AppliedYear = ToolbarYear;
        AppliedMonth = ToolbarMonth;
    }

    private ResponsibilityAllowancePeriodKey GetRequestedPeriod() => new(ToolbarYear, ToolbarMonth);

    private async Task EnsureRequestedPeriodLoadedAsync()
    {
        var requestedPeriod = GetRequestedPeriod();
        var shouldReload = !HasRequestedData
            || requestedPeriod.Year != AppliedYear
            || requestedPeriod.Month != AppliedMonth;
        if (!shouldReload)
        {
            return;
        }

        AppliedYear = requestedPeriod.Year;
        AppliedMonth = requestedPeriod.Month;
        await ReloadAsync();
    }

    private async Task OnPageSizeChanged(int value)
    {
        var normalizedValue = PageSizeOptions.Contains(value) ? value : PageSizeOptions[0];
        if (PageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;

        try
        {
            var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
            pageSize = normalizedValue;
            currentPageIndex = firstVisibleRecordIndex / PageSize;
            await ClearSelectionAsync();
            await ReloadAsync();
        }
        finally
        {
            IsChangingPageSize = false;
        }
    }

    private async Task OnActivePageIndexChangedAsync(int value)
    {
        if (!CanBrowsePages)
        {
            return;
        }

        var normalizedValue = Math.Clamp(value, 0, Math.Max(0, TotalPageCount - 1));
        if (normalizedValue == currentPageIndex)
        {
            return;
        }

        currentPageIndex = normalizedValue;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if(string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalizedValue;
        currentPageIndex = 0;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task OnSummaryBadgeClickAsync(string badgeKey)
    {
        if (!CanInteract
            || string.IsNullOrWhiteSpace(badgeKey)
            || string.Equals(ActiveSummaryBadgeKey, badgeKey, StringComparison.Ordinal)
            || !SummaryBadges.Any(badge => string.Equals(badge.Key, badgeKey, StringComparison.Ordinal)))
        {
            return;
        }

        ActiveSummaryBadgeKey = badgeKey;
        currentPageIndex = 0;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task OnEmptyStateActionClick()
    {
        if (!HasRequestedData || HasPendingPeriodChange)
        {
            await OnViewClick();
            return;
        }

        if (HasActiveSearch)
        {
            SearchText = null;
            currentPageIndex = 0;
            await ClearSelectionAsync();
            return;
        }

        if (HasActiveSummaryBadge)
        {
            ActiveSummaryBadgeKey = SummaryAllKey;
            currentPageIndex = 0;
            await ClearSelectionAsync();
            return;
        }

        await RefreshAllAsync();
    }

    private Task OnCalculateAbcClickAsync() => CalculateAbcAsync();

    private Task OnRecalculateClickAsync() => OpenRecalculateConfirmPopupAsync();

    private Task OnPerformanceBonusClickAsync() => OpenPerformanceBonusPopupAsync();

    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: true);

    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: false);

    #endregion







    #region Command cấp màn hình

    /// <summary>
    /// Tính riêng xếp loại ABC cho các dòng đang áp dụng THS của kỳ đã tải. Luồng
    /// này không refresh snapshot/bậc; backend đọc lại công tháng và công chuẩn.
    /// </summary>
    private async Task CalculateAbcAsync()
    {
        if (!CanRecalculate)
        {
            return;
        }

        try
        {
            ApplyToolbarPeriod();
            CalculatePayrollResponsibilityAllowanceAbcResult? result = null;
            await RunBusyAsync(
                $"Đang tính ABC kỳ {CurrentPeriodLabel}...",
                async () =>
                {
                    result = await AbcCommandProvider.CalculateAsync(
                        new RefreshPayrollResponsibilityAllowanceAbcRequest(AppliedYear, AppliedMonth, null),
                        disposalTokenSource.Token);
                    await ReloadAsync();
                });

            if (result is null || HasLoadError)
            {
                return;
            }

            if (result.Updated == 0)
            {
                ToastService.ShowInfo(
                    $"Kỳ {CurrentPeriodLabel} không có dòng đang áp dụng THS chưa khóa để tính ABC.");
                return;
            }

            ToastService.ShowSuccess(
                $"Đã tính ABC cho {result.Updated:N0}/{result.TotalRows:N0} dòng áp dụng THS kỳ {CurrentPeriodLabel}: A={result.RatedA:N0}, B={result.RatedB:N0}, C={result.RatedC:N0}, D={result.RatedD:N0}; bỏ qua khóa {result.SkippedLocked:N0}.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            // Component đã dispose; không hiển thị lỗi cho người dùng.
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Không thể tính ABC. {ex.Message}");
        }
    }

    private async Task RefreshAllAsync()
    {
        try
        {
            ApplyToolbarPeriod();
            RefreshPayrollResponsibilityAllowanceAbcResult? result = null;
            await RunBusyAsync(
                $"Đang làm mới snapshot phụ cấp trách nhiệm kỳ {CurrentPeriodLabel}...",
                async () =>
                {
                    result = await AbcCommandProvider.RefreshAsync(
                        new RefreshPayrollResponsibilityAllowanceAbcRequest(AppliedYear, AppliedMonth, null),
                        disposalTokenSource.Token);
                    await ReloadAsync();
                });

            if (result is not null && !HasLoadError)
            {
                ToastService.ShowSuccess($"Đã làm mới snapshot phụ cấp trách nhiệm: thêm {result.Inserted}, cập nhật {result.Updated}, bỏ qua khóa {result.SkippedLocked}, thiếu nguồn {result.SkippedMissingSource}.");
            }
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
            ToastService.ShowError("Không thể làm mới dữ liệu phụ cấp trách nhiệm.");
        }
    }

    private Task OpenRecalculateConfirmPopupAsync()
    {
        if (!CanRecalculate)
        {
            return Task.CompletedTask;
        }

        IsRecalculateConfirmPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseRecalculateConfirmPopup()
    {
        IsRecalculateConfirmPopupVisible = false;
    }

    private Task OpenPerformanceBonusPopupAsync()
    {
        if (!CanOperateOnCurrentDataset)
        {
            return Task.CompletedTask;
        }

        PerformanceBonusErrorMessage = null;
        IsPerformanceBonusPopupVisible = true;
        return Task.CompletedTask;
    }

    private void ClosePerformanceBonusPopup()
    {
        if (IsExecutingCommand)
        {
            return;
        }

        IsPerformanceBonusPopupVisible = false;
        PerformanceBonusErrorMessage = null;
    }

    private async Task SavePerformanceBonusAsync(decimal monthlyPerformanceBonusAmount)
    {
        if (!CanSavePerformanceBonus)
        {
            return;
        }

        if (monthlyPerformanceBonusAmount < 0m)
        {
            PerformanceBonusErrorMessage = "Hệ số thưởng hiệu suất không được âm.";
            return;
        }

        var snapshotRows = await AbcQueryProvider.LoadAllAsync(
            AppliedYear,
            AppliedMonth,
            disposalTokenSource.Token);
        var concurrencyTokens = snapshotRows
            .Select(row => new PayrollResponsibilityAllowanceAbcConcurrencyToken(
                row.EmployeeId,
                GetConcurrencyTimestamp(row)))
            .ToArray();

        try
        {
            UpdatePayrollResponsibilityPerformanceBonusForPeriodResult? result = null;
            await RunBusyAsync(
                $"Đang nhập thưởng hiệu suất cho kỳ {CurrentPeriodLabel}...",
                async () =>
                {
                    result = await AbcCommandProvider.UpdatePerformanceBonusForPeriodAsync(
                        AppliedYear,
                        AppliedMonth,
                        decimal.Round(monthlyPerformanceBonusAmount, 4, MidpointRounding.AwayFromZero),
                        concurrencyTokens,
                        disposalTokenSource.Token);
                    await ReloadAsync();
                });

            if (result is null)
            {
                return;
            }

            IsPerformanceBonusPopupVisible = false;
            PerformanceBonusErrorMessage = null;
            var message = $"Đã nhập thưởng hiệu suất cho {result.Updated:N0}/{result.TotalRows:N0} dòng kỳ {CurrentPeriodLabel}.";
            if (result.PerformanceBonusExcludedRows > 0)
            {
                message += $" {result.PerformanceBonusExcludedRows:N0} dòng không áp dụng THS khi tính tiền.";
            }

            if (result.SkippedLocked > 0)
            {
                message += $" Bỏ qua {result.SkippedLocked:N0} dòng đã khóa.";
            }

            if (result.SkippedLocked > 0 || result.PerformanceBonusExcludedRows > 0)
            {
                ToastService.ShowWarning(message);
            }
            else
            {
                ToastService.ShowSuccess(message);
            }
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (HrmApiException ex)
        {
            PerformanceBonusErrorMessage = ex.UserMessage;
        }
        catch
        {
            PerformanceBonusErrorMessage = "Không thể lưu thưởng hiệu suất. Vui lòng thử lại.";
        }
    }

    private async Task ConfirmRecalculateAsync()
    {
        CloseRecalculateConfirmPopup();

        if (!CanRecalculate)
        {
            return;
        }

        try
        {
            ApplyToolbarPeriod();
            RefreshPayrollResponsibilityAllowanceAbcResult? result = null;
            await RunBusyAsync(
                $"Đang làm mới snapshot phụ cấp trách nhiệm kỳ {CurrentPeriodLabel}...",
                async () =>
                {
                    result = await AbcCommandProvider.RefreshAsync(
                        new RefreshPayrollResponsibilityAllowanceAbcRequest(AppliedYear, AppliedMonth, null),
                        disposalTokenSource.Token);
                    await ReloadAsync();
                });

            if (result is not null && !HasLoadError)
            {
                ToastService.ShowSuccess(
                    $"Đã làm mới snapshot phụ cấp trách nhiệm: thêm {result.Inserted}, cập nhật {result.Updated}, bỏ qua khóa {result.SkippedLocked}, thiếu nguồn {result.SkippedMissingSource}. Xếp loại ABC được giữ nguyên.");
            }
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
    }

    #endregion

    #region Truy vấn cục bộ và định dạng hiển thị

    private bool CanAdjustRow(PayrollResponsibilityAllowanceAbcItemDto row) =>
        CanUseLoadedDataActions && row.Id != Guid.Empty && row.EmployeeId != Guid.Empty && !row.IsLocked;

    private bool CanViewCalculation(PayrollResponsibilityAllowanceAbcItemDto row) =>
        CanUseLoadedDataActions && row.Id != Guid.Empty;

    private bool CanRefreshRow(PayrollResponsibilityAllowanceAbcItemDto row) =>
        CanAdjustRow(row);

    private int GetMinimumSupportedMonth(int year) =>
        year == MinimumSupportedYear ? MinimumSupportedPeriod.Month : 1;

    private ResponsibilityAllowancePeriodKey GetDefaultPayrollPeriod()
    {
        var localNow = TimeZoneInfo.ConvertTime(TimeProvider.GetUtcNow(), ResolvePayrollTimeZone());
        var normalizedPeriod = NormalizeSelectedPeriod(localNow.Year, localNow.Month);

        if (normalizedPeriod.Year != localNow.Year || normalizedPeriod.Month != localNow.Month)
        {
            DefaultPeriodWarningMessage =
                $"Kỳ hiện tại {localNow.Month:00}/{localNow.Year} nằm ngoài phạm vi hỗ trợ; hệ thống chọn {normalizedPeriod.Month:00}/{normalizedPeriod.Year}.";
        }

        return normalizedPeriod;
    }

    private static ResponsibilityAllowancePeriodKey NormalizeSelectedPeriod(int year, int month)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedPeriod.Year, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        if (normalizedYear == MinimumSupportedPeriod.Year && normalizedMonth < MinimumSupportedPeriod.Month)
        {
            normalizedMonth = MinimumSupportedPeriod.Month;
        }

        return new ResponsibilityAllowancePeriodKey(normalizedYear, normalizedMonth);
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

    private bool MatchesAssignmentFilter(EmployeeAssignmentEditorRow row)
    {
        if (string.IsNullOrWhiteSpace(AssignmentSearchText))
        {
            return true;
        }

        var keyword = NormalizeText(AssignmentSearchText);
        var target = NormalizeText(
            $"{row.Employee.EmployeeCode} {row.Employee.LastName} {row.Employee.FirstName} {row.Employee.DepartmentName} {row.Employee.PositionName}");
        return target.Contains(keyword, StringComparison.Ordinal);
    }

    private string GetGradeLabel(Guid gradeId)
    {
        var grade = GradeRows.FirstOrDefault(row => row.Id == gradeId);
        return grade is null ? "Không tìm thấy bậc" : $"{grade.Code} - {grade.Name}";
    }

    private static string NormalizeText(string? value)
    {
        var normalizedCharacters = (value ?? string.Empty)
            .Normalize(NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .Select(char.ToLowerInvariant);

        return new string(normalizedCharacters.ToArray()).Trim();
    }

    private static string FormatOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Chưa có" : value.Trim();

    private string FormatCurrency(decimal amount) =>
        amount == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", amount);

    private static string FormatPeriodLabel(int year, int month) => $"{month:00}/{year}";

    private static DateTime GetConcurrencyTimestamp(PayrollResponsibilityAllowanceAbcItemDto row) =>
        row.UpdatedAtUtc ?? row.CreatedAtUtc;

    private string FormatNumber(decimal value) =>
        value.ToString("0.##", DisplayCulture);

    private string FormatWorkday(decimal value) =>
        value.ToString("0.0000", DisplayCulture);

    private string FormatPercentage(decimal value) =>
        value.ToString("P2", DisplayCulture);

    private static string GetAbcStatusCssClass(string? abcRating) =>
        string.Join(' ', "responsibility-abc-status", ResolveAbcStatusCssClass(abcRating));

    private static string GetAbcStatusText(string? abcRating)
    {
        var normalized = abcRating?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "NA" : normalized;
    }

    private static string ResolveAbcStatusCssClass(string? abcRating) =>
        (abcRating ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "A" => "responsibility-abc-status-a",
            "B" => "responsibility-abc-status-b",
            "C" => "responsibility-abc-status-c",
            "D" => "responsibility-abc-status-d",
            _ => "responsibility-abc-status-neutral"
        };

    private static string GetYesNoStatusCssClass(bool value) =>
        string.Join(' ', "yes-no-status", value ? "yes-no-status-yes" : "yes-no-status-no");

    private static string GetActiveTextCssClass(bool value) =>
        string.Join(' ', "yes-no-status", value ? "yes-no-status-yes" : "yes-no-status-neutral");

    private static string GetActiveText(bool isActive) => isActive ? "Đang dùng" : "Ngừng";

    private static string GetPerformanceBonusStatusText(bool isPerformanceBonusExcluded) =>
        isPerformanceBonusExcluded ? "Không áp dụng" : "Áp dụng";

    private static string GetLockStatusText(bool isLocked) => isLocked ? "Khóa" : "Mở";

    private string BuildCalculationDescription(PayrollResponsibilityAllowanceAbcItemDto row)
    {
        if (row.IsPerformanceBonusExcluded)
        {
            var missingWorkDays = Math.Max(row.StandardWorkDays - row.ActualWorkDays, 0m);
            return missingWorkDays <= 1m
                ? "Trách nhiệm thực tế = Trách nhiệm chuẩn"
                : "Trách nhiệm thực tế = Trách nhiệm chuẩn / Công chuẩn tháng x CTL";
        }

        return string.Equals(row.AbcRating, "D", StringComparison.OrdinalIgnoreCase)
            ? "Trách nhiệm thực tế = 70% x Trách nhiệm chuẩn x Hệ số THS hiệu lực / Công chuẩn tháng x CTL"
            : "Trách nhiệm thực tế = Trách nhiệm chuẩn x Hệ số ABCD x Hệ số THS hiệu lực";
    }

    private string BuildCalculationFormula(PayrollResponsibilityAllowanceAbcItemDto row)
    {
        return $"Kết quả do máy chủ tính cho snapshot hiện tại: {FormatCurrency(row.ActualResponsibilityAllowanceAmount)}";
    }

    private IReadOnlyList<CalculationDetailRow> BuildCalculationDetails(PayrollResponsibilityAllowanceAbcItemDto row)
    {
        return
        [
            new("Ngày tính ABC", FormatWorkday(row.ActualWorkDays), "Công hành chính hợp lệ trừ số ngày đi trễ/về sớm quy đổi từ attendance_workday_summaries của kỳ đang chọn."),
            new("Công chuẩn", FormatNumber(row.StandardWorkDays), "Lấy từ payroll_basic_salary_records.StandardWorkingDays."),
            new("ABC", row.AbcRating, "A/B/C/D được tính theo số ngày thiếu công của tháng."),
            new("THS", FormatPercentage(row.MonthlyPerformanceBonusAmount), "Hệ số thưởng hiệu suất đang lưu trên bảng ABC."),
            new("Áp dụng THS", row.IsPerformanceBonusExcluded ? "Không áp dụng" : "Áp dụng", "Bật/tắt ảnh hưởng trực tiếp tới công thức tính tiền thực tế."),
            new("Tiền chuẩn", FormatCurrency(row.StandardResponsibilityAllowanceAmount), "Lấy từ DS Cấp bậc của nguồn áp dụng tại kỳ đang xem."),
            new("Tiền thực tế", FormatCurrency(row.ActualResponsibilityAllowanceAmount), "Kết quả backend sau refresh/tính ABC/cập nhật THS.")
        ];
    }

    private static Guid ParseRequiredGuid(string? value, string fieldName)
    {
        return TryParseGuid(value, out var result)
            ? result
            : throw new InvalidOperationException($"{fieldName} là bắt buộc.");
    }

    private static bool TryParseGuid(string? value, out Guid result) =>
        Guid.TryParse(value, out result) && result != Guid.Empty;

    #endregion

    #region Giải phóng tài nguyên và mô hình nội bộ

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        reloadGate.Dispose();
        disposalTokenSource.Dispose();
    }

    #endregion
}
