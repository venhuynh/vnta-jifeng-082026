using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Dialogs;

public partial class PhuCapTrachNhiemGanNhanVienEditPopup
{
    private const int GradeLookupPageSize = 8;
    private static readonly int[] GradeLookupPageSizeSelectorItems = [8, 15, 30, 50];
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter, EditorRequired] public bool IsSaving { get; set; }
    [Parameter, EditorRequired] public PayrollResponsibilityAllowanceEmployeeAssignmentDto Assignment { get; set; } = default!;
    [Parameter, EditorRequired] public PhuCapTrachNhiemGanNhanVienEditModel Model { get; set; } = default!;
    [Parameter, EditorRequired] public EditContext EditContext { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> GradeOptions { get; set; } = [];
    [Parameter, EditorRequired] public EventCallback SaveRequested { get; set; }

    private bool GradeDropDownVisible { get; set; }
    private int GradeLookupPageIndex { get; set; }
    private string? gradeSearchText;
    private string PopupTitle => $"Điều chỉnh cấp bậc nhân viên - {Assignment.EmployeeCode}";
    private string PayrollPeriodDisplay => $"{Assignment.Month:00}/{Assignment.Year}";
    private PayrollResponsibilityAllowanceGradeDto? SelectedGrade => Model.GradeId is { } gradeId
        ? GradeOptions.FirstOrDefault(grade => grade.Id == gradeId)
        : null;
    private string SelectedStandardAllowanceAmountDisplay => SelectedGrade is null
        ? "Chưa có cấp bậc"
        : SelectedGrade.StandardResponsibilityAllowanceAmount.ToString("N0 'đ'", VietnameseCulture);
    private bool CanSelectGrade => !IsSaving;
    private string? GradeSearchText
    {
        get => gradeSearchText;
        set
        {
            if (gradeSearchText == value) return;
            gradeSearchText = value;
            GradeLookupPageIndex = 0;
        }
    }

    private IEnumerable<PayrollResponsibilityAllowanceGradeDto> FilteredGradeOptions => GradeOptions.Where(grade =>
        string.IsNullOrWhiteSpace(GradeSearchText) || new[] { grade.Code, grade.Name, grade.Note }
            .Any(value => value?.Contains(GradeSearchText, StringComparison.OrdinalIgnoreCase) == true));

    private Task OnGradeDropDownVisibleChanged(bool visible) { GradeDropDownVisible = visible; return Task.CompletedTask; }
    private Task OnGradeLookupPageIndexChanged(int pageIndex) { GradeLookupPageIndex = pageIndex; return Task.CompletedTask; }
    private Task OnGradeValueChanged(object? value)
    {
        Model.GradeId = ResolveGuid(value);
        EditContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(PhuCapTrachNhiemGanNhanVienEditModel.GradeId)));
        return Task.CompletedTask;
    }

    private Task OnGradeLookupRowClick(GridRowClickEventArgs args, IDropDownBox dropDownBox) =>
        args.Grid.GetDataItem(args.VisibleIndex) is PayrollResponsibilityAllowanceGradeDto grade ? SelectGradeAsync(grade, dropDownBox) : Task.CompletedTask;

    private Task SelectGradeAsync(PayrollResponsibilityAllowanceGradeDto grade, IDropDownBox? dropDownBox = null)
    {
        Model.GradeId = grade.Id;
        EditContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(PhuCapTrachNhiemGanNhanVienEditModel.GradeId)));
        GradeDropDownVisible = false;
        dropDownBox?.HideDropDown();
        return InvokeAsync(StateHasChanged);
    }

    private string GetGradeDisplayText(DropDownBoxQueryDisplayTextContext context)
    {
        var gradeId = ResolveGuid(context.Value) ?? Model.GradeId;
        var grade = gradeId.HasValue ? GradeOptions.FirstOrDefault(item => item.Id == gradeId.Value) : null;
        return grade is null ? string.Empty : $"{grade.Code} - {grade.Name}";
    }

    private static Guid? ResolveGuid(object? value) => value switch { Guid guid => guid, string text when Guid.TryParse(text, out var guid) => guid, _ => null };
}
