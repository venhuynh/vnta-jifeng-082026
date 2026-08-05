namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Owns selection state received from the data grid.</summary>
public partial class PhuCapCom
{
    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }
}
