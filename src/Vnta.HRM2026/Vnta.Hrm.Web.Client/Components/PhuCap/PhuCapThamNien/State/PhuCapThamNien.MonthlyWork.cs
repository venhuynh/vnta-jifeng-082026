using Vnta.Hrm.Web.Client.Components.Shared.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>Coordinator use case for the monthly-work reconciliation dialog.</summary>
public partial class PhuCapThamNien
{
    #region Đối chiếu bảng công tháng

    /// <summary>Mở cửa sổ đối chiếu bảng công và tải chi tiết cho bản ghi nhân viên được chọn.</summary>
    private async Task OpenMonthlyWorkPopupAsync(PhuCapThamNienRecord record)
    {
        if(!CanViewMonthlyWork(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupTitle = "Đối chiếu bảng công chi tiết";
        MonthlyWorkPopupContext =
            $"{record.EmployeeDisplay} - {record.DepartmentDisplay} - {record.PositionDisplay} - Tháng {AppliedPeriodLabel}";
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = record;
        MonthlyWorkPopupSalaryWorkDays = record.SalaryWorkDays ?? 0m;
        IsMonthlyWorkPopupVisible = true;
        await LoadMonthlyWorkPopupDataAsync(record);
    }

    /// <summary>Tải lại chi tiết bảng công của bản ghi đang mở trong cửa sổ.</summary>
    private async Task RefreshMonthlyWorkPopupAsync()
    {
        if(MonthlyWorkPopupRecord is null || IsMonthlyWorkPopupLoading || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        await LoadMonthlyWorkPopupDataAsync(MonthlyWorkPopupRecord);
    }

    /// <summary>Tải, chuyển đổi và sắp xếp dữ liệu bảng công theo từng ngày trong kỳ đã áp dụng.</summary>
    private async Task LoadMonthlyWorkPopupDataAsync(PhuCapThamNienRecord record)
    {
        IsMonthlyWorkPopupLoading = true;
        MonthlyWorkPopupErrorMessage = null;

        try
        {
            var fromDate = new DateOnly(AppliedYear, AppliedMonth, 1);
            var toDate = new DateOnly(
                AppliedYear,
                AppliedMonth,
                DateTime.DaysInMonth(AppliedYear, AppliedMonth));
            var monthlyWork = await MonthlyWorkSummaryDataProvider.LoadEmployeeMonthAsync(
                fromDate,
                toDate,
                record.EmployeeId,
                disposalTokenSource.Token);

            MonthlyWorkRows = monthlyWork?.DayCellsByDate.Values
                .OrderBy(day => day.WorkDate)
                .Select(day => new MonthlyWorkdayPopupRow(
                    day.Id,
                    day.WorkDate,
                    day.DayTypeDisplay,
                    string.IsNullOrWhiteSpace(day.ShiftShortName) ? "--" : day.ShiftShortName.Trim(),
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
            MonthlyWorkPopupErrorMessage = "Không thể tải bảng công tháng của nhân viên.";
        }
        finally
        {
            IsMonthlyWorkPopupLoading = false;
        }
    }

    /// <summary>Đóng cửa sổ bảng công và xóa toàn bộ ngữ cảnh đối chiếu.</summary>
    private void CloseMonthlyWorkPopup()
    {
        if(IsMonthlyWorkPopupLoading)
        {
            return;
        }

        IsMonthlyWorkPopupVisible = false;
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = null;
        MonthlyWorkPopupSalaryWorkDays = 0m;
    }

    #endregion

}

