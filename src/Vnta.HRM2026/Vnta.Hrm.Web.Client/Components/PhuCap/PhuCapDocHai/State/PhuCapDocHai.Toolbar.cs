using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Đại diện kiểu <c>PhuCapDocHai</c> phục vụ màn hình phụ cấp độc hại.</summary>
public partial class PhuCapDocHai
{
    #region Toolbar And Screen Actions

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedYearChangedAsync</c>.</summary>
    private Task OnSelectedYearChangedAsync(int year)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, year);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedMonthChangedAsync</c>.</summary>
    private Task OnSelectedMonthChangedAsync(int month)
    {
        var normalizedPeriod = NormalizeSelectedPeriod(month, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnViewRequestedAsync</c>.</summary>
    private async Task OnViewRequestedAsync()
    {
        if(!CanView)
        {
            return;
        }

        var normalizedPeriod = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        ToolbarMonth = normalizedPeriod.Month;
        ToolbarYear = normalizedPeriod.Year;
        AppliedMonth = ToolbarMonth;
        AppliedYear = ToolbarYear;
        PageIndex = 0;
        HasRequestedData = true;
        HasLoadError = false;

        await ReloadAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnRetryAsync</c>.</summary>
    private Task OnRetryAsync() =>
        !HasRequestedData || HasPendingPeriodChange
            ? OnViewRequestedAsync()
            : ReloadAsync();

    /// <summary>Xử lý sự kiện cho luồng <c>OnSelectedDataItemsChangedAsync</c>.</summary>
    private Task OnSelectedDataItemsChangedAsync(IReadOnlyList<object> items)
    {
        SelectedGridItems = items;
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnGridFilterCriteriaChangedAsync</c>.</summary>
    private Task OnGridFilterCriteriaChangedAsync(GridFilterCriteriaChangedEventArgs _)
    {
        IsAllowanceTotalSyncPending = true;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSummaryBadgeClickAsync</c>.</summary>
    private async Task OnSummaryBadgeClickAsync(string badgeKey)
    {
        if(string.IsNullOrWhiteSpace(badgeKey)
           || string.Equals(ActiveSummaryBadgeKey, badgeKey, StringComparison.Ordinal))
        {
            return;
        }

        ActiveSummaryBadgeKey = badgeKey;
        PageIndex = 0;
        ResetVisibleAllowanceTotal();
        IsAllowanceTotalSyncPending = true;
        await ClearGridSelectionAsync();
        if(HasRequestedData && !HasPendingPeriodChange)
        {
            await ReloadAsync();
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnSearchTextChanged</c>.</summary>
    private async Task OnSearchTextChanged(string? value)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if(string.Equals(SearchText, normalizedValue, StringComparison.Ordinal))
        {
            return;
        }

        SearchText = normalizedValue;
        PageIndex = 0;
        ResetVisibleAllowanceTotal();
        if(HasRequestedData && !HasPendingPeriodChange)
        {
            await ReloadAsync();
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnPageSizeChangedAsync</c>.</summary>
    private async Task OnPageSizeChangedAsync(int value)
    {
        var normalizedValue = PageSizeOptions.Any(option => option.Value == value)
            ? value
            : PageSizeOptions[0].Value;
        if(normalizedValue == AllPageSize && TotalRecordCount > AllPageSize)
        {
            ToastService.ShowWarning($"Chỉ có thể hiển thị tất cả khi kỳ lương có tối đa {AllPageSize:N0} dòng.");
            return;
        }
        if(PageSize == normalizedValue)
        {
            return;
        }

        IsChangingPageSize = true;
        LoadingText = "Đang cập nhật số dòng hiển thị...";

        try
        {
            var firstVisibleRecordIndex = PageIndex * PageSize;
            PageSize = normalizedValue;
            PageIndex = firstVisibleRecordIndex / PageSize;
            ClampPageIndex();
            await ClearGridSelectionAsync();
            if(HasRequestedData && !HasPendingPeriodChange)
            {
                await ReloadAsync();
            }
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }
        finally
        {
            IsChangingPageSize = false;
            LoadingText = DefaultLoadingText;
        }
    }

    /// <summary>Chuyển trang của pager tùy biến và tải trang tương ứng từ máy chủ.</summary>
    private async Task OnActivePageIndexChangedAsync(int value)
    {
        if(!CanBrowsePages)
        {
            return;
        }

        var normalizedValue = Math.Clamp(value, 0, Math.Max(0, TotalPageCount - 1));
        if(normalizedValue == PageIndex)
        {
            return;
        }

        PageIndex = normalizedValue;
        await ClearGridSelectionAsync();
        await ReloadAsync();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnEmptyStateActionClickAsync</c>.</summary>
    private async Task OnEmptyStateActionClickAsync()
    {
        if(HasLoadError)
        {
            await OnRetryAsync();
            return;
        }

        if(!HasRequestedData || HasPendingPeriodChange)
        {
            await OnViewRequestedAsync();
            return;
        }

        if(HasActiveSearch)
        {
            SearchText = null;
            PageIndex = 0;
            await ReloadAsync();
            return;
        }

        if(HasActiveSummaryBadge)
        {
            ActiveSummaryBadgeKey = SummaryAllKey;
            PageIndex = 0;
            ResetVisibleAllowanceTotal();
            IsAllowanceTotalSyncPending = true;
            await ClearGridSelectionAsync();
            return;
        }

        await OnRecalculateClickAsync();
    }

    /// <summary>Mở cho luồng <c>OpenRulesPopup</c>.</summary>
    private void OpenRulesPopup()
    {
        IsRulesPopupVisible = true;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnRecalculateClickAsync</c>.</summary>
    private Task OnRecalculateClickAsync()
    {
        if(CanRecalculate)
        {
            IsRecalculateConfirmPopupVisible = true;
        }

        return Task.CompletedTask;
    }

    /// <summary>Đóng cho luồng <c>CloseRecalculateConfirmPopup</c>.</summary>
    private void CloseRecalculateConfirmPopup()
    {
        if(!IsRecalculating)
        {
            IsRecalculateConfirmPopupVisible = false;
        }
    }

    /// <summary>Xác nhận cho luồng <c>ConfirmRecalculateAsync</c>.</summary>
    private async Task ConfirmRecalculateAsync()
    {
        if(!CanRecalculate)
        {
            return;
        }

        IsRecalculateConfirmPopupVisible = false;
        await RecalculateAsync();
    }

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanEditRow</c>.</summary>
    private bool CanEditRow(HazardAllowanceListItemDto record) => CanOperateOnCurrentDataset && !record.IsLocked;

    /// <summary>Mở cho luồng <c>OpenEditPopup</c>.</summary>
    private void OpenEditPopup(HazardAllowanceListItemDto record)
    {
        if(!CanEditRow(record))
        {
            return;
        }

        EditModel = new PhuCapDocHaiEditModel
        {
            PayrollAllowanceSummaryRecordId = record.PayrollAllowanceSummaryRecordId,
            EmployeeDisplay = $"{record.EmployeeCode} - {record.EmployeeName}",
            PayrollPeriod = FormatPayrollPeriod(record.PayrollMonth, record.PayrollYear),
            QualifiedWorkdayCount = record.QualifiedWorkdayCount,
            LateEarlyDeductionDays = record.LateEarlyDeductionDays,
            PayableWorkdayCount = record.PayableWorkdayCount,
            HazardAllowancePerDay = RoundVnd(record.HazardAllowancePerDay),
            HazardAllowanceAmount = RoundVnd(record.HazardAllowanceAmount),
            IsEligibleDepartment = record.IsEligibleDepartment,
            ExclusionReason = record.ExclusionReason,
            OriginalDetailUpdatedAtUtc = record.UpdatedAtUtc ?? record.CreatedAtUtc,
            OriginalSummaryUpdatedAtUtc = record.SummaryUpdatedAtUtc ?? record.UpdatedAtUtc ?? record.CreatedAtUtc
        };
        IsEditPopupVisible = true;
    }

    /// <summary>Đóng popup điều chỉnh khi không có thao tác lưu đang chạy.</summary>
    private void CloseEditPopup()
    {
        if(IsSavingEdit)
        {
            return;
        }

        IsEditPopupVisible = false;
    }

    /// <summary>Lưu cho luồng <c>SaveEditAsync</c>.</summary>
    private async Task SaveEditAsync()
    {
        if(!CanSaveEdit)
        {
            return;
        }

        var validationMessage = ValidateEditModel(EditModel);
        if(!string.IsNullOrWhiteSpace(validationMessage))
        {
            ToastService.ShowWarning(validationMessage);
            return;
        }

        try
        {
            IsSavingEdit = true;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            await ExecuteDataOperationAsync(
                cancellationToken => DataProvider.UpdateManualValuesAsync(
                    new UpdateHazardAllowanceManualValuesRequest(
                        EditModel.PayrollAllowanceSummaryRecordId,
                        EditModel.QualifiedWorkdayCount,
                        EditModel.LateEarlyDeductionDays,
                        EditModel.HazardAllowancePerDay,
                        EditModel.HazardAllowanceAmount,
                        EditModel.IsEligibleDepartment,
                        EditModel.ExclusionReason,
                        EditModel.OriginalDetailUpdatedAtUtc,
                        EditModel.OriginalSummaryUpdatedAtUtc,
                        RequestedBy: string.Empty),
                    cancellationToken));

            IsEditPopupVisible = false;
            await ReloadAsync();
            ToastService.ShowSuccess("Đã điều chỉnh phụ cấp độc hại.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(HazardAllowanceConflictException ex)
        {
            ShowOperationFailure(ex, "lưu điều chỉnh");
        }
        catch(InvalidOperationException ex)
        {
            ShowOperationFailure(ex, "lưu điều chỉnh");
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Không thể điều chỉnh phụ cấp độc hại cho kỳ {PayrollMonth}/{PayrollYear}.", AppliedMonth, AppliedYear);
            ShowOperationFailure(ex, "lưu điều chỉnh");
        }
        finally
        {
            IsSavingEdit = false;
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnColumnChooserRequested</c>.</summary>
    private Task OnColumnChooserRequested()
    {
        Grid?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    #endregion
}
