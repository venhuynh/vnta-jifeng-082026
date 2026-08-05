using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.TinhLuong.LuongCanBan;

public partial class LuongCanBanEditPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = "Thông tin lương căn bản";
    [Parameter] public BasicSalaryRecord Model { get; set; } = new();
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public bool IsCreatingNewSalaryRecord { get; set; }
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public EventCallback<BasicSalaryRecord> SaveRequested { get; set; }

    private BasicSalaryRecord? sourceModel;
    private BasicSalaryRecord Draft { get; set; } = new();
    private EditContext EditContext { get; set; } = new(new BasicSalaryRecord());

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(sourceModel, Model))
        {
            return;
        }

        sourceModel = Model;
        Draft = Clone(Model);
        EditContext = new EditContext(Draft);
    }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);

    private Task SaveAsync()
    {
        if (IsSaving || !CanSave || !EditContext.Validate())
        {
            return Task.CompletedTask;
        }

        return SaveRequested.InvokeAsync(Clone(Draft));
    }

    private static BasicSalaryRecord Clone(BasicSalaryRecord source) => new()
    {
        Id = source.Id,
        EmployeeId = source.EmployeeId,
        EmployeeCode = source.EmployeeCode,
        EmployeeName = source.EmployeeName,
        DepartmentName = source.DepartmentName,
        DepartmentPath = source.DepartmentPath,
        PositionName = source.PositionName,
        PayrollMonth = source.PayrollMonth,
        PayrollYear = source.PayrollYear,
        BasicSalary = source.BasicSalary,
        StandardWorkingDays = source.StandardWorkingDays,
        DailySalary = source.DailySalary,
        HourlySalary = source.HourlySalary,
        CreatedAtUtc = source.CreatedAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc
    };
}
