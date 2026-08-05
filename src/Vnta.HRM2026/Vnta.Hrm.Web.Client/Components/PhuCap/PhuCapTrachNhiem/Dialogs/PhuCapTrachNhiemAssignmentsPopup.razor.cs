using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Trình bày danh sách gán trách nhiệm nhân viên và phát sự kiện thao tác về cha.
/// </summary>
public partial class PhuCapTrachNhiemAssignmentsPopup
{
    [Parameter] public bool IsAssignmentsPopupVisible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public string AssignmentsPopupPeriodLabel { get; set; } = string.Empty;
    [Parameter] public string? AssignmentsPopupErrorMessage { get; set; }
    [Parameter] public string AssignmentSearchText { get; set; } = string.Empty;
    [Parameter] public EventCallback<string?> AssignmentSearchTextChanged { get; set; }
    [Parameter] public IReadOnlyList<EmployeeAssignmentEditorRow> Rows { get; set; } = [];
    [Parameter] public IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> GradeRows { get; set; } = [];
    [Parameter] public EventCallback ApplyPositionDefaultsRequested { get; set; }
    [Parameter] public EventCallback<EmployeeAssignmentEditorRow> SaveAssignmentRequested { get; set; }
    [Parameter] public Func<EmployeeAssignmentEditorModel, decimal> GetStandardAmount { get; set; } = static _ => 0m;

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    /// <summary>Xử lý sự kiện cho luồng <c>OnSearchInput</c>.</summary>
    private Task OnSearchInput(ChangeEventArgs args) => AssignmentSearchTextChanged.InvokeAsync(args.Value?.ToString());
    /// <summary>Áp dụng cho luồng <c>ApplyPositionDefaultsAsync</c>.</summary>
    private Task ApplyPositionDefaultsAsync() => ApplyPositionDefaultsRequested.InvokeAsync();
    /// <summary>Lưu cho luồng <c>SaveEmployeeAssignmentAsync</c>.</summary>
    private Task SaveEmployeeAssignmentAsync(EmployeeAssignmentEditorRow row) => SaveAssignmentRequested.InvokeAsync(row);
    /// <summary>Định dạng cho luồng <c>FormatCurrency</c>.</summary>
    private static string FormatCurrency(decimal value) =>
        value == 0m ? string.Empty : string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} đ", value);
}
