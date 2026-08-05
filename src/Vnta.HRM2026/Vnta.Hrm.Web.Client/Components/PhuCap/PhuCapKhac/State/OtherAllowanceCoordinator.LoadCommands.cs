using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

public sealed partial class OtherAllowanceCoordinator
{
    private Task LoadAsync() => LoadAsync(isSearchRequest: false);

    private async Task LoadAsync(bool isSearchRequest)
    {
        if(disposalTokenSource.IsCancellationRequested) return;

        var requestVersion = Interlocked.Increment(ref loadRequestVersion);
        var requestCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        var previousRequestCancellationTokenSource = Interlocked.Exchange(ref loadCancellationTokenSource, requestCancellationTokenSource);
        previousRequestCancellationTokenSource?.Cancel();

        IsLoading = true;
        ErrorMessage = null;
        var requestedPageIndex = CurrentPageIndex;
        LoadingText = isSearchRequest ? "Đang tìm kiếm phụ cấp khác..." : DefaultLoadingText;

        try
        {
            var page = await ReadDataProvider.SearchPageAsync(new OtherAllowanceFilter(
                AppliedMonth, AppliedYear, SearchText, Take: PageSize, Skip: requestedPageIndex * PageSize), requestCancellationTokenSource.Token);
            if(requestVersion != loadRequestVersion || disposalTokenSource.IsCancellationRequested) return;

            var lastPageIndex = page.TotalCount <= 0 ? 0 : (int)Math.Ceiling(page.TotalCount / (double)PageSize) - 1;
            if(requestedPageIndex > lastPageIndex)
            {
                currentPageIndex = lastPageIndex;
                await LoadAsync(isSearchRequest);
                return;
            }

            Rows = page.Rows;
            selectedItems = [];
            ServerTotalRecordCount = page.TotalCount;
            TotalAllowanceAmount = page.TotalAllowanceAmount;
            HasRequestedData = true;
            if(IsEditPopupVisible && HasPendingPeriodChange) CloseEditPopupCore();
        }
        catch(OperationCanceledException) when(requestCancellationTokenSource.IsCancellationRequested)
        {
            // A superseded request or a disposed component must not update UI state.
        }
        catch(Exception exception)
        {
            if(requestVersion != loadRequestVersion || disposalTokenSource.IsCancellationRequested) return;

            Logger.LogError(exception, "Không thể tải dữ liệu phụ cấp khác.");
            Rows = [];
            ServerTotalRecordCount = 0;
            TotalAllowanceAmount = 0m;
            ErrorMessage = "Hệ thống chưa thể tải dữ liệu phụ cấp khác. Vui lòng thử lại.";
            ToastService.ShowError(ErrorMessage);
        }
        finally
        {
            if(requestVersion == loadRequestVersion)
            {
                IsLoading = false;
                LoadingText = DefaultLoadingText;
                if(ReferenceEquals(loadCancellationTokenSource, requestCancellationTokenSource)) loadCancellationTokenSource = null;
            }

            requestCancellationTokenSource.Dispose();
        }
    }

    private async Task OnSearchTextChangedAsync(string? searchText)
    {
        if(!CanChangeFilters || disposalTokenSource.IsCancellationRequested) return;

        SearchText = NormalizeSearchText(searchText);
        currentPageIndex = 0;
        if(HasRequestedData && !HasPendingPeriodChange) await LoadAsync(isSearchRequest: true);
    }

    private async Task OnPageSizeChangedAsync(int value)
    {
        var normalizedValue = PageSizeOptions.Contains(value) ? value : PageSizeOptions[0];
        if(normalizedValue == PageSize || !CanChangeFilters) return;

        var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
        pageSize = normalizedValue;
        currentPageIndex = firstVisibleRecordIndex / PageSize;
        ClampCurrentPageIndex();
        await LoadAsync();
    }

    private async Task OnActivePageIndexChangedAsync(int value)
    {
        if(!CanBrowsePages) return;

        var normalizedValue = Math.Clamp(value, 0, Math.Max(0, TotalPageCount - 1));
        if(normalizedValue == CurrentPageIndex) return;

        currentPageIndex = normalizedValue;
        await LoadAsync();
    }

    private async Task OnEmptyStateActionRequestedAsync()
    {
        if(HasNoSearchResults) SearchText = null;
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            await OnViewRequestedAsync();
            return;
        }

        await LoadAsync(isSearchRequest: false);
    }

    private Task OnSelectedMonthChangedAsync(int month)
    {
        if(!CanChangeFilters) return Task.CompletedTask;
        var normalizedPeriod = NormalizeSelectedPeriod(month, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    private Task OnSelectedYearChangedAsync(int year)
    {
        if(!CanChangeFilters) return Task.CompletedTask;
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, year);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    private async Task OnViewRequestedAsync()
    {
        if(!CanView || disposalTokenSource.IsCancellationRequested) return;

        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        var isPeriodChanged = !HasRequestedData || ToolbarMonth != AppliedMonth || ToolbarYear != AppliedYear;
        if(isPeriodChanged)
        {
            CloseEditPopupCore();
            CloseMonthlyWorkPopup();
        }

        AppliedMonth = ToolbarMonth;
        AppliedYear = ToolbarYear;
        currentPageIndex = 0;
        HasRequestedData = true;
        await LoadAsync();
    }
}
