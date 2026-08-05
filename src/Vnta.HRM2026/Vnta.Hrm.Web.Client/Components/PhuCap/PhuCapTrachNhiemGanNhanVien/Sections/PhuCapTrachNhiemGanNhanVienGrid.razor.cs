using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Sections;

public partial class PhuCapTrachNhiemGanNhanVienGrid
{
    private IGrid? grid;

    [Parameter] public IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentDto> Records { get; set; } = [];
    [Parameter] public IReadOnlyList<object> SelectedDataItems { get; set; } = [];
    [Parameter] public int CurrentPageIndex { get; set; }
    [Parameter] public int PageSize { get; set; }
    [Parameter] public Func<decimal, string> FormatMoney { get; set; } = _ => string.Empty;
    [Parameter] public bool CanManageAssignments { get; set; }
    [Parameter] public string EmptyStateTitle { get; set; } = string.Empty;
    [Parameter] public string EmptyStateMessage { get; set; } = string.Empty;
    [Parameter] public string EmptyStateActionText { get; set; } = string.Empty;
    [Parameter] public Func<PayrollResponsibilityAllowanceEmployeeAssignmentDto, string> GetGradeLabel { get; set; } = _ => string.Empty;
    [Parameter] public Func<Guid?, string> GetGradeLabelCssClass { get; set; } = _ => string.Empty;
    [Parameter] public EventCallback<IReadOnlyList<object>> SelectedDataItemsChanged { get; set; }
    [Parameter] public EventCallback<PayrollResponsibilityAllowanceEmployeeAssignmentDto> EditRequested { get; set; }
    [Parameter] public EventCallback EmptyStateActionRequested { get; set; }

    private static string GetEmployeeDisplay(PayrollResponsibilityAllowanceEmployeeAssignmentDto employee) =>
        $"{employee.EmployeeCode} - {employee.EmployeeName}";

    public void ShowColumnChooser() => grid?.ShowColumnChooser();
}
