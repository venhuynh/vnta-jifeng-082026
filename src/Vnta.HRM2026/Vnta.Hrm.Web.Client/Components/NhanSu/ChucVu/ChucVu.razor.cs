using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Text;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.NhanSu.ChucVu;

public partial class ChucVu : IDisposable
{
    #region Constants

    private const string SummaryUsedKey = "used";
    private const string SummaryUnusedKey = "unused";
    private const string SummaryAllKey = "all";

    private sealed record PositionSummaryBadge(string Key, string Label, int Count);

    #endregion

    #region Dependencies

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);

    [Inject]
    private AttendancePositionDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    #endregion

    #region State

    private IReadOnlyList<AttendancePositionRecord> AllPositions { get; set; } = [];
    private IReadOnlyList<AttendancePositionRecord> VisiblePositions { get; set; } = [];
    private IReadOnlyList<PositionSummaryBadge> SummaryBadges { get; set; } = BuildSummaryBadges([]);
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private string ActiveSummaryBadgeKey { get; set; } = SummaryAllKey;
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private string CurrentLoadingText { get; set; } = HrmUiDefaults.LoadingText;
    private bool IsLoading { get; set; } = true;
    private bool IsRefreshing { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool IsSavingPosition { get; set; }
    private bool IsCreatingNewPosition { get; set; }
    private int PageSize { get; set; } = 50;
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;

    #endregion

    #region Derived State

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool CanInteract => !IsLoading && !IsRefreshing && !IsChangingPageSize && !IsSavingPosition && !HasLoadError;
    private bool CanCreate => CanInteract;
    private bool CanRefreshPositions => !IsLoading && !IsRefreshing && !IsChangingPageSize && !IsSavingPosition;
    private bool CanEditSelected => CanInteract && GetSelectedPositionCount() == 1;
    private bool CanDeleteSelected => CanInteract && GetSelectedPositionCount() > 0;
    private bool CanExport => !IsLoading && !IsRefreshing && !IsChangingPageSize && !IsSavingPosition && VisiblePositions.Count > 0;
    private bool CanExportSelected => CanExport && GetSelectedPositionCount() > 0;
    private bool ShowLoadingPanel => IsLoading || IsRefreshing || IsChangingPageSize || IsSavingPosition;
    private string LoadingText => CurrentLoadingText;
    private string EmptyStateTitle => !string.IsNullOrWhiteSpace(SearchText)
        ? "Không tìm thấy chức vụ phù hợp"
        : ActiveSummaryBadgeKey == SummaryAllKey
            ? "Chưa có chức vụ"
            : "Không có chức vụ trong nhóm đã chọn";
    private string EmptyStateMessage => !string.IsNullOrWhiteSpace(SearchText)
        ? "Hãy thử từ khóa khác hoặc xóa tìm kiếm để xem thêm dữ liệu."
        : ActiveSummaryBadgeKey == SummaryAllKey
            ? "Danh sách chức vụ sẽ hiển thị tại đây sau khi dữ liệu master `positions` được đồng bộ vào PostgreSQL đích."
            : "Hãy chuyển sang nhóm khác hoặc tải lại danh sách để xem thêm dữ liệu.";
    private string EmptyStateActionText => !string.IsNullOrWhiteSpace(SearchText)
        ? "Xóa tìm kiếm"
        : ActiveSummaryBadgeKey == SummaryAllKey
            ? "Tạo chức vụ"
            : "Xem tất cả";

    #endregion

    #region UI Entry Points

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ReloadAsync();
            await InvokeAsync(StateHasChanged);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    #endregion

    #region Data Loading

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
        CurrentLoadingText = "Đang tải dữ liệu chức vụ...";

        try
        {
            await ClearSelectionAsync();
            var positions = await DataProvider.GetAsync(disposalTokenSource.Token);
            UpdateLoadedPositions(positions);
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
            AllPositions = [];
            VisiblePositions = [];
            LoadErrorMessage = "Có lỗi khi tải dữ liệu chức vụ. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách chức vụ.");
        }
        finally
        {
            IsLoading = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private async Task RefreshEmployeeCountsAsync()
    {
        if (disposalTokenSource.IsCancellationRequested || !CanRefreshPositions)
        {
            return;
        }

        LoadErrorMessage = null;
        IsRefreshing = true;
        CurrentLoadingText = "Đang cập nhật số nhân viên còn làm việc...";

        try
        {
            await ClearSelectionAsync();
            await DataProvider.RefreshEmployeeCountsAsync(disposalTokenSource.Token);
            var positions = await DataProvider.GetAsync(disposalTokenSource.Token);
            UpdateLoadedPositions(positions);
            ToastService.ShowSuccess("Đã cập nhật số nhân viên còn làm việc cho chức vụ.");
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
            ToastService.ShowError("Không thể cập nhật số nhân viên còn làm việc cho chức vụ.");
        }
        finally
        {
            IsRefreshing = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    #endregion

    #region Screen Actions

    private async Task OnAddPositionClick()
    {
        if (!CanCreate || Grid is null)
        {
            return;
        }

        EditErrorMessage = null;
        await Grid.StartEditNewRowAsync();
    }

    private async Task OnEditPositionClick()
    {
        if (Grid is null)
        {
            return;
        }

        var position = GetSingleSelectedPosition();
        if (position is null)
        {
            ToastService.ShowWarning("Hãy chọn đúng một chức vụ để điều chỉnh.");
            return;
        }

        EditErrorMessage = null;
        await Grid.StartEditDataItemAsync(position, nameof(AttendancePositionRecord.Name));
    }

    private Task OnDeletePositionsClick()
    {
        var selectedPositions = GetSelectedPositions();
        if (selectedPositions.Count == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một chức vụ để xóa.");
            return Task.CompletedTask;
        }

        ToastService.ShowWarning("Màn Chức vụ hiện chưa hỗ trợ xóa.");
        return Task.CompletedTask;
    }

    private async Task OnEmptyStateActionClick()
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            SearchText = null;
            await ClearSelectionAsync();
            ApplySearchFilter();
            return;
        }

        if (!string.Equals(ActiveSummaryBadgeKey, SummaryAllKey, StringComparison.Ordinal))
        {
            ActiveSummaryBadgeKey = SummaryAllKey;
            await ClearSelectionAsync();
            ApplySearchFilter();
            return;
        }

        await OnAddPositionClick();
    }

    private async Task OnSummaryBadgeClick(string badgeKey)
    {
        if (string.Equals(badgeKey, ActiveSummaryBadgeKey, StringComparison.Ordinal))
        {
            return;
        }

        ActiveSummaryBadgeKey = badgeKey;
        await ClearSelectionAsync();
        ApplySearchFilter();
    }

    private async Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = NormalizeNullable(value);
        if (string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalizedValue;
        await ClearSelectionAsync();
        ApplySearchFilter();
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private async Task OnPageSizeChanged(int value)
    {
        if (PageSize == value)
        {
            return;
        }

        IsChangingPageSize = true;
        CurrentLoadingText = "Đang cập nhật số dòng hiển thị...";
        PageSize = value;

        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }
        finally
        {
            IsChangingPageSize = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

    private async Task OnCancelPositionEditClick()
    {
        if (Grid is not null)
        {
            await Grid.CancelEditAsync();
        }
    }

    private void OnCustomizeEditModel(GridCustomizeEditModelEventArgs e)
    {
        EditErrorMessage = null;
        IsCreatingNewPosition = e.IsNew;

        var model = (AttendancePositionRecord)e.EditModel;
        if (e.IsNew)
        {
            InitializeNewPositionDefaults(model);
        }
    }

    private async Task OnEditModelSaving(GridEditModelSavingEventArgs e)
    {
        EditErrorMessage = null;

        try
        {
            var editModel = (AttendancePositionRecord)e.EditModel;
            NormalizeEditModel(editModel);

            var now = DateTime.UtcNow;
            if (editModel.Id == Guid.Empty)
            {
                editModel.Id = Guid.NewGuid();
            }

            if (editModel.CreatedAtUtc == default)
            {
                editModel.CreatedAtUtc = now;
            }

            editModel.UpdatedAtUtc = now;

            var validationMessage = await DataProvider.ValidateAsync(editModel, disposalTokenSource.Token);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                EditErrorMessage = validationMessage;
                e.Cancel = true;
                return;
            }

            IsSavingPosition = true;
            CurrentLoadingText = e.IsNew
                ? "Đang tạo chức vụ..."
                : "Đang cập nhật chức vụ...";

            var positions = await DataProvider.SaveAsync(editModel, e.IsNew, disposalTokenSource.Token);
            UpdateLoadedPositions(positions);
            e.Reload = false;
            await ClearSelectionAsync();
            ToastService.ShowSuccess(e.IsNew ? "Đã thêm chức vụ." : "Đã cập nhật chức vụ.");
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }

            e.Cancel = true;
        }
        catch (InvalidOperationException ex)
        {
            EditErrorMessage = ex.Message;
            e.Cancel = true;
            ToastService.ShowError("Không thể lưu chức vụ.");
        }
        catch (Exception)
        {
            EditErrorMessage = "Không thể lưu dữ liệu chức vụ. Vui lòng kiểm tra lại thông tin.";
            e.Cancel = true;
            ToastService.ShowError("Không thể lưu chức vụ.");
        }
        finally
        {
            IsSavingPosition = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    private Task ExportAllDataToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync("chuc-vu"),
        "Đã bắt đầu xuất Excel chức vụ.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(
            "chuc-vu-da-chon",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất Excel cho các dòng đã chọn.");

    private Task ExportAllDataToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync("chuc-vu"),
        "Đã bắt đầu xuất PDF chức vụ.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync(
            "chuc-vu-da-chon",
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
            ToastService.ShowError("Không thể xuất dữ liệu chức vụ.");
        }
    }

    #endregion

    #region Helpers

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];

        if (Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private void UpdateLoadedPositions(IReadOnlyList<AttendancePositionRecord> positions)
    {
        AllPositions = positions;
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        var searchScopedPositions = BuildSearchScopedPositions();
        SummaryBadges = BuildSummaryBadges(searchScopedPositions);
        VisiblePositions = ApplySummaryFilter(searchScopedPositions);
    }

    private IReadOnlyList<AttendancePositionRecord> BuildSearchScopedPositions()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return AllPositions;
        }

        var searchText = SearchText.Trim();
        return AllPositions
            .Where(position => MatchesSearch(position, searchText))
            .ToArray();
    }

    private IReadOnlyList<AttendancePositionRecord> ApplySummaryFilter(IReadOnlyList<AttendancePositionRecord> positions) =>
        ActiveSummaryBadgeKey switch
        {
            SummaryUsedKey => positions.Where(position => position.EmployeeCount > 0).ToArray(),
            SummaryUnusedKey => positions.Where(position => position.EmployeeCount == 0).ToArray(),
            _ => positions
        };

    private static IReadOnlyList<PositionSummaryBadge> BuildSummaryBadges(IReadOnlyList<AttendancePositionRecord> positions)
    {
        var usedCount = positions.Count(position => position.EmployeeCount > 0);
        var unusedCount = positions.Count - usedCount;

        return
        [
            new(SummaryUsedKey, "Đang sử dụng", usedCount),
            new(SummaryUnusedKey, "Chưa gán", unusedCount),
            new(SummaryAllKey, "Tất cả", positions.Count)
        ];
    }

    private static bool MatchesSearch(AttendancePositionRecord position, string searchText) =>
        ContainsSearchText(position.Name, searchText)
        || ContainsSearchText(position.Description, searchText);

    private static bool ContainsSearchText(string? source, string searchText) =>
        !string.IsNullOrWhiteSpace(source)
        && source.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(SearchText) || displayText.Length == 0)
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
            builder.Append("<mark class=\"chuc-vu-search-highlight\">");
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

    private List<AttendancePositionRecord> GetSelectedPositions() => SelectedDataItems.OfType<AttendancePositionRecord>().ToList();

    private AttendancePositionRecord? GetSingleSelectedPosition()
    {
        var selectedPositions = GetSelectedPositions();
        return selectedPositions.Count == 1 ? selectedPositions[0] : null;
    }

    private int GetSelectedPositionCount() => GetSelectedPositions().Count;

    private static void NormalizeEditModel(AttendancePositionRecord model)
    {
        model.Code = NormalizeNullable(model.Code);
        model.Name = NormalizeNullable(model.Name);
        model.Description = NormalizeNullable(model.Description);
    }

    private static void InitializeNewPositionDefaults(AttendancePositionRecord model)
    {
        var utcNow = DateTime.UtcNow;

        model.Id = Guid.NewGuid();
        model.Code = BuildInternalCode(model.Id);
        model.Name = string.Empty;
        model.Description = null;
        model.Status = 0;
        model.EmployeeCount = 0;
        model.CreatedAtUtc = utcNow;
        model.UpdatedAtUtc = utcNow;
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildInternalCode(Guid id) =>
        $"POS-{id.ToString("N")[..8].ToUpperInvariant()}";

    #endregion

    #region Disposal

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    #endregion
}
