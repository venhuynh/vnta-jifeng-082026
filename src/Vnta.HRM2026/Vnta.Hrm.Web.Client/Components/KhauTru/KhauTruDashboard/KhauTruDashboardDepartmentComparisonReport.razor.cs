using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruDashboard;

public partial class KhauTruDashboardDepartmentComparisonReport
{
    [Parameter, EditorRequired] public IReadOnlyList<PayrollDeductionDashboardDepartmentTreeNodeDto> Comparison { get; set; } = [];
    [Parameter, EditorRequired] public int PayrollMonth { get; set; }
    [Parameter, EditorRequired] public int PayrollYear { get; set; }
    [Parameter] public bool IsRefreshing { get; set; }
    [Parameter] public EventCallback RefreshRequested { get; set; }
}
