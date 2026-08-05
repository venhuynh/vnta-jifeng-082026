using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan;

public partial class PhuCapChuyenCan
{
    /// <summary>Đặt lại cho luồng <c>ResetFiltersAsync</c>.</summary>
    private async Task ResetFiltersAsync()
    {
        ToolbarMonth = DefaultPayrollPeriod.Month;
        ToolbarYear = DefaultPayrollPeriod.Year;
        SearchText = null;
        ActiveSummaryBadgeKey = SummaryAllKey;

        if(!HasRequestedData)
        {
            return;
        }

        AppliedMonth = ToolbarMonth;
        AppliedYear = ToolbarYear;
        currentPageIndex = 0;
        await ReloadAsync();
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ClearSelectionAsync</c>.</summary>
    private async Task ClearSelectionAsync()
    {
        SelectionState.Clear();

        if(GridSection is null)
        {
            return;
        }

        await GridSection.ClearSelectionAsync();
    }

    /// <summary>Lấy cho luồng <c>GetSelectedResults</c>.</summary>
    private List<AttendanceAllowanceResultRecord> GetSelectedResults()
    {
        var visibleIds = Records.Select(record => record.Id).ToHashSet();
        return SelectedDataItems
            .OfType<AttendanceAllowanceResultRecord>()
            .Where(result => visibleIds.Contains(result.Id))
            .DistinctBy(result => result.Id)
            .ToList();
    }

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanEditRow</c>.</summary>
    private bool CanEditRow(AttendanceAllowanceResultRecord record) =>
        CanOperateOnCurrentDataset
        && record.Id != Guid.Empty
        && !record.IsLocked;

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanRefreshRow</c>.</summary>
    private bool CanRefreshRow(AttendanceAllowanceResultRecord record) =>
        CanOperateOnCurrentDataset
        && record.Id != Guid.Empty
        && !record.IsLocked;

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanViewMonthlyWork</c>.</summary>
    private bool CanViewMonthlyWork(AttendanceAllowanceResultRecord record) =>
        CanOperateOnCurrentDataset
        && !IsMonthlyWorkPopupLoading
        && record.EmployeeId is { } employeeId
        && employeeId != Guid.Empty;

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanToggleLockState</c>.</summary>
    private bool CanToggleLockState(AttendanceAllowanceResultRecord record) =>
        CanOperateOnCurrentDataset
        && record.Id != Guid.Empty;

    /// <summary>Làm mới cho luồng <c>RefreshRowAsync</c>.</summary>
    private async Task RefreshRowAsync(AttendanceAllowanceResultRecord record)
    {
        if(!CanRefreshRow(record))
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            CurrentLoadingText = $"Đang làm mới phụ cấp chuyên cần của {record.EmployeeDisplay}...";
            await InvokeAsync(StateHasChanged);

            var result = await RefreshDataProvider.RefreshRowAsync(
                AppliedMonth,
                AppliedYear,
                record.Id,
                disposalTokenSource.Token);

            await ReloadAsync();
            if(result.UpdatedCount > 0)
            {
                ToastService.ShowSuccess(
                    $"Đã làm mới dòng phụ cấp chuyên cần của {record.EmployeeDisplay}.");
            }
            else
            {
                ToastService.ShowInfo(
                    $"Không thể làm mới dòng phụ cấp chuyên cần của {record.EmployeeDisplay}; dòng đã bị khóa.");
            }
        }
        catch(OperationCanceledException)
        {
            // Hủy do component bị dispose hoặc request bị ngắt không phải lỗi nghiệp vụ.
        }
        catch(InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể làm mới phụ cấp chuyên cần của {record.EmployeeDisplay}.");
        }
        finally
        {
            IsRefreshing = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
            if(!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    /// <summary>Mở cho luồng <c>OpenMonthlyWorkPopupAsync</c>.</summary>
    private async Task OpenMonthlyWorkPopupAsync(AttendanceAllowanceResultRecord record)
    {
        if(!CanViewMonthlyWork(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupTitle = "Bảng công tháng";
        MonthlyWorkPopupContext =
            $"{record.EmployeeDisplay} - {record.DepartmentDisplay} - {record.PositionDisplay} - Tháng {FormatPayrollPeriod(AppliedMonth, AppliedYear)}";
        MonthlyWorkRows = [];
        MonthlyWorkPopupEmployeeId = record.EmployeeId!.Value;
        MonthlyWorkPopupMonth = AppliedMonth;
        MonthlyWorkPopupYear = AppliedYear;
        IsMonthlyWorkPopupVisible = true;
        IsMonthlyWorkPopupLoading = true;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        await LoadMonthlyWorkPopupDataAsync();
    }

    /// <summary>Làm mới cho luồng <c>RefreshMonthlyWorkPopupAsync</c>.</summary>
    private async Task RefreshMonthlyWorkPopupAsync()
    {
        if(!IsMonthlyWorkPopupVisible || IsMonthlyWorkPopupLoading || MonthlyWorkPopupEmployeeId == Guid.Empty)
        {
            return;
        }

        await LoadMonthlyWorkPopupDataAsync();
    }

    /// <summary>Tải cho luồng <c>LoadMonthlyWorkPopupDataAsync</c>.</summary>
    private async Task LoadMonthlyWorkPopupDataAsync()
    {
        if(MonthlyWorkPopupEmployeeId == Guid.Empty
            || MonthlyWorkPopupMonth is < 1 or > 12
            || MonthlyWorkPopupYear is < MinimumSupportedYear or > MaximumSupportedYear)
        {
            return;
        }

        IsMonthlyWorkPopupLoading = true;

        try
        {
            var fromDate = new DateOnly(MonthlyWorkPopupYear, MonthlyWorkPopupMonth, 1);
            var toDate = new DateOnly(
                MonthlyWorkPopupYear,
                MonthlyWorkPopupMonth,
                DateTime.DaysInMonth(MonthlyWorkPopupYear, MonthlyWorkPopupMonth));
            var monthlyWork = await MonthlyWorkSummaryDataProvider.LoadEmployeeMonthAsync(
                fromDate,
                toDate,
                MonthlyWorkPopupEmployeeId,
                disposalTokenSource.Token);

            MonthlyWorkRows = monthlyWork?.DayCellsByDate.Values
                .OrderBy(day => day.WorkDate)
                .Select(day => new MonthlyWorkdayPopupRow(
                    day.Id,
                    day.WorkDate,
                    day.DayTypeDisplay,
                    day.ShiftShortDisplay,
                    day.ShiftColorHex,
                    day.CheckInDisplay,
                    day.CheckOutDisplay,
                    string.IsNullOrWhiteSpace(day.Status) ? string.Empty : day.Status,
                    day.LateMinutes,
                    day.EarlyLeaveMinutes,
                    day.OvertimeMinutes,
                    day.OvertimeMinutes15,
                    day.OvertimeMinutes20,
                    day.OvertimeMinutes30,
                    day.IsLocked ? "Đã khóa" : "Mở",
                    day.IsLocked))
                .ToArray()
                ?? [];
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            IsMonthlyWorkPopupVisible = false;
        }
        catch(Exception)
        {
            IsMonthlyWorkPopupVisible = false;
            ToastService.ShowError("Không thể tải bảng công tháng của nhân viên.");
        }
        finally
        {
            IsMonthlyWorkPopupLoading = false;
        }
    }

    /// <summary>Đóng cho luồng <c>CloseMonthlyWorkPopup</c>.</summary>
    private void CloseMonthlyWorkPopup()
    {
        if(IsMonthlyWorkPopupLoading)
        {
            return;
        }

        IsMonthlyWorkPopupVisible = false;
        IsMonthlyWorkPopupLoading = false;
        MonthlyWorkRows = [];
        MonthlyWorkPopupEmployeeId = Guid.Empty;
        MonthlyWorkPopupMonth = 0;
        MonthlyWorkPopupYear = 0;
    }
}

