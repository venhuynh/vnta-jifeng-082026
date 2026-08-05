namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemCapBac;

/// <summary>Owns loading, cancellation and stale-result protection for grade configuration.</summary>
public partial class PhuCapTrachNhiemCapBac
{
    private async Task LoadLoadedPeriodAsync()
    {
        if (disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref ReloadState.RequestedVersion);
        CancelActiveReload();
        if (!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        HasRequestedData = true;
        ScreenErrorMessage = null;
        IsLoading = true;

        try
        {
            while (!disposalTokenSource.IsCancellationRequested
                   && ReloadState.ProcessedVersion < Volatile.Read(ref ReloadState.RequestedVersion))
            {
                var requestVersion = Volatile.Read(ref ReloadState.RequestedVersion);
                ReloadState.ProcessedVersion = requestVersion;
                await LoadLoadedPeriodCoreAsync(requestVersion, GetLoadedPeriod());
            }
        }
        finally
        {
            IsLoading = false;
            reloadGate.Release();
        }
    }

    private async Task LoadLoadedPeriodCoreAsync(int requestVersion, ResponsibilityAllowancePeriodKey period)
    {
        using var requestTokenSource = BeginReload();

        try
        {
            await ClearSelectionAsync();
            var config = await GradeConfigurationReadService.GetGradeConfigAsync(
                period.Year,
                period.Month,
                requestTokenSource.Token);
            if (ShouldDiscardReloadResult(requestVersion, period))
            {
                return;
            }

            LoadedGradeRows = config.Grades;
        }
        catch (OperationCanceledException) when (
            disposalTokenSource.IsCancellationRequested || ShouldDiscardReloadResult(requestVersion, period))
        {
            // A newer load superseded this request.
        }
        catch (Exception)
        {
            if (ShouldDiscardReloadResult(requestVersion, period))
            {
                return;
            }

            LoadedGradeRows = [];
            ScreenErrorMessage = "Có lỗi khi tải bảng cấp bậc trách nhiệm. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải bảng cấp bậc trách nhiệm.");
        }
        finally
        {
            if (ReferenceEquals(ReloadState.ActiveRequestTokenSource, requestTokenSource))
            {
                ReloadState.ActiveRequestTokenSource = null;
            }
        }
    }

    private ResponsibilityAllowancePeriodKey GetLoadedPeriod() => new(LoadedYear, LoadedMonth);

    private bool ShouldDiscardReloadResult(int requestVersion, ResponsibilityAllowancePeriodKey period) =>
        requestVersion != Volatile.Read(ref ReloadState.RequestedVersion)
        || period != GetLoadedPeriod();

    private CancellationTokenSource BeginReload()
    {
        var requestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        ReloadState.ActiveRequestTokenSource = requestTokenSource;
        return requestTokenSource;
    }

    private void CancelActiveReload() => ReloadState.ActiveRequestTokenSource?.Cancel();
}
