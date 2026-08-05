using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.Models;
using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien;

public partial class PhuCapTrachNhiemGanNhanVien
{
    private async Task LoadPeriodAsync()
    {
        if (!CanLoad)
        {
            return;
        }

        var period = NormalizeSelectedPeriod(ToolbarMonth, ToolbarYear);
        AppliedMonth = period.Month;
        AppliedYear = period.Year;
        ToolbarMonth = AppliedMonth;
        ToolbarYear = AppliedYear;
        HasRequestedData = true;
        CurrentPageIndex = 0;
        await ClearSelectionAsync();
        await ReloadAsync();
    }

    private async Task LoadFromPreviousMonthAsync()
    {
        if (!CanLoadFromPreviousMonth)
        {
            return;
        }

        try
        {
            IsLoadingPreviousMonth = true;
            var result = await AssignmentProvider.LoadFromPreviousMonthAsync(
                AppliedYear,
                AppliedMonth,
                disposalTokenSource.Token);
            await ReloadAsync();
            ToastService.ShowSuccess(
                $"Đã lấy giá trị từ kỳ trước cho {result.Updated:N0}/{result.TotalEmployees:N0} nhân viên của kỳ {AppliedPeriodLabel}.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (HrmApiException exception)
        {
            Logger.LogWarning(exception,
                "Không thể lấy giá trị gán cấp bậc từ kỳ trước cho kỳ {PayrollMonth}/{PayrollYear}. TraceId: {TraceId}",
                AppliedMonth,
                AppliedYear,
                exception.TraceId);
            ToastService.ShowError(exception.UserMessage);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception,
                "Không thể lấy giá trị gán cấp bậc từ kỳ trước cho kỳ {PayrollMonth}/{PayrollYear}.",
                AppliedMonth,
                AppliedYear);
            ToastService.ShowError("Không thể lấy giá trị gán cấp bậc từ kỳ trước cho danh sách nhân viên.");
        }
        finally
        {
            IsLoadingPreviousMonth = false;
        }
    }

    private void OpenEditPopup(PayrollResponsibilityAllowanceEmployeeAssignmentDto record)
    {
        if (!CanManageAssignments)
        {
            return;
        }

        EditingRecord = record;
        EditModel = PhuCapTrachNhiemGanNhanVienEditModel.From(record);
        EditContext = new EditContext(EditModel);
        IsEditPopupVisible = true;
    }

    private void CloseEditPopup(bool visible)
    {
        if (visible || IsSaving)
        {
            return;
        }

        IsEditPopupVisible = false;
        EditingRecord = null;
    }

    private async Task SaveAssignmentAsync()
    {
        if (EditingRecord is null || !CanManageAssignments)
        {
            return;
        }

        if (!EditModel.GradeId.HasValue)
        {
            ToastService.ShowWarning("Hãy chọn cấp bậc trách nhiệm cho nhân viên.");
            return;
        }

        try
        {
            IsSaving = true;
            await AssignmentProvider.UpdateAndRefreshAsync(
                new UpdatePayrollResponsibilityAllowanceEmployeeAssignmentRequest(
                    EditingRecord.Id,
                    AppliedYear,
                    AppliedMonth,
                    EditingRecord.EmployeeId,
                    EditModel.GradeId.Value,
                    NormalizeOptional(EditModel.Note),
                    EditingRecord.UpdatedAtUtc),
                disposalTokenSource.Token);
            IsEditPopupVisible = false;
            await ReloadAsync();
            ToastService.ShowSuccess($"Đã cập nhật cấp bậc của {EditingRecord.EmployeeCode} - {EditingRecord.EmployeeName}.");
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Logger.LogError(exception,
                "Không thể cập nhật cấp bậc nhân viên {EmployeeCode} cho kỳ {PayrollMonth}/{PayrollYear}.",
                EditingRecord.EmployeeCode,
                AppliedMonth,
                AppliedYear);
            ToastService.ShowError("Không thể cập nhật cấp bậc nhân viên. Vui lòng thử lại.");
        }
        finally
        {
            IsSaving = false;
        }
    }
}
