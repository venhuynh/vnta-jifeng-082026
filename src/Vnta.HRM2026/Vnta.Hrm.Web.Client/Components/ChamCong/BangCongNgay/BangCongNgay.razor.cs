using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.ChamCong.BangCongNgay;

public partial class BangCongNgay : IDisposable
{
    #region Constants

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private const int SearchResultLimit = 2000;
    private const string SummaryAllKey = "all";
    private const string SummaryAttendanceKey = "attendance";
    private const string SummaryOvertimeRegistrationKey = "overtime-registration";
    private const string SummaryStatusKeyPrefix = "status:";
    private const string EmptySummaryStatusKey = "(empty)";

    #endregion

    #region Dependencies

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);

    [Inject]
    private IAttendanceWorkdaySummaryReadService AttendanceWorkdaySummaryReadService { get; set; } = default!;

    [Inject]
    private IAttendanceWorkdaySummaryService AttendanceWorkdaySummaryService { get; set; } = default!;

    [Inject]
    private IHrmDialogService DialogService { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    [Inject]
    private HrmOperationExecutor OperationExecutor { get; set; } = default!;

    #endregion

    #region State

    private IReadOnlyList<AttendanceWorkdaySummaryRecord> Summaries { get; set; } = [];
    private IReadOnlyList<WorkdaySummaryBadge> SummaryBadges { get; set; } = BuildSummaryBadges([]);
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private HashSet<Guid> BusyRowActionIds { get; } = [];
    private IGrid? Grid { get; set; }
    private string ActiveSummaryBadgeKey { get; set; } = SummaryAllKey;
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private DateTime? ToolbarDate { get; set; } = DateTime.Today;
    private DateOnly AppliedWorkDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    private AttendanceWorkdaySummaryRecord? DetailSummary { get; set; }
    private bool IsLoading { get; set; }
    private bool IsReloadingSummaries { get; set; }
    private bool IsRebuildingWorkdaySummaries { get; set; }
    private bool IsChangingPageSize { get; set; }
    private bool IsChangingLockState { get; set; }
    private bool IsDetailPopupVisible { get; set; }
    private bool IsRebuildConfirmPopupVisible { get; set; }
    private bool HasRequestedData { get; set; }
    private bool HasLoadedOnce { get; set; }
    private int PageSize { get; set; } = 50;
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;

    #endregion

    #region Derived State

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool ShowLoadingPanel =>
        IsLoading || IsReloadingSummaries || IsRebuildingWorkdaySummaries || IsChangingPageSize || IsChangingLockState;
    private bool CanInteract => !ShowLoadingPanel;
    private bool CanChangeFilters => CanInteract;
    private bool CanRefreshData => CanInteract;
    private bool HasPendingWorkDateChange =>
        HasRequestedData && GetPendingWorkDate() != AppliedWorkDate;
    private bool CanOperateOnCurrentDataset =>
        CanInteract && HasRequestedData && !HasLoadError && !HasPendingWorkDateChange;
    private bool CanRebuildWorkdaySummaries => CanOperateOnCurrentDataset;
    private bool CanChangeLockState => CanOperateOnCurrentDataset && VisibleSummaries.Count > 0;
    private bool CanLockDisplayedSummaries => CanChangeLockState && GetLockActionTargets().Any(summary => !summary.IsLocked);
    private bool CanUnlockDisplayedSummaries => CanChangeLockState && GetLockActionTargets().Any(summary => summary.IsLocked);
    private bool CanExport => !ShowLoadingPanel && !HasLoadError && VisibleSummaries.Count > 0;
    private bool CanExportSelected => CanExport && GetSelectedSummaryCount() > 0;
    private string AppliedWorkDateLabel => FormatDate(AppliedWorkDate);
    private IReadOnlyList<AttendanceWorkdaySummaryRecord> VisibleSummaries =>
        FilterSummariesByBadge(Summaries, ActiveSummaryBadgeKey);
    private int TotalOvertimeMinutes => Summaries.Sum(summary => Math.Max(0, summary.OvertimeMinutes));
    private int TotalLateEarlyMinutes => Summaries.Sum(summary => summary.LateEarlyTotalMinutes);
    private bool ShowInitialEmptyState => !HasRequestedData;
    private string EmptyStateTitle => ShowInitialEmptyState
        ? "Chưa tải bảng công ngày"
        : !string.IsNullOrWhiteSpace(SearchText)
        ? "Không tìm thấy kết quả chấm công phù hợp"
        : ActiveSummaryBadgeKey == SummaryAllKey
            ? "Chưa có kết quả chấm công trong ngày đã chọn"
            : "Không có kết quả chấm công ở nhóm đã chọn";
    private string EmptyStateMessage => ShowInitialEmptyState
        ? $"Chọn ngày công rồi bấm Xem để tải dữ liệu ngày {FormatDate(GetPendingWorkDate())}."
        : !string.IsNullOrWhiteSpace(SearchText)
        ? "Hãy thử từ khóa khác hoặc xóa tìm kiếm để xem thêm dữ liệu."
        : ActiveSummaryBadgeKey == SummaryAllKey
            ? $"Không có dữ liệu chấm công cho ngày {FormatDate(AppliedWorkDate)} theo bộ lọc hiện tại."
            : "Hãy chuyển sang nhóm kết quả khác hoặc tải lại danh sách để xem thêm dữ liệu.";
    private string EmptyStateActionText => ShowInitialEmptyState
        ? "Xem dữ liệu"
        : !string.IsNullOrWhiteSpace(SearchText)
        ? "Xóa tìm kiếm"
        : ActiveSummaryBadgeKey == SummaryAllKey
            ? "Tải lại"
            : "Xem tất cả";
    private string LoadingPanelText => IsRebuildingWorkdaySummaries
        ? "Đang tính công dữ liệu bảng công ngày..."
        : IsChangingPageSize
            ? "Đang cập nhật số dòng hiển thị..."
            : HasLoadedOnce
                ? "Đang cập nhật bảng công ngày..."
                : "Đang tải bảng công ngày...";

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
        var isInitialLoad = !HasRequestedData;
        HasRequestedData = true;
        LoadErrorMessage = null;
        AppliedWorkDate = GetPendingWorkDate();

        if (isInitialLoad)
        {
            IsLoading = true;
        }
        else
        {
            IsReloadingSummaries = true;
        }

        try
        {
            await ClearSelectionAsync();
            var outcome = await OperationExecutor.ExecuteAsync(
                cancellationToken => AttendanceWorkdaySummaryReadService.SearchAsync(BuildFilter(), cancellationToken),
                "Không thể tải dữ liệu bảng công ngày. Vui lòng thử lại.",
                disposalTokenSource.Token,
                showFailureToast: false);

            if (!outcome.Succeeded)
            {
                if (outcome.Status == HrmOperationStatus.Canceled)
                {
                    return;
                }

                Summaries = [];
                SummaryBadges = BuildSummaryBadges([]);
                DetailSummary = null;
                IsDetailPopupVisible = false;
                LoadErrorMessage = outcome.Message ?? "Có lỗi khi tải dữ liệu bảng công ngày. Vui lòng thử lại.";
                return;
            }

            var rows = outcome.Value ?? [];
            Summaries = rows.Select(MapRecord).ToList();
            SummaryBadges = BuildSummaryBadges(Summaries);
            HasLoadedOnce = true;
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
            Summaries = [];
            SummaryBadges = BuildSummaryBadges([]);
            DetailSummary = null;
            IsDetailPopupVisible = false;
            LoadErrorMessage = "Có lỗi khi tải dữ liệu bảng công ngày. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải bảng công ngày.");
        }
        finally
        {
            if (isInitialLoad)
            {
                IsLoading = false;
            }
            else
            {
                IsReloadingSummaries = false;
            }
        }
    }

    #endregion

    #region Screen Actions

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = NormalizeSearchText(value);
        if (string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        SearchText = normalizedValue;
        return ReloadAsync();
    }

    private Task OnToolbarDateChanged(DateTime? value)
    {
        ToolbarDate = value?.Date ?? DateTime.Today;
        return Task.CompletedTask;
    }

    private void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

    private async Task SelectSummaryAsync(string badgeKey)
    {
        if (!CanChangeFilters || string.Equals(badgeKey, ActiveSummaryBadgeKey, StringComparison.Ordinal))
        {
            return;
        }

        ActiveSummaryBadgeKey = badgeKey;
        await ClearSelectionAsync();
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

    private Task OnRebuildWorkdaySummaryClickAsync()
    {
        if (!CanRebuildWorkdaySummaries)
        {
            return Task.CompletedTask;
        }

        IsRebuildConfirmPopupVisible = true;
        return Task.CompletedTask;
    }

    private Task OnRebuildConfirmPopupVisibleChangedAsync(bool visible)
    {
        IsRebuildConfirmPopupVisible = visible;
        return Task.CompletedTask;
    }

    private async Task ConfirmRebuildWorkdaySummaryAsync()
    {
        IsRebuildConfirmPopupVisible = false;

        if (disposalTokenSource.IsCancellationRequested || !CanRebuildWorkdaySummaries)
        {
            return;
        }

        LoadErrorMessage = null;
        IsRebuildingWorkdaySummaries = true;

        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            await ClearSelectionAsync();

            var outcome = await OperationExecutor.ExecuteAsync(
                cancellationToken => AttendanceWorkdaySummaryService.RebuildAsync(
                    new RebuildAttendanceWorkdaySummaryRequest(AppliedWorkDate),
                    cancellationToken),
                "Không thể tính công ngày đang hiển thị.",
                disposalTokenSource.Token);

            if (!outcome.Succeeded)
            {
                return;
            }

            var result = outcome.Value!;

            ToastService.ShowSuccess(
                $"Đã tính công cho {result.UpdatedSummaryCount:N0} nhân viên từ {result.TotalPunchCount:N0} lượt chấm công, giữ nguyên {result.SkippedLockedCount:N0} dòng đã khóa.");

            await ReloadAsync();
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
            ToastService.ShowWarning(ex.Message);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể tính công ngày đang hiển thị.");
        }
        finally
        {
            IsRebuildingWorkdaySummaries = false;
        }
    }

    private async Task SetDisplayedLockStateAsync(bool isLocked)
    {
        if (!CanChangeLockState || IsChangingLockState)
        {
            return;
        }

        var selectedSummaries = GetSelectedSummaries();
        var targetSummaries = GetLockActionTargets();
        var scopeText = selectedSummaries.Count > 0 ? "các dòng đang chọn" : "toàn bộ dữ liệu đang hiển thị";
        var actionText = isLocked ? "khóa" : "mở khóa";

        var confirmed = await DialogService.ConfirmAsync(
            $"Bạn có chắc muốn {actionText} {targetSummaries.Count:N0} dòng bảng công ngày ({scopeText}) của ngày {FormatDate(AppliedWorkDate)}?",
            title: isLocked ? "Khóa bảng công ngày" : "Mở khóa bảng công ngày",
            okText: isLocked ? "Khóa" : "Mở khóa",
            cancelText: "Hủy",
            renderStyle: isLocked ? MessageBoxRenderStyle.Warning : MessageBoxRenderStyle.Primary);

        if (!confirmed)
        {
            return;
        }

        IsChangingLockState = true;
        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var changedCount = 0;
            foreach (var summary in targetSummaries)
            {
                if (summary.IsLocked == isLocked)
                {
                    continue;
                }

                await AttendanceWorkdaySummaryService.SetLockStateAsync(
                    new SetAttendanceWorkdaySummaryLockStateRequest(summary.Id, isLocked),
                    disposalTokenSource.Token);
                summary.IsLocked = isLocked;
                changedCount++;
            }

            await ClearSelectionAsync();
            ToastService.ShowSuccess(
                changedCount == 0
                    ? $"Không có dòng nào cần {actionText}."
                    : $"Đã {actionText} {changedCount:N0}/{targetSummaries.Count:N0} dòng bảng công ngày.");
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
            ToastService.ShowWarning(ex.Message);
        }
        catch (Exception)
        {
            ToastService.ShowError($"Không thể {actionText} bảng công ngày.");
        }
        finally
        {
            IsChangingLockState = false;
        }
    }

    private Task OpenDetailPopupAsync(AttendanceWorkdaySummaryRecord summary)
    {
        DetailSummary = summary;
        IsDetailPopupVisible = true;
        return Task.CompletedTask;
    }

    private Task OnDetailSummarySaved(AttendanceWorkdaySummaryRecord updatedSummary)
    {
        Summaries = Summaries
            .Select(summary => summary.Id == updatedSummary.Id ? updatedSummary : summary)
            .ToList();
        SummaryBadges = BuildSummaryBadges(Summaries);
        SelectedDataItems = SelectedDataItems
            .Select(item => item is AttendanceWorkdaySummaryRecord summary && summary.Id == updatedSummary.Id
                ? updatedSummary
                : item)
            .Cast<object>()
            .ToList();
        DetailSummary = updatedSummary;

        return Task.CompletedTask;
    }

    private async Task DeleteSummaryAsync(AttendanceWorkdaySummaryRecord summary)
    {
        if (disposalTokenSource.IsCancellationRequested || !CanInteract)
        {
            return;
        }

        if (summary.IsLocked)
        {
            ToastService.ShowWarning("Dòng bảng công ngày đã khóa, không thể xóa.");
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            $"Bạn có chắc muốn xóa dòng bảng công ngày của {summary.EmployeeDisplay} ngày {FormatDate(summary.WorkDate)}?",
            title: "Xóa dòng bảng công ngày",
            okText: "Xóa",
            cancelText: "Hủy",
            renderStyle: MessageBoxRenderStyle.Danger);

        if (!confirmed)
        {
            return;
        }

        if (!BusyRowActionIds.Add(summary.Id))
        {
            return;
        }

        try
        {
            await InvokeAsync(StateHasChanged);
            await AttendanceWorkdaySummaryService.DeleteAsync([summary.Id], disposalTokenSource.Token);
            RemoveSummaryFromState(summary.Id);
            ToastService.ShowSuccess($"Đã xóa dòng bảng công ngày của {summary.EmployeeDisplay}.");
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
            ToastService.ShowWarning(ex.Message);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xóa dòng bảng công ngày.");
        }
        finally
        {
            BusyRowActionIds.Remove(summary.Id);
        }
    }

    private async Task ToggleSummaryLockStateAsync(AttendanceWorkdaySummaryRecord summary)
    {
        if (disposalTokenSource.IsCancellationRequested || !CanInteract)
        {
            return;
        }

        if (!BusyRowActionIds.Add(summary.Id))
        {
            return;
        }

        var nextLockedState = !summary.IsLocked;
        var previousLockedState = summary.IsLocked;
        summary.IsLocked = nextLockedState;

        try
        {
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            await AttendanceWorkdaySummaryService.SetLockStateAsync(
                new SetAttendanceWorkdaySummaryLockStateRequest(summary.Id, nextLockedState),
                disposalTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            summary.IsLocked = previousLockedState;
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            summary.IsLocked = previousLockedState;
            ToastService.ShowWarning(ex.Message);
        }
        catch (Exception)
        {
            summary.IsLocked = previousLockedState;
            ToastService.ShowError("Không thể cập nhật trạng thái khóa của bảng công ngày.");
        }
        finally
        {
            BusyRowActionIds.Remove(summary.Id);
        }
    }

    private Task ExportAllDataToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync("attendance-workday-summaries"),
        "Đã bắt đầu xuất Excel cho bảng công ngày.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(
            "attendance-workday-summaries-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất Excel cho các dòng đang chọn.");

    private Task ExportAllDataToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync("attendance-workday-summaries"),
        "Đã bắt đầu xuất PDF cho bảng công ngày.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync(
            "attendance-workday-summaries-selected",
            new GridPdfExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất PDF cho các dòng đang chọn.");

    private async Task ExportAsync(Func<Task> exportAction, string successMessage)
    {
        if (Grid is null)
        {
            ToastService.ShowWarning("Bảng công ngày chưa sẵn sàng để xuất.");
            return;
        }

        try
        {
            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất bảng công ngày.");
        }
    }

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];
        DetailSummary = null;
        IsDetailPopupVisible = false;

        if (Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private async Task OnEmptyStateActionClick()
    {
        if (ShowInitialEmptyState)
        {
            await ReloadAsync();
            return;
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            SearchText = null;
            await ReloadAsync();
            return;
        }

        if (ActiveSummaryBadgeKey != SummaryAllKey)
        {
            ActiveSummaryBadgeKey = SummaryAllKey;
            await ClearSelectionAsync();
            return;
        }

        await ReloadAsync();
    }

    #endregion

    #region Query And Mapping Helpers

    private List<AttendanceWorkdaySummaryRecord> GetSelectedSummaries()
    {
        var visibleIds = VisibleSummaries.Select(summary => summary.Id).ToHashSet();

        return SelectedDataItems
            .OfType<AttendanceWorkdaySummaryRecord>()
            .Where(summary => visibleIds.Contains(summary.Id))
            .DistinctBy(summary => summary.Id)
            .ToList();
    }

    private int GetSelectedSummaryCount() => GetSelectedSummaries().Count;

    private List<AttendanceWorkdaySummaryRecord> GetLockActionTargets()
    {
        var selectedSummaries = GetSelectedSummaries();
        return selectedSummaries.Count > 0
            ? selectedSummaries
            : VisibleSummaries.ToList();
    }

    private bool CanUseRowAction(AttendanceWorkdaySummaryRecord summary) =>
        CanInteract && !BusyRowActionIds.Contains(summary.Id);

    private bool CanDeleteSummary(AttendanceWorkdaySummaryRecord summary) =>
        CanUseRowAction(summary) && !summary.IsLocked;

    private static string GetEditActionLabel() => "Sửa dòng công";

    private static string GetDeleteActionLabel(AttendanceWorkdaySummaryRecord summary) =>
        summary.IsLocked
            ? "Dòng đã khóa, không thể xóa"
            : "Xóa dòng công";

    private string GetLockActionIconUrl(AttendanceWorkdaySummaryRecord summary) =>
        summary.IsLocked
            ? VntaDevExpressIcons.Unlock
            : VntaDevExpressIcons.Lock;

    private string GetLockActionLabel(AttendanceWorkdaySummaryRecord summary) =>
        summary.IsLocked
            ? "Mở khóa dòng công"
            : "Khóa dòng công";

    private void RemoveSummaryFromState(Guid summaryId)
    {
        Summaries = Summaries
            .Where(summary => summary.Id != summaryId)
            .ToList();
        SummaryBadges = BuildSummaryBadges(Summaries);
        SelectedDataItems = SelectedDataItems
            .OfType<AttendanceWorkdaySummaryRecord>()
            .Where(summary => summary.Id != summaryId)
            .Cast<object>()
            .ToList();

        if (DetailSummary?.Id == summaryId)
        {
            DetailSummary = null;
            IsDetailPopupVisible = false;
        }
    }

    private AttendanceWorkdaySummaryFilter BuildFilter() =>
        new(
            AppliedWorkDate,
            AppliedWorkDate,
            SearchText,
            SearchResultLimit);

    private DateOnly GetDefaultWorkDate() =>
        DateOnly.FromDateTime(DateTime.Today);

    private DateOnly GetPendingWorkDate() =>
        DateOnly.FromDateTime((ToolbarDate ?? DateTime.Today).Date);

    private static string? NormalizeSearchText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = GetDisplayValue(value);
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
            builder.Append("<mark class=\"bang-cong-ngay-search-highlight\">");
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

    private string FormatDate(DateOnly value) => value.ToString("dd/MM/yyyy", DisplayCulture);

    private string FormatOptionalMinutes(int value) => value <= 0
        ? string.Empty
        : value.ToString("N0", DisplayCulture);

    private string FormatHours(int minutes) =>
        (Math.Max(0, minutes) / 60m).ToString("0.##", DisplayCulture);

    private static string GetResultTextCssClass(string? status) => status switch
    {
        "FULL_WORK" => "result-text result-text-success hrm-grid-status",
        "VR" => "result-text result-text-success hrm-grid-status",
        "MISSING_LOG" => "result-text result-text-warning hrm-grid-status",
        "LATE_EARLY" => "result-text result-text-warning hrm-grid-status",
        "TS" => "result-text result-text-warning hrm-grid-status",
        "ABNORMAL" => "result-text result-text-danger hrm-grid-status",
        "KP" => "result-text result-text-danger hrm-grid-status",
        _ => "result-text result-text-neutral hrm-grid-status"
    };

    private static string GetDisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();

    private static IReadOnlyList<AttendanceWorkdaySummaryRecord> FilterSummariesByBadge(
        IReadOnlyList<AttendanceWorkdaySummaryRecord> summaries,
        string badgeKey) =>
        summaries.Where(summary => MatchesSummaryBadge(summary, badgeKey)).ToList();

    private static IReadOnlyList<WorkdaySummaryBadge> BuildSummaryBadges(
        IReadOnlyList<AttendanceWorkdaySummaryRecord> summaries)
    {
        var statusBadges = summaries
            .GroupBy(summary => NormalizeSummaryStatusValue(summary.Status), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => GetStatusSortOrder(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new WorkdaySummaryBadge(
                $"{SummaryStatusKeyPrefix}{group.Key}",
                GetDisplayStatusValue(group.Key),
                group.Count(),
                $"Lọc các dòng có Kết quả: {GetDisplayStatusValue(group.Key)}"));

        return
        [
            new(SummaryAllKey, "Tất cả", summaries.Count, "Hiển thị tất cả dòng bảng công ngày"),
            new(SummaryAttendanceKey, "Có chấm công", summaries.Count(HasAttendance), "Lọc các dòng có giờ vào hoặc giờ ra"),
            .. statusBadges,
            new(SummaryOvertimeRegistrationKey, "Có ĐKTC", summaries.Count(summary => summary.IsRegisterForOT), "Lọc các dòng đã đăng ký tăng ca")
        ];
    }

    private static bool MatchesSummaryBadge(
        AttendanceWorkdaySummaryRecord summary,
        string badgeKey)
    {
        if (string.Equals(badgeKey, SummaryAllKey, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(badgeKey, SummaryAttendanceKey, StringComparison.Ordinal))
        {
            return HasAttendance(summary);
        }

        if (string.Equals(badgeKey, SummaryOvertimeRegistrationKey, StringComparison.Ordinal))
        {
            return summary.IsRegisterForOT;
        }

        return badgeKey.StartsWith(SummaryStatusKeyPrefix, StringComparison.Ordinal)
            && string.Equals(
                NormalizeSummaryStatusValue(summary.Status),
                badgeKey[SummaryStatusKeyPrefix.Length..],
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAttendance(AttendanceWorkdaySummaryRecord summary) =>
        !string.IsNullOrWhiteSpace(summary.CheckInAt) || !string.IsNullOrWhiteSpace(summary.CheckOutAt);

    private static string NormalizeSummaryStatusValue(string? status) =>
        string.IsNullOrWhiteSpace(status)
            ? EmptySummaryStatusKey
            : status.Trim().ToUpperInvariant();

    private static string GetDisplayStatusValue(string status) =>
        string.Equals(status, EmptySummaryStatusKey, StringComparison.Ordinal) ? "--" : status;

    private static int GetStatusSortOrder(string status) => status switch
    {
        "FULL_WORK" => 0,
        "VR" => 1,
        "LATE_EARLY" => 2,
        "MISSING_LOG" => 3,
        "TS" => 4,
        "ABNORMAL" => 5,
        "KP" => 6,
        EmptySummaryStatusKey => int.MaxValue,
        _ => 100
    };

    private static string GetSummaryButtonCssClass(string badgeKey)
    {
        var cssClass = "bang-cong-ngay-summary-button";
        if (string.Equals(badgeKey, SummaryAllKey, StringComparison.Ordinal))
        {
            return $"{cssClass} bang-cong-ngay-summary-button--all";
        }

        if (string.Equals(badgeKey, SummaryAttendanceKey, StringComparison.Ordinal))
        {
            return $"{cssClass} bang-cong-ngay-summary-button--attendance";
        }

        if (string.Equals(badgeKey, SummaryOvertimeRegistrationKey, StringComparison.Ordinal))
        {
            return $"{cssClass} bang-cong-ngay-summary-button--overtime-registration";
        }

        var status = badgeKey.StartsWith(SummaryStatusKeyPrefix, StringComparison.Ordinal)
            ? badgeKey[SummaryStatusKeyPrefix.Length..]
            : string.Empty;
        return $"{cssClass} {GetStatusSummaryButtonCssClass(status)}";
    }

    private static string GetStatusSummaryButtonCssClass(string status) => status switch
    {
        "FULL_WORK" or "VR" => "bang-cong-ngay-summary-button--success",
        "MISSING_LOG" or "LATE_EARLY" or "TS" => "bang-cong-ngay-summary-button--warning",
        "ABNORMAL" or "KP" => "bang-cong-ngay-summary-button--danger",
        _ => "bang-cong-ngay-summary-button--neutral"
    };

    private static string GetShiftBadgeCssClass(AttendanceWorkdaySummaryRecord summary) =>
        summary.ShiftShortDisplay == "--"
            ? "shift-text shift-text-empty hrm-grid-status"
            : "shift-text hrm-grid-status";

    private static string? GetShiftBadgeStyle(AttendanceWorkdaySummaryRecord summary)
    {
        if (summary.ShiftShortDisplay == "--"
            || !TryNormalizeHexColor(summary.ShiftColorHex, out var backgroundColor))
        {
            return null;
        }

        return $"color: {backgroundColor};";
    }

    private static bool TryNormalizeHexColor(string? value, out string normalizedValue)
    {
        normalizedValue = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmedValue = value.Trim();
        if (trimmedValue.Length != 7 || trimmedValue[0] != '#')
        {
            return false;
        }

        if (!int.TryParse(trimmedValue.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        normalizedValue = trimmedValue.ToUpperInvariant();
        return true;
    }

    private static AttendanceWorkdaySummaryRecord MapRecord(AttendanceWorkdaySummaryListItemDto row) =>
        new()
        {
            Id = row.Id,
            EmployeeId = row.EmployeeId,
            EmployeeCode = row.EmployeeCode,
            EmployeeName = row.EmployeeName,
            DepartmentName = row.DepartmentName,
            PositionName = row.PositionName,
            WorkDate = row.WorkDate,
            DayType = row.DayType,
            ShiftId = row.ShiftId,
            ShiftCode = row.ShiftCode,
            ShiftShortName = row.ShiftShortName,
            ShiftName = row.ShiftName,
            ShiftColorHex = row.ShiftColorHex,
            ScheduledStartAt = row.ScheduledStartAt,
            ScheduledEndAt = row.ScheduledEndAt,
            CheckInAt = row.CheckInAt,
            CheckOutAt = row.CheckOutAt,
            LateMinutes = row.LateMinutes,
            EarlyLeaveMinutes = row.EarlyLeaveMinutes,
            Status = row.Status,
            IsLocked = row.IsLocked,
            OvertimeMinutes = row.OvertimeMinutes,
            OvertimeMinutes15 = row.OvertimeMinutes15,
            OvertimeMinutes20 = row.OvertimeMinutes20,
            OvertimeMinutes30 = row.OvertimeMinutes30,
            CheckInForOT15 = row.CheckInForOT15,
            IsRegisterForOT = row.IsRegisterForOT,
            RequireDocument = row.RequireDocument,
            Note = row.Note,
            ComputedAtUtc = row.ComputedAtUtc,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedAtUtc = row.UpdatedAtUtc
        };

    #endregion

    #region Disposal

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
        reloadGate.Dispose();
    }

    private sealed record WorkdaySummaryBadge(string Key, string Label, int Count, string Tooltip);

    #endregion
}
