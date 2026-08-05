using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Sections;

public partial class PhuCapTrachNhiemGanNhanVienSummary
{
    [Parameter] public PayrollResponsibilityAllowanceEmployeeAssignmentSummaryDto Summary { get; set; } = new(0, 0, 0);
    [Parameter] public string SelectedKey { get; set; } = string.Empty;
    [Parameter] public string AssignedKey { get; set; } = string.Empty;
    [Parameter] public string UnassignedKey { get; set; } = string.Empty;
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public bool CanInteract { get; set; }
    [Parameter] public EventCallback<string> SelectionChanged { get; set; }
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
}
