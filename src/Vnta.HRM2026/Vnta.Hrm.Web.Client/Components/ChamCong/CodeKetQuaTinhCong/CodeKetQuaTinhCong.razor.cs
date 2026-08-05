using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.ChamCong.CodeKetQuaTinhCong;

public partial class CodeKetQuaTinhCong : IDisposable
{
    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private CancellationTokenSource? activeReloadTokenSource;
    private int reloadRequestedVersion;
    private int reloadProcessedVersion;
    private bool hasCompletedInitialLoad;

    [Inject]
    private AttendanceStatusCodeDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    private IReadOnlyList<AttendanceStatusCodeRecord> Records { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsRefreshing { get; set; }
    private bool IsExporting { get; set; }
    private bool IsSavingFlags { get; set; }
    private bool IsDeletingStatusCode { get; set; }
    private bool IsDetailVisible { get; set; }
    private AttendanceStatusCodeRecord? DetailRecord { get; set; }

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool ShowLoadingPanel => IsLoading || IsRefreshing || IsSavingFlags || IsDeletingStatusCode;
    private bool CanInteract => !ShowLoadingPanel && !IsExporting && !HasLoadError;
    private bool CanRefresh => !ShowLoadingPanel && !IsExporting;
    private bool CanExport => CanInteract && Records.Count > 0;
    private bool CanExportSelected => CanExport && GetSelectedRecords().Count > 0;
    private bool CanViewDetail => CanInteract && SelectedRecord is not null;
    private string LoadingText => IsDeletingStatusCode
        ? "Đang xóa mã kết quả tính công..."
        : IsSavingFlags
        ? "Đang cập nhật cờ phụ cấp/khấu trừ..."
        : IsRefreshing
        ? "Đang làm mới danh mục mã kết quả tính công..."
        : "Đang tải danh mục mã kết quả tính công...";
    private AttendanceStatusCodeRecord? SelectedRecord =>
        GetSelectedRecords().Count == 1 ? GetSelectedRecords()[0] : null;

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
        activeReloadTokenSource?.Cancel();

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
        IsLoading = !hasCompletedInitialLoad;
        IsRefreshing = hasCompletedInitialLoad;

        using var requestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        activeReloadTokenSource = requestTokenSource;

        try
        {
            await ClearSelectionAsync();
            var records = await DataProvider.GetAsync(requestTokenSource.Token);

            if (!requestTokenSource.IsCancellationRequested)
            {
                Records = records;
                hasCompletedInitialLoad = true;
            }
        }
        catch (OperationCanceledException) when (requestTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            Records = [];
            LoadErrorMessage = "Có lỗi khi tải danh mục mã kết quả tính công. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh mục mã kết quả tính công.");
        }
        finally
        {
            if (ReferenceEquals(activeReloadTokenSource, requestTokenSource))
            {
                activeReloadTokenSource = null;
            }

            IsLoading = false;
            IsRefreshing = false;
        }
    }

    private async Task OnSearchTextChangedAsync(string? searchText)
    {
        var normalizedSearchText = string.IsNullOrWhiteSpace(searchText)
            ? null
            : searchText.Trim();

        if (string.Equals(SearchText, normalizedSearchText, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalizedSearchText;
        await ClearSelectionAsync();
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private Task OnDetailPopupVisibleChanged(bool visible)
    {
        IsDetailVisible = visible;

        if (!visible)
        {
            DetailRecord = null;
        }

        return Task.CompletedTask;
    }

    private Task OpenDetailAsync()
    {
        if (SelectedRecord is { } selectedRecord)
        {
            DetailRecord = selectedRecord;
            IsDetailVisible = true;
        }

        return Task.CompletedTask;
    }

    private async Task OnEditModelSaving(GridEditModelSavingEventArgs e)
    {
        if (e.IsNew
            || e.DataItem is not AttendanceStatusCodeRecord source
            || e.EditModel is not AttendanceStatusCodeRecord editModel)
        {
            e.Cancel = true;
            ToastService.ShowError("Chỉ hỗ trợ cập nhật cờ phụ cấp/khấu trừ cho mã kết quả tính công hiện có.");
            return;
        }

        if (IsSavingFlags)
        {
            e.Cancel = true;
            return;
        }

        IsSavingFlags = true;

        try
        {
            await InvokeAsync(StateHasChanged);
            var updatedRecord = await DataProvider.UpdateFlagsAsync(
                source,
                editModel,
                disposalTokenSource.Token);

            ApplyUpdatedRecord(updatedRecord);
            e.Reload = false;
            ToastService.ShowSuccess("Đã cập nhật cờ phụ cấp/khấu trừ.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            e.Cancel = true;
        }
        catch (InvalidOperationException ex)
        {
            e.Cancel = true;
            ToastService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            e.Cancel = true;
            ToastService.ShowError("Không thể cập nhật cờ phụ cấp/khấu trừ.");
        }
        finally
        {
            IsSavingFlags = false;
        }
    }

    private async Task OnDataItemDeleting(GridDataItemDeletingEventArgs e)
    {
        if (e.DataItem is not AttendanceStatusCodeRecord record || record.Id == Guid.Empty)
        {
            ToastService.ShowError("Không xác định được mã kết quả tính công cần xóa.");
            return;
        }

        if (IsDeletingStatusCode)
        {
            return;
        }

        IsDeletingStatusCode = true;

        try
        {
            await InvokeAsync(StateHasChanged);
            await DataProvider.DeleteAsync(record.Id, disposalTokenSource.Token);

            RemoveRecord(record.Id);
            e.Reload = false;
            ToastService.ShowSuccess("Đã xóa mã kết quả tính công.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xóa mã kết quả tính công.");
        }
        finally
        {
            IsDeletingStatusCode = false;
        }
    }

    private void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

    private Task ExportAllDataToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync("attendance-status-codes"),
        "Đã xuất Excel danh mục mã kết quả tính công.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(
            "attendance-status-codes-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã xuất Excel cho các dòng đã chọn.");

    private Task ExportAllDataToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync("attendance-status-codes"),
        "Đã xuất PDF danh mục mã kết quả tính công.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync(
            "attendance-status-codes-selected",
            new GridPdfExportOptions { ExportSelectedRowsOnly = true }),
        "Đã xuất PDF cho các dòng đã chọn.");

    private async Task ExportAsync(Func<Task> exportAction, string successMessage)
    {
        if (Grid is null)
        {
            ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất.");
            return;
        }

        if (IsExporting)
        {
            return;
        }

        IsExporting = true;

        try
        {
            await InvokeAsync(StateHasChanged);
            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất danh mục mã kết quả tính công.");
        }
        finally
        {
            IsExporting = false;
        }
    }

    private async Task ClearSelectionAsync()
    {
        SelectedDataItems = [];
        IsDetailVisible = false;
        DetailRecord = null;

        if (Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    private List<AttendanceStatusCodeRecord> GetSelectedRecords()
    {
        var recordIds = Records.Select(record => record.Id).ToHashSet();

        return SelectedDataItems
            .OfType<AttendanceStatusCodeRecord>()
            .Where(record => recordIds.Contains(record.Id))
            .DistinctBy(record => record.Id)
            .ToList();
    }

    private void ApplyUpdatedRecord(AttendanceStatusCodeRecord updatedRecord)
    {
        Records = Records
            .Select(record => record.Id == updatedRecord.Id ? updatedRecord : record)
            .ToArray();

        SelectedDataItems = SelectedDataItems
            .Select(item => item is AttendanceStatusCodeRecord record && record.Id == updatedRecord.Id
                ? updatedRecord
                : item)
            .ToArray();

        if (DetailRecord?.Id == updatedRecord.Id)
        {
            DetailRecord = updatedRecord;
        }
    }

    private void RemoveRecord(Guid recordId)
    {
        Records = Records
            .Where(record => record.Id != recordId)
            .ToArray();

        SelectedDataItems = SelectedDataItems
            .Where(item => item is not AttendanceStatusCodeRecord record || record.Id != recordId)
            .ToArray();

        if (DetailRecord?.Id == recordId)
        {
            IsDetailVisible = false;
            DetailRecord = null;
        }
    }

    public void Dispose()
    {
        activeReloadTokenSource?.Cancel();
        disposalTokenSource.Cancel();
        reloadGate.Dispose();
        disposalTokenSource.Dispose();
    }
}
