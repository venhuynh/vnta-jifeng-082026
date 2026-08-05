using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapCom;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Đại diện kiểu <c>PhuCapCom</c> phục vụ màn hình phụ cấp cơm.</summary>
public partial class PhuCapCom : IDisposable
{
    #region Hằng số và cấu hình màn hình

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private const int AllPageSize = 5000;
    /// <summary>Danh sách kích thước trang chuẩn của màn hình phụ cấp cơm.</summary>
    private static readonly IReadOnlyList<PageSizeOption> PageSizeOptions =
    [
        new(20, "20"),
        new(50, "50"),
        new(100, "100"),
        new(AllPageSize, "Tất cả")
    ];
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private const int MinimumSupportedMonth = 6;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private const int MinimumSupportedYear = 2026;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private const int MaximumSupportedYear = 2100;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private const string SummaryAllKey = "all";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private const string SummaryWithAllowanceKey = "with-allowance";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private const string SummaryWithoutAllowanceKey = "without-allowance";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private const string LockScopeSelectedRows = "selected-rows";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private const string LockScopeWholePeriod = "whole-period";

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    /// <summary>Thực hiện xử lý cho luồng <c>readonly</c>.</summary>
    private static readonly (int Month, int Year) DefaultReferencePeriod = GetDefaultPayrollPeriod();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private static readonly int DefaultReferenceMonth = DefaultReferencePeriod.Month;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private static readonly int DefaultReferenceYear = DefaultReferencePeriod.Year;

    #endregion

    #region Phụ thuộc được tiêm

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private readonly CancellationTokenSource disposalTokenSource = new();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    /// <summary>Trạng thái reload chống ghi đè kết quả cũ của màn hình phụ cấp cơm.</summary>
    private PhuCapComReloadState ReloadState { get; } = new();
    private PhuCapComSelectionState SelectionState { get; } = new();

    [Inject]
    /// <summary>Giá trị <c>DataProvider</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private IPhuCapComDataProvider DataProvider { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>MonthlyWorkSummaryDataProvider</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private IMonthlyWorkSummaryDataProvider MonthlyWorkSummaryDataProvider { get; set; } = default!;

