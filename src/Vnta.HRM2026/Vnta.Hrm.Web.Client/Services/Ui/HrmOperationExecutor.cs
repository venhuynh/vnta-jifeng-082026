using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Services.Ui;

public enum HrmOperationStatus
{
    Succeeded,
    Canceled,
    Failed
}

public sealed record HrmOperationResult<T>(
    HrmOperationStatus Status,
    T? Value = default,
    string? Message = null,
    bool IsRetryable = false)
{
    public bool Succeeded => Status == HrmOperationStatus.Succeeded;
}

public sealed class HrmOperationExecutor(
    IHrmToastService toastService,
    NavigationManager navigationManager)
{
    public async Task<HrmOperationResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string fallbackErrorMessage,
        CancellationToken cancellationToken = default,
        bool showFailureToast = true)
    {
        try
        {
            return new(HrmOperationStatus.Succeeded, await operation(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(HrmOperationStatus.Canceled);
        }
        catch (HrmApiException exception) when (exception.Kind == HrmApiErrorKind.Unauthenticated)
        {
            RedirectToLogin();
            return new(HrmOperationStatus.Failed, Message: exception.UserMessage);
        }
        catch (HrmApiException exception)
        {
            if (showFailureToast)
            {
                toastService.ShowError(exception.UserMessage);
            }

            return new(HrmOperationStatus.Failed, Message: exception.UserMessage, IsRetryable: exception.IsRetryable);
        }
        catch (Exception)
        {
            if (showFailureToast)
            {
                toastService.ShowError(fallbackErrorMessage);
            }

            return new(HrmOperationStatus.Failed, Message: fallbackErrorMessage);
        }
    }

    private void RedirectToLogin()
    {
        var returnUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        var loginUrl = navigationManager.GetUriWithQueryParameters(
            "Account/Login",
            new Dictionary<string, object?>
            {
                ["ReturnUrl"] = returnUrl
            });

        navigationManager.NavigateTo(loginUrl, forceLoad: true);
    }
}
