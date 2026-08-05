using DevExpress.Blazor;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.QuanTri.LenhMayChamCong;

public partial class LenhMayChamCong : IDisposable
{
    [Inject] private IAdmsDeviceCommandService DeviceCommandService { get; set; } = default!;

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject] private IHrmDialogService DialogService { get; set; } = default!;

    [Inject] private IHrmToastService ToastService { get; set; } = default!;

    private readonly CancellationTokenSource disposalTokenSource = new();
    private readonly LenhMayChamCongPageState State = new();

    private IGrid? Grid { get; set; }
    private IReadOnlyList<AdmsDeviceCommandSummaryDto> Rows { get; set; } = Array.Empty<AdmsDeviceCommandSummaryDto>();

    private IReadOnlyList<object> selectedDataItems = [];
    private IReadOnlyList<object> SelectedDataItems
    {
        get => selectedDataItems;
        set
        {
            selectedDataItems = value;
            SyncSelectionState();
        }
    }

    private AdmsDeviceCommandSummaryDto? SelectedCommand { get; set; }
    private AdmsDeviceCommandSummaryDto? FocusedCommand { get; set; }
    private AdmsDeviceCommandDetailDto? EditingDetail { get; set; }
    private int FocusedCommandVisibleIndex { get; set; } = -1;
    private string PopupEditFormHeaderText { get; set; } = string.Empty;
    private string? EditErrorMessage { get; set; }
    private string? ErrorMessage { get; set; }
    private bool CanView { get; set; }
    private bool CanManage { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool IsDataLoaded { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        CanView = authState.User.Identity?.IsAuthenticated == true;

        // App hiện chưa có permission bridge chi tiết cho ADMS command management.
        // Tạm thời giữ cùng mức quyền với route để team review luồng UI trước.
        CanManage = CanView;

        IsLoading = CanView;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && CanView)
        {
            await LoadAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        IsDataLoaded = false;
        ErrorMessage = null;

        try
        {
            Rows = await DeviceCommandService.SearchAsync(State.ToFilter(), disposalTokenSource.Token);
            RestoreSelectedCommand();
            IsDataLoaded = true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Rows = Array.Empty<AdmsDeviceCommandSummaryDto>();
            SelectedCommand = null;
            SelectedDataItems = [];
            ErrorMessage = ex.Message;
            IsDataLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAsync()
    {
        ClearSelection();
        await LoadAsync();
        if (string.IsNullOrWhiteSpace(ErrorMessage))
        {
            ToastService.ShowSuccess("Đã làm mới danh sách lệnh máy chấm công.");
        }
    }

    private async Task ResetAsync()
    {
        State.SearchTerm = null;
        State.Status = null;
        State.CommitFrom = null;
        State.CommitTo = null;
        ClearSelection();
        await LoadAsync();
    }

    private async Task DeleteAllCommandsAsync()
    {
        if (!CanManage || IsLoading)
        {
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            "Thao tác này sẽ xóa toàn bộ dữ liệu lệnh máy chấm công và đặt lại ID từ đầu. Bạn có chắc muốn tiếp tục?",
            "Xác nhận xóa tất cả",
            "Xóa tất cả",
            "Hủy",
            MessageBoxRenderStyle.Danger);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await DeviceCommandService.DeleteAllAsync(disposalTokenSource.Token);
            ClearSelection();
            await LoadAsync();
            ToastService.ShowSuccess("Đã xóa toàn bộ dữ liệu lệnh máy chấm công và đặt lại ID.");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
    }

    private async Task OnSearchTextChanged(string? value)
    {
        State.SearchTerm = value;
        await LoadAsync();
    }

    private async Task DeleteFocusedItem_Click()
    {
        var targets = GetSelectedCommands();
        if (targets.Count == 0)
        {
            return;
        }

        await ConfirmAndDeleteAsync(targets);
    }

    private async Task EditItemAsync(AdmsDeviceCommandSummaryDto row)
    {
        if (!CanEditRow(row))
        {
            return;
        }

        FocusedCommand = row;
        EditingDetail = null;
        EditErrorMessage = null;

        try
        {
            EditingDetail = await DeviceCommandService.GetDetailAsync(row.Id, disposalTokenSource.Token);

            var visibleIndex = GetVisibleIndex(row);
            if (visibleIndex >= 0)
            {
                await Grid!.StartEditRowAsync(visibleIndex);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
    }

    private async Task OpenDeleteAsync(AdmsDeviceCommandSummaryDto row)
    {
        await ConfirmAndDeleteAsync([row]);
    }

    private async Task OnCancelCommandEditClick()
    {
        if (Grid is not null)
        {
            await Grid.CancelEditAsync();
        }
    }

    private async Task ConfirmAndDeleteAsync(IReadOnlyList<AdmsDeviceCommandSummaryDto> rows)
    {
        if (!CanManage || rows.Count == 0)
        {
            return;
        }

        var blockedRow = rows.FirstOrDefault(row => !CanDeleteRow(row));
        if (blockedRow is not null)
        {
            ToastService.ShowWarning("Không thể xóa lệnh đã phản hồi hoặc đã hủy.");
            return;
        }

        var message = rows.Count == 1
            ? $"Bạn có chắc muốn xóa lệnh của thiết bị {rows[0].DeviceSn}?"
            : $"Bạn có chắc muốn xóa {rows.Count} lệnh máy chấm công đã chọn?";

        var confirmed = await DialogService.ConfirmAsync(
            message,
            "Xác nhận xóa",
            "Xóa",
            "Hủy",
            MessageBoxRenderStyle.Danger);

        if (!confirmed)
        {
            return;
        }

        try
        {
            foreach (var row in rows)
            {
                await DeviceCommandService.DeleteAsync(row.Id, disposalTokenSource.Token);
            }

            ToastService.ShowSuccess(rows.Count == 1
                ? "Đã xóa lệnh máy chấm công."
                : $"Đã xóa {rows.Count} lệnh máy chấm công.");
            await LoadAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
    }

    private void OnCustomizeEditModel(GridCustomizeEditModelEventArgs e)
    {
        EditErrorMessage = null;

        if (e.IsNew)
        {
            e.EditModel = LenhMayChamCongEditModel.Create();
            PopupEditFormHeaderText = "Tạo lệnh máy chấm công";
            return;
        }

        var row = (AdmsDeviceCommandSummaryDto)e.DataItem!;
        e.EditModel = EditingDetail?.Id == row.Id
            ? LenhMayChamCongEditModel.FromDetail(EditingDetail)
            : LenhMayChamCongEditModel.FromSummary(row);
        PopupEditFormHeaderText = "Cập nhật lệnh máy chấm công";
    }

    private async Task OnEditModelSaving(GridEditModelSavingEventArgs e)
    {
        var model = (LenhMayChamCongEditModel)e.EditModel;
        EditErrorMessage = null;

        if (!ValidateEditModel(model, out var validationError))
        {
            EditErrorMessage = validationError;
            e.Cancel = true;
            return;
        }

        try
        {
            var detail = e.IsNew
                ? await DeviceCommandService.CreateAsync(model.ToRequest(), disposalTokenSource.Token)
                : await DeviceCommandService.UpdateAsync(model.Id!.Value, model.ToRequest(), disposalTokenSource.Token);

            State.SelectedCommandId = detail.Id;
            await LoadAsync();
            e.Reload = false;
            ToastService.ShowSuccess(
                e.IsNew ? "Đã tạo lệnh máy chấm công." : "Đã cập nhật lệnh máy chấm công.",
                detail.DeviceSn);
        }
        catch (OperationCanceledException)
        {
            e.Cancel = true;
        }
        catch (Exception ex)
        {
            EditErrorMessage = ex.Message;
            ToastService.ShowError(ex.Message);
            e.Cancel = true;
        }
    }

    private void OnFocusedRowChanged(GridFocusedRowChangedEventArgs e)
    {
        FocusedCommand = e.DataItem as AdmsDeviceCommandSummaryDto;
        FocusedCommandVisibleIndex = e.VisibleIndex;

        if (FocusedCommand is not null)
        {
            SelectCommand(FocusedCommand);
        }
    }

    private IReadOnlyList<AdmsDeviceCommandSummaryDto> GetSelectedCommands()
    {
        return SelectedDataItems
            .OfType<AdmsDeviceCommandSummaryDto>()
            .DistinctBy(item => item.Id)
            .ToArray();
    }

    private bool CanDeleteSelectedCommand()
    {
        var selected = GetSelectedCommands();
        return selected.Count > 0 && selected.All(CanDeleteRow);
    }

    private bool CanEditRow(AdmsDeviceCommandSummaryDto row)
    {
        return CanManage
            && !row.ResponseTime.HasValue;
    }

    private bool CanDeleteRow(AdmsDeviceCommandSummaryDto row)
    {
        return CanEditRow(row);
    }

    private string GetStatusCssClass(AdmsDeviceCommandSummaryDto row)
    {
        var suffix = row.Status switch
        {
            AdmsDeviceCommandStatus.Success => "success",
            AdmsDeviceCommandStatus.Error => "error",
            AdmsDeviceCommandStatus.Transmitted => "transmitted",
            AdmsDeviceCommandStatus.NoResponse => "no-response",
            AdmsDeviceCommandStatus.Cancelled => "cancelled",
            _ => "pending"
        };

        return $"adms-device-commands-status adms-device-commands-status--{suffix}";
    }

    private int GetVisibleIndex(AdmsDeviceCommandSummaryDto item)
    {
        if (Grid is null)
        {
            return -1;
        }

        if (FocusedCommand?.Id == item.Id && FocusedCommandVisibleIndex >= 0)
        {
            return FocusedCommandVisibleIndex;
        }

        var visibleRowCount = Grid.GetVisibleRowCount();
        for (var visibleIndex = 0; visibleIndex < visibleRowCount; visibleIndex++)
        {
            if (Grid.GetDataItem(visibleIndex) is AdmsDeviceCommandSummaryDto row && row.Id == item.Id)
            {
                return visibleIndex;
            }
        }

        return -1;
    }

    private void SyncSelectionState()
    {
        var selected = selectedDataItems
            .OfType<AdmsDeviceCommandSummaryDto>()
            .DistinctBy(item => item.Id)
            .ToArray();

        if (selected.Length == 0)
        {
            SelectedCommand = null;
            FocusedCommand = null;
            FocusedCommandVisibleIndex = -1;
            State.SelectedCommandId = null;
            return;
        }

        if (FocusedCommand is null || selected.All(item => item.Id != FocusedCommand.Id))
        {
            FocusedCommand = selected[0];
            FocusedCommandVisibleIndex = GetVisibleIndex(FocusedCommand);
        }

        SelectedCommand = selected.Length == 1 ? selected[0] : null;
        State.SelectedCommandId = SelectedCommand?.Id;
    }

    private void SelectCommand(AdmsDeviceCommandSummaryDto row)
    {
        SelectedCommand = row;
        State.SelectedCommandId = row.Id;
    }

    private void RestoreSelectedCommand()
    {
        if (!State.SelectedCommandId.HasValue)
        {
            return;
        }

        SelectedCommand = Rows.FirstOrDefault(row => row.Id == State.SelectedCommandId.Value);
        if (SelectedCommand is not null)
        {
            SelectedDataItems = [SelectedCommand];
        }
    }

    private void ClearSelection()
    {
        SelectedCommand = null;
        FocusedCommand = null;
        FocusedCommandVisibleIndex = -1;
        State.SelectedCommandId = null;
        SelectedDataItems = [];
    }

    private static bool ValidateEditModel(LenhMayChamCongEditModel model, out string? error)
    {
        if (string.IsNullOrWhiteSpace(model.DeviceSn))
        {
            error = "Số serial thiết bị là bắt buộc.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(model.Content))
        {
            error = "Nội dung lệnh là bắt buộc.";
            return false;
        }

        error = null;
        return true;
    }

    public void Dispose()
    {
        disposalTokenSource.Cancel();
        disposalTokenSource.Dispose();
    }
}
