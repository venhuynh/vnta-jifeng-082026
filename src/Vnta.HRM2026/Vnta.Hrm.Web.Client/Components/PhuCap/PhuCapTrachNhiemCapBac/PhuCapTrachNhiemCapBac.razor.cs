using System.Globalization;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemCapBac;

/// <summary>Đại diện kiểu <c>PhuCapTrachNhiemCapBac</c> phục vụ màn hình phụ cấp trách nhiệm cấp bậc.</summary>
public partial class PhuCapTrachNhiemCapBac : IDisposable
{
    #region Constants

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private static readonly ResponsibilityAllowancePeriodKey MinimumSupportedPeriod = new(2026, 6);

    #endregion

    #region Dependencies

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private readonly CancellationTokenSource disposalTokenSource = new();
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private ResponsibilityAllowanceGradeReloadState ReloadState { get; } = new();
    private ResponsibilityAllowanceGradeSelectionState SelectionState { get; } = new();

    [Inject]
    /// <summary>Giá trị <c>WorkflowService</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private IPayrollResponsibilityAllowanceGradeConfigurationReadService GradeConfigurationReadService { get; set; } = default!;

    [Inject]
    private IPayrollResponsibilityAllowanceGradeConfigurationWriteService GradeConfigurationWriteService { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>ToastService</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    /// <summary>Giá trị <c>DialogService</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private IHrmDialogService DialogService { get; set; } = default!;

    #endregion

    #region Component References

    /// <summary>Giá trị <c>Grid</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private IGrid? Grid { get; set; }
    /// <summary>Giá trị <c>EditorEditContext</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private EditContext? EditorEditContext { get; set; }

    #endregion

    #region State

    /// <summary>Giá trị <c>LoadedGradeRows</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> LoadedGradeRows { get; set; } = [];
    /// <summary>Giá trị <c>SelectedGridItems</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private IReadOnlyList<object> SelectedGridItems
    {
        get => SelectionState.Items;
        set => SelectionState.Items = value;
    }

    /// <summary>Giá trị <c>EditorModel</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private PhuCapTrachNhiemCapBacEditModel EditorModel { get; set; } = PhuCapTrachNhiemCapBacEditModel.CreateDefault();
    /// <summary>Giá trị <c>EditingGradeId</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private Guid? EditingGradeId { get; set; }

    /// <summary>Giá trị <c>LoadedMonth</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private int LoadedMonth { get; set; } = MinimumSupportedPeriod.Month;
    /// <summary>Giá trị <c>LoadedYear</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private int LoadedYear { get; set; } = MinimumSupportedPeriod.Year;

    /// <summary>Giá trị <c>SearchQuery</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string? SearchQuery { get; set; }
    /// <summary>Giá trị <c>ScreenErrorMessage</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string? ScreenErrorMessage { get; set; }
    /// <summary>Giá trị <c>EditorErrorMessage</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string? EditorErrorMessage { get; set; }
    /// <summary>Giá trị <c>CommandLoadingText</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string CommandLoadingText { get; set; } = HrmUiDefaults.LoadingText;

    /// <summary>Giá trị <c>IsLoading</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool IsLoading { get; set; }
    /// <summary>Giá trị <c>IsExecutingCommand</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool IsExecutingCommand { get; set; }
    /// <summary>Giá trị <c>IsChangingPageSize</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool IsChangingPageSize { get; set; }
    /// <summary>Giá trị <c>HasRequestedData</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool HasRequestedData { get; set; }
    /// <summary>Giá trị <c>IsEditorVisible</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool IsEditorVisible { get; set; }
    /// <summary>Giá trị <c>IsEditorSaving</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool IsEditorSaving { get; set; }

    /// <summary>Giá trị <c>PageSize</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private int PageSize { get; set; } = 50;
    #endregion

    #region Derived State

    /// <summary>Giá trị <c>LoadedPeriodLabel</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string LoadedPeriodLabel => FormatPeriodLabel(LoadedYear, LoadedMonth);

    /// <summary>Giá trị <c>EditorPopupTitle</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string EditorPopupTitle => EditingGradeId.HasValue
        ? $"Sửa cấp bậc trách nhiệm - {LoadedPeriodLabel}"
        : $"Thêm cấp bậc trách nhiệm - {LoadedPeriodLabel}";

    /// <summary>Giá trị <c>EditorPrimaryActionText</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string EditorPrimaryActionText => EditingGradeId.HasValue
        ? "Lưu thay đổi"
        : "Tạo cấp bậc";

    /// <summary>Giá trị <c>HasActiveSearch</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool HasActiveSearch => !string.IsNullOrWhiteSpace(SearchQuery);
    /// <summary>Giá trị <c>HasLoadError</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool HasLoadError => !string.IsNullOrWhiteSpace(ScreenErrorMessage);
    /// <summary>Giá trị <c>IsEditorOpen</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool IsEditorOpen => IsEditorVisible;
    /// <summary>Giá trị <c>ShowLoadingPanel</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool ShowLoadingPanel => IsLoading || IsExecutingCommand || IsChangingPageSize;

    /// <summary>Giá trị <c>CanUseLoadedDataActions</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool CanUseLoadedDataActions =>
        !ShowLoadingPanel
        && !HasLoadError
        && HasRequestedData
        && !IsEditorOpen;

    /// <summary>Giá trị <c>CanExport</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool CanExport => CanUseLoadedDataActions && VisibleGradeRows.Count > 0;
    /// <summary>Giá trị <c>CanExportSelected</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool CanExportSelected => CanExport && GetSelectedVisibleGradeRows().Count > 0;
    /// <summary>Giá trị <c>CanSearchScreen</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool CanSearchScreen => HasRequestedData && !HasLoadError && !ShowLoadingPanel && !IsEditorOpen;
    /// <summary>Giá trị <c>CanCreateGrade</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool CanCreateGrade => CanUseLoadedDataActions;
    /// <summary>Giá trị <c>CanUseEmptyStateAction</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool CanUseEmptyStateAction => HasRequestedData && !HasLoadError && !ShowLoadingPanel && !IsEditorOpen;
    /// <summary>Giá trị <c>CanCloseEditorPopup</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool CanCloseEditorPopup => !IsEditorSaving;
    /// <summary>Giá trị <c>CanSaveEditor</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private bool CanSaveEditor => EditorEditContext is not null && !IsEditorSaving && !IsLoading && !IsChangingPageSize;

    /// <summary>Giá trị <c>LoadingText</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string LoadingText => IsChangingPageSize
        ? "Đang cập nhật số dòng hiển thị..."
        : IsExecutingCommand
            ? CommandLoadingText
            : "Đang tải dữ liệu cấp bậc trách nhiệm...";

    /// <summary>Giá trị <c>EditorLoadingText</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string EditorLoadingText => string.IsNullOrWhiteSpace(CommandLoadingText)
        ? HrmUiDefaults.LoadingText
        : CommandLoadingText;

    /// <summary>Giá trị <c>VisibleGradeRows</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> VisibleGradeRows =>
        LoadedGradeRows
            .Where(MatchesSearchQuery)
            .OrderBy(row => row.DisplayOrder)
            .ThenBy(row => row.Code)
            .ToArray();

    /// <summary>Giá trị <c>EmptyStateTitle</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string EmptyStateTitle => !HasRequestedData
        ? "Đang chuẩn bị dữ liệu cấp bậc trách nhiệm"
        : HasActiveSearch
            ? "Không tìm thấy cấp bậc phù hợp"
            : $"Kỳ {LoadedPeriodLabel} chưa có cấp bậc trách nhiệm";

    /// <summary>Giá trị <c>EmptyStateMessage</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string EmptyStateMessage => !HasRequestedData
        ? "Màn hình đang tải dữ liệu mặc định cho bảng cấp bậc trách nhiệm."
        : HasActiveSearch
            ? "Hãy thử từ khóa khác hoặc xóa tìm kiếm để xem thêm dữ liệu."
            : "Bạn có thể tạo mới cấp bậc trách nhiệm cho kỳ hiện tại.";

    /// <summary>Giá trị <c>EmptyStateActionText</c> được sử dụng bởi màn hình phụ cấp trách nhiệm cấp bậc.</summary>
    private string EmptyStateActionText => !HasRequestedData
        ? "Tải dữ liệu"
        : HasActiveSearch
            ? "Xóa tìm kiếm"
            : "Thêm cấp bậc";

    #endregion

    #region Lifecycle

    /// <summary>Xử lý sự kiện cho luồng <c>OnInitializedAsync</c>.</summary>
    protected override async Task OnInitializedAsync()
    {
        ResetEditorState();
        await LoadLoadedPeriodAsync();
    }

    #endregion

    #region Toolbar And Screen Actions

    /// <summary>Xử lý sự kiện cho luồng <c>OnRetryRequestedAsync</c>.</summary>
    private async Task OnRetryRequestedAsync()
    {
        await LoadLoadedPeriodAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnReloadRequestedAsync</c>.</summary>
    private async Task OnReloadRequestedAsync()
    {
        await LoadLoadedPeriodAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnColumnChooserRequested</c>.</summary>
    private void OnColumnChooserRequested() => Grid?.ShowColumnChooser();

    #endregion

    #region Filter And Paging

    /// <summary>Xử lý sự kiện cho luồng <c>OnSearchTextChangedAsync</c>.</summary>
    private Task OnSearchTextChangedAsync(string? value)
    {
        var normalizedValue = NormalizeNullableText(value);
        if (string.Equals(SearchQuery, normalizedValue, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        SearchQuery = normalizedValue;
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnPageSizeChangedAsync</c>.</summary>
    private async Task OnPageSizeChangedAsync(int value)
    {
        if (PageSize == value)
        {
            return;
        }

        IsChangingPageSize = true;
        PageSize = value;

        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }
        finally
        {
            IsChangingPageSize = false;
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnEmptyStateActionRequestedAsync</c>.</summary>
    private async Task OnEmptyStateActionRequestedAsync()
    {
        if (!HasRequestedData)
        {
            await LoadLoadedPeriodAsync();
            return;
        }

        if (HasActiveSearch)
        {
            SearchQuery = null;
            return;
        }

        OpenCreateEditor();
    }

    #endregion

    #region Selection

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

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    /// <summary>Lấy cho luồng <c>GetSelectedVisibleGradeRows</c>.</summary>
    private List<PayrollResponsibilityAllowanceGradeDto> GetSelectedVisibleGradeRows()
    {
        var visibleIds = VisibleGradeRows.Select(row => row.Id).ToHashSet();
        return SelectedGridItems
            .OfType<PayrollResponsibilityAllowanceGradeDto>()
            .Where(row => visibleIds.Contains(row.Id))
            .DistinctBy(row => row.Id)
            .ToList();
    }

    #endregion

    #region Popup Editor

    /// <summary>Xử lý sự kiện cho luồng <c>OnCreateRequestedAsync</c>.</summary>
    private Task OnCreateRequestedAsync()
    {
        OpenCreateEditor();
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnEditRequestedAsync</c>.</summary>
    private Task OnEditRequestedAsync(PayrollResponsibilityAllowanceGradeDto grade)
    {
        OpenEditEditor(grade);
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnDeleteRequestedAsync</c>.</summary>
    private async Task OnDeleteRequestedAsync(PayrollResponsibilityAllowanceGradeDto grade)
    {
        if (!CanDeleteLoadedGrade(grade))
        {
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            $"Bạn có chắc muốn ngừng dùng cấp bậc {grade.Code} - {grade.Name} trong kỳ {FormatPeriodLabel(grade.Year, grade.Month)} không?",
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
                $"Đang ngừng dùng cấp bậc trách nhiệm {grade.Code}...",
                async () =>
                {
                    await GradeConfigurationWriteService.SaveGradeAsync(
                        BuildDeactivateGradeRequest(grade),
                        disposalTokenSource.Token);

                    await LoadLoadedPeriodAsync();
                });

            if (!HasLoadError)
            {
                ToastService.ShowSuccess($"Đã ngừng dùng cấp bậc trách nhiệm {grade.Code}.");
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
        if (!CanCreateGrade)
        {
            return;
        }

        ResetEditorState();
        IsEditorVisible = true;
    }

    /// <summary>Mở cho luồng <c>OpenEditEditor</c>.</summary>
    private void OpenEditEditor(PayrollResponsibilityAllowanceGradeDto grade)
    {
        if (!CanEditLoadedGrade(grade))
        {
            return;
        }

        EditingGradeId = grade.Id;
        EditorErrorMessage = null;
        EditorModel = PhuCapTrachNhiemCapBacEditModel.CreateFrom(grade);
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
                EditingGradeId.HasValue
                    ? $"Đang lưu cấp bậc trách nhiệm kỳ {LoadedPeriodLabel}..."
                    : $"Đang thêm cấp bậc trách nhiệm kỳ {LoadedPeriodLabel}...",
                async () =>
                {
                    await GradeConfigurationWriteService.SaveGradeAsync(
                        BuildSaveGradeRequestFromEditor(),
                        disposalTokenSource.Token);

                    await LoadLoadedPeriodAsync();
                });

            if (!HasLoadError)
            {
                CloseEditor();
                ToastService.ShowSuccess("Đã lưu cấp bậc trách nhiệm.");
            }
            else
            {
                EditorErrorMessage = "Đã lưu cấp bậc nhưng chưa thể tải lại danh sách. Hãy thử lại để đồng bộ giao diện.";
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
        EditingGradeId = null;
        EditorErrorMessage = null;
        EditorModel = PhuCapTrachNhiemCapBacEditModel.CreateDefault();
        EditorEditContext = new EditContext(EditorModel);
    }

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeEditorModel</c>.</summary>
    private void NormalizeEditorModel()
    {
        EditorModel.Code = (EditorModel.Code ?? string.Empty).Trim();
        EditorModel.Name = (EditorModel.Name ?? string.Empty).Trim();
        EditorModel.Note = string.IsNullOrWhiteSpace(EditorModel.Note)
            ? string.Empty
            : EditorModel.Note.Trim();
    }

    #endregion

    #region Export

    /// <summary>Xử lý sự kiện cho luồng <c>OnExportAllExcelRequestedAsync</c>.</summary>
    private Task OnExportAllExcelRequestedAsync() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(BuildExportFileName()),
        "Đã bắt đầu xuất Excel cấp bậc trách nhiệm.");

    /// <summary>Xử lý sự kiện cho luồng <c>OnExportSelectedExcelRequestedAsync</c>.</summary>
    private Task OnExportSelectedExcelRequestedAsync() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(
            $"{BuildExportFileName()}-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất Excel cho các dòng đã chọn.");

    /// <summary>Xử lý sự kiện cho luồng <c>OnExportAllPdfRequestedAsync</c>.</summary>
    private Task OnExportAllPdfRequestedAsync() => ExportAsync(
        () => Grid!.ExportToPdfAsync(BuildExportFileName()),
        "Đã bắt đầu xuất PDF cấp bậc trách nhiệm.");

    /// <summary>Xử lý sự kiện cho luồng <c>OnExportSelectedPdfRequestedAsync</c>.</summary>
    private Task OnExportSelectedPdfRequestedAsync() => ExportAsync(
        () => Grid!.ExportToPdfAsync(
            $"{BuildExportFileName()}-selected",
            new GridPdfExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất PDF cho các dòng đã chọn.");

    /// <summary>Xuất cho luồng <c>ExportAsync</c>.</summary>
    private async Task ExportAsync(Func<Task> exportAction, string successMessage)
    {
        if (Grid is null)
        {
            ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất.");
            return;
        }

        try
        {
            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu cấp bậc trách nhiệm.");
        }
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

    /// <summary>Tạo cho luồng <c>BuildSaveGradeRequestFromEditor</c>.</summary>
    private SavePayrollResponsibilityAllowanceGradeRequest BuildSaveGradeRequestFromEditor() =>
        new(
            EditingGradeId,
            LoadedYear,
            LoadedMonth,
            EditorModel.Code.Trim(),
            EditorModel.Name.Trim(),
            EditorModel.StandardResponsibilityAllowanceAmount,
            EditorModel.DisplayOrder,
            EditorModel.IsActive,
            EditorModel.Note.Trim());

    /// <summary>Tạo cho luồng <c>BuildDeactivateGradeRequest</c>.</summary>
    private static SavePayrollResponsibilityAllowanceGradeRequest BuildDeactivateGradeRequest(
        PayrollResponsibilityAllowanceGradeDto grade) =>
        new(
            grade.Id,
            grade.Year,
            grade.Month,
            grade.Code,
            grade.Name,
            grade.StandardResponsibilityAllowanceAmount,
            grade.DisplayOrder,
            false,
            grade.Note?.Trim());

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanEditLoadedGrade</c>.</summary>
    private bool CanEditLoadedGrade(PayrollResponsibilityAllowanceGradeDto row) =>
        CanUseLoadedDataActions && row.Year == LoadedYear && row.Month == LoadedMonth;

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanDeleteLoadedGrade</c>.</summary>
    private bool CanDeleteLoadedGrade(PayrollResponsibilityAllowanceGradeDto row) =>
        CanEditLoadedGrade(row) && row.IsActive;

    /// <summary>Thực hiện xử lý cho luồng <c>MatchesSearchQuery</c>.</summary>
    private bool MatchesSearchQuery(PayrollResponsibilityAllowanceGradeDto row)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return true;
        }

        var keyword = NormalizeText(SearchQuery);
        var target = NormalizeText($"{row.Code} {row.Name} {row.Note}");
        return target.Contains(keyword, StringComparison.Ordinal);
    }

    /// <summary>Tạo cho luồng <c>BuildExportFileName</c>.</summary>
    private string BuildExportFileName() => $"responsibility-allowance-grades-{LoadedYear:D4}-{LoadedMonth:D2}";

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeNullableText</c>.</summary>
    private static string? NormalizeNullableText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeText</c>.</summary>
    private static string NormalizeText(string? value)
    {
        return (value ?? string.Empty)
            .Normalize(NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .Aggregate(string.Empty, (current, ch) => current + char.ToLowerInvariant(ch))
            .Trim();
    }

    /// <summary>Định dạng cho luồng <c>FormatCurrency</c>.</summary>
    private string FormatCurrency(decimal amount) =>
        amount == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", amount);

    /// <summary>Định dạng cho luồng <c>FormatPeriodLabel</c>.</summary>
    private static string FormatPeriodLabel(int year, int month) => $"{month:00}/{year}";

    /// <summary>Lấy cho luồng <c>GetActiveTextCssClass</c>.</summary>
    private static string GetActiveTextCssClass(bool value) =>
        string.Join(' ', "yes-no-status", value ? "yes-no-status-yes" : "yes-no-status-neutral");

    /// <summary>Lấy cho luồng <c>GetActiveText</c>.</summary>
    private static string GetActiveText(bool isActive) => isActive ? "Đang dùng" : "Ngừng";

    #endregion

    #region Disposal

    /// <summary>Giải phóng tài nguyên cho luồng <c>Dispose</c>.</summary>
    public void Dispose()
    {
        CancelActiveReload();
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    #endregion

    #region Nested Types

    /// <summary>Thực hiện xử lý cho luồng <c>ResponsibilityAllowancePeriodKey</c>.</summary>
    private readonly record struct ResponsibilityAllowancePeriodKey(int Year, int Month);

    #endregion
}
