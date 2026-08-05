using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>Coordinator use case for manual seniority-allowance adjustments.</summary>
public partial class PhuCapThamNien
{
    #region Quy tắc và chỉnh sửa thủ công

    /// <summary>Đóng cửa sổ hiển thị quy tắc tính phụ cấp.</summary>
    private void CloseRulesPopup()
    {
        IsRulesPopupVisible = false;
    }

    /// <summary>Khởi tạo mô hình chỉnh sửa từ bản ghi được chọn và mở cửa sổ chỉnh sửa.</summary>
    private void OpenEditPopup(PhuCapThamNienRecord record)
    {
        if(!CanOperateOnCurrentDataset)
        {
            return;
        }

        if(record.IsLocked)
        {
            ToastService.ShowWarning("Dòng phụ cấp thâm niên đã khóa nên không thể chỉnh sửa thủ công.");
            return;
        }

        EditModel = new PhuCapThamNienEditModel
        {
            PayrollAllowanceSummaryRecordId = record.PayrollAllowanceSummaryRecordId,
            EmployeeDisplay = record.EmployeeDisplay,
            AllowanceAmount = record.AllowanceAmount,
            Note = record.Note,
            IsLocked = record.IsLocked,
            OriginalUpdatedAtUtc = record.UpdatedAtUtc
        };
        EditPopupTitle = $"Sửa phụ cấp thâm niên - {record.EmployeeDisplay}";
        EditErrorMessage = null;
        IsEditPopupVisible = true;
    }

    /// <summary>Đóng cửa sổ chỉnh sửa khi không có thao tác lưu đang chạy.</summary>
    private void CloseEditPopup()
    {
        if(IsSavingEdit)
        {
            return;
        }

        CloseEditPopupCore();
    }

    /// <summary>Đóng cửa sổ chỉnh sửa và đặt lại mô hình về giá trị mặc định.</summary>
    private void CloseEditPopupCore()
    {
        IsEditPopupVisible = false;
        EditModel = new();
        EditPopupTitle = "Sửa phụ cấp thâm niên";
        EditErrorMessage = null;
    }

    /// <summary>Lưu thay đổi phụ cấp thủ công và cập nhật bản ghi tương ứng trong lưới.</summary>
    private async Task SaveEditAsync(PhuCapThamNienEditModel draft)
    {
        if(!CanSaveEdit)
        {
            return;
        }

        EditModel = new PhuCapThamNienEditModel
        {
            PayrollAllowanceSummaryRecordId = draft.PayrollAllowanceSummaryRecordId,
            EmployeeDisplay = draft.EmployeeDisplay,
            AllowanceAmount = draft.AllowanceAmount,
            Note = draft.Note,
            IsLocked = draft.IsLocked,
            OriginalUpdatedAtUtc = draft.OriginalUpdatedAtUtc
        };

        try
        {
            IsSavingEdit = true;
            SetLoadingText($"Đang cập nhật phụ cấp thâm niên của {EditModel.EmployeeDisplay}...");

            var updatedRecord = await DataProvider.UpdateManualValuesAsync(
                EditModel.PayrollAllowanceSummaryRecordId,
                EditModel.AllowanceAmount,
                EditModel.Note,
                EditModel.OriginalUpdatedAtUtc,
                disposalTokenSource.Token);

            CloseEditPopupCore();
            await ReloadAsync();
            ToastService.ShowSuccess($"Đã cập nhật phụ cấp thâm niên của {updatedRecord.EmployeeDisplay}.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(HrmApiException exception) when(exception.Kind == HrmApiErrorKind.Conflict)
        {
            EditErrorMessage = "Dữ liệu đã được thay đổi hoặc khóa bởi thao tác khác. Vui lòng tải lại trước khi lưu lại.";
            ToastService.ShowWarning(EditErrorMessage);
        }
        catch(Exception)
        {
            EditErrorMessage = $"Không thể cập nhật phụ cấp thâm niên của {EditModel.EmployeeDisplay}. Vui lòng kiểm tra lại thông tin và thử lại.";
            ToastService.ShowError(EditErrorMessage);
        }
        finally
        {
            IsSavingEdit = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    #endregion

}
