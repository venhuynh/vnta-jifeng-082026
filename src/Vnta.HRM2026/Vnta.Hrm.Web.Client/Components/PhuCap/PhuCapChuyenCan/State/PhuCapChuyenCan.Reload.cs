using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.State;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan;

public partial class PhuCapChuyenCan
{
    /// <summary>Thực hiện xử lý cho luồng <c>ReloadAsync</c>.</summary>
    private async Task ReloadAsync()
    {
        if(!HasRequestedData || HasPendingPeriodChange || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref ReloadLifecycleState.RequestedVersion);
        CancelActiveReload();
        if(!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        IsLoading = true;
        HasLoadError = false;
        CurrentLoadingText = "Đang tải dữ liệu phụ cấp chuyên cần...";
        await InvokeAsync(StateHasChanged);

        try
        {
            while(!disposalTokenSource.IsCancellationRequested
                  && !HasPendingPeriodChange
                  && ReloadLifecycleState.ProcessedVersion < Volatile.Read(ref ReloadLifecycleState.RequestedVersion))
            {
                var requestVersion = Volatile.Read(ref ReloadLifecycleState.RequestedVersion);
                ReloadLifecycleState.ProcessedVersion = requestVersion;
                await ReloadCoreAsync(requestVersion, CreateReloadSnapshot());
            }
        }
        finally
        {
            IsLoading = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
            reloadGate.Release();

            if(!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ReloadCoreAsync</c>.</summary>
    private async Task ReloadCoreAsync(
        int requestVersion,
        AttendanceAllowanceReloadSnapshot snapshot)
    {
        using var requestTokenSource = BeginReload();
        var cancellationToken = requestTokenSource.Token;

        try
        {
            await ClearSelectionAsync();
            var page = await ReadDataProvider.SearchPageAsync(
                FilterFactory.CreatePageFilter(snapshot),
                cancellationToken);

            if(ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            if(page.TotalCount == 0 && snapshot.PageIndex != 0)
            {
                currentPageIndex = 0;
                Interlocked.Increment(ref ReloadLifecycleState.RequestedVersion);
                return;
            }

            if(page.TotalCount > 0)
            {
                if(snapshot.PageSize == AllPageSize && page.TotalCount > AllPageSize)
                {
                    pageSize = PageSizeOptions[0].Value;
                    currentPageIndex = 0;
                    ToastService.ShowWarning($"Kỳ lương có hơn {AllPageSize:N0} dòng nên màn hình chuyển về 20 dòng/trang.");
                    Interlocked.Increment(ref ReloadLifecycleState.RequestedVersion);
                    return;
                }

                var maximumPageIndex = Math.Max(0, (int)Math.Ceiling(page.TotalCount / (double)PageSize) - 1);
                if(snapshot.PageIndex > maximumPageIndex)
                {
                    currentPageIndex = maximumPageIndex;
                    Interlocked.Increment(ref ReloadLifecycleState.RequestedVersion);
                    return;
                }
            }

            Records = page.Rows;
            totalRecordCount = page.TotalCount;
            periodTotalCount = page.PeriodTotalCount;
            periodCanLockCount = page.PeriodCanLockCount;
            periodCanUnlockCount = page.PeriodCanUnlockCount;
            periodSummaryLockedCount = page.PeriodSummaryLockedCount;
            SummaryBadges = BuildSummaryBadges(
                page.OpenCount,
                page.LockedCount,
                page.AttendanceClassACount,
                page.AttendanceClassBCount,
                page.AttendanceClassCCount);
        }
        catch(OperationCanceledException) when(
            disposalTokenSource.IsCancellationRequested || ShouldDiscardReloadResult(requestVersion, snapshot))
        {
            // Đổi kỳ, đổi trang hoặc hủy component: kết quả cũ không được phép commit.
        }
        catch(Exception)
        {
            if(ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            Records = [];
            totalRecordCount = 0;
            periodTotalCount = 0;
            periodCanLockCount = 0;
            periodCanUnlockCount = 0;
            periodSummaryLockedCount = 0;
            SummaryBadges = BuildSummaryBadges(0, 0, 0, 0, 0);
            HasLoadError = true;
            ToastService.ShowError("Không thể tải danh sách phụ cấp chuyên cần.");
        }
        finally
        {
            if(ReferenceEquals(ReloadLifecycleState.ActiveRequestTokenSource, requestTokenSource))
            {
                ReloadLifecycleState.ActiveRequestTokenSource = null;
            }
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>CreateReloadSnapshot</c>.</summary>
    private AttendanceAllowanceReloadSnapshot CreateReloadSnapshot() =>
        new(
            AppliedMonth,
            AppliedYear,
            NormalizeNullable(SearchText),
            GetLockStateForBadge(ActiveSummaryBadgeKey),
            GetAttendanceClassForBadge(ActiveSummaryBadgeKey),
            CurrentPageIndex,
            PageSize);

    /// <summary>Thực hiện xử lý cho luồng <c>ShouldDiscardReloadResult</c>.</summary>
    private bool ShouldDiscardReloadResult(
        int requestVersion,
        AttendanceAllowanceReloadSnapshot snapshot) =>
        requestVersion != Volatile.Read(ref ReloadLifecycleState.RequestedVersion)
        || !HasRequestedData
        || HasPendingPeriodChange
        || snapshot != CreateReloadSnapshot();

    /// <summary>Thực hiện xử lý cho luồng <c>BeginReload</c>.</summary>
    private CancellationTokenSource BeginReload()
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        ReloadLifecycleState.ActiveRequestTokenSource = cancellationTokenSource;
        return cancellationTokenSource;
    }

    /// <summary>Kiểm tra điều kiện cho luồng <c>CancelActiveReload</c>.</summary>
    private void CancelActiveReload() => ReloadLifecycleState.ActiveRequestTokenSource?.Cancel();
}
