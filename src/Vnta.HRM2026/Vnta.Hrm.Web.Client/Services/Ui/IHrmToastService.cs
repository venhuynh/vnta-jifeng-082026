using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using System;

namespace Vnta.Hrm.Web.Client.Services.Ui;

public interface IHrmToastService {
    void Show(
        string message,
        string? title = null,
        Action<ToastOptions>? configure = null,
        RenderFragment? template = null);

    void ShowSuccess(
        string message,
        string? title = null,
        Action<ToastOptions>? configure = null,
        RenderFragment? template = null);

    void ShowInfo(
        string message,
        string? title = null,
        Action<ToastOptions>? configure = null,
        RenderFragment? template = null);

    void ShowWarning(
        string message,
        string? title = null,
        Action<ToastOptions>? configure = null,
        RenderFragment? template = null);

    void ShowError(
        string message,
        string? title = null,
        Action<ToastOptions>? configure = null,
        RenderFragment? template = null);

    void Close(string toastId);
}
