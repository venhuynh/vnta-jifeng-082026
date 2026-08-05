using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.TinhLuong.LuongCanBan;

public partial class LuongCanBan : IDisposable
{
    private const int PreviousMonthSyncTargetMonth = 7;
    private const int PreviousMonthSyncTargetYear = 2026;
    private const string PreviousMonthSyncSourcePeriodLabel = "06/2026";
    private const string PreviousMonthSyncTargetPeriodLabel = "07/2026";
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);

    [Inject]
    private BasicSalaryDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IHrmDialogService DialogService { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private ILogger<LuongCanBan> Logger { get; set; } = default!;

    private IReadOnlyList<BasicSalaryRecord> SalaryRecords { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IReadOnlyList<BasicSalaryInfoRow> DetailRows { get; set; } = [];
    private IGrid? Grid { get; set; }
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private string? DetailErrorMessage { get; set; }
    private string DetailEmptyMessage { get; set; } =
        "Bản ghi lương căn bản này chưa có dữ liệu chi tiết để hiển thị.";
    private BasicSalaryRecord? SelectedSalaryRecord { get; set; }
    private BasicSalaryRecord EditModel { get; set; } = new();
    private DateTime? DetailResponseTime { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsRefreshing { get; set; }
    private bool IsSyncingPreviousMonth { get; set; }
    private bool IsCreatingNewSalaryRecord { get; set; }
    private bool IsEditPopupVisible { get; set; }
    private bool IsSavingEdit { get; set; }
    private bool IsDeletingSalaryRecords { get; set; }
    private bool IsDetailPopupVisible { get; set; }
    private bool IsDetailLoading { get; set; }
    private bool IsChangingPageSize { get; set; }
    private int PageSize { get; set; } = 20;
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool CanInteract => !IsLoading && !IsRefreshing && !IsChangingPageSize && !IsSyncingPreviousMonth && !IsEditPopupVisible && !IsSavingEdit && !IsDeletingSalaryRecords && !HasLoadError;
    private bool CanCreate => CanInteract;
    private bool CanRefresh => !IsLoading && !IsRefreshing && !IsChangingPageSize && !IsSyncingPreviousMonth && !IsEditPopupVisible && !IsSavingEdit && !IsDeletingSalaryRecords;
    private bool CanEditSelected => CanInteract && GetSelectedSalaryRecordCount() == 1;
    private bool CanDeleteSelected => CanInteract && GetSelectedSalaryRecordCount() > 0;
    private bool CanShowDetailSelected => CanInteract && GetSelectedSalaryRecordCount() == 1;
    private bool CanSyncFromPreviousMonth => !IsLoading && !IsRefreshing && !IsChangingPageSize && !IsSyncingPreviousMonth && !IsEditPopupVisible && !IsSavingEdit && !IsDeletingSalaryRecords && !HasLoadError;
    private bool CanExport => !IsLoading && !IsRefreshing && !IsChangingPageSize && !IsSyncingPreviousMonth && !IsEditPopupVisible && !IsSavingEdit && !IsDeletingSalaryRecords && VisibleSalaryRecords.Count > 0;
    private bool CanExportSelected => CanExport && GetSelectedSalaryRecordCount() > 0;
    private bool ShowLoadingPanel => IsLoading || IsRefreshing || IsChangingPageSize || IsSyncingPreviousMonth;
    private IReadOnlyList<BasicSalaryRecord> VisibleSalaryRecords => SalaryRecords;
    private string SyncFromPreviousMonthTooltip =>
        $"Đồng bộ dữ liệu lương căn bản từ tháng {PreviousMonthSyncSourcePeriodLabel} sang tháng {PreviousMonthSyncTargetPeriodLabel}";
    private string LoadingPanelText => IsSyncingPreviousMonth
        ? $"Đang đồng bộ lương căn bản từ tháng {PreviousMonthSyncSourcePeriodLabel} sang tháng {PreviousMonthSyncTargetPeriodLabel}..."
        : IsRefreshing
            ? "Đang làm mới danh sách lương căn bản..."
            : IsChangingPageSize
                ? "Đang cập nhật số dòng hiển thị..."
                : HrmUiDefaults.LoadingText;
    private string EmptyStateTitle => !string.IsNullOrWhiteSpace(SearchText)
        ? "Không tìm thấy lương căn bản phù hợp"
        : "Chưa có lương căn bản";
    private string EmptyStateMessage => !string.IsNullOrWhiteSpace(SearchText)
        ? "Hãy thử từ khóa khác hoặc xóa bộ lọc tìm kiếm để xem thêm dữ liệu."
        : "Bắt đầu bằng cách tạo bản ghi lương căn bản đầu tiên cho kỳ lương của nhân viên.";
    private string EmptyStateActionText => !string.IsNullOrWhiteSpace(SearchText)
        ? "Xóa tìm kiếm"
        : "Tạo lương căn bản";
    private string EditPopupTitle => IsCreatingNewSalaryRecord
        ? "Khởi tạo lương căn bản"
        : "Điều chỉnh lương căn bản";
    private bool CanEditPopupFields => !IsSavingEdit;
    private bool CanSaveEdit => CanEditPopupFields;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ReloadAsync();
            await InvokeAsync(StateHasChanged);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task ReloadAsync()
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref reloadRequestedVersion);
        if (!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        try
        {
            while (!disposalTokenSource.IsCancellationRequested
                   && reloadProcessedVersion < Volatile.Read(ref reloadRequestedVersion))
            {
                reloadProcessedVersion = Volatile.Read(ref reloadRequestedVersion);
                await ReloadCoreAsync();
            }
        }
        finally
        {
            reloadGate.Release();
        }
    }

    private async Task ReloadCoreAsync()
    {
        LoadErrorMessage = null;
        EditErrorMessage = null;
        IsLoading = true;

        try
        {
            await ClearSelectionAsync();
            SalaryRecords = await DataProvider.SearchAsync(BuildListFilter(), disposalTokenSource.Token);
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
            Logger.LogError(ex, "Không thể tải danh sách lương căn bản.");
            SalaryRecords = [];
            DetailRows = [];
            DetailResponseTime = null;
            SelectedSalaryRecord = null;
            IsDetailPopupVisible = false;
            LoadErrorMessage = "Có lỗi khi tải dữ liệu lương căn bản. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách lương căn bản.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAsync()
    {
        if (disposalTokenSource.IsCancellationRequested || !CanRefresh)
        {
            return;
        }

        LoadErrorMessage = null;
        IsRefreshing = true;

        try
        {
            await ReloadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private async Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
        if (string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalizedValue;
        await ReloadAsync();
    }

    private async Task OnPageSizeChanged(int value)
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

    private async Task OnEmptyStateActionClick()
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            SearchText = null;
            await ReloadAsync();
            return;
        }

        await OnAddSalaryRecordClick();
    }

    private Task OnAddSalaryRecordClick()
    {
        if (!CanCreate)
        {
            return Task.CompletedTask;
        }

        var model = new BasicSalaryRecord();
        InitializeNewSalaryRecordDefaults(model);
        OpenEditPopup(model, isNew: true);
        return Task.CompletedTask;
    }

    private async Task OnEditSalaryRecordClick()
    {
        var salaryRecord = GetSingleSelectedSalaryRecord();
        if (salaryRecord is null)
        {
            ToastService.ShowWarning("Hãy chọn đúng một bản ghi lương căn bản để điều chỉnh.");
            return;
        }

        OpenEditPopup(salaryRecord);
    }

    private Task CloseEditPopupAsync(bool visible)
    {
        if (!visible && !IsSavingEdit)
        {
            EditErrorMessage = null;
            IsEditPopupVisible = false;
        }

        return Task.CompletedTask;
    }

    private async Task OnDeleteSalaryRecordsClick()
    {
        var selectedSalaryRecords = GetSelectedSalaryRecords();
        if (selectedSalaryRecords.Count == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một bản ghi lương căn bản để xóa.");
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            selectedSalaryRecords.Count == 1
                ? $"Bạn có chắc muốn xóa bản ghi `{selectedSalaryRecords[0].SummaryText}`?"
                : $"Bạn có chắc muốn xóa {selectedSalaryRecords.Count} bản ghi lương căn bản đã chọn?",
            title: "Xác nhận xóa",
            okText: "Xóa",
            cancelText: "Hủy",
            renderStyle: MessageBoxRenderStyle.Danger);

        if (!confirmed)
        {
            return;
        }

        await DeleteSalaryRecordsAsync(selectedSalaryRecords);
    }

    private async Task DeleteSalaryRecordAsync(BasicSalaryRecord salaryRecord)
    {
        if (!CanDeleteSalaryRecord(salaryRecord))
        {
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            $"Bạn có chắc muốn xóa bản ghi `{salaryRecord.SummaryText}`?",
            title: "Xác nhận xóa",
            okText: "Xóa",
            cancelText: "Hủy",
            renderStyle: MessageBoxRenderStyle.Danger);

        if (confirmed)
        {
            await DeleteSalaryRecordsAsync([salaryRecord]);
        }
    }

    private async Task DeleteSalaryRecordsAsync(IReadOnlyCollection<BasicSalaryRecord> salaryRecords)
    {
        if (salaryRecords.Count == 0 || IsDeletingSalaryRecords || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsDeletingSalaryRecords = true;
        try
        {
            await DataProvider.DeleteAsync(
                salaryRecords.Select(record => record.Id),
                disposalTokenSource.Token);
            await ReloadAsync();
            ToastService.ShowSuccess(salaryRecords.Count == 1
                ? "Đã xóa bản ghi lương căn bản."
                : "Đã xóa các bản ghi lương căn bản đã chọn.");
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
            ToastService.ShowError(salaryRecords.Count == 1
                ? "Không thể xóa bản ghi lương căn bản."
                : "Không thể xóa các bản ghi lương căn bản đã chọn.");
        }
        finally
        {
            IsDeletingSalaryRecords = false;
        }
    }

    private Task OnShowSalaryDetailClick()
    {
        var salaryRecord = GetSingleSelectedSalaryRecord();
        if (salaryRecord is null)
        {
            ToastService.ShowWarning("Hãy chọn đúng một bản ghi lương căn bản để xem chi tiết.");
            return Task.CompletedTask;
        }

        return OpenSalaryDetailAsync(salaryRecord);
    }

    private async Task OnSyncFromPreviousMonthClickAsync()
    {
        if (disposalTokenSource.IsCancellationRequested || !CanSyncFromPreviousMonth)
        {
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            $"Hệ thống sẽ đồng bộ dữ liệu lương căn bản từ tháng {PreviousMonthSyncSourcePeriodLabel} sang tháng {PreviousMonthSyncTargetPeriodLabel}. " +
            $"Các bản ghi tháng {PreviousMonthSyncTargetPeriodLabel} đã có sẽ được cập nhật theo dữ liệu tháng {PreviousMonthSyncSourcePeriodLabel}; " +
            $"các bản ghi chưa có sẽ được tạo mới. Bạn có muốn tiếp tục?",
            title: "Lấy từ tháng trước",
            okText: "Đồng bộ",
            cancelText: "Hủy",
            renderStyle: MessageBoxRenderStyle.Primary);

        if (!confirmed)
        {
            return;
        }

        IsSyncingPreviousMonth = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await DataProvider.SyncFromPreviousMonthAsync(
                new SyncBasicSalaryFromPreviousMonthRequest
                {
                    TargetMonth = PreviousMonthSyncTargetMonth,
                    TargetYear = PreviousMonthSyncTargetYear
                },
                disposalTokenSource.Token);

            await ReloadAsync();

            if (result.SourceRecordCount == 0)
            {
                ToastService.ShowWarning(
                    $"Không có dữ liệu lương căn bản tháng {PreviousMonthSyncSourcePeriodLabel} để đồng bộ sang tháng {PreviousMonthSyncTargetPeriodLabel}.");
            }
            else
            {
                ToastService.ShowSuccess(BuildSyncFromPreviousMonthSummaryMessage(result));
            }
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            ToastService.ShowError(
                $"Không thể đồng bộ dữ liệu lương căn bản từ tháng {PreviousMonthSyncSourcePeriodLabel} sang tháng {PreviousMonthSyncTargetPeriodLabel}.");
        }
        finally
        {
            IsSyncingPreviousMonth = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task OpenSalaryDetailAsync(BasicSalaryRecord salaryRecord)
    {
        SelectedSalaryRecord = salaryRecord;
        IsDetailPopupVisible = true;
        LoadSalaryDetail(salaryRecord);
        return Task.CompletedTask;
    }

    private Task RetrySalaryDetailAsync()
    {
        if (SelectedSalaryRecord is not null)
        {
            LoadSalaryDetail(SelectedSalaryRecord);
        }

        return Task.CompletedTask;
    }

    private void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

    private Task ExportAllDataToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync("basic-salaries"),
        "Đã bắt đầu xuất Excel.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(
            "basic-salaries-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất Excel cho các dòng đã chọn.");

    private Task ExportAllDataToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync("basic-salaries"),
        "Đã bắt đầu xuất PDF.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync(
            "basic-salaries-selected",
            new GridPdfExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất PDF cho các dòng đã chọn.");

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
            ToastService.ShowError("Không thể xuất dữ liệu lương căn bản.");
        }
    }

    private void LoadSalaryDetail(BasicSalaryRecord salaryRecord)
    {
        DetailRows = [];
        DetailResponseTime = salaryRecord.UpdatedAtUtc ?? salaryRecord.CreatedAtUtc;
        DetailErrorMessage = null;
        IsDetailLoading = true;
        DetailEmptyMessage = "Bản ghi lương căn bản này chưa có dữ liệu chi tiết để hiển thị.";

        try
        {
            DetailRows = BuildDetailRows(salaryRecord);
            if (DetailRows.Count == 0)
            {
                DetailEmptyMessage = "Chưa có đủ dữ liệu chi tiết cho bản ghi lương căn bản này.";
            }
        }
        catch (Exception)
        {
            DetailRows = [];
            DetailErrorMessage = "Có lỗi khi chuẩn bị dữ liệu chi tiết lương căn bản. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải chi tiết lương căn bản.");
        }
        finally
        {
            IsDetailLoading = false;
        }
    }

    private void OpenEditPopup(BasicSalaryRecord model, bool isNew = false)
    {
        EditErrorMessage = null;
        IsCreatingNewSalaryRecord = isNew;
        EditModel = CloneSalaryRecord(model);
        IsEditPopupVisible = true;
    }

    private async Task SaveSalaryRecordAsync(BasicSalaryRecord draft)
    {
        if (IsSavingEdit || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        EditErrorMessage = null;
        IsSavingEdit = true;

        try
        {
            NormalizeEditModel(draft);
            CalculateDerivedSalaryValues(draft);

            var now = DateTime.UtcNow;
            if (draft.CreatedAtUtc == default)
            {
                draft.CreatedAtUtc = now;
            }

            draft.UpdatedAtUtc = now;

            var validationMessage = await DataProvider.ValidateAsync(draft, disposalTokenSource.Token);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                EditErrorMessage = validationMessage;
                return;
            }

            await DataProvider.SaveAsync(draft, IsCreatingNewSalaryRecord, disposalTokenSource.Token);
            await ReloadAsync();
            IsEditPopupVisible = false;
            ToastService.ShowSuccess(IsCreatingNewSalaryRecord
                ? "Đã thêm bản ghi lương căn bản."
                : "Đã cập nhật bản ghi lương căn bản.");
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }

        }
        catch (InvalidOperationException ex)
        {
            EditErrorMessage = ex.Message;
            ToastService.ShowError("Không thể lưu bản ghi lương căn bản.");
        }
        catch (Exception)
        {
            EditErrorMessage = "Không thể lưu dữ liệu lương căn bản. Vui lòng kiểm tra lại thông tin.";
            ToastService.ShowError("Không thể lưu bản ghi lương căn bản.");
        }
        finally
        {
            IsSavingEdit = false;
        }
    }

    private string FormatDateTime(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        var normalized = value.Value.Kind == DateTimeKind.Unspecified
            ? value.Value
            : value.Value.ToLocalTime();

        return normalized.ToString("dd/MM/yyyy HH:mm", DisplayCulture);
    }

    private async Task ClearSelectionAsync(bool closeDetailPopup = true)
    {
        SelectedDataItems = [];
        SelectedSalaryRecord = null;
        DetailRows = [];
        DetailErrorMessage = null;
        DetailResponseTime = null;
        if (closeDetailPopup)
        {
            IsDetailPopupVisible = false;
        }

        if (Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private List<BasicSalaryRecord> GetSelectedSalaryRecords() =>
        SelectedDataItems
            .OfType<BasicSalaryRecord>()
            .Where(IsVisibleSalaryRecord)
            .DistinctBy(record => record.Id)
            .ToList();

    private BasicSalaryRecord? GetSingleSelectedSalaryRecord()
    {
        var selectedSalaryRecords = GetSelectedSalaryRecords();
        return selectedSalaryRecords.Count == 1 ? selectedSalaryRecords[0] : null;
    }

    private int GetSelectedSalaryRecordCount() => GetSelectedSalaryRecords().Count;

    private bool CanEditSalaryRecord(BasicSalaryRecord salaryRecord) =>
        CanInteract && IsVisibleSalaryRecord(salaryRecord);

    private bool CanShowSalaryDetail(BasicSalaryRecord salaryRecord) =>
        CanInteract && IsVisibleSalaryRecord(salaryRecord);

    private bool CanDeleteSalaryRecord(BasicSalaryRecord salaryRecord) =>
        CanInteract && IsVisibleSalaryRecord(salaryRecord);

    private IReadOnlyList<BasicSalaryInfoRow> BuildDetailRows(BasicSalaryRecord salaryRecord)
    {
        var rows = new List<BasicSalaryInfoRow>();

        AddDetailRow(rows, "employee-name", "Nhân viên", salaryRecord.EmployeeDisplayText);
        AddDetailRow(rows, "employee-code", "Mã nhân viên", salaryRecord.EmployeeCode);
        AddDetailRow(rows, "department", "Phòng ban", salaryRecord.DepartmentDisplayText);
        AddDetailRow(rows, "job-title", "Chức danh", salaryRecord.PositionDisplayText);
        AddDetailRow(rows, "period", "Kỳ lương áp dụng", salaryRecord.PeriodDisplayText);
        AddDetailRow(rows, "basic-salary", "Lương căn bản", FormatCurrency(salaryRecord.BasicSalary));
        AddDetailRow(rows, "working-days", "Số ngày làm việc tiêu chuẩn", FormatNumber(salaryRecord.StandardWorkingDays));
        AddDetailRow(rows, "daily-salary", "Lương ngày", FormatCurrency(salaryRecord.DailySalary));
        AddDetailRow(rows, "hourly-salary", "Lương giờ", FormatCurrency(salaryRecord.HourlySalary));
        AddDetailRow(rows, "created-at", "Ngày tạo", FormatDateTime(salaryRecord.CreatedAtUtc));
        AddDetailRow(rows, "updated-at", "Cập nhật lần cuối", FormatDateTime(salaryRecord.UpdatedAtUtc));

        return rows;
    }

    private static void AddDetailRow(
        ICollection<BasicSalaryInfoRow> rows,
        string key,
        string information,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        rows.Add(new BasicSalaryInfoRow(key, information, value.Trim()));
    }

    private string FormatCurrency(decimal value) =>
        value.ToString("n2", DisplayCulture);

    private string FormatNumber(decimal value) =>
        value.ToString("n2", DisplayCulture);

    private static string BuildSyncFromPreviousMonthSummaryMessage(SyncBasicSalaryFromPreviousMonthResult result)
    {
        return
            $"Đã lấy dữ liệu lương căn bản từ tháng {result.SourceMonth:00}/{result.SourceYear:0000} sang tháng {result.TargetMonth:00}/{result.TargetYear:0000}. " +
            $"Thêm mới: {result.CreatedRecordCount:N0}, cập nhật: {result.UpdatedRecordCount:N0}, giữ nguyên: {result.UnchangedRecordCount:N0}.";
    }

    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = FormatOptional(value);
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var searchText = SearchText.Trim();
        if (searchText.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var startIndex = 0;
        var builder = new StringBuilder(displayText.Length + 32);
        while (true)
        {
            var matchIndex = displayText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                break;
            }

            builder.Append(WebUtility.HtmlEncode(displayText[startIndex..matchIndex]));
            builder.Append("<mark class=\"basic-salary-search-highlight\">");
            builder.Append(WebUtility.HtmlEncode(displayText.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            startIndex = matchIndex + searchText.Length;
        }

        if (builder.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        builder.Append(WebUtility.HtmlEncode(displayText[startIndex..]));
        return new MarkupString(builder.ToString());
    }

    private BasicSalaryFilter BuildListFilter() => new(SearchText);

    private bool IsVisibleSalaryRecord(BasicSalaryRecord record) =>
        VisibleSalaryRecords.Any(row => row.Id == record.Id);

    private static string FormatOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Chưa có" : value.Trim();

    private static void NormalizeEditModel(BasicSalaryRecord model)
    {
        model.EmployeeCode = NormalizeNullable(model.EmployeeCode);
        model.EmployeeName = NormalizeNullable(model.EmployeeName);
        model.DepartmentName = NormalizeNullable(model.DepartmentName);
        model.DepartmentPath = NormalizeNullable(model.DepartmentPath);
        model.PositionName = NormalizeNullable(model.PositionName);

        if (model.EmployeeId == Guid.Empty)
        {
            model.EmployeeId = null;
        }
    }

    private static void CalculateDerivedSalaryValues(BasicSalaryRecord model)
    {
        if (model.BasicSalary > 0 && model.StandardWorkingDays > 0 && model.DailySalary <= 0)
        {
            model.DailySalary = decimal.Round(
                model.BasicSalary / model.StandardWorkingDays,
                4,
                MidpointRounding.AwayFromZero);
        }

        if (model.DailySalary > 0 && model.HourlySalary <= 0)
        {
            model.HourlySalary = decimal.Round(
                model.DailySalary / 8m,
                4,
                MidpointRounding.AwayFromZero);
        }
    }

    private static void InitializeNewSalaryRecordDefaults(BasicSalaryRecord model)
    {
        var utcNow = DateTime.UtcNow;
        var today = DateTime.Today;

        model.Id = Guid.NewGuid();
        model.EmployeeId = null;
        model.EmployeeCode = null;
        model.EmployeeName = null;
        model.DepartmentName = null;
        model.DepartmentPath = null;
        model.PositionName = null;
        model.PayrollMonth = today.Month;
        model.PayrollYear = today.Year;
        model.BasicSalary = 0;
        model.StandardWorkingDays = 26m;
        model.DailySalary = 0;
        model.HourlySalary = 0;
        model.CreatedAtUtc = utcNow;
        model.UpdatedAtUtc = utcNow;
    }

    private static BasicSalaryRecord CloneSalaryRecord(BasicSalaryRecord source) => new()
    {
        Id = source.Id,
        EmployeeId = source.EmployeeId,
        EmployeeCode = source.EmployeeCode,
        EmployeeName = source.EmployeeName,
        DepartmentName = source.DepartmentName,
        DepartmentPath = source.DepartmentPath,
        PositionName = source.PositionName,
        PayrollMonth = source.PayrollMonth,
        PayrollYear = source.PayrollYear,
        BasicSalary = source.BasicSalary,
        StandardWorkingDays = source.StandardWorkingDays,
        DailySalary = source.DailySalary,
        HourlySalary = source.HourlySalary,
        CreatedAtUtc = source.CreatedAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc
    };

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }
}
