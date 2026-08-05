using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Models.Employees;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.GiamTruGiaCanh;

public partial class GiamTruGiaCanhEditPopup
{
    #region Trạng thái và tham số

    private GiamTruGiaCanhEditModel? editContextModel;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = "Cập nhật người phụ thuộc";
    [Parameter] public GiamTruGiaCanhEditModel Model { get; set; } = new();
    [Parameter] public IReadOnlyList<EmployeeRecord> Employees { get; set; } = [];
    [Parameter] public IReadOnlyList<string> GenderOptions { get; set; } = [];
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }

    #endregion

    #region Trạng thái suy diễn

    private EditContext EditContext { get; set; } = default!;
    private bool CanEditFields => !IsSaving;
    private bool CanChangeEmployee => CanEditFields && Model.Id == Guid.Empty;
    private bool CanSave => !IsSaving
        && Model.EmployeeId != Guid.Empty
        && !string.IsNullOrWhiteSpace(Model.DependentFullName);
    private string EmployeeDisplay => Employees
        .FirstOrDefault(employee => employee.Id == Model.EmployeeId)?
        .EmployeeLookupText
        ?? "—";

    #endregion

    #region Vòng đời và sự kiện

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(editContextModel, Model))
        {
            editContextModel = Model;
            EditContext = new EditContext(Model);
        }
    }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);

    private Task SaveAsync()
    {
        if (CanSave && EditContext.Validate())
        {
            return SaveRequested.InvokeAsync();
        }

        return Task.CompletedTask;
    }

    #endregion
}
