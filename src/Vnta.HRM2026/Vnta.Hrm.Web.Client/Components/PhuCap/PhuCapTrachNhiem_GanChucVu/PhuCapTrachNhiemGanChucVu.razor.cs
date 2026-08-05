using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Sockets;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem_GanChucVu;

/// <summary>Đại diện kiểu <c>PhuCapTrachNhiemGanChucVu</c> phục vụ màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
public partial class PhuCapTrachNhiemGanChucVu : IDisposable
{
    #region Constants

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private static readonly ResponsibilityAllowancePeriodKey MinimumSupportedPeriod = new(2026, 6);
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private static readonly IReadOnlyList<MonthOption> MonthOptions =
        Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, $"Tháng {month:00}"))
            .ToArray();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private static readonly IReadOnlyList<PageSizeOption> PageSizeOptions =
    [
        new(10, "10"),
        new(20, "20"),
        new(50, "50"),
        new(100, "100")
    ];
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private const int MinimumSupportedYear = 2026;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private const int MaximumSupportedYear = 2100;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private const int DefaultPageSize = 20;

    #endregion

    #region Dependencies

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private readonly CancellationTokenSource disposalTokenSource = new();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    /// <summary>Giá trị <c>activeLoadTokenSource</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private CancellationTokenSource? activeLoadTokenSource;

    [Inject]
    /// <summary>Giá trị <c>PositionAssignmentDataProvider</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private ResponsibilityPositionAssignmentDataProvider PositionAssignmentDataProvider { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>PositionDataProvider</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private AttendancePositionDataProvider PositionDataProvider { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>ToastService</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>DialogService</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private IHrmDialogService DialogService { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>Logger</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private ILogger<PhuCapTrachNhiemGanChucVu> Logger { get; set; } = default!;

    #endregion

    #region State

    /// <summary>Giá trị <c>Grid</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private IGrid? Grid { get; set; }
    /// <summary>Giá trị <c>ExportGrid</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private IGrid? ExportGrid { get; set; }
    /// <summary>Giá trị <c>exportGridRenderCompletionSource</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private TaskCompletionSource<bool>? exportGridRenderCompletionSource;
    /// <summary>Giá trị <c>EditorEditContext</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private EditContext? EditorEditContext { get; set; }

    /// <summary>Giá trị <c>LoadedGradeRows</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> LoadedGradeRows { get; set; } = [];
    /// <summary>Giá trị <c>LoadedMappingRows</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private IReadOnlyList<PayrollResponsibilityAllowanceGradePositionDto> LoadedMappingRows { get; set; } = [];
    /// <summary>Giá trị <c>ExportRows</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private IReadOnlyList<ResponsibilityPositionAssignmentExportRow> ExportRows { get; set; } = [];
    /// <summary>Giá trị <c>PositionRows</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private IReadOnlyList<AttendancePositionRecord> PositionRows { get; set; } = [];
    /// <summary>Giá trị <c>SelectedGridItems</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private IReadOnlyList<object> SelectedGridItems { get; set; } = [];

    /// <summary>Giá trị <c>EditorModel</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private PhuCapTrachNhiemGanChucVuEditModel EditorModel { get; set; } = PhuCapTrachNhiemGanChucVuEditModel.CreateDefault();
    /// <summary>Giá trị <c>EditingMappingId</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private Guid? EditingMappingId { get; set; }

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int toolbarMonth = MinimumSupportedPeriod.Month;
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int toolbarYear = MinimumSupportedPeriod.Year;
    /// <summary>Giá trị <c>LoadedMonth</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int LoadedMonth { get; set; } = MinimumSupportedPeriod.Month;
    /// <summary>Giá trị <c>LoadedYear</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int LoadedYear { get; set; } = MinimumSupportedPeriod.Year;
    /// <summary>Giá trị <c>PageSize</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int PageSize { get; set; } = DefaultPageSize;
    /// <summary>Giá trị <c>CurrentPageIndex</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int CurrentPageIndex { get; set; }
    /// <summary>Giá trị <c>TotalMappingCount</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int TotalMappingCount { get; set; }

    /// <summary>Giá trị <c>SearchQuery</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string? SearchQuery { get; set; }
    /// <summary>Giá trị <c>ScreenErrorMessage</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string? ScreenErrorMessage { get; set; }
    /// <summary>Giá trị <c>EditorErrorMessage</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string? EditorErrorMessage { get; set; }
    /// <summary>Giá trị <c>CommandLoadingText</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string CommandLoadingText { get; set; } = HrmUiDefaults.LoadingText;

    /// <summary>Giá trị <c>IsLoading</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool IsLoading { get; set; }
    /// <summary>Giá trị <c>IsExecutingCommand</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool IsExecutingCommand { get; set; }
    /// <summary>Giá trị <c>IsChangingPageSize</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool IsChangingPageSize { get; set; }
    /// <summary>Giá trị <c>HasRequestedData</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool HasRequestedData { get; set; }
    /// <summary>Giá trị <c>IsEditorVisible</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool IsEditorVisible { get; set; }
    /// <summary>Giá trị <c>IsEditorSaving</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool IsEditorSaving { get; set; }

    /// <summary>Giá trị <c>reloadRequestedVersion</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int reloadRequestedVersion;
    /// <summary>Giá trị <c>reloadProcessedVersion</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int reloadProcessedVersion;

    #endregion

    #region Derived State

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int ToolbarMonth
    {
        get => toolbarMonth;
        set => ApplyToolbarPeriod(NormalizeSelectedPeriod(value, toolbarYear));
    }

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int ToolbarYear
    {
        get => toolbarYear;
        set => ApplyToolbarPeriod(NormalizeSelectedPeriod(toolbarMonth, value));
    }

    /// <summary>Giá trị <c>AvailableMonthOptions</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private IReadOnlyList<MonthOption> AvailableMonthOptions =>
        ToolbarYear == MinimumSupportedPeriod.Year
            ? MonthOptions.Where(static option => option.Value >= MinimumSupportedPeriod.Month).ToArray()
            : MonthOptions;

    /// <summary>Giá trị <c>CurrentPeriodLabel</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string CurrentPeriodLabel => FormatPeriodLabel(ToolbarYear, ToolbarMonth);
    /// <summary>Giá trị <c>LoadedPeriodLabel</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string LoadedPeriodLabel => FormatPeriodLabel(LoadedYear, LoadedMonth);

    /// <summary>Giá trị <c>EditorPopupTitle</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string EditorPopupTitle => EditingMappingId.HasValue
        ? $"Sửa gán chức vụ - {LoadedPeriodLabel}"
        : $"Thêm gán chức vụ - {LoadedPeriodLabel}";

    /// <summary>Giá trị <c>EditorPrimaryActionText</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string EditorPrimaryActionText => EditingMappingId.HasValue
        ? "Lưu thay đổi"
        : "Tạo gán chức vụ";

    /// <summary>Giá trị <c>HasActiveSearch</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool HasActiveSearch => !string.IsNullOrWhiteSpace(SearchQuery);
    /// <summary>Giá trị <c>HasLoadError</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool HasLoadError => !string.IsNullOrWhiteSpace(ScreenErrorMessage);
    /// <summary>Giá trị <c>HasGradeOptions</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool HasGradeOptions => LoadedGradeRows.Count > 0;
    /// <summary>Giá trị <c>HasPendingPeriodChange</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool HasPendingPeriodChange =>
        HasRequestedData
        && (ToolbarMonth != LoadedMonth || ToolbarYear != LoadedYear);
    /// <summary>Giá trị <c>IsEditorOpen</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool IsEditorOpen => IsEditorVisible;
    /// <summary>Giá trị <c>ShowLoadingPanel</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool ShowLoadingPanel => IsLoading || IsExecutingCommand || IsChangingPageSize;

    /// <summary>Giá trị <c>CanUseLoadedDataActions</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanUseLoadedDataActions =>
        !ShowLoadingPanel
        && !HasLoadError
        && HasRequestedData
        && !HasPendingPeriodChange
        && !IsEditorOpen;

    /// <summary>Giá trị <c>CanReload</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanReload => !ShowLoadingPanel && !IsEditorOpen;
    /// <summary>Giá trị <c>CanView</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanView => CanReload;
    /// <summary>Giá trị <c>CanChangeFilters</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanChangeFilters => CanReload;
    /// <summary>Giá trị <c>CanExport</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanExport => CanUseLoadedDataActions && LoadedMappingRows.Count > 0;
    /// <summary>Giá trị <c>CanSearchScreen</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanSearchScreen => HasRequestedData && !HasLoadError && !HasPendingPeriodChange && !ShowLoadingPanel && !IsEditorOpen;
    /// <summary>Giá trị <c>CanCreateMapping</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanCreateMapping => CanUseLoadedDataActions && HasGradeOptions;
    /// <summary>Giá trị <c>CanSyncFromPreviousMonth</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanSyncFromPreviousMonth =>
        CanUseLoadedDataActions
        && HasGradeOptions
        && CanUsePreviousPeriod(LoadedYear, LoadedMonth);
    /// <summary>Giá trị <c>CanUseEmptyStateAction</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanUseEmptyStateAction => HasRequestedData && !HasLoadError && !ShowLoadingPanel && !IsEditorOpen;
    /// <summary>Giá trị <c>CanCloseEditorPopup</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanCloseEditorPopup => !IsEditorSaving;
    /// <summary>Giá trị <c>CanSaveEditor</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanSaveEditor => EditorEditContext is not null && !IsEditorSaving && !IsLoading && !IsChangingPageSize;

    /// <summary>Giá trị <c>LoadingText</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string LoadingText => IsChangingPageSize
        ? "Đang cập nhật số dòng hiển thị..."
        : IsExecutingCommand
            ? CommandLoadingText
            : "Đang tải dữ liệu gán chức vụ...";

    /// <summary>Giá trị <c>EditorLoadingText</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string EditorLoadingText => string.IsNullOrWhiteSpace(CommandLoadingText)
        ? HrmUiDefaults.LoadingText
        : CommandLoadingText;

    /// <summary>Giá trị <c>TotalPageCount</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int TotalPageCount => TotalMappingCount <= 0
        ? 1
        : (int)Math.Ceiling(TotalMappingCount / (double)PageSize);
    /// <summary>Giá trị <c>CanBrowsePages</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private bool CanBrowsePages => CanUseLoadedDataActions && TotalMappingCount > PageSize;
    /// <summary>Giá trị <c>CurrentPageStartRecord</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int CurrentPageStartRecord => TotalMappingCount == 0 ? 0 : CurrentPageIndex * PageSize + 1;
    /// <summary>Giá trị <c>CurrentPageEndRecord</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private int CurrentPageEndRecord => TotalMappingCount == 0
        ? 0
        : Math.Min(TotalMappingCount, CurrentPageIndex * PageSize + LoadedMappingRows.Count);
    /// <summary>Giá trị <c>PagerSummaryText</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string PagerSummaryText => TotalMappingCount == 0
        ? "Chưa có dữ liệu gán chức vụ"
        : $"Hiển thị {CurrentPageStartRecord:N0}-{CurrentPageEndRecord:N0} / {TotalMappingCount:N0} gán chức vụ";

    /// <summary>Giá trị <c>EmptyStateTitle</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string EmptyStateTitle => !HasRequestedData
        ? "Đang chuẩn bị dữ liệu gán chức vụ"
        : HasActiveSearch
            ? "Không tìm thấy gán chức vụ phù hợp"
            : !HasGradeOptions
                ? $"Kỳ {LoadedPeriodLabel} chưa có cấp bậc trách nhiệm"
                : $"Kỳ {LoadedPeriodLabel} chưa có dữ liệu gán chức vụ";

    /// <summary>Giá trị <c>EmptyStateMessage</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string EmptyStateMessage => !HasRequestedData
        ? "Màn hình đang tải dữ liệu mặc định cho cấu hình gán chức vụ."
        : HasActiveSearch
            ? "Hãy thử từ khóa khác hoặc xóa tìm kiếm để xem thêm dữ liệu."
            : !HasGradeOptions
                ? "Hãy cấu hình cấp bậc trách nhiệm trước, sau đó quay lại màn hình này để gán chức vụ."
                : "Bạn có thể tạo mới cấu hình gán chức vụ cho kỳ hiện tại.";

    /// <summary>Giá trị <c>EmptyStateActionText</c> được sử dụng bởi màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private string EmptyStateActionText => !HasRequestedData
        ? "Tải dữ liệu"
        : HasActiveSearch
            ? "Xóa tìm kiếm"
            : !HasGradeOptions
                ? "Làm mới"
                : "Thêm gán chức vụ";

    #endregion

    #region UI Entry Points

    /// <summary>Xử lý sự kiện cho luồng <c>OnInitializedAsync</c>.</summary>
    protected override async Task OnInitializedAsync()
    {
        var currentPeriod = NormalizeSelectedPeriod(DateTime.Today.Month, DateTime.Today.Year);
        ApplyToolbarPeriod(currentPeriod);
        LoadedMonth = currentPeriod.Month;
        LoadedYear = currentPeriod.Year;

        ResetEditorState();
        await LoadLoadedPeriodAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnAfterRenderAsync</c>.</summary>
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        exportGridRenderCompletionSource?.TrySetResult(true);
        return Task.CompletedTask;
    }

    #endregion

    #region Data Loading

    /// <summary>Thực hiện xử lý cho luồng <c>ReloadAsync</c>.</summary>
    private async Task ReloadAsync()
    {
        if (!CanReload || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        ApplyToolbarPeriod(normalizedPeriod);
        if (LoadedMonth != normalizedPeriod.Month || LoadedYear != normalizedPeriod.Year)
        {
            CurrentPageIndex = 0;
        }
        LoadedMonth = normalizedPeriod.Month;
        LoadedYear = normalizedPeriod.Year;

        await LoadLoadedPeriodAsync();
    }

    /// <summary>Tải cho luồng <c>LoadLoadedPeriodAsync</c>.</summary>
    private async Task LoadLoadedPeriodAsync()
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref reloadRequestedVersion);
        CancelActiveLoad();
        if (!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        try
        {
            while (!disposalTokenSource.IsCancellationRequested
                   && reloadProcessedVersion < Volatile.Read(ref reloadRequestedVersion))
            {
                var requestVersion = Volatile.Read(ref reloadRequestedVersion);
                reloadProcessedVersion = requestVersion;
                await LoadLoadedPeriodCoreAsync(requestVersion, CaptureLoadRequest());
            }
        }
        finally
        {
            reloadGate.Release();
        }
    }

    /// <summary>Tải cho luồng <c>LoadLoadedPeriodCoreAsync</c>.</summary>
    private async Task LoadLoadedPeriodCoreAsync(int requestVersion, ResponsibilityPositionAssignmentLoadRequest request)
    {
        HasRequestedData = true;
        ScreenErrorMessage = null;
        IsLoading = true;

        using var requestTokenSource = BeginLoad();
        try
        {
            await ClearSelectionAsync();

            var screenData = await PositionAssignmentDataProvider.LoadAsync(
                request.Year,
                request.Month,
                request.SearchText,
                request.PageIndex * request.PageSize,
                request.PageSize,
                requestTokenSource.Token);

            if (ShouldDiscardLoadResult(requestVersion, request))
            {
                return;
            }

            if (screenData.TotalCount > 0 && request.PageIndex >= (int)Math.Ceiling(screenData.TotalCount / (double)request.PageSize))
            {
                CurrentPageIndex = Math.Max(0, (int)Math.Ceiling(screenData.TotalCount / (double)request.PageSize) - 1);
                Interlocked.Increment(ref reloadRequestedVersion);
                return;
            }

            LoadedGradeRows = screenData.Grades
                .OrderBy(row => row.DisplayOrder)
                .ThenBy(row => row.Code)
                .ToArray();

            LoadedMappingRows = screenData.Mappings;
            TotalMappingCount = screenData.TotalCount;
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested || ShouldDiscardLoadResult(requestVersion, request))
        {
            // Request bị thay thế hoặc component đã dispose là luồng bình thường.
        }
        catch (Exception exception)
        {
            if (ShouldDiscardLoadResult(requestVersion, request))
            {
                return;
            }

            Logger.LogError(
                exception,
                "Không thể tải danh sách gán chức vụ. Kỳ={Month:00}/{Year}, Trang={PageIndex}, Kích thướcTrang={PageSize}.",
                request.Month,
                request.Year,
                request.PageIndex,
                request.PageSize);
            LoadedGradeRows = [];
            LoadedMappingRows = [];
            TotalMappingCount = 0;
            ScreenErrorMessage = IsDataSourceUnavailable(exception)
                ? "Dịch vụ dữ liệu phụ cấp trách nhiệm hiện chưa sẵn sàng. Vui lòng kiểm tra kết nối cơ sở dữ liệu rồi thử lại."
                : "Có lỗi khi tải danh sách gán chức vụ. Vui lòng thử lại.";
            ToastService.ShowError(ScreenErrorMessage);
        }
        finally
        {
            if (ReferenceEquals(activeLoadTokenSource, requestTokenSource))
            {
                activeLoadTokenSource = null;
            }
            IsLoading = false;
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>CaptureLoadRequest</c>.</summary>
    private ResponsibilityPositionAssignmentLoadRequest CaptureLoadRequest() =>
        new(LoadedYear, LoadedMonth, SearchQuery, CurrentPageIndex, PageSize);

    /// <summary>Thực hiện xử lý cho luồng <c>ShouldDiscardLoadResult</c>.</summary>
    private bool ShouldDiscardLoadResult(int requestVersion, ResponsibilityPositionAssignmentLoadRequest request) =>
        requestVersion != Volatile.Read(ref reloadRequestedVersion)
        || CaptureLoadRequest() != request;

    /// <summary>Thực hiện xử lý cho luồng <c>BeginLoad</c>.</summary>
    private CancellationTokenSource BeginLoad()
    {
        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        activeLoadTokenSource = tokenSource;
        return tokenSource;
    }

    /// <summary>Kiểm tra điều kiện cho luồng <c>CancelActiveLoad</c>.</summary>
    private void CancelActiveLoad() => activeLoadTokenSource?.Cancel();

    /// <summary>Kiểm tra trạng thái cho luồng <c>IsDataSourceUnavailable</c>.</summary>
    private static bool IsDataSourceUnavailable(Exception exception) =>
        exception.GetBaseException() is SocketException or TimeoutException;

    #endregion

    #region Toolbar And Screen Actions

    /// <summary>Xử lý sự kiện cho luồng <c>OnRetryRequestedAsync</c>.</summary>
    private async Task OnRetryRequestedAsync()
    {
        await ReloadAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedYearChangedAsync</c>.</summary>
    private Task OnSelectedYearChangedAsync(int year)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, year);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedMonthChangedAsync</c>.</summary>
    private Task OnSelectedMonthChangedAsync(int month)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(month, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnViewRequestedAsync</c>.</summary>
    private Task OnViewRequestedAsync() => ReloadAsync();

    /// <summary>Xử lý sự kiện cho luồng <c>OnReloadRequestedAsync</c>.</summary>
    private async Task OnReloadRequestedAsync()
    {
        await LoadLoadedPeriodAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnCopyMappingsFromPreviousMonthAsync</c>.</summary>
    private async Task OnCopyMappingsFromPreviousMonthAsync()
    {
        if (!CanSyncFromPreviousMonth)
        {
            return;
        }

        var previousPeriod = GetPreviousPeriod(LoadedYear, LoadedMonth);
        var confirmed = await DialogService.ConfirmAsync(
            $"Bạn có muốn lấy mapping chức vụ từ kỳ {previousPeriod.Month:00}/{previousPeriod.Year} sang kỳ {LoadedPeriodLabel} không?",
            title: "Lấy từ tháng trước",
            okText: "Đồng bộ",
            cancelText: "Hủy",
            renderStyle: MessageBoxRenderStyle.Primary);
        if (!confirmed)
        {
            return;
        }

        try
        {
            CopyResponsibilityPositionAssignmentsResult copyResult = default!;
            await ExecuteCommandAsync(
                $"Đang lấy mapping chức vụ từ kỳ {previousPeriod.Month:00}/{previousPeriod.Year}...",
                async () =>
                {
                    copyResult = await PositionAssignmentDataProvider.CopyFromPreviousPeriodAsync(
                        LoadedYear,
                        LoadedMonth,
                        disposalTokenSource.Token);
                    await LoadLoadedPeriodAsync();
                });

            if (HasLoadError)
            {
                return;
            }

            if (copyResult.SourceCount == 0)
            {
                ToastService.ShowWarning($"Kỳ {previousPeriod.Month:00}/{previousPeriod.Year} chưa có mapping chức vụ để lấy.");
                return;
            }

            var changedCount = copyResult.CreatedCount + copyResult.UpdatedCount;
            if (changedCount == 0)
            {
                ToastService.ShowWarning("Không có mapping chức vụ nào khớp với cấp bậc hiện tại.");
                return;
            }

            var summarySuffix = copyResult.SkippedMissingGradeCount > 0
                ? $" Bỏ qua {copyResult.SkippedMissingGradeCount} dòng không khớp cấp bậc."
                : string.Empty;
            ToastService.ShowSuccess(
                $"Đã đồng bộ {changedCount} mapping chức vụ từ tháng trước " +
                $"({copyResult.CreatedCount} thêm mới, {copyResult.UpdatedCount} cập nhật).{summarySuffix}");
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

    /// <summary>Xử lý sự kiện cho luồng <c>OnColumnChooserRequested</c>.</summary>
    private void OnColumnChooserRequested() => Grid?.ShowColumnChooser();

    /// <summary>Xử lý sự kiện cho luồng <c>OnSearchTextChangedAsync</c>.</summary>
    private async Task OnSearchTextChangedAsync(string? value)
    {
        var normalizedValue = NormalizeNullableText(value);
        if (string.Equals(SearchQuery, normalizedValue, StringComparison.Ordinal))
        {
            return;
        }

        SearchQuery = normalizedValue;
        CurrentPageIndex = 0;
        await LoadLoadedPeriodAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnPageSizeChangedAsync</c>.</summary>
    private async Task OnPageSizeChangedAsync(int value)
    {
        var normalizedValue = PageSizeOptions.Any(option => option.Value == value)
            ? value
            : DefaultPageSize;
        if (PageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;
        var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
        PageSize = normalizedValue;
        CurrentPageIndex = firstVisibleRecordIndex / PageSize;

        try
        {
            await LoadLoadedPeriodAsync();
        }
        finally
        {
            IsChangingPageSize = false;
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnEmptyStateActionRequestedAsync</c>.</summary>
    private async Task OnEmptyStateActionRequestedAsync()
    {
        if (!HasRequestedData || HasPendingPeriodChange || !HasGradeOptions)
        {
            await ReloadAsync();
            return;
        }

        if (HasActiveSearch)
        {
            SearchQuery = null;
            await LoadLoadedPeriodAsync();
            return;
        }

        await OnCreateRequestedAsync();
    }

    #endregion

    #region Selection And Grid Helpers

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedGridItemsChangedAsync</c>.</summary>
    private Task OnSelectedGridItemsChangedAsync(IReadOnlyList<object> items)
    {
        SelectedGridItems = items;
        return Task.CompletedTask;
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ClearSelectionAsync</c>.</summary>
    private async Task ClearSelectionAsync()
    {
        SelectedGridItems = [];

        if (Grid is null)
        {
            return;
        }

        try
        {
            await Grid.DeselectAllAsync();
            Grid.SetFocusedRowIndex(-1);
        }
        catch (ObjectDisposedException)
        {
            // Grid có thể đã bị dispose trong chu kỳ render sau một lần tải thất bại.
            // Đây không phải lỗi tải dữ liệu và không được che lấp lỗi gốc.
            Grid = null;
        }
    }

    #endregion

    #region Popup Editor

    /// <summary>Xử lý sự kiện cho luồng <c>OnCreateRequestedAsync</c>.</summary>
    private async Task OnCreateRequestedAsync()
    {
        if (!CanUseLoadedDataActions)
        {
            return;
        }

        if (!HasGradeOptions)
        {
            ToastService.ShowWarning("Hãy tạo ít nhất một cấp bậc trách nhiệm trước khi gán chức vụ.");
            return;
        }

        try
        {
            await EnsureLookupDataAsync();
            OpenCreateEditor();
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
            ToastService.ShowError("Không thể tải danh sách chức vụ.");
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnEditRequestedAsync</c>.</summary>
    private async Task OnEditRequestedAsync(PayrollResponsibilityAllowanceGradePositionDto mapping)
    {
        if (!CanEditLoadedMapping(mapping))
        {
            return;
        }

        try
        {
            await EnsureLookupDataAsync();
            OpenEditEditor(mapping);
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
            ToastService.ShowError("Không thể tải dữ liệu chức vụ để chỉnh sửa.");
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnDeleteRequestedAsync</c>.</summary>
    private async Task OnDeleteRequestedAsync(PayrollResponsibilityAllowanceGradePositionDto mapping)
    {
        if (!CanDeleteLoadedMapping(mapping))
        {
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            $"Bạn có chắc muốn ngừng dùng gán chức vụ {mapping.PositionCode} - {mapping.PositionName} trong kỳ {LoadedPeriodLabel} không?",
            title: "Xác nhận xóa",
            okText: "Xóa",
            cancelText: "Hủy",
            renderStyle: MessageBoxRenderStyle.Danger);
        if (!confirmed)
        {
            return;
        }

        try
        {
            await ExecuteCommandAsync(
                $"Đang ngừng dùng gán chức vụ {mapping.PositionCode}...",
                async () =>
                {
                    await PositionAssignmentDataProvider.DeactivateAsync(
                        new DeactivateResponsibilityPositionAssignmentRequest(
                            mapping.Id,
                            LoadedYear,
                            LoadedMonth,
                            mapping.UpdatedAtUtc),
                        disposalTokenSource.Token);
                    await LoadLoadedPeriodAsync();
                });

            if (!HasLoadError)
            {
                ToastService.ShowSuccess($"Đã ngừng dùng gán chức vụ {mapping.PositionCode}.");
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

    /// <summary>Xử lý sự kiện cho luồng <c>OnEditorVisibilityChangedAsync</c>.</summary>
    private Task OnEditorVisibilityChangedAsync(bool visible)
    {
        if (visible)
        {
            IsEditorVisible = true;
            return Task.CompletedTask;
        }

        if (!CanCloseEditorPopup)
        {
            return Task.CompletedTask;
        }

        CloseEditor();
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnCancelEditorRequestedAsync</c>.</summary>
    private Task OnCancelEditorRequestedAsync()
    {
        if (!CanCloseEditorPopup)
        {
            return Task.CompletedTask;
        }

        CloseEditor();
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSaveEditorRequestedAsync</c>.</summary>
    private async Task OnSaveEditorRequestedAsync()
    {
        if (EditorEditContext is null || IsEditorSaving)
        {
            return;
        }

        NormalizeEditorModel();
        if (!EditorEditContext.Validate())
        {
            return;
        }

        await SaveEditorChangesAsync();
    }

    /// <summary>Mở cho luồng <c>OpenCreateEditor</c>.</summary>
    private void OpenCreateEditor()
    {
        if (!CanCreateMapping)
        {
            return;
        }

        ResetEditorState();
        IsEditorVisible = true;
    }

    /// <summary>Mở cho luồng <c>OpenEditEditor</c>.</summary>
    private void OpenEditEditor(PayrollResponsibilityAllowanceGradePositionDto mapping)
    {
        if (!CanEditLoadedMapping(mapping))
        {
            return;
        }

        EditingMappingId = mapping.Id;
        EditorErrorMessage = null;
        EditorModel = PhuCapTrachNhiemGanChucVuEditModel.CreateFrom(mapping);
        EditorEditContext = new EditContext(EditorModel);
        IsEditorVisible = true;
    }

    /// <summary>Lưu cho luồng <c>SaveEditorChangesAsync</c>.</summary>
    private async Task SaveEditorChangesAsync()
    {
        if (EditorEditContext is null)
        {
            return;
        }

        EditorErrorMessage = null;
        IsEditorSaving = true;

        try
        {
            await ExecuteCommandAsync(
                EditingMappingId.HasValue
                    ? $"Đang lưu gán chức vụ kỳ {LoadedPeriodLabel}..."
                    : $"Đang thêm gán chức vụ kỳ {LoadedPeriodLabel}...",
                async () =>
                {
                    await PositionAssignmentDataProvider.SaveAsync(
                        BuildSaveMappingRequestFromEditor(),
                        disposalTokenSource.Token);

                    await LoadLoadedPeriodAsync();
                });

            if (!HasLoadError)
            {
                CloseEditor();
                ToastService.ShowSuccess("Đã lưu gán chức vụ.");
            }
            else
            {
                EditorErrorMessage = "Đã lưu gán chức vụ nhưng chưa thể tải lại danh sách. Hãy thử lại để đồng bộ giao diện.";
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
            EditorErrorMessage = ex.Message;
        }
        finally
        {
            IsEditorSaving = false;
        }
    }

    /// <summary>Đóng cho luồng <c>CloseEditor</c>.</summary>
    private void CloseEditor()
    {
        IsEditorVisible = false;
        ResetEditorState();
    }

    /// <summary>Đặt lại cho luồng <c>ResetEditorState</c>.</summary>
    private void ResetEditorState()
    {
        EditingMappingId = null;
        EditorErrorMessage = null;
        EditorModel = PhuCapTrachNhiemGanChucVuEditModel.CreateDefault();
        EditorEditContext = new EditContext(EditorModel);
    }

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeEditorModel</c>.</summary>
    private void NormalizeEditorModel()
    {
        EditorModel.Note = string.IsNullOrWhiteSpace(EditorModel.Note)
            ? string.Empty
            : EditorModel.Note.Trim();
    }

    /// <summary>Thực hiện xử lý cho luồng <c>EnsureLookupDataAsync</c>.</summary>
    private async Task EnsureLookupDataAsync()
    {
        if (PositionRows.Count > 0)
        {
            return;
        }

        PositionRows = (await PositionDataProvider.GetAsync(disposalTokenSource.Token))
            .OrderBy(row => row.Code)
            .ThenBy(row => row.Name)
            .ToArray();
    }

    #endregion

    #region Export

    /// <summary>Xử lý sự kiện cho luồng <c>OnExportAllExcelRequestedAsync</c>.</summary>
    private Task OnExportAllExcelRequestedAsync() => ExportCurrentPeriodAsync(
        () => ExportGrid!.ExportToXlsxAsync(BuildExportFileName()),
        "Excel");

    /// <summary>Xử lý sự kiện cho luồng <c>OnExportAllPdfRequestedAsync</c>.</summary>
    private Task OnExportAllPdfRequestedAsync() => ExportCurrentPeriodAsync(
        () => ExportGrid!.ExportToPdfAsync(BuildExportFileName()),
        "PDF");

    /// <summary>Xuất cho luồng <c>ExportCurrentPeriodAsync</c>.</summary>
    private async Task ExportCurrentPeriodAsync(Func<Task> exportAction, string fileFormat)
    {
        if (!CanExport || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        var exported = false;
        try
        {
            await ExecuteCommandAsync(
                $"Đang chuẩn bị xuất toàn bộ dữ liệu gán chức vụ trách nhiệm kỳ {LoadedPeriodLabel}...",
                async () =>
                {
                    var exportItems = await PositionAssignmentDataProvider.LoadAllForExportAsync(
                        LoadedYear,
                        LoadedMonth,
                        disposalTokenSource.Token);
                    ExportRows = exportItems
                        .Select(row => new ResponsibilityPositionAssignmentExportRow(
                            row.Id,
                            row.Year,
                            row.Month,
                            row.PositionCode,
                            row.PositionName,
                            row.GradeCode,
                            row.GradeName,
                            row.Status,
                            row.Note))
                        .ToArray();
                    if (ExportRows.Count == 0)
                    {
                        ToastService.ShowInfo($"Không có dữ liệu gán chức vụ trách nhiệm kỳ {LoadedPeriodLabel} để xuất file.");
                        return;
                    }

                    exportGridRenderCompletionSource = new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    await InvokeAsync(StateHasChanged);
                    await exportGridRenderCompletionSource.Task.WaitAsync(disposalTokenSource.Token);

                    if (ExportGrid is null)
                    {
                        throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
                    }

                    await exportAction();
                    exported = true;
                });

            if (exported)
            {
                ToastService.ShowInfo(
                    $"Đã bắt đầu xuất toàn bộ dữ liệu gán chức vụ trách nhiệm kỳ {LoadedPeriodLabel} ra {fileFormat}.");
            }
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            // Component đã được dispose; không hiển thị lỗi cho người dùng.
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu gán chức vụ trách nhiệm.");
        }
        finally
        {
            ExportRows = [];
            exportGridRenderCompletionSource = null;

            if (!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnActivePageIndexChangedAsync</c>.</summary>
    private async Task OnActivePageIndexChangedAsync(int value)
    {
        if (!CanBrowsePages)
        {
            return;
        }

        var pageIndex = Math.Clamp(value, 0, Math.Max(0, TotalPageCount - 1));
        if (CurrentPageIndex == pageIndex)
        {
            return;
        }

        CurrentPageIndex = pageIndex;
        await LoadLoadedPeriodAsync();
    }

    #endregion

    #region Helpers

    /// <summary>Thực hiện xử lý cho luồng <c>ExecuteCommandAsync</c>.</summary>
    private async Task ExecuteCommandAsync(string loadingText, Func<Task> action)
    {
        IsExecutingCommand = true;
        CommandLoadingText = loadingText;

        try
        {
            await action();
        }
        finally
        {
            IsExecutingCommand = false;
            CommandLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    /// <summary>Tạo cho luồng <c>BuildSaveMappingRequestFromEditor</c>.</summary>
    private SaveResponsibilityPositionAssignmentRequest BuildSaveMappingRequestFromEditor() =>
        new(
            EditingMappingId,
            LoadedYear,
            LoadedMonth,
            EditorModel.GradeId ?? throw new InvalidOperationException("Cấp bậc là bắt buộc."),
            EditorModel.PositionId ?? throw new InvalidOperationException("Chức vụ là bắt buộc."),
            EditorModel.IsActive,
            NormalizeNullableText(EditorModel.Note),
            EditorModel.OriginalUpdatedAtUtc);

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanEditLoadedMapping</c>.</summary>
    private bool CanEditLoadedMapping(PayrollResponsibilityAllowanceGradePositionDto row) =>
        CanUseLoadedDataActions && row.Year == LoadedYear && row.Month == LoadedMonth;

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanDeleteLoadedMapping</c>.</summary>
    private bool CanDeleteLoadedMapping(PayrollResponsibilityAllowanceGradePositionDto row) =>
        CanEditLoadedMapping(row) && row.IsActive;

    /// <summary>Lấy cho luồng <c>GetGradeLabel</c>.</summary>
    private string GetGradeLabel(Guid gradeId)
    {
        var grade = LoadedGradeRows.FirstOrDefault(row => row.Id == gradeId);
        return grade is null ? "Không tìm thấy cấp bậc" : $"{grade.Code} - {grade.Name}";
    }

    /// <summary>Định dạng tiền phụ cấp cơ bản theo cấp bậc trách nhiệm.</summary>
    private string FormatStandardResponsibilityAllowanceAmount(Guid gradeId)
    {
        var grade = LoadedGradeRows.FirstOrDefault(row => row.Id == gradeId);
        return grade is null
            ? string.Empty
            : $"{grade.StandardResponsibilityAllowanceAmount:N0} đ";
    }

    /// <summary>Lấy cho luồng <c>GetGradeLabelCssClass</c>.</summary>
    private string GetGradeLabelCssClass(Guid gradeId)
    {
        var grade = LoadedGradeRows.FirstOrDefault(row => row.Id == gradeId);
        return string.Join(
            ' ',
            "responsibility-grade",
            grade?.IsActive == true ? "responsibility-grade-active" : "responsibility-grade-inactive");
    }

    /// <summary>Tạo cho luồng <c>BuildExportFileName</c>.</summary>
    private string BuildExportFileName() => $"responsibility-position-assignments-{LoadedYear:D4}-{LoadedMonth:D2}";

    /// <summary>Tạo cho luồng <c>BuildExportRows</c>.</summary>
    private IReadOnlyList<ResponsibilityPositionAssignmentExportRow> BuildExportRows() =>
        LoadedMappingRows
            .OrderBy(row => row.PositionCode)
            .ThenBy(row => row.PositionName)
            .Select(row =>
            {
                var grade = LoadedGradeRows.FirstOrDefault(item => item.Id == row.GradeId);
                return new ResponsibilityPositionAssignmentExportRow(
                    row.Id,
                    row.Year,
                    row.Month,
                    row.PositionCode,
                    row.PositionName,
                    grade?.Code ?? string.Empty,
                    grade?.Name ?? "Không tìm thấy cấp bậc trách nhiệm",
                    GetActiveText(row.IsActive),
                    row.Note);
            })
            .ToArray();

    /// <summary>Định dạng cho luồng <c>FormatOptional</c>.</summary>
    private static string FormatOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeNullableText</c>.</summary>
    private static string? NormalizeNullableText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    /// <summary>Áp dụng cho luồng <c>ApplyToolbarPeriod</c>.</summary>
    private void ApplyToolbarPeriod(ResponsibilityAllowancePeriodKey period)
    {
        toolbarMonth = period.Month;
        toolbarYear = period.Year;
    }

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanUsePreviousPeriod</c>.</summary>
    private static bool CanUsePreviousPeriod(int year, int month) =>
        year > MinimumSupportedPeriod.Year
        || (year == MinimumSupportedPeriod.Year && month > MinimumSupportedPeriod.Month);

    /// <summary>Lấy cho luồng <c>GetPreviousPeriod</c>.</summary>
    private static ResponsibilityAllowancePeriodKey GetPreviousPeriod(int year, int month) =>
        month == 1
            ? new ResponsibilityAllowancePeriodKey(year - 1, 12)
            : new ResponsibilityAllowancePeriodKey(year, month - 1);

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeSelectedPeriod</c>.</summary>
    private static ResponsibilityAllowancePeriodKey NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedPeriod.Year, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);

        if (normalizedYear == MinimumSupportedPeriod.Year && normalizedMonth < MinimumSupportedPeriod.Month)
        {
            normalizedMonth = MinimumSupportedPeriod.Month;
        }

        return new ResponsibilityAllowancePeriodKey(normalizedYear, normalizedMonth);
    }

    /// <summary>Định dạng cho luồng <c>FormatPeriodLabel</c>.</summary>
    private static string FormatPeriodLabel(int year, int month) => $"{month:00}/{year}";

    /// <summary>Lấy cho luồng <c>GetActiveTextCssClass</c>.</summary>
    private static string GetActiveTextCssClass(bool value) =>
        string.Join(' ', "yes-no-status", value ? "yes-no-status-yes" : "yes-no-status-neutral");

    /// <summary>Lấy cho luồng <c>GetActiveText</c>.</summary>
    private static string GetActiveText(bool isActive) => isActive ? "Đang dùng" : "Ngừng";

    #endregion

    #region Disposal And Nested Types

    /// <summary>Giải phóng tài nguyên cho luồng <c>Dispose</c>.</summary>
    public void Dispose()
    {
        CancelActiveLoad();
        activeLoadTokenSource?.Dispose();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ResponsibilityAllowancePeriodKey</c>.</summary>
    private readonly record struct ResponsibilityAllowancePeriodKey(int Year, int Month);

    /// <summary>Thực hiện xử lý cho luồng <c>ResponsibilityPositionAssignmentLoadRequest</c>.</summary>
    private readonly record struct ResponsibilityPositionAssignmentLoadRequest(
        int Year,
        int Month,
        string? SearchText,
        int PageIndex,
        int PageSize);

    /// <summary>Đại diện kiểu <c>MonthOption</c> phục vụ màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private sealed record MonthOption(int Value, string Text);

    /// <summary>Đại diện kiểu <c>PageSizeOption</c> phục vụ màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private sealed record PageSizeOption(int Value, string Text);

    /// <summary>Đại diện kiểu <c>ResponsibilityPositionAssignmentExportRow</c> phục vụ màn hình gán phụ cấp trách nhiệm theo chức vụ.</summary>
    private sealed record ResponsibilityPositionAssignmentExportRow(
        Guid Id,
        int Year,
        int Month,
        string PositionCode,
        string PositionName,
        string GradeCode,
        string GradeName,
        string Status,
        string? Note);

    #endregion
}
