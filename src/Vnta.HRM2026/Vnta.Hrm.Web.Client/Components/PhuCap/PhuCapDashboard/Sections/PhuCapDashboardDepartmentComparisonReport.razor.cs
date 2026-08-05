using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDashboard;

public partial class PhuCapDashboardDepartmentComparisonReport
{
    [Parameter, EditorRequired] public IReadOnlyList<PayrollAllowanceDashboardDepartmentTreeNodeDto> Comparison { get; set; } = [];
    [Parameter, EditorRequired] public int PayrollMonth { get; set; }
    [Parameter, EditorRequired] public int PayrollYear { get; set; }
    [Parameter] public bool IsRefreshing { get; set; }
    [Parameter] public EventCallback RefreshRequested { get; set; }
}