    [Inject]
    private IPhuCapComFilterFactory FilterFactory { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>ToastService</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private IHrmToastService ToastService { get; set; } = default!;

    #endregion

    #region Trạng thái giao diện

    /// <summary>Giá trị <c>Records</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private IReadOnlyList<MealAllowanceRecord> Records { get; set; } = [];
    /// <summary>Giá trị <c>SelectedDataItems</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private IReadOnlyList<object> SelectedDataItems
    {
        get => SelectionState.Items;
        set => SelectionState.Items = value;
    }
    /// <summary>Giá trị <c>ExportRecords</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private IReadOnlyList<MealAllowanceRecord> ExportRecords { get; set; } = [];
    /// <summary>Giá trị <c>CurrentSummary</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private MealAllowanceSummaryDto CurrentSummary { get; set; } = EmptySummary;
    /// <summary>Giá trị <c>Grid</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private PhuCapComGrid? Grid { get; set; }
    /// <summary>Giá trị <c>ExportGrid</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private PhuCapComExportGrid? ExportGrid { get; set; }
    /// <summary>Giá trị <c>exportGridRenderCompletionSource</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;
    /// <summary>Giá trị <c>SelectedAllowanceSummaryKey</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string SelectedAllowanceSummaryKey { get; set; } = SummaryAllKey;
    /// <summary>Giá trị <c>SearchText</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string? SearchText { get; set; }
    /// <summary>Giá trị <c>LoadErrorMessage</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string? LoadErrorMessage { get; set; }
    /// <summary>Giá trị <c>LoadingText</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string LoadingText { get; set; } = HrmUiDefaults.LoadingText;
    /// <summary>Giá trị <c>ToolbarMonth</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int ToolbarMonth { get; set; } = DefaultReferenceMonth;
    /// <summary>Giá trị <c>ToolbarYear</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int ToolbarYear { get; set; } = DefaultReferenceYear;
    /// <summary>Giá trị <c>AppliedMonth</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int? AppliedMonth { get; set; }
    /// <summary>Giá trị <c>AppliedYear</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int? AppliedYear { get; set; }
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private int pageSize = PageSizeOptions[0].Value;
    /// <summary>Giá trị <c>currentPageIndex</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int currentPageIndex;
    /// <summary>Giá trị <c>totalRecordCount</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int totalRecordCount;
    /// <summary>Giá trị <c>IsLoading</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool IsLoading { get; set; }
    /// <summary>Giá trị <c>IsRefreshing</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool IsRefreshing { get; set; }
    /// <summary>Giá trị <c>IsChangingPageSize</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool IsChangingPageSize { get; set; }
    /// <summary>Giá trị <c>IsExporting</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool IsExporting { get; set; }
    /// <summary>Giá trị <c>IsRecalculateConfirmPopupVisible</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool IsRecalculateConfirmPopupVisible { get; set; }
    /// <summary>Giá trị <c>IsLockActionPopupVisible</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool IsLockActionPopupVisible { get; set; }
    /// <summary>Điều khiển trạng thái hiển thị cửa sổ điều chỉnh phụ cấp cơm.</summary>
    private bool IsEditPopupVisible { get; set; }
    /// <summary>Cho biết thao tác lưu điều chỉnh phụ cấp cơm đang chạy.</summary>
    private bool IsSavingEdit { get; set; }
    /// <summary>Giá trị <c>IsRulesPopupVisible</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool IsRulesPopupVisible { get; set; }
    /// <summary>Giá trị <c>IsMonthlyWorkPopupVisible</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool IsMonthlyWorkPopupVisible { get; set; }
    /// <summary>Giá trị <c>IsMonthlyWorkPopupLoading</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool IsMonthlyWorkPopupLoading { get; set; }
    /// <summary>Giá trị <c>HasRequestedData</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool HasRequestedData { get; set; }
    /// <summary>Giá trị <c>PendingLockActionState</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool PendingLockActionState { get; set; } = true;
    /// <summary>Giá trị <c>PendingLockActionMonth</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int PendingLockActionMonth { get; set; }
    /// <summary>Giá trị <c>PendingLockActionYear</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int PendingLockActionYear { get; set; }
    /// <summary>Giá trị <c>SelectedLockActionScope</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;
    /// <summary>Mô hình dữ liệu được liên kết với biểu mẫu điều chỉnh phụ cấp cơm.</summary>
    private PhuCapComEditModel EditModel { get; set; } = new();
    /// <summary>Tiêu đề hiển thị của cửa sổ điều chỉnh phụ cấp cơm.</summary>
    private string EditPopupTitle { get; set; } = "Điều chỉnh phụ cấp cơm";
    /// <summary>Thông báo validation của biểu mẫu điều chỉnh thủ công.</summary>
    private string? EditValidationMessage { get; set; }
    /// <summary>Giá trị <c>MonthlyWorkPopupErrorMessage</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string? MonthlyWorkPopupErrorMessage { get; set; }
    /// <summary>Giá trị <c>MonthlyWorkPopupTitle</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string MonthlyWorkPopupTitle { get; set; } = "Bảng công tháng";
    /// <summary>Giá trị <c>MonthlyWorkPopupContext</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    /// <summary>Giá trị <c>MonthlyWorkPopupRecord</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private MealAllowanceRecord? MonthlyWorkPopupRecord { get; set; }
    /// <summary>Giá trị <c>MonthlyWorkRows</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    // Reload revisions are owned by ReloadState so concurrent UI events cannot
    // overwrite a newer filter result with a stale response.
    #endregion

    #region Trạng thái suy diễn và quyền thao tác

    /// <summary>Giá trị <c>HasLoadError</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    /// <summary>Giá trị <c>HasAppliedPeriod</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool HasAppliedPeriod => AppliedMonth.HasValue && AppliedYear.HasValue;
    /// <summary>Giá trị <c>HasPendingPeriodChange</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool HasPendingPeriodChange =>
        HasRequestedData
        && HasAppliedPeriod
        && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);
    /// <summary>Giá trị <c>ShowLoadingPanel</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool ShowLoadingPanel =>
        IsLoading
        || IsRefreshing
        || IsChangingPageSize
        || IsExporting
        || IsSavingEdit;
    /// <summary>Giá trị <c>CanInteract</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    /// <summary>Giá trị <c>CanReload</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanReload => !ShowLoadingPanel;
    /// <summary>Giá trị <c>CanChangeFilters</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanChangeFilters => !ShowLoadingPanel;
    /// <summary>Giá trị <c>CanOperateOnCurrentDataset</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanOperateOnCurrentDataset => CanInteract && HasRequestedData && HasAppliedPeriod && !HasPendingPeriodChange;
    /// <summary>Giá trị <c>CanRefreshSnapshot</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanRefreshSnapshot => CanOperateOnCurrentDataset;
    /// <summary>Giá trị <c>CanRecalculate</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanRecalculate => CanRefreshSnapshot;
    /// <summary>Giá trị <c>CanOpenLockAction</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanOpenLockAction => CanOperateOnCurrentDataset;
    /// <summary>Giá trị <c>CanOpenUnlockAction</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanOpenUnlockAction => CanOperateOnCurrentDataset;
    /// <summary>Giá trị <c>SelectedRecordCount</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int SelectedRecordCount => GetSelectedRecords().Count;
    /// <summary>Giá trị <c>CanChooseSelectedRowsScope</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanChooseSelectedRowsScope => SelectedRecordCount > 0;
    /// <summary>Giá trị <c>CanConfirmLockAction</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanConfirmLockAction =>
        CanOperateOnCurrentDataset
        && (IsWholePeriodLockActionScope(SelectedLockActionScope) || CanChooseSelectedRowsScope);
    /// <summary>Cho biết có thể xuất dữ liệu của kỳ lương đang áp dụng.</summary>
    private bool CanExport => CanOperateOnCurrentDataset;
    /// <summary>Cho biết dòng có được phép điều chỉnh thủ công hay không.</summary>
    private bool CanEditRow(MealAllowanceRecord record) =>
        CanOperateOnCurrentDataset && !record.IsLocked;
    /// <summary>Cho biết biểu mẫu điều chỉnh đủ điều kiện để lưu.</summary>
    private bool CanSaveEdit =>
        !IsSavingEdit
        && !HasPendingPeriodChange
        && EditModel.Id != Guid.Empty
        && !EditModel.IsLocked
        && EditModel.QualifiedMealDays >= 0;
    /// <summary>Kiểm tra điều kiện cho luồng <c>CanRefreshRow</c>.</summary>
    private bool CanRefreshRow(MealAllowanceRecord record) =>
        CanOperateOnCurrentDataset
        && !record.IsLocked;
    /// <summary>Kiểm tra điều kiện cho luồng <c>CanToggleLock</c>.</summary>
    private bool CanToggleLock(MealAllowanceRecord _) => CanOperateOnCurrentDataset;
    /// <summary>Kiểm tra điều kiện cho luồng <c>CanViewMonthlyWork</c>.</summary>
    private bool CanViewMonthlyWork(MealAllowanceRecord record) =>
        CanOperateOnCurrentDataset
        && record.EmployeeId is { } employeeId
        && employeeId != Guid.Empty;
    /// <summary>Giá trị <c>CanOpenRules</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanOpenRules => !ShowLoadingPanel;
    /// <summary>Giá trị <c>CanBrowsePages</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanBrowsePages => CanInteract && HasRequestedData && totalRecordCount > 0 && !HasPendingPeriodChange;
    /// <summary>Giá trị <c>CanResetFilters</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private bool CanResetFilters =>
        ToolbarMonth != DefaultReferenceMonth
        || ToolbarYear != DefaultReferenceYear
        || !string.IsNullOrWhiteSpace(SearchText);
    /// <summary>Giá trị <c>AvailableMonthOptions</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private IReadOnlyList<MonthOption> AvailableMonthOptions => BuildMonthOptions(ToolbarYear);
    /// <summary>Giá trị <c>VisibleRecords</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private IReadOnlyList<MealAllowanceRecord> VisibleRecords => Records;
    /// <summary>Giá trị <c>PageSize</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int PageSize => pageSize;
    /// <summary>Danh sách kích thước trang khả dụng theo tổng số dòng.</summary>
    private IReadOnlyList<PageSizeOption> AvailablePageSizeOptions => totalRecordCount > AllPageSize
        ? PageSizeOptions.Where(option => option.Value != AllPageSize).ToArray()
        : PageSizeOptions;
    /// <summary>Cho biết lưới đang hiển thị toàn bộ dữ liệu trong kỳ lương.</summary>
    private bool IsShowingAllRows => PageSize == AllPageSize;
    /// <summary>Mô tả đơn vị của kích thước trang hiện tại.</summary>
    private string PageSizeDescription => IsShowingAllRows ? "tất cả dòng" : "dòng/trang";
    /// <summary>Giá trị <c>CurrentPageIndex</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int CurrentPageIndex => currentPageIndex;
    /// <summary>Giá trị <c>TotalPageCount</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int TotalPageCount => totalRecordCount <= 0 ? 1 : (int)Math.Ceiling(totalRecordCount / (double)PageSize);
    /// <summary>Giá trị <c>CurrentPageStartRecord</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int CurrentPageStartRecord => totalRecordCount == 0 ? 0 : CurrentPageIndex * PageSize + 1;
    /// <summary>Giá trị <c>CurrentPageEndRecord</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private int CurrentPageEndRecord => totalRecordCount == 0 ? 0 : Math.Min(totalRecordCount, CurrentPageIndex * PageSize + Records.Count);
    /// <summary>Giá trị <c>PagerSummaryText</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string PagerSummaryText => !HasRequestedData || HasLoadError || totalRecordCount == 0
        ? "Chưa có dữ liệu để hiển thị"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {totalRecordCount:N0} dòng";
    /// <summary>Giá trị <c>ToolbarPayrollPeriodDisplay</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string ToolbarPayrollPeriodDisplay => $"{ToolbarMonth:00}/{ToolbarYear}";
    /// <summary>Giá trị <c>AppliedPayrollPeriodDisplay</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string AppliedPayrollPeriodDisplay => HasAppliedPeriod
        ? FormatPayrollPeriod(AppliedMonth!.Value, AppliedYear!.Value)
        : ToolbarPayrollPeriodDisplay;
    /// <summary>Giá trị <c>PendingLockActionPeriodLabel</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string PendingLockActionPeriodLabel => FormatPayrollPeriod(PendingLockActionMonth, PendingLockActionYear);
    /// <summary>Giá trị <c>LockActionPopupTitle</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string LockActionPopupTitle => PendingLockActionState
        ? "Khóa dữ liệu phụ cấp cơm"
        : "Mở khóa dữ liệu phụ cấp cơm";
    /// <summary>Giá trị <c>LockActionPromptText</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa dữ liệu phụ cấp cơm."
        : "Chọn phạm vi cần mở khóa dữ liệu phụ cấp cơm.";
    /// <summary>Giá trị <c>LockActionScopeContextText</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string LockActionScopeContextText =>
        $"Kỳ lương áp dụng: {PendingLockActionPeriodLabel}. Lựa chọn toàn bộ kỳ sẽ bỏ qua các bộ lọc và nhóm dữ liệu đang hiển thị.";
    /// <summary>Giá trị <c>SelectedRowsScopeDescription</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {SelectedRecordCount:N0} dòng đang được chọn trong lưới."
        : "Chưa có dòng nào được chọn trong lưới hiện tại.";
    /// <summary>Giá trị <c>WholePeriodScopeDescription</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string WholePeriodScopeDescription => PendingLockActionState
        ? $"Áp dụng cho toàn bộ dữ liệu phụ cấp cơm của kỳ {PendingLockActionPeriodLabel}."
        : $"Mở khóa toàn bộ dữ liệu phụ cấp cơm của kỳ {PendingLockActionPeriodLabel}.";
    /// <summary>Giá trị <c>EmptyStateTitle</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string EmptyStateTitle => !HasRequestedData
        ? "Chưa tải dữ liệu phụ cấp cơm"
        : !string.IsNullOrWhiteSpace(SearchText)
        ? "Không tìm thấy kết quả phù hợp"
        : "Không có dữ liệu phụ cấp cơm";
    /// <summary>Giá trị <c>EmptyStateMessage</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string EmptyStateMessage => !HasRequestedData
        ? "Chọn tháng và năm kỳ lương, sau đó nhấn Xem để tải dữ liệu."
        : !string.IsNullOrWhiteSpace(SearchText)
        ? "Hãy thử từ khóa khác hoặc xóa tìm kiếm để xem dữ liệu."
        : $"Chưa có dữ liệu phụ cấp cơm cho kỳ {AppliedPayrollPeriodDisplay}.";
    /// <summary>Giá trị <c>EmptyStateActionText</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string EmptyStateActionText => !HasRequestedData
        ? "Xem dữ liệu"
        : CanResetFilters
            ? "Đặt lại bộ lọc"
            : "Tải lại";

    /// <summary>Giá trị <c>EmptyStateActionIcon</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private string EmptyStateActionIcon => !HasRequestedData
        ? VntaDevExpressIcons.Search
        : CanResetFilters
            ? VntaDevExpressIcons.Reset
            : VntaDevExpressIcons.Refresh;

    #endregion

    #region Điều phối vòng đời render

    /// <summary>Xử lý sự kiện cho luồng <c>OnAfterRenderAsync</c>.</summary>
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        return base.OnAfterRenderAsync(firstRender);
    }

    #endregion


    #region Giải phóng tài nguyên

    /// <summary>Giải phóng tài nguyên cho luồng <c>Dispose</c>.</summary>
    public void Dispose()
    {
        CancelActiveReload();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    /// <summary>Giá trị <c>EmptySummary</c> được sử dụng bởi màn hình phụ cấp cơm.</summary>
    private static MealAllowanceSummaryDto EmptySummary { get; } = new(0, 0, 0, 0, 0, 0, 0, 0m);

    #endregion
}
