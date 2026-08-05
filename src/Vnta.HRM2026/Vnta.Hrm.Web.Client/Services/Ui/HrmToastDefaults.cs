using DevExpress.Blazor;
using System;

namespace Vnta.Hrm.Web.Client.Services.Ui;

public static class HrmToastDefaults {
    public const string ProviderName = "HrmGlobalToast";
    public const string ToastCssClass = "hrm-toast";
    public const int MaxToastCount = 5;
    public const string Width = "360px";
    public const bool ShowCloseButton = true;
    public const bool ShowIcon = true;
    public const bool FreezeOnClick = true;
    public const bool StickToViewport = true;
    public const ToastAnimationType AnimationType = ToastAnimationType.Slide;
    public const HorizontalAlignment HorizontalAlignment = HorizontalAlignment.Right;
    public const VerticalEdge VerticalAlignment = VerticalEdge.Bottom;
    public const ToastRenderStyle RenderStyle = ToastRenderStyle.Primary;
    public const ToastThemeMode ThemeMode = ToastThemeMode.Pastel;
    public const string SuccessTitle = "Thành công";
    public const string InfoTitle = "Thông báo";
    public const string WarningTitle = "Cảnh báo";
    public const string ErrorTitle = "Lỗi";
    public static readonly TimeSpan DisplayTime = TimeSpan.FromSeconds(5);
}
