using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.DangKyPheDuyet.PheDuyetTangCa;

public partial class PheDuyetTangCaStatusActionPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public OvertimeRegistrationStatus? TargetStatus { get; set; }
    [Parameter] public int RequestCount { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback ConfirmRequested { get; set; }

    private bool CanConfirm => !IsSaving && TargetStatus is not null && RequestCount > 0;
    private string RequestCountText => RequestCount == 1 ? "phiếu tăng ca đã chọn" : $"{RequestCount} phiếu tăng ca đã chọn";
    private string Title => TargetStatus switch
    {
        OvertimeRegistrationStatus.Approved => "Phê duyệt phiếu tăng ca",
        OvertimeRegistrationStatus.Returned => "Trả lại phiếu tăng ca",
        OvertimeRegistrationStatus.Rejected => "Từ chối phiếu tăng ca",
        _ => "Cập nhật trạng thái phiếu tăng ca"
    };
    private string PromptText => TargetStatus switch
    {
        OvertimeRegistrationStatus.Approved => $"Duyệt {RequestCountText}?",
        OvertimeRegistrationStatus.Returned => $"Trả lại {RequestCountText}?",
        OvertimeRegistrationStatus.Rejected => $"Từ chối {RequestCountText}?",
        _ => "Xác nhận cập nhật trạng thái phiếu tăng ca?"
    };
    private string ImpactText => TargetStatus switch
    {
        OvertimeRegistrationStatus.Approved => "Phân công tăng ca trong các phiếu này sẽ được áp dụng vào dữ liệu chấm công.",
        OvertimeRegistrationStatus.Returned => "Các phiếu này sẽ được trả về để người đăng ký điều chỉnh và gửi lại phê duyệt.",
        OvertimeRegistrationStatus.Rejected => "Các phiếu này sẽ được chuyển sang trạng thái từ chối.",
        _ => string.Empty
    };
    private string ConfirmText => TargetStatus switch
    {
        OvertimeRegistrationStatus.Approved => "Duyệt",
        OvertimeRegistrationStatus.Returned => "Trả lại",
        OvertimeRegistrationStatus.Rejected => "Từ chối",
        _ => "Xác nhận"
    };
    private string ConfirmIconUrl => TargetStatus switch
    {
        OvertimeRegistrationStatus.Returned => VntaDevExpressIcons.Reset,
        OvertimeRegistrationStatus.Rejected => VntaDevExpressIcons.Cancel,
        _ => VntaDevExpressIcons.TaskList
    };
    private ButtonRenderStyle ConfirmRenderStyle => TargetStatus == OvertimeRegistrationStatus.Rejected
        ? ButtonRenderStyle.Danger
        : ButtonRenderStyle.Primary;

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);

    private Task ConfirmAsync() => ConfirmRequested.InvokeAsync();
}
