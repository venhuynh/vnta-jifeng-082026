namespace Vnta.Hrm.Web.Client.Services.Api;

internal static class HrmReadRetryPolicy
{
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan[] RetryDelays = [
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(600)
    ];

    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutTokenSource.CancelAfter(AttemptTimeout);

            try
            {
                return await operation(timeoutTokenSource.Token);
            }
            catch (HrmApiException exception) when (exception.IsRetryable && attempt < RetryDelays.Length)
            {
                await Task.Delay(RetryDelays[attempt], cancellationToken);
            }
            catch (HttpRequestException) when (attempt < RetryDelays.Length)
            {
                await Task.Delay(RetryDelays[attempt], cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested
                                                       && timeoutTokenSource.IsCancellationRequested
                                                       && attempt < RetryDelays.Length)
            {
                await Task.Delay(RetryDelays[attempt], cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested
                                                       && timeoutTokenSource.IsCancellationRequested)
            {
                throw new HrmApiException(
                    HrmApiErrorKind.Unavailable,
                    System.Net.HttpStatusCode.ServiceUnavailable,
                    "Dịch vụ đang phản hồi chậm. Vui lòng thử lại.",
                    traceId: null);
            }
        }
    }
}
