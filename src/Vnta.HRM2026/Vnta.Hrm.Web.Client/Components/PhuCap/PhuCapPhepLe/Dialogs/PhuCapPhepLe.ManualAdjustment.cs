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
    #region Popup And Manual Edit Flow

    /// <summary>Xử lý sự kiện cho luồng <c>OnManualEditButtonClick</c>.</summary>
    private void OnManualEditButtonClick(LeaveHolidayAllowanceRecord record)
    {
        if (!CanOperateOnCurrentDataset)
        {
            return;
        }

        if (record.IsLocked)
        {
            ToastService.ShowWarning("Dòng phụ cấp Phép - Lễ đã khóa, không thể điều chỉnh.");
            return;
        }

        // Popup chỉnh trên bản sao của dòng đang chọn để người dùng nhập thử mà không làm thay đổi dữ liệu trên lưới trước khi lưu thành công.
        ManualEditErrorMessage = null;
        ManualEditModel = LeaveHolidayManualEditModel.FromRecord(record);
        ManualEditFormContext = new EditContext(ManualEditModel);
        ManualEditPopupTitle = $"Điều chỉnh phụ cấp Phép - Lễ - {record.EmployeeDisplay}";
        IsManualEditPopupVisible = true;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnManualEditCancelButtonClick</c>.</summary>
    private void OnManualEditCancelButtonClick()
    {
        if (IsSavingManualEdit)
        {
            return;
        }

        ResetManualEditPopupState();
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnManualEditSaveButtonClickAsync</c>.</summary>
    private async Task OnManualEditSaveButtonClickAsync()
    {
        if (ManualEditModel is null || ManualEditFormContext is null || IsSavingManualEdit)
        {
            return;
        }

        ManualEditModel.Normalize();
        ManualEditErrorMessage = null;
        if (!ManualEditFormContext.Validate())
        {
            return;
        }

        try
        {
            IsSavingManualEdit = true;

            var updatedRecord = await ExecuteDataOperationAsync(
                token => DataProvider.UpdateManualValuesAsync(
                    ManualEditModel.Id,
                    ManualEditModel.DailyWageAmount,
                    ManualEditModel.LeaveDayCount,
                    ManualEditModel.HolidayDayCount,
                    ManualEditModel.Note,
                    ManualEditModel.OriginalUpdatedAtUtc,
                    token),
                disposalTokenSource.Token);

            ResetManualEditPopupState();
            await ApplyManualEditResultAsync(updatedRecord);
            ToastService.ShowSuccess($"Đã cập nhật phụ cấp Phép - Lễ của {updatedRecord.EmployeeDisplay}.");
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            ManualEditErrorMessage = ex.Message;
        }
        finally
        {
            IsSavingManualEdit = false;
        }
    }

    /// <summary>Đặt lại cho luồng <c>ResetManualEditPopupState</c>.</summary>
    private void ResetManualEditPopupState()
    {
        IsManualEditPopupVisible = false;
        ManualEditModel = null;
        ManualEditFormContext = null;
        ManualEditErrorMessage = null;
        ManualEditPopupTitle = DefaultManualEditPopupTitle;
    }

    // Search text đang đi server-side; nếu người dùng vừa sửa ghi chú hoặc dữ liệu có thể làm dòng hết còn match,
    // cần reload lại snapshot hiện tại thay vì chỉ vá cục bộ một dòng trong state client.
    /// <summary>Áp dụng cho luồng <c>ApplyManualEditResultAsync</c>.</summary>
    private async Task ApplyManualEditResultAsync(LeaveHolidayAllowanceRecord updatedRecord)
    {
        if (HasActiveSearch)
        {
            await ReloadDataAsync();
            return;
        }

        ReplaceUpdatedRecordInState(updatedRecord);
    }

    #endregion
}
