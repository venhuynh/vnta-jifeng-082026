namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien;

public partial class PhuCapTrachNhiemGanNhanVien
{
    private async Task ClearSelectionAsync()
    {
        SelectionState.Clear();
        await InvokeAsync(StateHasChanged);
    }
}
