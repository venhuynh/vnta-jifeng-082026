using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;
using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

public sealed partial class OtherAllowanceCoordinator
{
    private void OpenRulesPopup()
    {
        if(CanInteract)
        {
            IsRulesPopupVisible = true;
        }
    }

    private async Task OpenCreatePopupAsync()
    {
        if(!CanCreate || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        try
        {
            IsLoading = true;
            LoadingText = "Đang chuẩn bị danh sách nhân viên...";
            var result = await CreateDataProvider.SearchCreateEmployeesAsync(AppliedMonth, AppliedYear, MaximumSearchResultTake, disposalTokenSource.Token);
            CreateEmployeeOptions = result.Rows.Where(row => row.Id != Guid.Empty && !row.IsLocked)
                .OrderBy(row => row.EmployeeDisplay)
                .Select(row => new PhuCapKhacEmployeeOption(row.Id, row.EmployeeDisplay)).ToArray();
            if(CreateEmployeeOptions.Count == 0)
            {
                ToastService.ShowWarning("Không có nhân viên với bản ghi tổng hợp phụ cấp đang mở trong kỳ lương này.");
                return;
            }

            var selectedEmployee = CreateEmployeeOptions[0];
            EditModel = new PhuCapKhacEditModel
            {
                PayrollAllowanceSummaryRecordId = selectedEmployee.PayrollAllowanceSummaryRecordId,
                EmployeeDisplay = selectedEmployee.EmployeeDisplay,
                PayrollMonth = AppliedMonth,
                PayrollYear = AppliedYear,
                PayrollPeriodDisplay = $"{AppliedMonth:00}/{AppliedYear}",
                IsFixedAmount = true,
                IsLocked = false
            };
            IsCreateMode = true;
            EditPopupTitle = "Thêm phụ cấp khác";
            EditErrorMessage = null;
            IsEditPopupVisible = true;
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(UnauthorizedAccessException) { ToastService.ShowWarning("Bạn không có quyền thêm phụ cấp khác."); }
        catch(Exception exception)
        {
            Logger.LogError(exception, "Không thể chuẩn bị biểu mẫu thêm phụ cấp khác cho kỳ {PayrollMonth}/{PayrollYear}.", AppliedMonth, AppliedYear);
            ToastService.ShowError("Không thể chuẩn bị biểu mẫu thêm phụ cấp khác. Vui lòng thử lại.");
        }
        finally
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                IsLoading = false;
                LoadingText = DefaultLoadingText;
            }
        }
    }

    private void OpenEditPopup(OtherAllowanceListItemDto row)
    {
        if(!CanOperateOnCurrentDataset) return;
        if(row.Id == Guid.Empty) { ToastService.ShowWarning("Dòng phụ cấp khác không hợp lệ nên không thể điều chỉnh."); return; }
        if(row.IsLocked) { ToastService.ShowWarning("Dòng phụ cấp khác đã khóa nên không thể điều chỉnh."); return; }

        EditModel = new PhuCapKhacEditModel
        {
            Id = row.Id,
            PayrollAllowanceSummaryRecordId = row.PayrollAllowanceSummaryRecordId,
            EmployeeDisplay = GetEmployeeDisplay(row),
            PayrollMonth = row.PayrollMonth,
            PayrollYear = row.PayrollYear,
            PayrollPeriodDisplay = $"{row.PayrollMonth:00}/{row.PayrollYear}",
            AllowanceName = row.AllowanceName,
            IsFixedAmount = row.IsFixedAmount,
            AllowanceAmount = row.AllowanceAmount,
            Note = row.Note,
            IsLocked = row.IsLocked,
            OriginalUpdatedAtUtc = row.UpdatedAtUtc ?? row.CreatedAtUtc
        };
        EditPopupTitle = $"Sửa phụ cấp khác - {GetEmployeeDisplay(row)}";
        IsCreateMode = false;
        CreateEmployeeOptions = [];
        EditErrorMessage = null;
        IsEditPopupVisible = true;
    }

    private void CloseEditPopup()
    {
        if(!IsSavingEdit) CloseEditPopupCore();
    }

