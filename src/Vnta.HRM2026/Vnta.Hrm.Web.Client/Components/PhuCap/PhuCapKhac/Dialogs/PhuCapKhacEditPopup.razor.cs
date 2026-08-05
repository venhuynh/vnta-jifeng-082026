using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.State;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

public partial class PhuCapKhacEditPopup
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter, EditorRequired] public OtherAllowanceEditDialogState State { get; set; } = default!;
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = "Sửa phụ cấp khác";
    [Parameter] public PhuCapKhacEditModel Model { get; set; } = new();
    [Parameter] public bool IsCreateMode { get; set; }
    [Parameter] public IReadOnlyList<PhuCapKhacEmployeeOption> EmployeeOptions { get; set; } = [];
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public EventCallback<PhuCapKhacEditModel> SaveRequested { get; set; }

    private PhuCapKhacEditModel? sourceModel;
    private PhuCapKhacEditModel Draft { get; set; } = new();

    protected override void OnParametersSet()
    {
        if(ReferenceEquals(sourceModel, State.Model))
        {
            return;
        }

        sourceModel = State.Model;
        Draft = Clone(State.Model);
    }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    private Task SaveAsync() => SaveRequested.InvokeAsync(Clone(Draft));

    private Task OnAllowanceNameChangedAsync(string? value)
    {
        Draft.AllowanceName = value ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnEmployeeChangedAsync(Guid payrollAllowanceSummaryRecordId)
    {
        var employee = EmployeeOptions.FirstOrDefault(option =>
            option.PayrollAllowanceSummaryRecordId == payrollAllowanceSummaryRecordId);
        Draft.PayrollAllowanceSummaryRecordId = payrollAllowanceSummaryRecordId;
        Draft.EmployeeDisplay = employee?.EmployeeDisplay ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnIsFixedAmountChangedAsync(bool value)
    {
        Draft.IsFixedAmount = value;
        if(!value)
        {
            Draft.AllowanceAmount = 0m;
        }

        return Task.CompletedTask;
    }

    private Task OnAllowanceAmountChangedAsync(decimal value)
    {
        Draft.AllowanceAmount = OtherAllowanceAmountPreview.Calculate(Draft.IsFixedAmount, value);
        return Task.CompletedTask;
    }

    private Task OnNoteChangedAsync(string? value)
    {
        Draft.Note = value;
        return Task.CompletedTask;
    }

    private static PhuCapKhacEditModel Clone(PhuCapKhacEditModel source) => new()
    {
        Id = source.Id,
        PayrollAllowanceSummaryRecordId = source.PayrollAllowanceSummaryRecordId,
        EmployeeDisplay = source.EmployeeDisplay,
        PayrollMonth = source.PayrollMonth,
        PayrollYear = source.PayrollYear,
        PayrollPeriodDisplay = source.PayrollPeriodDisplay,
        AllowanceName = source.AllowanceName,
        IsFixedAmount = source.IsFixedAmount,
        AllowanceAmount = source.AllowanceAmount,
        Note = source.Note,
        IsLocked = source.IsLocked,
        OriginalUpdatedAtUtc = source.OriginalUpdatedAtUtc
    };
}
