using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Owns the load lifecycle and stale-result protection for the page.</summary>
public partial class PhuCapCom
{
    private async Task ReloadAsync()
    {
        if(disposalTokenSource.IsCancellationRequested || !HasAppliedPeriod)
        {
            return;
        }

        Interlocked.Increment(ref ReloadState.RequestedVersion);
        CancelActiveReload();
        if(!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        HasRequestedData = true;
        IsLoading = true;
        LoadErrorMessage = null;
        LoadingText = "Đang tải dữ liệu phụ cấp cơm...";
        await InvokeAsync(StateHasChanged);

        try
        {
            while(!disposalTokenSource.IsCancellationRequested
                  && !HasPendingPeriodChange
                  && ReloadState.ProcessedVersion < Volatile.Read(ref ReloadState.RequestedVersion))
            {
                var requestVersion = Volatile.Read(ref ReloadState.RequestedVersion);
                ReloadState.ProcessedVersion = requestVersion;
                await ReloadCoreAsync(requestVersion, CreateReloadSnapshot());
            }
        }
        finally
        {
            IsLoading = false;
            LoadingText = HrmUiDefaults.LoadingText;
            reloadGate.Release();

            if(!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task ReloadCoreAsync(
        int requestVersion,
        MealAllowanceReloadSnapshot snapshot)
    {
        using var requestTokenSource = BeginReload();
        var cancellationToken = requestTokenSource.Token;

        try
        {
            var summary = await DataProvider.GetSummaryAsync(
                FilterFactory.CreateSummaryFilter(snapshot, PageSizeOptions[^1].Value),
                cancellationToken);
            if(ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            var page = await DataProvider.SearchPageAsync(
                FilterFactory.CreateListFilter(
                    snapshot,
                    SelectedAllowanceSummaryKey == SummaryAllKey
                        ? null
                        : SelectedAllowanceSummaryKey),
                cancellationToken);
            if(ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            if(snapshot.PageSize == AllPageSize && page.TotalCount > AllPageSize)
            {
                pageSize = PageSizeOptions[0].Value;
                currentPageIndex = 0;
                ToastService.ShowWarning($"Kỳ lương có hơn {AllPageSize:N0} dòng nên màn hình chuyển về 20 dòng/trang.");
                Interlocked.Increment(ref ReloadState.RequestedVersion);
                return;
            }

            var maximumPageIndex = page.TotalCount <= 0
                ? 0
                : Math.Max(0, (int)Math.Ceiling(page.TotalCount / (double)snapshot.PageSize) - 1);
            if(snapshot.PageIndex > maximumPageIndex)
            {
                currentPageIndex = maximumPageIndex;
                Interlocked.Increment(ref ReloadState.RequestedVersion);
                return;
            }

            CurrentSummary = summary;
            Records = page.Rows;
            totalRecordCount = page.TotalCount;
            await PruneSelectionToVisibleRecordsAsync();
        }
        catch(OperationCanceledException) when(
            disposalTokenSource.IsCancellationRequested || ShouldDiscardReloadResult(requestVersion, snapshot))
        {
            // Changing filters or disposing the component is expected.
        }
        catch(Exception)
        {
            if(ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            Records = [];
            totalRecordCount = 0;
            CurrentSummary = EmptySummary;
            LoadErrorMessage = "Không thể tải dữ liệu phụ cấp cơm. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải danh sách phụ cấp cơm.");
        }
        finally
        {
            if(ReferenceEquals(ReloadState.ActiveRequestTokenSource, requestTokenSource))
            {
                ReloadState.ActiveRequestTokenSource = null;
            }
        }
    }

    private MealAllowanceReloadSnapshot CreateReloadSnapshot()
    {
        if(AppliedMonth is not { } appliedMonth || AppliedYear is not { } appliedYear)
        {
            throw new InvalidOperationException("Chưa có kỳ lương đã áp dụng để tải dữ liệu phụ cấp cơm.");
        }

        return new MealAllowanceReloadSnapshot(
            appliedMonth,
            appliedYear,
            SearchText,
            CurrentPageIndex,
            PageSize);
    }

    private bool ShouldDiscardReloadResult(
        int requestVersion,
        MealAllowanceReloadSnapshot snapshot) =>
        requestVersion != Volatile.Read(ref ReloadState.RequestedVersion)
        || !HasAppliedPeriod
        || HasPendingPeriodChange
        || snapshot != CreateReloadSnapshot();

    private CancellationTokenSource BeginReload()
    {
        var requestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        ReloadState.ActiveRequestTokenSource = requestTokenSource;
        return requestTokenSource;
    }

    private void CancelActiveReload() => ReloadState.ActiveRequestTokenSource?.Cancel();
}
