using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.NhanSu.ChiTietNhanVien;

namespace Vnta.Hrm.Web.Client.Components.NhanSu.ChiTietNhanVien;

public partial class ChiTietNhanVienEditForm
{
    private static readonly IReadOnlyList<ChiTietNhanVienStatusOption> StatusOptions =
    [
        new(ChiTietNhanVienEmploymentStatus.Probation, "Thử việc"),
        new(ChiTietNhanVienEmploymentStatus.Official, "Chính thức"),
        new(ChiTietNhanVienEmploymentStatus.Resigned, "Nghỉ việc")
    ];

    private ChiTietNhanVienEditModel? observedModel;

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public bool IsCreateMode { get; set; }

    [Parameter]
    public ChiTietNhanVienEditModel? Model { get; set; }

    [Parameter]
    public IReadOnlyList<AttendanceDepartmentRecord> Departments { get; set; } = [];

    [Parameter]
    public IReadOnlyList<AttendancePositionRecord> Positions { get; set; } = [];

    [Parameter]
    public bool IsBusy { get; set; }

    [Parameter]
    public string? LookupErrorMessage { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public EventCallback<ChiTietNhanVienEditModel> SaveRequested { get; set; }

    private EditContext? EditContext { get; set; }
    private bool CanEdit => !IsBusy && string.IsNullOrWhiteSpace(LookupErrorMessage);
    private bool CanSave => CanEdit && Model is not null;
    private bool CanClose => !IsBusy;
    private string HeaderText => IsCreateMode ? "Tạo hồ sơ nhân viên" : "Điều chỉnh hồ sơ nhân viên";
    private string SaveButtonText => IsCreateMode ? "Tạo mới" : "Lưu";

    protected override void OnParametersSet()
    {
        if(ReferenceEquals(observedModel, Model))
        {
            return;
        }

        observedModel = Model;
        EditContext = Model is null ? null : new EditContext(Model);
    }

    private async Task SaveAsync()
    {
        if(!CanSave || Model is null || EditContext is null || !EditContext.Validate())
        {
            return;
        }

        await SaveRequested.InvokeAsync(Model);
    }

    private async Task CancelAsync()
    {
        if(CanClose)
        {
            await VisibleChanged.InvokeAsync(false);
        }
    }

    private async Task OnVisibleChangedAsync(bool visible)
    {
        if(visible || CanClose)
        {
            await VisibleChanged.InvokeAsync(visible);
        }
    }

    private sealed record ChiTietNhanVienStatusOption(ChiTietNhanVienEmploymentStatus Value, string Text);
}
