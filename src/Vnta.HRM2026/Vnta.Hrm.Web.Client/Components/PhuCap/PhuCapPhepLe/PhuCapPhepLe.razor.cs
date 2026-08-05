using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

/// <summary>Đại diện kiểu <c>PhuCapPhepLe</c> phục vụ màn hình phụ cấp phép lễ.</summary>
public partial class PhuCapPhepLe : IDisposable
{
    #region Constants

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private const int MinimumSupportedMonth = 6;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private const int MinimumSupportedYear = 2026;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private const int MaximumSupportedYear = 2100;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private const int DefaultPageSize = 50;
    /// <summary>Các lựa chọn số dòng cho pager tùy biến.</summary>
    private static readonly int[] PageSizeOptions = [DefaultPageSize, 100, 200];
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private const string DefaultLoadingText = "Đang tải dữ liệu phụ cấp Phép - Lễ...";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private const string DefaultManualEditPopupTitle = "Điều chỉnh phụ cấp Phép - Lễ";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private const string MissingBasicSalaryReferenceNote = "Không tồn tại lương căn bản để tham chiếu.";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private const string LockScopeSelectedRows = "selected-rows";
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private const string LockScopeWholePeriod = "whole-period";

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    /// <summary>Thực hiện xử lý cho luồng <c>readonly</c>.</summary>
    private static readonly (int Month, int Year) DefaultPayrollPeriod = GetDefaultPayrollPeriod();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private static readonly IReadOnlyList<LeaveHolidayAllowanceMonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new LeaveHolidayAllowanceMonthOption(month, $"Tháng {month:00}"))
            .ToArray();

    #endregion

    #region Dependencies

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private readonly CancellationTokenSource disposalTokenSource = new();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    // Interactive Server permits overlapping UI callbacks. Serialize provider calls so a late
    // row/popup command cannot race the screen's reload snapshot.
    private readonly SemaphoreSlim dataOperationGate = new(1, 1);
    private readonly PhuCapPhepLeFilterState filterState = new();
    private readonly PhuCapPhepLeSelectionState selectionState = new();

    [Inject]
    /// <summary>Giá trị <c>DataProvider</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private ILeaveHolidayAllowanceDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IPhuCapPhepLeFilterFactory FilterFactory { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>DialogService</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private IHrmDialogService DialogService { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>ToastService</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>Logger</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private ILogger<PhuCapPhepLe> Logger { get; set; } = default!;

    #endregion

    #region Component References

    /// <summary>Giá trị <c>LeaveHolidayGrid</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private PhuCapPhepLeGrid? GridSection { get; set; }
    /// <summary>Lưới ẩn dùng để xuất toàn bộ tập dữ liệu sau lọc.</summary>
    private PhuCapPhepLeExportGrid? ExportGridSection { get; set; }

    #endregion

    #region Screen State

    /// <summary>Giá trị <c>AllRecords</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private IReadOnlyList<LeaveHolidayAllowanceRecord> AllRecords { get; set; } = [];
    /// <summary>Giá trị <c>VisibleRecords</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private IReadOnlyList<LeaveHolidayAllowanceRecord> VisibleRecords { get; set; } = [];
    /// <summary>Giá trị <c>SelectedGridItems</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private IReadOnlyList<object> SelectedGridItems
    {
        get => selectionState.Items;
        set => selectionState.Items = value;
    }

    /// <summary>Giá trị <c>SearchText</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string? SearchText
    {
        get => filterState.SearchText;
        set => filterState.SearchText = value;
    }
    /// <summary>Giá trị <c>LoadErrorMessage</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string? LoadErrorMessage { get; set; }
    /// <summary>Giá trị <c>ManualEditErrorMessage</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string? ManualEditErrorMessage { get; set; }
    /// <summary>Giá trị <c>LoadingPanelText</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string LoadingPanelText { get; set; } = DefaultLoadingText;

    /// <summary>Giá trị <c>ToolbarMonth</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int ToolbarMonth { get; set; } = DefaultPayrollPeriod.Month;
    /// <summary>Giá trị <c>ToolbarYear</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int ToolbarYear { get; set; } = DefaultPayrollPeriod.Year;
    /// <summary>Giá trị <c>AppliedMonth</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int AppliedMonth { get; set; } = DefaultPayrollPeriod.Month;
    /// <summary>Giá trị <c>AppliedYear</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int AppliedYear { get; set; } = DefaultPayrollPeriod.Year;
    /// <summary>Số dòng hiển thị trên mỗi trang.</summary>
    private int pageSize = DefaultPageSize;
    /// <summary>Chỉ số trang hiện tại, bắt đầu từ không.</summary>
    private int currentPageIndex;
    /// <summary>Giá trị <c>CurrentLockFilter</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private LeaveHolidayAllowanceLockFilter CurrentLockFilter { get; set; } = LeaveHolidayAllowanceLockFilter.All;

    /// <summary>Giá trị <c>HasRequestedData</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool HasRequestedData { get; set; }
    /// <summary>Giá trị <c>IsLoadingData</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool IsLoadingData { get; set; }
    /// <summary>Giá trị <c>IsProcessingScreenAction</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool IsProcessingScreenAction { get; set; }
    /// <summary>Giá trị <c>IsChangingPageSize</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool IsChangingPageSize { get; set; }
    /// <summary>Giá trị <c>IsRulesPopupVisible</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool IsRulesPopupVisible { get; set; }
    /// <summary>Giá trị <c>IsMonthlyWorkPopupVisible</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool IsMonthlyWorkPopupVisible { get; set; }
    /// <summary>Giá trị <c>IsMonthlyWorkPopupLoading</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool IsMonthlyWorkPopupLoading { get; set; }
    /// <summary>Giá trị <c>IsManualEditPopupVisible</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool IsManualEditPopupVisible { get; set; }
    /// <summary>Giá trị <c>IsSavingManualEdit</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool IsSavingManualEdit { get; set; }
    /// <summary>Giá trị <c>IsLockActionPopupVisible</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool IsLockActionPopupVisible { get; set; }
    /// <summary>Giá trị <c>PendingLockActionState</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool PendingLockActionState { get; set; }

    /// <summary>Giá trị <c>PendingLockActionMonth</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int PendingLockActionMonth { get; set; }
    /// <summary>Giá trị <c>PendingLockActionYear</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int PendingLockActionYear { get; set; }
    /// <summary>Giá trị <c>SelectedLockActionScope</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;

    /// <summary>Giá trị <c>ManualEditModel</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private LeaveHolidayManualEditModel? ManualEditModel { get; set; }
    /// <summary>Giá trị <c>ManualEditFormContext</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private EditContext? ManualEditFormContext { get; set; }
    /// <summary>Giá trị <c>ManualEditPopupTitle</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string ManualEditPopupTitle { get; set; } = DefaultManualEditPopupTitle;
    /// <summary>Giá trị <c>MonthlyWorkPopupErrorMessage</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string? MonthlyWorkPopupErrorMessage { get; set; }
    /// <summary>Giá trị <c>MonthlyWorkPopupTitle</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string MonthlyWorkPopupTitle { get; set; } = "Đối chiếu bảng công chi tiết";
    /// <summary>Giá trị <c>MonthlyWorkPopupContext</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string MonthlyWorkPopupContext { get; set; } = string.Empty;
    /// <summary>Giá trị <c>MonthlyWorkRows</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private IReadOnlyList<MonthlyWorkdayPopupRow> MonthlyWorkRows { get; set; } = [];
    /// <summary>Giá trị <c>MonthlyWorkPopupRecord</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private LeaveHolidayAllowanceRecord? MonthlyWorkPopupRecord { get; set; }

    /// <summary>Giá trị <c>reloadRequestedVersion</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int reloadRequestedVersion;
    /// <summary>Giá trị <c>reloadProcessedVersion</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int reloadProcessedVersion;

    #endregion

    #region Derived State

    /// <summary>Giá trị <c>AvailableMonthOptions</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private IReadOnlyList<LeaveHolidayAllowanceMonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? MonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : MonthOptions;

    /// <summary>Giá trị <c>HasLoadError</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    /// <summary>Giá trị <c>HasActiveSearch</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool HasActiveSearch => !string.IsNullOrWhiteSpace(SearchText);
    /// <summary>Giá trị <c>HasActiveRefinement</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool HasActiveRefinement => CurrentLockFilter != LeaveHolidayAllowanceLockFilter.All || HasActiveSearch;
    /// <summary>Giá trị <c>HasPendingPeriodChange</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool HasPendingPeriodChange =>
        HasRequestedData
        && (ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear);
    /// <summary>Giá trị <c>ShowLoadingPanel</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool ShowLoadingPanel => IsLoadingData || IsProcessingScreenAction || IsChangingPageSize;
    /// <summary>Giá trị <c>CanInteract</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    /// <summary>Giá trị <c>CanOperateOnCurrentDataset</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanOperateOnCurrentDataset => CanInteract && HasRequestedData && !HasPendingPeriodChange;
    /// <summary>Giá trị <c>CanView</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanView => !ShowLoadingPanel;
    /// <summary>Giá trị <c>CanChangeFilters</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanChangeFilters => !ShowLoadingPanel;
    /// <summary>Giá trị <c>CanOpenColumnChooser</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanOpenColumnChooser => GridSection is not null && CanOperateOnCurrentDataset;
    /// <summary>Giá trị <c>CanExport</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanExport => CanOperateOnCurrentDataset && VisibleRecords.Count > 0;
    /// <summary>Giá trị <c>CanExportSelected</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanExportSelected => CanExport && SelectedVisibleRecordCount > 0;
    /// <summary>Giá trị <c>CanOpenLockAction</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanOpenLockAction => CanOperateOnCurrentDataset;
    /// <summary>Giá trị <c>CanOpenUnlockAction</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanOpenUnlockAction => CanOperateOnCurrentDataset;
    /// <summary>Giá trị <c>CanRecalculate</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanRecalculate => CanOperateOnCurrentDataset;
    /// <summary>Giá trị <c>CanSearchScreen</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanSearchScreen => CanChangeFilters;
    /// <summary>Các dòng của trang hiện tại sau khi áp dụng bộ lọc khóa.</summary>
    private IReadOnlyList<LeaveHolidayAllowanceRecord> PagedRecords =>
        VisibleRecords
            .Skip(CurrentPageIndex * PageSize)
            .Take(PageSize)
            .ToArray();
    private int PageSize => pageSize;
    private int CurrentPageIndex => currentPageIndex;
    private int TotalRecordCount => VisibleRecords.Count;
    private int TotalPageCount => TotalRecordCount <= 0
        ? 1
        : (int)Math.Ceiling(TotalRecordCount / (double)PageSize);
    private int CurrentPageStartRecord => TotalRecordCount == 0
        ? 0
        : CurrentPageIndex * PageSize + 1;
    private int CurrentPageEndRecord => TotalRecordCount == 0
        ? 0
        : Math.Min(TotalRecordCount, CurrentPageIndex * PageSize + PagedRecords.Count);
    private bool CanBrowsePages => CanOperateOnCurrentDataset && TotalRecordCount > 0;
    private string PagerSummaryText => !HasRequestedData || HasLoadError || TotalRecordCount == 0
        ? "Chưa có trang dữ liệu"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalRecordCount:N0} dòng";
    /// <summary>Giá trị <c>CanSaveManualEdit</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanSaveManualEdit =>
        CanOperateOnCurrentDataset
        && !IsSavingManualEdit
        && ManualEditModel is not null
        && ManualEditFormContext is not null;

    /// <summary>Giá trị <c>AllRecordCount</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int AllRecordCount => AllRecords.Count;
    /// <summary>Giá trị <c>OpenRecordCount</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int OpenRecordCount => AllRecords.Count(record => !record.IsLocked);
    /// <summary>Giá trị <c>LockedRecordCount</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int LockedRecordCount => AllRecords.Count(record => record.IsLocked);
    /// <summary>Giá trị <c>SelectedVisibleRecordCount</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private int SelectedVisibleRecordCount => GetSelectedVisibleRecords().Count;
    /// <summary>Giá trị <c>TotalLeaveHolidayAllowanceAmount</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private decimal TotalLeaveHolidayAllowanceAmount => PagedRecords.Sum(record => record.LeaveHolidayAllowanceAmount);
    /// <summary>Giá trị <c>ManualEditSaveButtonText</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string ManualEditSaveButtonText => IsSavingManualEdit ? "Đang lưu..." : "Lưu";

    /// <summary>Giá trị <c>CurrentPayrollPeriodDisplay</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string CurrentPayrollPeriodDisplay => FormatPayrollPeriod(AppliedMonth, AppliedYear);
    /// <summary>Giá trị <c>RequestedPayrollPeriodDisplay</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string RequestedPayrollPeriodDisplay => FormatPayrollPeriod(ToolbarMonth, ToolbarYear);
    /// <summary>Giá trị <c>PendingLockActionPeriodDisplay</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string PendingLockActionPeriodDisplay => FormatPayrollPeriod(PendingLockActionMonth, PendingLockActionYear);
    /// <summary>Giá trị <c>CanChooseSelectedRowsLockScope</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanChooseSelectedRowsLockScope => GetSelectedVisibleRecords().Count > 0;
    /// <summary>Giá trị <c>IsWholePeriodLockScope</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool IsWholePeriodLockScope => string.Equals(SelectedLockActionScope, LockScopeWholePeriod, StringComparison.Ordinal);
    /// <summary>Giá trị <c>CanConfirmLockAction</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private bool CanConfirmLockAction =>
        CanOperateOnCurrentDataset
        && !IsProcessingScreenAction
        && (IsWholePeriodLockScope || CanChooseSelectedRowsLockScope);
    /// <summary>Giá trị <c>LockActionPopupTitle</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string LockActionPopupTitle => PendingLockActionState ? "Khóa dữ liệu" : "Mở khóa dữ liệu";
    /// <summary>Giá trị <c>LockActionPopupPrompt</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string LockActionPopupPrompt => PendingLockActionState
        ? "Chọn phạm vi khóa dữ liệu Phép - Lễ. Dòng đã khóa sẽ không thể nhập tay và sẽ được bỏ qua khi tính lại hoặc đồng bộ."
        : "Chọn phạm vi mở khóa dữ liệu Phép - Lễ. Các dòng được mở khóa sẽ có thể nhập tay trở lại.";
    /// <summary>Giá trị <c>LockActionPopupContext</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string LockActionPopupContext => $"Thao tác áp dụng cho kỳ lương đang hiển thị: {PendingLockActionPeriodDisplay}.";
    /// <summary>Giá trị <c>SelectedLockActionScopeDescription</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string SelectedLockActionScopeDescription => CanChooseSelectedRowsLockScope
        ? $"Áp dụng cho {SelectedVisibleRecordCount:N0} dòng đang chọn và còn hiển thị trong lưới."
        : "Chưa có dòng hợp lệ đang chọn trong dữ liệu hiển thị.";
    /// <summary>Giá trị <c>WholePeriodLockActionScopeDescription</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string WholePeriodLockActionScopeDescription =>
        $"Áp dụng cho toàn bộ {AllRecordCount:N0} dòng của kỳ {PendingLockActionPeriodDisplay}.";

    /// <summary>Giá trị <c>EmptyStateTitle</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string EmptyStateTitle => !HasRequestedData
        ? "Sẵn sàng tải dữ liệu phụ cấp Phép - Lễ"
        : HasPendingPeriodChange
            ? $"Kỳ {RequestedPayrollPeriodDisplay} chưa được tải"
            : HasActiveRefinement
            ? "Không tìm thấy dòng phụ cấp Phép - Lễ phù hợp"
            : "Chưa có dữ liệu phụ cấp Phép - Lễ";

    /// <summary>Giá trị <c>EmptyStateMessage</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string EmptyStateMessage => !HasRequestedData
        ? "Chọn tháng, năm kỳ lương rồi nhấn Xem để chuẩn bị và tải dữ liệu khi bạn sẵn sàng."
        : HasPendingPeriodChange
            ? $"Bạn đã đổi kỳ sang {RequestedPayrollPeriodDisplay}. Nhấn Xem để áp dụng kỳ mới cho lưới dữ liệu."
            : HasActiveRefinement
            ? "Hãy nới điều kiện tìm kiếm hoặc đặt lại bộ lọc để xem thêm dữ liệu."
            : "Dữ liệu Phép - Lễ sẽ được chuẩn bị từ snapshot tổng hợp phụ cấp của kỳ lương đang chọn.";

    /// <summary>Giá trị <c>EmptyStateActionText</c> được sử dụng bởi màn hình phụ cấp phép lễ.</summary>
    private string EmptyStateActionText => !HasRequestedData
        ? "Xem dữ liệu"
        : HasPendingPeriodChange
            ? "Xem kỳ đã chọn"
            : HasActiveRefinement
            ? "Đặt lại bộ lọc"
            : "Tải lại";

    #endregion

    #region IDisposable

    /// <summary>Giải phóng tài nguyên cho luồng <c>Dispose</c>.</summary>
    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        // A provider call may still be unwinding its finally block after cancellation.
        // Do not dispose either semaphore here; doing so would turn cancellation into an
        // ObjectDisposedException while it releases the gate. Both are circuit-local and
        // become collectible with this component.
    }

    #endregion
}