    private void CloseEditPopupCore()
    {
        IsEditPopupVisible = false;
        IsSavingEdit = false;
        IsCreateMode = false;
        CreateEmployeeOptions = [];
        EditModel = new();
        EditPopupTitle = "Sửa phụ cấp khác";
        EditErrorMessage = null;
    }

    private async Task SaveEditCoreAsync(PhuCapKhacEditModel draft)
    {
        if(!CanSaveEdit) return;
        EditModel = CloneEditModel(draft);
        if(!TryValidateEditModel(EditModel, out var validationMessage)) { EditErrorMessage = validationMessage; return; }
        try
        {
            IsSavingEdit = true;
            EditErrorMessage = null;
            LoadingText = $"Đang cập nhật phụ cấp khác của {EditModel.EmployeeDisplay}...";
            var isCreating = IsCreateMode;
            var employeeDisplay = EditModel.EmployeeDisplay;
            if(isCreating)
                await CreateDataProvider.CreateAsync(EditModel.PayrollAllowanceSummaryRecordId, EditModel.AllowanceName, EditModel.IsFixedAmount, EditModel.AllowanceAmount, EditModel.Note, disposalTokenSource.Token);
            else
                await UpdateDataProvider.UpdateAsync(EditModel.Id, EditModel.AllowanceName, EditModel.IsFixedAmount, EditModel.AllowanceAmount, EditModel.Note, EditModel.OriginalUpdatedAtUtc, disposalTokenSource.Token);
            CloseEditPopupCore();
            await LoadAsync();
            ToastService.ShowSuccess(isCreating ? $"Đã thêm phụ cấp khác cho {employeeDisplay}." : $"Đã cập nhật phụ cấp khác của {employeeDisplay}.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(OtherAllowanceConflictException)
        {
            EditErrorMessage = "Dữ liệu đã được thay đổi hoặc khóa bởi thao tác khác. Vui lòng tải lại trước khi lưu lại.";
            ToastService.ShowWarning(EditErrorMessage);
        }
        catch(HrmApiException exception) when(exception.Kind == HrmApiErrorKind.Conflict) { EditErrorMessage = exception.UserMessage; ToastService.ShowWarning(EditErrorMessage); await LoadAsync(); }
        catch(UnauthorizedAccessException) { EditErrorMessage = IsCreateMode ? "Bạn không có quyền thêm phụ cấp khác." : "Bạn không có quyền điều chỉnh phụ cấp khác."; ToastService.ShowWarning(EditErrorMessage); }
        catch(HrmApiException exception) when(exception.Kind is HrmApiErrorKind.Unauthenticated or HrmApiErrorKind.Forbidden) { EditErrorMessage = IsCreateMode ? "Bạn không có quyền thêm phụ cấp khác." : "Bạn không có quyền điều chỉnh phụ cấp khác."; ToastService.ShowWarning(EditErrorMessage); }
        catch(InvalidOperationException exception) { EditErrorMessage = exception.Message; ToastService.ShowWarning(EditErrorMessage); }
        catch(Exception exception)
        {
            Logger.LogError(exception, "Không thể lưu phụ cấp khác của {EmployeeDisplay}.", EditModel.EmployeeDisplay);
            EditErrorMessage = IsCreateMode ? "Không thể thêm phụ cấp khác. Vui lòng kiểm tra lại thông tin và thử lại." : "Không thể cập nhật phụ cấp khác. Vui lòng kiểm tra lại thông tin và thử lại.";
            ToastService.ShowError(EditErrorMessage);
        }
        finally
        {
            if(!disposalTokenSource.IsCancellationRequested) { IsSavingEdit = false; LoadingText = DefaultLoadingText; }
        }
    }

    private static bool TryValidateEditModel(PhuCapKhacEditModel model, out string? message)
    {
        var allowanceName = model.AllowanceName?.Trim();
        if(string.IsNullOrWhiteSpace(allowanceName)) { message = "Tên phụ cấp là bắt buộc."; return false; }
        if(allowanceName.Length > 256) { message = "Tên phụ cấp không được vượt quá 256 ký tự."; return false; }
        if(model.AllowanceAmount < 0m) { message = "Số tiền phụ cấp không được nhỏ hơn 0."; return false; }
        message = null;
        return true;
    }
}
