using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Owns the note-edit popup lifecycle and its save workflow.</summary>
public partial class PhuCapTongHop
{
    private Task OpenManualEditPopupAsync()
    {
        if(SelectedRowCount != 1 || SelectedRecord is null)
        {
            ToastService.ShowWarning("Hãy chọn đúng một dòng tổng hợp phụ cấp để điều chỉnh.");
            return Task.CompletedTask;
        }

        return OpenManualEditPopupAsync(SelectedRecord);
    }

    private Task OpenManualEditPopupAsync(PayrollAllowanceSummaryRecord row)
    {
        if(!CanOperateOnCurrentDataset) return Task.CompletedTask;
        if(row.IsLocked)
        {
            ToastService.ShowWarning("Dòng tổng hợp phụ cấp đã khóa, hãy mở khóa trước khi điều chỉnh.");
            return Task.CompletedTask;
        }

        ManualEditErrorMessage = null;
        ManualEditModel = PhuCapTongHopManualEditModel.FromRecord(row);
        IsManualEditPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseManualEditPopup()
    {
        if(IsSavingManualValues) return;
        IsManualEditPopupVisible = false;
        ManualEditModel = null;
        ManualEditErrorMessage = null;
    }

    private async Task SaveManualValuesAsync()
    {
        if(ManualEditModel is null || IsSavingManualValues) return;
        ManualEditErrorMessage = ValidateManualEditModel(ManualEditModel);
        if(!string.IsNullOrWhiteSpace(ManualEditErrorMessage)) return;

        try
        {
            IsSavingManualValues = true;
            await RenderBusyStateAsync();
            await DataProvider.UpdateManualValuesAsync(ManualEditModel.ToRequest(), disposalTokenSource.Token);
            IsManualEditPopupVisible = false;
            ManualEditModel = null;
            ManualEditErrorMessage = null;
            await ReloadAsync();
            ToastService.ShowSuccess("Đã điều chỉnh và lưu dòng tổng hợp phụ cấp.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested) throw;
        }
        catch(Exception ex)
        {
            ManualEditErrorMessage = ex.Message;
        }
        finally
        {
            IsSavingManualValues = false;
        }
    }
}
