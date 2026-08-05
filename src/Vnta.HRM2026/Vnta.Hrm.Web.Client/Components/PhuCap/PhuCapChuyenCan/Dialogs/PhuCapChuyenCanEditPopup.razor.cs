using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Dialogs;

using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Models;

/// <summary>Đại diện kiểu <c>PhuCapChuyenCanEditPopup</c> phục vụ màn hình phụ cấp chuyên cần.</summary>
public partial class PhuCapChuyenCanEditPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public PhuCapChuyenCanEditModel Model { get; set; } = default!;
    [Parameter] public EditContext EditContext { get; set; } = default!;
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) =>
        !visible && IsSaving ? Task.CompletedTask : VisibleChanged.InvokeAsync(visible);

    /// <summary>Đóng cho luồng <c>CloseAsync</c>.</summary>
    private Task CloseAsync() => IsSaving ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);

    /// <summary>Lưu cho luồng <c>SaveAsync</c>.</summary>
    private Task SaveAsync() => SaveRequested.InvokeAsync();
}
