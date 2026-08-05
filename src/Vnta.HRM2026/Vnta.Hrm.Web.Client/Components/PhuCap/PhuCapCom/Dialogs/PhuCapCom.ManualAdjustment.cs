using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Sở hữu lifecycle điều chỉnh thủ công một snapshot Phụ cấp cơm.</summary>
public partial class PhuCapCom
{
    private void OpenEditPopup(MealAllowanceRecord record)
    {
        if(!CanOperateOnCurrentDataset)
        {
            return;
        }

        if(record.IsLocked)
        {
            ToastService.ShowWarning("Dòng phụ cấp cơm đã khóa nên không thể điều chỉnh.");
            return;
        }

        EditModel = new PhuCapComEditModel
        {
            Id = record.Id,
            EmployeeDisplay = record.EmployeeDisplay,
            QualifiedMealDays = record.QualifiedMealDays,
            Overtime1900Days = record.Overtime1900Days,
            MealAllowancePerQualifiedDay = record.MealAllowancePerQualifiedDay,
            Note = record.Note,
            IsLocked = record.IsLocked,
            OriginalUpdatedAtUtc = record.UpdatedAtUtc
        };
        EditModel.RecalculateAmount();
        EditValidationMessage = null;
        EditPopupTitle = $"Điều chỉnh phụ cấp cơm - {record.EmployeeDisplay}";
        IsEditPopupVisible = true;
    }

    private Task OnEditQualifiedMealDaysChangedAsync(int value)
    {
        if(IsSavingEdit || EditModel.IsLocked)
        {
            return Task.CompletedTask;
        }

        EditModel.QualifiedMealDays = Math.Max(0, value);
        EditModel.RecalculateAmount();
        EditValidationMessage = null;
        return Task.CompletedTask;
    }

    private void CloseEditPopup()
    {
        if(!IsSavingEdit)
        {
            CloseEditPopupCore();
        }
    }

    private void CloseEditPopupCore()
    {
        IsEditPopupVisible = false;
        EditModel = new();
        EditValidationMessage = null;
        EditPopupTitle = "Điều chỉnh phụ cấp cơm";
    }

    private async Task SaveEditAsync()
    {
        if(!CanSaveEdit)
        {
            return;
        }

        var employeeDisplay = EditModel.EmployeeDisplay;
        try
        {
            EditValidationMessage = null;
            IsSavingEdit = true;
            LoadingText = $"Đang cập nhật phụ cấp cơm của {employeeDisplay}...";
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var updatedRecord = await DataProvider.UpdateManualValuesAsync(
                EditModel.Id,
                EditModel.QualifiedMealDays,
                EditModel.Note,
                EditModel.OriginalUpdatedAtUtc,
                disposalTokenSource.Token);

            Records = Records
                .Select(record => record.Id == updatedRecord.Id ? updatedRecord : record)
                .ToArray();
            SelectedDataItems = SelectedDataItems
                .Select(item => item is MealAllowanceRecord record && record.Id == updatedRecord.Id
                    ? (object)updatedRecord
                    : item)
                .ToArray();
            CloseEditPopupCore();
            await ReloadAsync();
            ToastService.ShowSuccess($"Đã cập nhật phụ cấp cơm của {updatedRecord.EmployeeDisplay}.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã được dispose; không tiếp tục cập nhật popup.
        }
        catch(InvalidOperationException ex)
        {
            EditValidationMessage = ex.Message;
            ToastService.ShowError(ex.Message);
        }
        catch(Exception)
        {
            EditValidationMessage = $"Không thể cập nhật phụ cấp cơm của {employeeDisplay}.";
            ToastService.ShowError(EditValidationMessage);
        }
        finally
        {
            IsSavingEdit = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }
}
