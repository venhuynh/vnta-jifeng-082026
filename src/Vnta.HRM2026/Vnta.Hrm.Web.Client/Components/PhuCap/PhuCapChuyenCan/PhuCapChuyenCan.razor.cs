using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Export;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Sections;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.State;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapChuyenCan;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan;

/// <summary>Đại diện kiểu <c>PhuCapChuyenCan</c> phục vụ màn hình phụ cấp chuyên cần.</summary>
public partial class PhuCapChuyenCan : IDisposable
{
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const int MinimumSupportedMonth = 6;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const int MinimumSupportedYear = 2026;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const int MaximumSupportedYear = 2100;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const int AllPageSize = 5000;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const string LockScopeSelectedRows = "selected-rows";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const string LockScopeWholePeriod = "whole-period";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const string SummaryAllKey = "all";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const string SummaryAttendanceClassAKey = "cc-a";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const string SummaryAttendanceClassBKey = "cc-b";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const string SummaryAttendanceClassCKey = "cc-c";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const string SummaryOpenKey = "open";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private const string SummaryLockedKey = "locked";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    /// <summary>Thực hiện xử lý cho luồng <c>readonly</c>.</summary>
    private static readonly (int Month, int Year) DefaultPayrollPeriod = GetDefaultPayrollPeriod();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private static readonly IReadOnlyList<MonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, $"Tháng {month:00}"))
            .ToArray();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private static readonly IReadOnlyList<PageSizeOption> PageSizeOptions =
    [
        new(20, "20"),
        new(50, "50"),
        new(100, "100"),
        new(AllPageSize, "Tất cả")
    ];

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private readonly CancellationTokenSource disposalTokenSource = new();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private AttendanceAllowanceReloadLifecycleState ReloadLifecycleState { get; } = new();

    [Inject]
    /// <summary>Giá trị <c>DataProvider</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private IAttendanceAllowanceReadDataProvider ReadDataProvider { get; set; } = default!;

    [Inject]
    private IAttendanceAllowanceExportDataProvider ExportDataProvider { get; set; } = default!;

    [Inject]
    private IAttendanceAllowanceRefreshDataProvider RefreshDataProvider { get; set; } = default!;

    [Inject]
    private IAttendanceAllowanceManualAdjustmentDataProvider ManualAdjustmentDataProvider { get; set; } = default!;

    [Inject]
    private IAttendanceAllowanceLockDataProvider LockDataProvider { get; set; } = default!;

    [Inject]
    private IAttendanceAllowanceFilterFactory FilterFactory { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>MonthlyWorkSummaryDataProvider</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private MonthlyWorkSummaryDataProvider MonthlyWorkSummaryDataProvider { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>ToastService</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private IHrmToastService ToastService { get; set; } = default!;

    /// <summary>Giá trị <c>Records</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private IReadOnlyList<AttendanceAllowanceResultRecord> Records { get; set; } = [];
    /// <summary>Bản ghi allowlist của toàn kỳ dùng riêng cho lưới xuất tệp.</summary>
    private IReadOnlyList<AttendanceAllowanceExportRowDto> ExportRecords { get; set; } = [];
    /// <summary>Giá trị <c>SummaryBadges</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private IReadOnlyList<AttendanceAllowanceSummaryBadge> SummaryBadges { get; set; } = BuildSummaryBadges(0, 0, 0, 0, 0);
    /// <summary>Giá trị <c>SelectedDataItems</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private AttendanceAllowanceSelectionState SelectionState { get; } = new();
    private IReadOnlyList<object> SelectedDataItems
    {
        get => SelectionState.Items;
        set => SelectionState.Items = value;
    }
    /// <summary>Tham chiếu đến section lưới kết quả phụ cấp chuyên cần.</summary>
    private PhuCapChuyenCanGrid? GridSection { get; set; }
    /// <summary>Tham chiếu đến thành phần xuất tệp có schema cố định.</summary>
    private PhuCapChuyenCanExportGrid? ExportSource { get; set; }
    /// <summary>Hoàn tất sau khi lưới export render xong.</summary>
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;
    /// <summary>Giá trị <c>ActiveSummaryBadgeKey</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string ActiveSummaryBadgeKey { get; set; } = SummaryAllKey;
    /// <summary>Giá trị <c>SearchText</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string? SearchText { get; set; }
    /// <summary>Cho biết lần tải dữ liệu gần nhất bị lỗi và cần cho phép người dùng thử lại.</summary>
    private bool HasLoadError { get; set; }
    /// <summary>Giá trị <c>CurrentLoadingText</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string CurrentLoadingText { get; set; } = HrmUiDefaults.LoadingText;
    /// <summary>Giá trị <c>ToolbarMonth</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int ToolbarMonth { get; set; } = DefaultPayrollPeriod.Month;
    /// <summary>Giá trị <c>ToolbarYear</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int ToolbarYear { get; set; } = DefaultPayrollPeriod.Year;
    /// <summary>Giá trị <c>AppliedMonth</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int AppliedMonth { get; set; } = DefaultPayrollPeriod.Month;
    /// <summary>Giá trị <c>AppliedYear</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int AppliedYear { get; set; } = DefaultPayrollPeriod.Year;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private int pageSize = PageSizeOptions[0].Value;
    /// <summary>Giá trị <c>currentPageIndex</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int currentPageIndex;
    /// <summary>Giá trị <c>totalRecordCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int totalRecordCount;
    /// <summary>Giá trị <c>periodTotalCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int periodTotalCount;
    /// <summary>Giá trị <c>periodCanLockCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int periodCanLockCount;
    /// <summary>Giá trị <c>periodCanUnlockCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int periodCanUnlockCount;
    /// <summary>Giá trị <c>periodSummaryLockedCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int periodSummaryLockedCount;
    /// <summary>Giá trị <c>IsLoading</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsLoading { get; set; }
    /// <summary>Giá trị <c>IsRefreshing</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsRefreshing { get; set; }
    /// <summary>Giá trị <c>IsChangingPageSize</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsChangingPageSize { get; set; }
    /// <summary>Giá trị <c>IsConfirmationBusy</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsConfirmationBusy { get; set; }
    /// <summary>Cho biết hệ thống đang tải hoặc tạo tệp export.</summary>
    private bool IsExporting { get; set; }
    /// <summary>Giá trị <c>IsRulesPopupVisible</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsRulesPopupVisible { get; set; }
    /// <summary>Giá trị <c>IsRulesLoading</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsRulesLoading { get; set; }
    /// <summary>Giá trị <c>AttendanceAllowanceRule</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private AttendanceAllowanceRuleDto? AttendanceAllowanceRule { get; set; }
    /// <summary>Giá trị <c>IsMonthlyWorkPopupVisible</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsMonthlyWorkPopupVisible { get; set; }
    /// <summary>Giá trị <c>IsMonthlyWorkPopupLoading</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsMonthlyWorkPopupLoading { get; set; }
    /// <summary>Giá trị <c>MonthlyWorkPopupTitle</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string MonthlyWorkPopupTitle { get; set; } = "Bảng công tháng";
    /// <summary>Giá trị <c>MonthlyWorkPopupContext</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    /// <summary>Giá trị <c>MonthlyWorkRows</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    /// <summary>Giá trị <c>MonthlyWorkPopupEmployeeId</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private Guid MonthlyWorkPopupEmployeeId { get; set; }
    /// <summary>Giá trị <c>MonthlyWorkPopupMonth</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int MonthlyWorkPopupMonth { get; set; }
    /// <summary>Giá trị <c>MonthlyWorkPopupYear</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int MonthlyWorkPopupYear { get; set; }
    /// <summary>Giá trị <c>IsEditPopupVisible</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsEditPopupVisible { get; set; }
    /// <summary>Giá trị <c>IsSavingEdit</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsSavingEdit { get; set; }
    /// <summary>Giá trị <c>IsRecalculateConfirmPopupVisible</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsRecalculateConfirmPopupVisible { get; set; }
    /// <summary>Giá trị <c>IsLockActionPopupVisible</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsLockActionPopupVisible { get; set; }
    /// <summary>Giá trị <c>PendingLockActionState</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool PendingLockActionState { get; set; }
    /// <summary>Giá trị <c>PendingLockActionMonth</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int PendingLockActionMonth { get; set; } = DefaultPayrollPeriod.Month;
    /// <summary>Giá trị <c>PendingLockActionYear</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int PendingLockActionYear { get; set; } = DefaultPayrollPeriod.Year;
    /// <summary>Giá trị <c>SelectedLockActionScope</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;
    /// <summary>Giá trị <c>EditModel</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private PhuCapChuyenCanEditModel EditModel { get; set; } = new();
    /// <summary>Giá trị <c>EditContext</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private EditContext EditContext { get; set; } = new(new PhuCapChuyenCanEditModel());
    /// <summary>Giá trị <c>HasRequestedData</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool HasRequestedData { get; set; }

    /// <summary>Giá trị <c>AvailableMonthOptions</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private IReadOnlyList<MonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : MonthOptions;
    /// <summary>Giá trị <c>HasPendingPeriodChange</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool HasPendingPeriodChange =>
        HasRequestedData
        && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);
    /// <summary>Giá trị <c>ShowLoadingPanel</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool ShowLoadingPanel => IsLoading || IsRefreshing || IsChangingPageSize || IsExporting || IsSavingEdit;
    /// <summary>Giá trị <c>CanInteract</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    /// <summary>Giá trị <c>CanView</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanView => !ShowLoadingPanel;
    /// <summary>Giá trị <c>CanChangeFilters</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanChangeFilters => !ShowLoadingPanel;
    /// <summary>Giá trị <c>CanOperateOnCurrentDataset</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanOperateOnCurrentDataset => CanInteract && HasRequestedData && !HasPendingPeriodChange;
    /// <summary>Giá trị <c>CanRecalculate</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanRecalculate => CanOperateOnCurrentDataset;
    /// <summary>Giá trị <c>CanExport</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanExport => CanOperateOnCurrentDataset && periodTotalCount > 0;
    /// <summary>Giá trị <c>CanExportSelected</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanExportSelected => CanExport && SelectedRecordCount > 0;
    /// <summary>Giá trị <c>CanLockSelectedRecords</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanLockSelectedRecords => CanOperateOnCurrentDataset;
    /// <summary>Giá trị <c>CanUnlockSelectedRecords</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanUnlockSelectedRecords => CanOperateOnCurrentDataset;
    /// <summary>Giá trị <c>SelectedRecordCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int SelectedRecordCount => GetSelectedResults().Count;
    /// <summary>Giá trị <c>VisibleActualAllowanceTotal</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private decimal VisibleActualAllowanceTotal => Records.Sum(record => record.ActualAllowanceAmount);
    /// <summary>Giá trị <c>CanChooseSelectedRowsScope</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanChooseSelectedRowsScope => SelectedRecordCount > 0;
    /// <summary>Giá trị <c>CanConfirmLockAction</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanConfirmLockAction =>
        CanOperateOnCurrentDataset
        && (string.Equals(SelectedLockActionScope, LockScopeWholePeriod, StringComparison.Ordinal) || CanChooseSelectedRowsScope);
    /// <summary>Giá trị <c>CanEditFields</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanEditFields => !IsSavingEdit && !EditModel.IsLocked && !HasPendingPeriodChange;
    /// <summary>Giá trị <c>CanSaveEdit</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanSaveEdit =>
        !IsSavingEdit
        && !HasPendingPeriodChange
        && EditModel.Id != Guid.Empty
        && !EditModel.IsLocked;
    /// <summary>Giá trị <c>CanResetFilters</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanResetFilters =>
        ToolbarMonth != DefaultPayrollPeriod.Month
        || ToolbarYear != DefaultPayrollPeriod.Year
        || !string.IsNullOrWhiteSpace(SearchText)
        || ActiveSummaryBadgeKey != SummaryAllKey;
    /// <summary>Giá trị <c>PageSize</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int PageSize => pageSize;
    /// <summary>Giá trị <c>AvailablePageSizeOptions</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private IReadOnlyList<PageSizeOption> AvailablePageSizeOptions => TotalRecordCount > AllPageSize
        ? PageSizeOptions.Where(option => option.Value != AllPageSize).ToArray()
        : PageSizeOptions;
    /// <summary>Giá trị <c>IsShowingAllRows</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool IsShowingAllRows => PageSize == AllPageSize;
    /// <summary>Giá trị <c>PageSizeDescription</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string PageSizeDescription => IsShowingAllRows ? "tất cả dòng" : "dòng/trang";
    /// <summary>Giá trị <c>CurrentPageIndex</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int CurrentPageIndex => currentPageIndex;
    /// <summary>Giá trị <c>TotalRecordCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int TotalRecordCount => totalRecordCount;
    /// <summary>Giá trị <c>PeriodTotalCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int PeriodTotalCount => periodTotalCount;
    /// <summary>Giá trị <c>PeriodCanLockCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int PeriodCanLockCount => periodCanLockCount;
    /// <summary>Giá trị <c>PeriodCanUnlockCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int PeriodCanUnlockCount => periodCanUnlockCount;
    /// <summary>Giá trị <c>PeriodSummaryLockedCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int PeriodSummaryLockedCount => periodSummaryLockedCount;
    /// <summary>Giá trị <c>TotalPageCount</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int TotalPageCount => TotalRecordCount <= 0
        ? 1
        : (int)Math.Ceiling(TotalRecordCount / (double)PageSize);
    /// <summary>Giá trị <c>CurrentPageStartRecord</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int CurrentPageStartRecord => TotalRecordCount == 0
        ? 0
        : CurrentPageIndex * PageSize + 1;
    /// <summary>Giá trị <c>CurrentPageEndRecord</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private int CurrentPageEndRecord => TotalRecordCount == 0
        ? 0
        : Math.Min(TotalRecordCount, CurrentPageIndex * PageSize + Records.Count);
    /// <summary>Giá trị <c>CanBrowsePages</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private bool CanBrowsePages => CanOperateOnCurrentDataset && TotalRecordCount > 0;
    /// <summary>Giá trị <c>LoadingText</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string LoadingText => CurrentLoadingText;
    /// <summary>Giá trị <c>CurrentPeriodLabel</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string CurrentPeriodLabel => FormatPayrollPeriod(ToolbarMonth, ToolbarYear);
    /// <summary>Giá trị <c>AppliedPeriodLabel</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string AppliedPeriodLabel => FormatPayrollPeriod(AppliedMonth, AppliedYear);
    /// <summary>Giá trị <c>PagerSummaryText</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string PagerSummaryText => !HasRequestedData || HasLoadError || TotalRecordCount == 0
        ? "Chưa có dữ liệu để hiển thị"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalRecordCount:N0} dòng";
    /// <summary>Giá trị <c>LockActionPopupTitle</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string LockActionPopupTitle => PendingLockActionState ? "Khóa phụ cấp chuyên cần" : "Mở khóa phụ cấp chuyên cần";
    /// <summary>Giá trị <c>PendingLockActionPeriodLabel</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string PendingLockActionPeriodLabel => FormatPayrollPeriod(PendingLockActionMonth, PendingLockActionYear);
    /// <summary>Giá trị <c>LockActionPromptText</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa dữ liệu phụ cấp chuyên cần."
        : "Chọn phạm vi cần mở khóa dữ liệu phụ cấp chuyên cần.";
    /// <summary>Giá trị <c>LockActionScopeContextText</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string LockActionScopeContextText =>
        $"Kỳ lương áp dụng: {PendingLockActionPeriodLabel}. Lựa chọn toàn kỳ sẽ bỏ qua bộ lọc tìm kiếm đang hiển thị.";
    /// <summary>Giá trị <c>SelectedRowsScopeDescription</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {SelectedRecordCount:N0} dòng đang được chọn trong trang hiện tại."
        : "Chưa có dòng nào được chọn trong trang hiện tại.";
    /// <summary>Giá trị <c>WholePeriodScopeDescription</c> được sử dụng bởi màn hình phụ cấp chuyên cần.</summary>
    private string WholePeriodScopeDescription => BuildWholePeriodScopeDescription(
        PendingLockActionState,
        PendingLockActionPeriodLabel,
        PeriodTotalCount,
        PeriodCanLockCount,
        PeriodCanUnlockCount,
        PeriodSummaryLockedCount);

    /// <summary>Xử lý sự kiện cho luồng <c>OnInitializedAsync</c>.</summary>
    protected override Task OnInitializedAsync() => base.OnInitializedAsync();

    /// <summary>Đánh dấu nguồn xuất tệp sẵn sàng sau render.</summary>
    private Task OnExportSourceRendered()
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }








    /// <summary>Giải phóng tài nguyên cho luồng <c>Dispose</c>.</summary>
    public void Dispose()
    {
        CancelActiveReload();
        disposalTokenSource.Cancel();
        reloadGate.Dispose();
        disposalTokenSource.Dispose();
    }
}
