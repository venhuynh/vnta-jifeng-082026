using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.CaKip.CaiDatCa;

public partial class CaiDatCa : IDisposable
{
    private readonly CancellationTokenSource disposalTokenSource = new();

    [Inject]
    private AttendanceShiftDataProvider DataProvider { get; set; } = default!;

    [Inject]
    private IHrmToastService ToastService { get; set; } = default!;

    private IReadOnlyList<AttendanceShiftRecord> Shifts { get; set; } = [];
    private IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    private IGrid? Grid { get; set; }
    private string? SearchText { get; set; }
    private string? LoadErrorMessage { get; set; }
    private string? EditErrorMessage { get; set; }
    private string LoadingText { get; set; } = "Đang tải dữ liệu...";
    private bool IsLoading { get; set; } = true;
    private bool IsCreatingNewShift { get; set; }

    private bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);
    private bool CanInteract => !IsLoading && !HasLoadError;
    private bool CanCreate => CanInteract;
    private bool CanEditSelected => CanInteract && GetSelectedShiftCount() == 1;
    private bool CanDeleteSelected => CanInteract && GetSelectedShiftCount() > 0;
    private bool CanExport => !IsLoading && Shifts.Count > 0;
    private bool CanExportSelected => CanExport && GetSelectedShiftCount() > 0;

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

        LoadErrorMessage = null;
        EditErrorMessage = null;
        LoadingText = "Đang tải dữ liệu ca làm...";
        IsLoading = true;

        try
        {
            await ClearSelectionAsync();
            Shifts = await DataProvider.GetAsync(disposalTokenSource.Token);
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
            Shifts = [];
            LoadErrorMessage = "Có lỗi khi tải dữ liệu ca làm. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách ca làm.");
        }
        finally
        {
            IsLoading = false;
            LoadingText = "Đang tải dữ liệu...";
        }
    }

    private async Task OnAddShiftClick()
    {
        if (!CanCreate || Grid is null)
        {
            return;
        }

        EditErrorMessage = null;
        await Grid.StartEditNewRowAsync();
    }

    private async Task OnEditShiftClick()
    {
        if (Grid is null)
        {
            return;
        }

        var shift = GetSingleSelectedShift();
        if (shift is null)
        {
            ToastService.ShowWarning("Hãy chọn đúng một ca làm để điều chỉnh.");
            return;
        }

        EditErrorMessage = null;
        await Grid.StartEditDataItemAsync(shift, nameof(AttendanceShiftRecord.Name));
    }

    private Task OnDeleteShiftsClick()
    {
        var selectedShifts = GetSelectedShifts();
        if (selectedShifts.Count == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một ca làm để xóa.");
            return Task.CompletedTask;
        }

        ToastService.ShowWarning("Màn Cài đặt ca hiện chưa hỗ trợ xóa để tránh ảnh hưởng dữ liệu phân ca/bảng công.");
        return Task.CompletedTask;
    }

    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }

    private void OnColumnChooserItemClick(ToolbarItemClickEventArgs _) => Grid?.ShowColumnChooser();

    private async Task OnCancelShiftEditClick()
    {
        if (Grid is not null)
        {
            await Grid.CancelEditAsync();
        }
    }

    private void OnCustomizeEditModel(GridCustomizeEditModelEventArgs e)
    {
        EditErrorMessage = null;
        IsCreatingNewShift = e.IsNew;
        var model = (AttendanceShiftRecord)e.EditModel;

        if (e.IsNew)
        {
            InitializeNewShiftDefaults(model);
        }
        else
        {
            model.SyncWorkingDayFlags();
        }
    }

    private async Task OnEditModelSaving(GridEditModelSavingEventArgs e)
    {
        EditErrorMessage = null;

        try
        {
            var editModel = (AttendanceShiftRecord)e.EditModel;
            NormalizeEditModel(editModel);
            editModel.SyncWorkingDaysFromFlags();

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

            LoadingText = e.IsNew
                ? "Đang tạo ca làm..."
                : "Đang cập nhật ca làm...";
            IsLoading = true;

            Shifts = await DataProvider.SaveAsync(editModel, e.IsNew, disposalTokenSource.Token);
            e.Reload = false;
            await ClearSelectionAsync();
            ToastService.ShowSuccess(e.IsNew ? "Đã thêm ca làm." : "Đã cập nhật ca làm.");
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
            ToastService.ShowError("Không thể lưu ca làm.");
        }
        catch (Exception)
        {
            EditErrorMessage = "Không thể lưu dữ liệu ca làm. Vui lòng kiểm tra lại thông tin.";
            e.Cancel = true;
            ToastService.ShowError("Không thể lưu ca làm.");
        }
        finally
        {
            IsLoading = false;
            LoadingText = "Đang tải dữ liệu...";
        }
    }

    private Task ExportAllDataToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync("attendance-shifts"),
        "Đã bắt đầu xuất Excel ca làm.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        () => Grid!.ExportToXlsxAsync(
            "attendance-shifts-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất Excel cho các dòng đã chọn.");

    private Task ExportAllDataToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync("attendance-shifts"),
        "Đã bắt đầu xuất PDF ca làm.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        () => Grid!.ExportToPdfAsync(
            "attendance-shifts-selected",
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
            ToastService.ShowError("Không thể xuất dữ liệu ca làm.");
        }
    }

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

    private List<AttendanceShiftRecord> GetSelectedShifts() => SelectedDataItems.OfType<AttendanceShiftRecord>().ToList();

    private AttendanceShiftRecord? GetSingleSelectedShift()
    {
        var selectedShifts = GetSelectedShifts();
        return selectedShifts.Count == 1 ? selectedShifts[0] : null;
    }

    private int GetSelectedShiftCount() => GetSelectedShifts().Count;

    private static void NormalizeEditModel(AttendanceShiftRecord model)
    {
        model.Code = NormalizeNullable(model.Code);
        model.Name = NormalizeNullable(model.Name);
        model.ShortName = NormalizeNullable(model.ShortName);
        model.Description = NormalizeNullable(model.Description);
        model.DepartmentGroup = NormalizeNullable(model.DepartmentGroup);
        model.StartTime = NormalizeNullable(model.StartTime);
        model.EndTime = NormalizeNullable(model.EndTime);
        model.BreakStartTime = NormalizeNullable(model.BreakStartTime);
        model.BreakEndTime = NormalizeNullable(model.BreakEndTime);
        model.ColorHex = NormalizeNullable(model.ColorHex);
    }

    private static void InitializeNewShiftDefaults(AttendanceShiftRecord model)
    {
        var utcNow = DateTime.UtcNow;

        model.Id = Guid.NewGuid();
        model.Code = BuildInternalCode(model.Id);
        model.Name = string.Empty;
        model.ShortName = null;
        model.Description = null;
        model.DepartmentGroup = "Chung";
        model.StartTime = "08:00";
        model.EndTime = "17:00";
        model.IsOvernight = false;
        model.BreakStartTime = "12:00";
        model.BreakEndTime = "13:00";
        model.Status = 1;
        model.ColorHex = "#2563EB";
        model.WorksMonday = true;
        model.WorksTuesday = true;
        model.WorksWednesday = true;
        model.WorksThursday = true;
        model.WorksFriday = true;
        model.WorksSaturday = false;
        model.WorksSunday = false;
        model.SyncWorkingDaysFromFlags();
        model.CreatedAtUtc = utcNow;
        model.UpdatedAtUtc = utcNow;
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildInternalCode(Guid id) =>
        $"SHIFT-{id.ToString("N")[..8].ToUpperInvariant()}";

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }
}
