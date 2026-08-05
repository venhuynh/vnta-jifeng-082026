using DevExpress.Blazor;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

public sealed partial class OtherResponsibilityAllowanceCoordinator
{
    private Task OnSelectedGridItemsChanged(IReadOnlyList<object> items)
    {
        SelectedGridItems = items;
        return Task.CompletedTask;
    }

    private Task OnGridChanged(IGrid? grid)
    {
        AllowanceGrid = grid;
        return Task.CompletedTask;
    }

    private async Task ClearGridSelectionAsync()
    {
        SelectedGridItems = [];
        if (AllowanceGrid is null) return;
        await AllowanceGrid.DeselectAllAsync();
        AllowanceGrid.SetFocusedRowIndex(-1);
    }

    private List<OtherResponsibilityAllowanceRecord> GetSelectedRecords()
    {
        var visibleIds = VisibleRecords.Select(record => record.Id).ToHashSet();
        return SelectedGridItems.OfType<OtherResponsibilityAllowanceRecord>()
            .Where(record => visibleIds.Contains(record.Id))
            .DistinctBy(record => record.Id)
            .ToList();
    }

    private int GetSelectedRecordCount() => GetSelectedRecords().Count;
}
