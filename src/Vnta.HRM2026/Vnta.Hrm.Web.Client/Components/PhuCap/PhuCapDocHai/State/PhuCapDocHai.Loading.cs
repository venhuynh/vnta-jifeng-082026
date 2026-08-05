using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Đại diện kiểu <c>PhuCapDocHai</c> phục vụ màn hình phụ cấp độc hại.</summary>
public partial class PhuCapDocHai
{
    #region Data Loading

    /// <summary>Thực hiện xử lý cho luồng <c>ReloadAsync</c>.</summary>
    private async Task ReloadAsync()
    {
        if(!HasRequestedData || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        Interlocked.Increment(ref reloadRequestedVersion);
        if(!await reloadGate.WaitAsync(0, disposalTokenSource.Token))
        {
            return;
        }

        try
        {
            while(!disposalTokenSource.IsCancellationRequested
                  && reloadProcessedVersion < Volatile.Read(ref reloadRequestedVersion))
            {
                reloadProcessedVersion = Volatile.Read(ref reloadRequestedVersion);
                await ReloadCoreAsync();
            }
        }
        finally
        {
            reloadGate.Release();
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ReloadCoreAsync</c>.</summary>
    private async Task ReloadCoreAsync()
    {
        BeginBusyState(DefaultLoadingText);

        try
        {
            await LoadGridRecordsAsync();
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Không thể tải phụ cấp độc hại cho kỳ {PayrollMonth}/{PayrollYear}.", AppliedMonth, AppliedYear);
            LoadedRecords = [];
            ResetVisibleAllowanceTotal();
            HasLoadError = true;
            ToastService.ShowError("Không thể tải danh sách phụ cấp độc hại.");
        }
        finally
        {
            EndBusyState();
        }
    }

    /// <summary>Tải cho luồng <c>LoadGridRecordsAsync</c>.</summary>
    private async Task LoadGridRecordsAsync()
    {
        var result = await ExecuteDataOperationAsync(async cancellationToken =>
        {
            await ClearGridSelectionAsync();
            var baseFilter = BuildBaseFilter();
            var summary = await DataProvider.GetSummaryAsync(baseFilter, cancellationToken);
            var page = await DataProvider.SearchPageAsync(BuildPageFilter(), cancellationToken);
            if(page.TotalCount > 0)
            {
                var lastPageIndex = Math.Max(0, (int)Math.Ceiling(page.TotalCount / (double)PageSize) - 1);
                if(PageIndex > lastPageIndex)
                {
                    PageIndex = lastPageIndex;
                    page = await DataProvider.SearchPageAsync(BuildPageFilter(), cancellationToken);
                }
            }
            return (Summary: summary, Page: page);
        });

        Summary = result.Summary;
        LoadedRecords = result.Page.Rows;
        TotalCount = Math.Max(0, result.Page.TotalCount);
        ClampPageIndex();
        ResetVisibleAllowanceTotal();
        IsAllowanceTotalSyncPending = true;
    }

    /// <summary>Tuần tự hóa mọi truy cập dữ liệu dùng chung DbContext của InteractiveServer circuit.</summary>
    private async Task ExecuteDataOperationAsync(Func<CancellationToken, Task> operation)
    {
        await dataOperationGate.WaitAsync(disposalTokenSource.Token);
        try
        {
            await operation(disposalTokenSource.Token);
        }
        finally
        {
            dataOperationGate.Release();
        }
    }

    /// <summary>Phiên bản trả kết quả cho query hoặc command cần DTO về UI.</summary>
    private async Task<T> ExecuteDataOperationAsync<T>(Func<CancellationToken, Task<T>> operation)
    {
        await dataOperationGate.WaitAsync(disposalTokenSource.Token);
        try
        {
            return await operation(disposalTokenSource.Token);
        }
        finally
        {
            dataOperationGate.Release();
        }
    }

    #endregion
    #region Selection Helpers

    /// <summary>Thực hiện xử lý cho luồng <c>ClearGridSelectionAsync</c>.</summary>
    private async Task ClearGridSelectionAsync()
    {
        SelectedGridItems = [];

        if(Grid is null)
        {
            return;
        }

        await Grid.DeselectAllAsync();
        Grid.SetFocusedRowIndex(-1);
    }

    /// <summary>Lấy cho luồng <c>GetSelectedVisibleRecords</c>.</summary>
    private List<HazardAllowanceListItemDto> GetSelectedVisibleRecords() =>
        SelectedGridItems
            .OfType<HazardAllowanceListItemDto>()
            .Where(IsVisibleGridRecord)
            .DistinctBy(record => record.PayrollAllowanceSummaryRecordId)
            .ToList();

    /// <summary>Lấy cho luồng <c>GetSelectedVisibleRecordCount</c>.</summary>
    private int GetSelectedVisibleRecordCount() => GetSelectedVisibleRecords().Count;

    /// <summary>Kiểm tra trạng thái cho luồng <c>IsVisibleGridRecord</c>.</summary>
    private bool IsVisibleGridRecord(HazardAllowanceListItemDto record) =>
        PagedRecords.Any(row => row.PayrollAllowanceSummaryRecordId == record.PayrollAllowanceSummaryRecordId);

    #endregion
}
