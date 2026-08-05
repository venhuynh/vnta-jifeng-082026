namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop;

/// <summary>Owns selection received from the deduction-summary grid.</summary>
public partial class KhauTruTongKet
{
    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }
}
