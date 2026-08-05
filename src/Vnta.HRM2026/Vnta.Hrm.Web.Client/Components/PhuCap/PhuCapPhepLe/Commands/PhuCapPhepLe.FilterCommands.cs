using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLe
{
    #region Toolbar And Filter Handlers

    /// <summary>Xử lý sự kiện cho luồng <c>OnViewRequestedAsync</c>.</summary>
    private async Task OnViewRequestedAsync()
    {
        if (!CanView)
        {
            return;
        }

        ApplyToolbarPeriod();
        LoadErrorMessage = null;
        currentPageIndex = 0;

        try
        {
            IsLoadingData = true;
            SetLoadingPanelText($"Đang chuẩn bị dữ liệu phụ cấp Phép - Lễ kỳ {RequestedPayrollPeriodDisplay}...");
            await ClearGridSelectionAsync();
            await ExecuteDataOperationAsync(
                token => DataProvider.PreparePeriodAsync(ToolbarYear, ToolbarMonth, token),
                disposalTokenSource.Token);

            AppliedMonth = ToolbarMonth;
            AppliedYear = ToolbarYear;
            HasRequestedData = true;

            await ReloadDataAsync();
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            LoadErrorMessage = "Không thể chuẩn bị dữ liệu phụ cấp Phép - Lễ. Vui lòng thử lại.";
            ToastService.ShowError(LoadErrorMessage);
        }
        finally
        {
            IsLoadingData = false;
            SetLoadingPanelText(DefaultLoadingText);
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnRetryLoadButtonClickAsync</c>.</summary>
    private Task OnRetryLoadButtonClickAsync()
    {
        if (!HasRequestedData || HasPendingPeriodChange)
        {
            return OnViewRequestedAsync();
        }

        return ReloadDataAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedMonthChangedAsync</c>.</summary>
    private Task OnSelectedMonthChangedAsync(int value)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(value, ToolbarYear);
        if (normalizedPeriod.Month == ToolbarMonth && normalizedPeriod.Year == ToolbarYear)
        {
            return Task.CompletedTask;
        }

        (ToolbarMonth, ToolbarYear) = normalizedPeriod;
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedYearChangedAsync</c>.</summary>
    private Task OnSelectedYearChangedAsync(int value)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, value);
        if (normalizedPeriod.Month == ToolbarMonth && normalizedPeriod.Year == ToolbarYear)
        {
            return Task.CompletedTask;
        }

        (ToolbarMonth, ToolbarYear) = normalizedPeriod;
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSearchTextChangedAsync</c>.</summary>
    private Task OnSearchTextChangedAsync(string? value)
    {
        var normalizedValue = NormalizeOptional(value);
        if (string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        SearchText = normalizedValue;
        currentPageIndex = 0;
        if (!HasRequestedData || HasPendingPeriodChange)
        {
            return Task.CompletedTask;
        }

        return ReloadDataAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnPageSizeChangedAsync</c>.</summary>
    private async Task OnPageSizeChangedAsync(int value)
    {
        var normalizedValue = PageSizeOptions.Contains(value) ? value : DefaultPageSize;
        if (PageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;
        SetLoadingPanelText("Đang cập nhật số dòng hiển thị...");

        try
        {
            var firstVisibleRecordIndex = CurrentPageIndex * PageSize;
            pageSize = normalizedValue;
            currentPageIndex = firstVisibleRecordIndex / PageSize;
            ClampCurrentPageIndex();
            await ClearGridSelectionAsync();
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }
        finally
        {
            IsChangingPageSize = false;
            SetLoadingPanelText(DefaultLoadingText);
        }
    }

    /// <summary>Chuyển trang của pager tùy biến.</summary>
    private async Task OnActivePageIndexChangedAsync(int value)
    {
        if (!CanBrowsePages)
        {
            return;
        }

        var normalizedValue = Math.Clamp(value, 0, Math.Max(0, TotalPageCount - 1));
        if (normalizedValue == currentPageIndex)
        {
            return;
        }

        currentPageIndex = normalizedValue;
        await ClearGridSelectionAsync();
    }

    /// <summary>Mở cho luồng <c>OpenRulesPopup</c>.</summary>
    private void OpenRulesPopup()
    {
        IsRulesPopupVisible = true;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnColumnChooserRequested</c>.</summary>
    private Task OnColumnChooserRequested()
    {
        if (!CanOpenColumnChooser)
        {
            return Task.CompletedTask;
        }

        GridSection?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnEmptyStateActionButtonClickAsync</c>.</summary>
    private async Task OnEmptyStateActionButtonClickAsync()
    {
        if (!HasRequestedData || HasPendingPeriodChange)
        {
            await OnViewRequestedAsync();
            return;
        }

        if (HasActiveRefinement)
        {
            await ResetFiltersAndReloadAsync();
            return;
        }

        await ReloadDataAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnLockFilterChipClickAsync</c>.</summary>
    private async Task OnLockFilterChipClickAsync(LeaveHolidayAllowanceLockFilter filter)
    {
        if (!CanChangeFilters || CurrentLockFilter == filter)
        {
            return;
        }

        CurrentLockFilter = filter;
        currentPageIndex = 0;
        ApplyCurrentLockFilter();
        await ClearGridSelectionAsync();
    }

    /// <summary>Đặt lại cho luồng <c>ResetFiltersAndReloadAsync</c>.</summary>
    private async Task ResetFiltersAndReloadAsync()
    {
        SearchText = null;
        CurrentLockFilter = LeaveHolidayAllowanceLockFilter.All;
        currentPageIndex = 0;
        await ReloadDataAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnLockSelectedAsync</c>.</summary>
    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: true);

    /// <summary>Xử lý sự kiện cho luồng <c>OnUnlockSelectedAsync</c>.</summary>
    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: false);

    #endregion
}
