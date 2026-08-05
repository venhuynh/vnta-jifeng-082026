using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Owns snapshot loading, cancellation and stale-result protection.</summary>
public partial class PhuCapTrachNhiem
{
    private async Task ReloadAsync()
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
        LoadErrorMessage = null;
        IsLoading = true;

        try
        {
            while (!disposalTokenSource.IsCancellationRequested
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
            reloadGate.Release();
        }
    }

    private async Task ReloadCoreAsync(int requestVersion, PhuCapTrachNhiemReloadSnapshot snapshot)
    {
        using var requestTokenSource = BeginReload();

        try
        {
            await ClearSelectionAsync();
            var page = await AbcQueryProvider.SearchAsync(
                QueryFactory.Create(snapshot),
                requestTokenSource.Token);
            if (ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            // A mutation can make the current page exceed the last page. Schedule
            // a new immutable query for the corrected page instead of rendering it empty.
            var lastPageIndex = page.TotalCount <= 0
                ? 0
                : (int)Math.Ceiling(page.TotalCount / (double)snapshot.PageSize) - 1;
            if (snapshot.PageIndex > lastPageIndex)
            {
                currentPageIndex = lastPageIndex;
                Interlocked.Increment(ref ReloadState.RequestedVersion);
                return;
            }

            AbcRows = page.Rows;
            AbcTotalCount = page.TotalCount;
            AbcSummary = page.Summary;
            ClampCurrentPageIndex();
            SyncPopupTargetsAfterReload();
        }
        catch (OperationCanceledException) when (
            disposalTokenSource.IsCancellationRequested || ShouldDiscardReloadResult(requestVersion, snapshot))
        {
            // A newer UI snapshot superseded this request.
        }
        catch (Exception)
        {
            if (ShouldDiscardReloadResult(requestVersion, snapshot))
            {
                return;
            }

            AbcRows = [];
            AbcTotalCount = 0;
            AbcSummary = new PayrollResponsibilityAllowanceAbcSummaryDto(0, 0, 0, 0, 0, 0, 0, 0);
            LoadErrorMessage = "Có lỗi khi tải dữ liệu phụ cấp trách nhiệm. Vui lòng thử lại.";
            ToastService.ShowError("Không thể tải workflow phụ cấp trách nhiệm.");
        }
        finally
        {
            if (ReferenceEquals(ReloadState.ActiveRequestTokenSource, requestTokenSource))
            {
                ReloadState.ActiveRequestTokenSource = null;
            }
        }
    }

    private PhuCapTrachNhiemReloadSnapshot CreateReloadSnapshot() =>
        new(
            AppliedYear,
            AppliedMonth,
            SearchText,
            string.Equals(ActiveSummaryBadgeKey, SummaryAllKey, StringComparison.Ordinal)
                ? null
                : ActiveSummaryBadgeKey,
            CurrentPageIndex,
            PageSize);

    private bool ShouldDiscardReloadResult(int requestVersion, PhuCapTrachNhiemReloadSnapshot snapshot) =>
        requestVersion != Volatile.Read(ref ReloadState.RequestedVersion)
        || HasPendingPeriodChange
        || snapshot != CreateReloadSnapshot();

    private CancellationTokenSource BeginReload()
    {
        var requestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        ReloadState.ActiveRequestTokenSource = requestTokenSource;
        return requestTokenSource;
    }

    private void CancelActiveReload() => ReloadState.ActiveRequestTokenSource?.Cancel();

    private void InvalidateReloadForPendingPeriodChange()
    {
        if (!HasRequestedData)
        {
            return;
        }

        Interlocked.Increment(ref ReloadState.RequestedVersion);
        CancelActiveReload();
    }
}
