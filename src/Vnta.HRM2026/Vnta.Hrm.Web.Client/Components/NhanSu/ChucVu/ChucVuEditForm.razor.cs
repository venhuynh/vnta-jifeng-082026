using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Components.NhanSu.ChucVu;

public partial class ChucVuEditForm
{
    [Parameter] public AttendancePositionRecord? Model { get; set; }

    [Parameter] public GridEditFormTemplateContext? EditFormContext { get; set; }

    [Parameter] public string? ErrorMessage { get; set; }

    [Parameter] public bool IsCreatingNewPosition { get; set; }

    string FormGroupCaption => IsCreatingNewPosition
        ? "Thông tin khởi tạo"
        : "Thông tin điều chỉnh";

    bool CanShowEmployeeCount => !IsCreatingNewPosition && Model is not null;
}
