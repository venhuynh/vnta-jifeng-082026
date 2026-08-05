namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Owns loading, stale-result protection, and request cancellation.</summary>
public partial class PhuCapTongHop
{
    private async Task ReloadAsync()
    {
        if(disposalTokenSource.IsCancellationRequested || !HasRequestedData)
        {
            return;
        }

        Interlocked.Increment(ref ReloadState.RequestedVersion);
        CancelActiveReload();
        if(!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        IsLoading = true;
        LoadErrorMessage = null;
        ManualEditErrorMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            while(!disposalTokenSource.IsCancellationRequested && ReloadState.ProcessedVersion < Volatile.Read(ref ReloadState.RequestedVersion))
            {
                var requestVersion = Volatile.Read(ref ReloadState.RequestedVersion);
                ReloadState.ProcessedVersion = requestVersion;
                await ReloadCoreAsync(requestVersion, CreateReloadSnapshot());
            }
        }
        finally
        {
            IsLoading = false;
            reloadGate.Release();
            if(!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task ReloadCoreAsync(int requestVersion, PayrollAllowanceSummaryReloadSnapshot snapshot)
    {
        using var requestTokenSource = BeginReload();
        var cancellationToken = requestTokenSource.Token;

        try
        {
            await ClearSelectionAsync();
            var summary = await DataProvider.GetSummaryAsync(FilterFactory.CreateSummaryFilter(snapshot), cancellationToken);
            var page = await DataProvider.SearchAsync(FilterFactory.CreateListFilter(snapshot), cancellationToken);

            if(ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            if(page.TotalCount > 0)
            {
                var maximumPageIndex = Math.Max(0, (int)Math.Ceiling(page.TotalCount / (double)PageSize) - 1);
                if(snapshot.PageIndex > maximumPageIndex)
                {
                    currentPageIndex = maximumPageIndex;
                    Interlocked.Increment(ref ReloadState.RequestedVersion);
                    return;
                }
            }

            Summary = summary;
            SummaryBadges = BuildSummaryBadges(summary);
            Records = page.Rows;
            totalRecordCount = page.TotalCount;
            ResetVisibleAllowanceTotals();
            IsAllowanceTotalsSyncPending = true;
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested && !ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                throw;
            }
        }
        catch(Exception)
        {
            if(ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            Records = [];
            Summary = EmptySummary;
            SummaryBadges = BuildSummaryBadges(EmptySummary);
            totalRecordCount = 0;
            ResetVisibleAllowanceTotals();
            LoadErrorMessage = "Có lỗi khi tải dữ liệu tổng hợp phụ cấp. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách tổng hợp phụ cấp.");
        }
        finally
        {
            if(ReferenceEquals(ReloadState.ActiveRequestTokenSource, requestTokenSource))
            {
                ReloadState.ActiveRequestTokenSource = null;
            }
        }
    }

    private PayrollAllowanceSummaryReloadSnapshot CreateReloadSnapshot() =>
        new(AppliedMonth, AppliedYear, NormalizeOptional(SearchText), GetLockFilterValue(), CurrentPageIndex, PageSize);

    private bool ShouldDiscardReloadResult(int requestVersion, PayrollAllowanceSummaryReloadSnapshot snapshot) =>
        requestVersion != Volatile.Read(ref ReloadState.RequestedVersion) || !HasRequestedData || snapshot != CreateReloadSnapshot();

    private CancellationTokenSource BeginReload()
    {
        var requestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        ReloadState.ActiveRequestTokenSource = requestTokenSource;
        return requestTokenSource;
    }

    private void CancelActiveReload() => ReloadState.ActiveRequestTokenSource?.Cancel();
}
