using DevExpress.Blazor;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Export;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemKhac;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

/// <summary>Coordinates screen workflows; sections only consume immutable view states.</summary>
public sealed partial class OtherResponsibilityAllowanceCoordinator(
    IOtherResponsibilityAllowanceDataProvider dataProvider,
    IHrmToastService toastService,
    OtherResponsibilityAllowanceGridExporter gridExporter) : IDisposable
{
    private const int MinimumSupportedYear = 2026;
    private const int MinimumSupportedMonth = 6;
    private const int MaximumSupportedYear = 2100;
    private const int DefaultPageSize = 50;
    private const string LockScopeSelectedRows = "selected-rows";
    private const string LockScopeWholePeriod = "whole-period";

    private static readonly (int Month, int Year) DefaultPayrollPeriod = GetDefaultPayrollPeriod();
    private static readonly IReadOnlyList<OtherResponsibilityAllowanceMonthOption> AllMonthOptions =
        Enumerable.Range(1, 12).Select(month => new OtherResponsibilityAllowanceMonthOption(month, $"Tháng {month:00}")).ToArray();

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly OtherResponsibilityAllowanceReloadState reloadState = new();
    private readonly SemaphoreSlim screenActionGate = new(1, 1);
    private Func<Task>? requestRenderAsync;

    private IOtherResponsibilityAllowanceDataProvider DataProvider { get; } = dataProvider;
    private IHrmToastService ToastService { get; } = toastService;
    private OtherResponsibilityAllowanceGridExporter GridExporter { get; } = gridExporter;

    private IGrid? AllowanceGrid { get; set; }
    private IReadOnlyList<OtherResponsibilityAllowanceRecord> AllRecords { get; set; } = [];
    private IReadOnlyList<OtherResponsibilityAllowanceRecord> VisibleRecords { get; set; } = [];
    private IReadOnlyList<object> SelectedGridItems { get; set; } = [];
    private string? SearchText { get; set; }
    private string? DataLoadErrorMessage { get; set; }
    private string CurrentLoadingText { get; set; } = HrmUiDefaults.LoadingText;
    private int ToolbarMonth { get; set; } = DefaultPayrollPeriod.Month;
    private int ToolbarYear { get; set; } = DefaultPayrollPeriod.Year;
    private int AppliedMonth { get; set; } = DefaultPayrollPeriod.Month;
    private int AppliedYear { get; set; } = DefaultPayrollPeriod.Year;
    private int PageSize { get; set; } = DefaultPageSize;
    private bool IsLoading { get; set; }
    private bool IsRunningScreenAction { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool IsRulesPopupVisible { get; set; }
    private bool IsRecalculateConfirmPopupVisible { get; set; }
    private bool IsLockActionPopupVisible { get; set; }
    private bool PendingLockActionState { get; set; } = true;
    private int PendingLockActionMonth { get; set; } = DefaultPayrollPeriod.Month;
    private int PendingLockActionYear { get; set; } = DefaultPayrollPeriod.Year;
    private string SelectedLockActionScope { get; set; } = LockScopeSelectedRows;
    private bool HasRequestedData { get; set; }

    private bool IsDisposalRequested => disposalTokenSource.IsCancellationRequested;
    private bool HasLoadError => !string.IsNullOrWhiteSpace(DataLoadErrorMessage);
    private bool HasActiveSearch => !string.IsNullOrWhiteSpace(SearchText);
    private bool HasPendingPeriodChange => ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear;
    private bool ShowLoadingPanel => IsLoading || IsChangingPageSize || IsRunningScreenAction;
    private bool CanInteract => !ShowLoadingPanel && !HasLoadError;
    private bool CanUseAppliedPeriodActions => CanInteract && HasRequestedData && !HasPendingPeriodChange;
    private bool CanView => !ShowLoadingPanel;
    private bool CanOpenActionsMenu => CanUseAppliedPeriodActions;
    private bool CanChangeFilters => !ShowLoadingPanel;
    private bool CanOpenRules => CanInteract;
    private bool CanSearchScreen => CanUseAppliedPeriodActions;
    private bool CanExport => CanUseAppliedPeriodActions && VisibleRecords.Count > 0;
    private bool CanExportSelected => CanExport && GetSelectedRecordCount() > 0;
    private bool CanEmptyStateAction => !ShowLoadingPanel;
    private bool CanChooseSelectedRowsScope => GetSelectedRecords().Count > 0;
    private bool CanConfirmLockAction => CanUseAppliedPeriodActions &&
        (SelectedLockActionScope == LockScopeWholePeriod || CanChooseSelectedRowsScope);
    private bool CanConfirmRecalculate => IsRecalculateConfirmPopupVisible && CanUseAppliedPeriodActions;
    private IReadOnlyList<OtherResponsibilityAllowanceMonthOption> AvailableMonthOptions =>
        AllMonthOptions.Where(option => option.Value >= GetMinimumSupportedMonth(ToolbarYear)).ToArray();
    private string LoadingText => CurrentLoadingText;
    private string AppliedPeriodLabel => FormatPayrollPeriod(AppliedMonth, AppliedYear);
    private string PendingLockActionPeriodLabel => FormatPayrollPeriod(PendingLockActionMonth, PendingLockActionYear);
    private string LockActionPopupTitle => PendingLockActionState ? "Khóa dữ liệu phụ cấp trách nhiệm khác" : "Mở khóa dữ liệu phụ cấp trách nhiệm khác";
    private string LockActionPromptText => PendingLockActionState ? "Chọn phạm vi cần khóa dữ liệu phụ cấp trách nhiệm khác." : "Chọn phạm vi cần mở khóa dữ liệu phụ cấp trách nhiệm khác.";
    private string LockActionContextText => $"Kỳ lương áp dụng: {PendingLockActionPeriodLabel}.";
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope ? $"Áp dụng cho {GetSelectedRecordCount():N0} dòng đang được chọn trong lưới." : "Chưa có dòng nào được chọn trong lưới hiện tại.";
    private string WholePeriodScopeDescription => $"Áp dụng cho toàn bộ dữ liệu phụ cấp trách nhiệm khác của kỳ {PendingLockActionPeriodLabel}.";
    private string PeriodHintCssClass => HasPendingPeriodChange ? "responsibility-period-hint responsibility-period-hint-pending" : "responsibility-period-hint";
    private string PeriodHintText => !HasRequestedData ? $"Chọn kỳ {FormatPayrollPeriod(ToolbarMonth, ToolbarYear)} và nhấn Xem để tải dữ liệu." : HasPendingPeriodChange ? $"Đang hiển thị kỳ {AppliedPeriodLabel}. Nhấn Xem để áp dụng kỳ {FormatPayrollPeriod(ToolbarMonth, ToolbarYear)}." : $"Dữ liệu phụ cấp trách nhiệm khác kỳ {AppliedPeriodLabel}.";
    private string EmptyStateTitle => !HasRequestedData ? "Chưa tải dữ liệu phụ cấp trách nhiệm khác" : HasActiveSearch ? "Không tìm thấy kết quả phù hợp" : "Không có dữ liệu phụ cấp trách nhiệm khác";
    private string EmptyStateMessage => !HasRequestedData ? "Chọn tháng và năm kỳ lương, sau đó nhấn Xem để tải dữ liệu." : HasActiveSearch ? "Hãy thử từ khóa khác hoặc xóa tìm kiếm để xem dữ liệu." : $"Chưa có dữ liệu phụ cấp trách nhiệm khác cho kỳ {AppliedPeriodLabel}.";
    private string EmptyStateActionText => !HasRequestedData ? "Xem dữ liệu" : HasActiveSearch ? "Xóa tìm kiếm" : "Tải lại";

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadState.Dispose();
        screenActionGate.Dispose();
    }

    private static int GetMinimumSupportedMonth(int year) => year == MinimumSupportedYear ? MinimumSupportedMonth : 1;
    private static string FormatPayrollPeriod(int payrollMonth, int payrollYear) => $"{payrollMonth:00}/{payrollYear}";
    private static string? NormalizeOptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        var year = Math.Clamp(localNow.Year, MinimumSupportedYear, MaximumSupportedYear);
        return (Math.Clamp(localNow.Month, GetMinimumSupportedMonth(year), 12), year);
    }
}
