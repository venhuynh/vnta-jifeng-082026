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
    #region Monthly Work Popup

    /// <summary>Mở cho luồng <c>OpenMonthlyWorkPopupAsync</c>.</summary>
    private async Task OpenMonthlyWorkPopupAsync(LeaveHolidayAllowanceRecord record)
    {
        if (!CanViewMonthlyWork(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupContext =
            $"{record.EmployeeDisplay} - {record.DepartmentDisplay} - {record.PositionDisplay} - Tháng {CurrentPayrollPeriodDisplay}";
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = record;
        IsMonthlyWorkPopupVisible = true;

        await LoadMonthlyWorkPopupDataAsync(record);
    }

    /// <summary>Làm mới cho luồng <c>RefreshMonthlyWorkPopupAsync</c>.</summary>
    private async Task RefreshMonthlyWorkPopupAsync()
    {
        if (MonthlyWorkPopupRecord is null
            || IsMonthlyWorkPopupLoading
            || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        await LoadMonthlyWorkPopupDataAsync(MonthlyWorkPopupRecord);
    }

    /// <summary>Tải cho luồng <c>LoadMonthlyWorkPopupDataAsync</c>.</summary>
    private async Task LoadMonthlyWorkPopupDataAsync(LeaveHolidayAllowanceRecord record)
    {
        IsMonthlyWorkPopupLoading = true;
        MonthlyWorkPopupErrorMessage = null;

        try
        {
            var monthlyWork = await ExecuteDataOperationAsync(
                token => DataProvider.LoadEmployeeMonthlyWorkAsync(
                    record.Id,
                    record.EmployeeId,
                    AppliedYear,
                    AppliedMonth,
                    token),
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
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            IsMonthlyWorkPopupVisible = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Không thể tải bảng công tháng cho dòng phụ cấp Phép - Lễ {AllowanceRecordId}.",
                record.Id);
            MonthlyWorkPopupErrorMessage = "Không thể tải bảng công tháng của nhân viên. Vui lòng thử lại.";
        }
        finally
        {
            IsMonthlyWorkPopupLoading = false;
        }
    }

    /// <summary>Đóng cho luồng <c>CloseMonthlyWorkPopup</c>.</summary>
    private void CloseMonthlyWorkPopup()
    {
        if (IsMonthlyWorkPopupLoading)
        {
            return;
        }

        IsMonthlyWorkPopupVisible = false;
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = null;
    }

    #endregion
}
