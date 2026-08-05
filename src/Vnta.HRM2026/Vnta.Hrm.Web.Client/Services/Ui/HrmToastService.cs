using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using System;
using System.Linq;

namespace Vnta.Hrm.Web.Client.Services.Ui;

public sealed class HrmToastService(IToastNotificationService toastService) : IHrmToastService {
    public void Show(
        string message,
        string? title = null,
        Action<ToastOptions>? configure = null,
        RenderFragment? template = null) =>
        ShowCore(message, title, configure, template);

    public void ShowSuccess(
        string message,
        string? title = null,
        Action<ToastOptions>? configure = null,
        RenderFragment? template = null) =>
        ShowSemantic(
            message,
            title ?? HrmToastDefaults.SuccessTitle,
            ToastRenderStyle.Success,
            ToastThemeMode.Saturated,
            configure,
            template);

    public void ShowInfo(
        string message,
        string? title = null,
        Action<ToastOptions>? configure = null,
        RenderFragment? template = null) =>
        ShowSemantic(
            message,
            title ?? HrmToastDefaults.InfoTitle,
            ToastRenderStyle.Info,
            ToastThemeMode.Pastel,
            configure,
            template);

    public void ShowWarning(
        string message,
        string? title = null,
        Action<ToastOptions>? configure = null,
        RenderFragment? template = null) =>
        ShowSemantic(
            message,
            title ?? HrmToastDefaults.WarningTitle,
            ToastRenderStyle.Warning,
            ToastThemeMode.Light,
            configure,
            template);

    public void ShowError(
        string message,
        string? title = null,
        Action<ToastOptions>? configure = null,
        RenderFragment? template = null) =>
        ShowSemantic(
            message,
            title ?? HrmToastDefaults.ErrorTitle,
            ToastRenderStyle.Danger,
            ToastThemeMode.Saturated,
            configure,
            template);

    public void Close(string toastId) {
        if (string.IsNullOrWhiteSpace(toastId)) {
            return;
        }

        toastService.CloseToast(toastId);
    }

    void ShowSemantic(
        string message,
        string title,
        ToastRenderStyle renderStyle,
        ToastThemeMode themeMode,
        Action<ToastOptions>? configure,
        RenderFragment? template) =>
        ShowCore(
            message,
            title,
            options => {
                options.RenderStyle = renderStyle;
                options.ThemeMode = themeMode;
                configure?.Invoke(options);
            },
            template);

    void ShowCore(
        string message,
        string? title,
        Action<ToastOptions>? configure,
        RenderFragment? template) {
        if (string.IsNullOrWhiteSpace(message)) {
            return;
        }

        var toastOptions = CreateBaseOptions(message, title);
        configure?.Invoke(toastOptions);
        toastOptions.CssClass = MergeCssClasses(HrmToastDefaults.ToastCssClass, toastOptions.CssClass);

        if (template is null) {
            toastService.ShowToast(toastOptions);
            return;
        }

        toastService.ShowToast(toastOptions, template);
    }

    static ToastOptions CreateBaseOptions(string message, string? title) =>
        new() {
            ProviderName = HrmToastDefaults.ProviderName,
            RenderStyle = HrmToastDefaults.RenderStyle,
            ThemeMode = HrmToastDefaults.ThemeMode,
            DisplayTime = HrmToastDefaults.DisplayTime,
            FreezeOnClick = HrmToastDefaults.FreezeOnClick,
            ShowCloseButton = HrmToastDefaults.ShowCloseButton,
            ShowIcon = HrmToastDefaults.ShowIcon,
            CssClass = HrmToastDefaults.ToastCssClass,
            Title = title,
            Text = message
        };

    static string MergeCssClasses(params string?[] cssClasses) =>
        string.Join(
            " ",
            cssClasses
                .Where(cssClass => !string.IsNullOrWhiteSpace(cssClass))
                .SelectMany(cssClass => cssClass!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.Ordinal));
}
