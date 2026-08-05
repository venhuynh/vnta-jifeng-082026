using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

public sealed partial class OtherResponsibilityAllowanceCoordinator
{
    private Task OnOpenRulesPopupClick()
    {
        OpenRulesPopup();
        return Task.CompletedTask;
    }

    private void OpenRulesPopup() => IsRulesPopupVisible = true;

    private Task OnColumnChooserRequested()
    {
        AllowanceGrid?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    private async Task RunScreenActionAsync(string loadingText, Func<Task> action)
    {
        IsRunningScreenAction = true;
        CurrentLoadingText = loadingText;
        await RequestRenderAsync();
        await Task.Yield();
        try
        {
            await action();
        }
        finally
        {
            IsRunningScreenAction = false;
            ResetLoadingText();
        }
    }

    private void ResetLoadingText() => CurrentLoadingText = HrmUiDefaults.LoadingText;
    private Task RequestRenderAsync() => requestRenderAsync?.Invoke() ?? Task.CompletedTask;
}
