using System.Globalization;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Employees;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.Api;
using Vnta.Hrm.Web.Client.Services.Ui;
using Vnta.Hrm.Web.Client.Components.Shared.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Đại diện kiểu <c>PhuCapTrachNhiem</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public partial class PhuCapTrachNhiem
{
    #region Thao tác dòng ABC và điều chỉnh

    /// <summary>Mở cho luồng <c>OpenAdjustmentPopupAsync</c>.</summary>
    private async Task OpenAdjustmentPopupAsync(PayrollResponsibilityAllowanceAbcItemDto row)
    {
        if (!CanUseLoadedDataActions)
        {
            return;
        }

        if (row.IsLocked)
        {
            ToastService.ShowError("Dòng trách nhiệm đã bị khóa, không thể điều chỉnh.");
            return;
        }

        AdjustmentPopupErrorMessage = null;
        AdjustmentTargetRow = row;
        IsAdjustmentPopupVisible = true;
        await LoadAdjustmentContextAsync(row);
    }

    /// <summary>Tải cho luồng <c>LoadAdjustmentContextAsync</c>.</summary>
    private async Task LoadAdjustmentContextAsync(PayrollResponsibilityAllowanceAbcItemDto row)
    {
        AdjustmentPopupErrorMessage = null;
        IsLoadingAdjustmentContext = true;
        AdjustmentContext = null;

        try
        {
            AdjustmentContext = await AbcQueryProvider.GetUpdateContextAsync(
                row.EmployeeId,
                row.Year,
                row.Month,
                disposalTokenSource.Token);

            AdjustmentForm = new AdjustmentFormModel
            {
                GradeIdText = AdjustmentContext.EmployeeAssignment?.GradeId?.ToString()
                    ?? AdjustmentContext.SelectedSource.GradeId?.ToString()
                    ?? string.Empty,
                IsActive = AdjustmentContext.EmployeeAssignment?.GradeId.HasValue
                    ?? (AdjustmentContext.SelectedSource.GradeId.HasValue
                        && AdjustmentContext.SelectedSource.StandardResponsibilityAllowanceAmount > 0),
                MonthlyPerformanceBonusAmount = AdjustmentContext.CurrentAbcRecord?.MonthlyPerformanceBonusAmount ?? row.MonthlyPerformanceBonusAmount,
                IsPerformanceBonusExcluded = AdjustmentContext.CurrentAbcRecord?.IsPerformanceBonusExcluded ?? row.IsPerformanceBonusExcluded,
                Note = AdjustmentContext.EmployeeAssignment?.Note
                    ?? AdjustmentContext.CurrentAbcRecord?.Note
                    ?? row.Note
                    ?? string.Empty
            };
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
            AdjustmentPopupErrorMessage = ex.Message;
        }
        finally
        {
            IsLoadingAdjustmentContext = false;
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnAdjustmentPerformanceBonusChanged</c>.</summary>
    private void OnAdjustmentPerformanceBonusChanged(decimal value) =>
        AdjustmentForm.MonthlyPerformanceBonusAmount = value;

    /// <summary>Lưu cho luồng <c>SaveAdjustmentAsync</c>.</summary>
    private async Task SaveAdjustmentAsync()
    {
        AdjustmentPopupErrorMessage = null;
        if (AdjustmentTargetRow is null)
        {
            return;
        }

        try
        {
            await RunBusyAsync(
                $"Đang lưu điều chỉnh trách nhiệm cho {AdjustmentTargetRow.EmployeeCode}...",
                async () =>
                {
                    var gradeId = AdjustmentForm.IsActive && TryParseGuid(AdjustmentForm.GradeIdText, out var parsedGradeId)
                        ? parsedGradeId
                        : (Guid?)null;

                    await AbcCommandProvider.SaveAdjustmentAsync(
                        new SavePayrollResponsibilityAllowanceAdjustmentRequest(
                            AdjustmentContext?.EmployeeAssignment?.Id,
                            AdjustmentTargetRow.Year,
                            AdjustmentTargetRow.Month,
                            AdjustmentTargetRow.EmployeeId,
                            gradeId,
                            AdjustmentForm.IsActive,
                            AdjustmentForm.Note,
                            AdjustmentForm.MonthlyPerformanceBonusAmount,
                            AdjustmentForm.IsPerformanceBonusExcluded,
                            GetConcurrencyTimestamp(AdjustmentTargetRow)),
                        disposalTokenSource.Token);

                    await ReloadAsync();
                });

            if (HasLoadError || AdjustmentTargetRow is null)
            {
                return;
            }

            var reloaded = AbcRows.FirstOrDefault(row => row.EmployeeId == AdjustmentTargetRow.EmployeeId);
            if (reloaded is not null)
            {
                AdjustmentTargetRow = reloaded;
                await LoadAdjustmentContextAsync(reloaded);
            }

            ToastService.ShowSuccess("Đã lưu điều chỉnh trách nhiệm.");
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
            AdjustmentPopupErrorMessage = ex.Message;
        }
    }

    /// <summary>Mở cho luồng <c>OpenCalculationPopup</c>.</summary>
    private void OpenCalculationPopup(PayrollResponsibilityAllowanceAbcItemDto row)
    {
        if (!CanUseLoadedDataActions)
        {
            return;
        }

        CalculationPopupRecord = row;
        IsCalculationPopupVisible = true;
    }

    /// <summary>Mở cho luồng <c>OpenMonthlyWorkPopupAsync</c>.</summary>
    private async Task OpenMonthlyWorkPopupAsync(PayrollResponsibilityAllowanceAbcItemDto row)
    {
        if (!CanViewMonthlyWork(row) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupTitle = "Đối chiếu bảng công chi tiết";
        MonthlyWorkPopupContext =
            $"{row.EmployeeCode} - {row.EmployeeName} - Tháng {row.Month:00}/{row.Year}";
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = row;
        IsMonthlyWorkPopupVisible = true;
        await LoadMonthlyWorkPopupDataAsync(row);
    }

    /// <summary>Làm mới cho luồng <c>RefreshMonthlyWorkPopupAsync</c>.</summary>
    private async Task RefreshMonthlyWorkPopupAsync()
    {
        if (MonthlyWorkPopupRecord is null || IsMonthlyWorkPopupLoading || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        await LoadMonthlyWorkPopupDataAsync(MonthlyWorkPopupRecord);
    }

    /// <summary>Tải cho luồng <c>LoadMonthlyWorkPopupDataAsync</c>.</summary>
    private async Task LoadMonthlyWorkPopupDataAsync(PayrollResponsibilityAllowanceAbcItemDto row)
    {
        IsMonthlyWorkPopupLoading = true;
        MonthlyWorkPopupErrorMessage = null;

        try
        {
            var fromDate = new DateOnly(row.Year, row.Month, 1);
            var toDate = new DateOnly(row.Year, row.Month, DateTime.DaysInMonth(row.Year, row.Month));
            var monthlyWork = await MonthlyWorkSummaryDataProvider.LoadEmployeeMonthAsync(
                fromDate,
                toDate,
                row.EmployeeId,
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
        catch
        {
            MonthlyWorkPopupErrorMessage = "Không thể tải bảng công tháng của nhân viên.";
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

    /// <summary>Làm mới cho luồng <c>RefreshSingleRowAsync</c>.</summary>
    private async Task RefreshSingleRowAsync(PayrollResponsibilityAllowanceAbcItemDto row)
    {
        if (!CanRefreshRow(row))
        {
            return;
        }

        try
        {
            RecalculatePayrollResponsibilityAllowanceAbcResult? result = null;
            await RunBusyAsync(
                $"Đang làm mới dòng trách nhiệm của {row.EmployeeCode}...",
                async () =>
                {
                    result = await AbcCommandProvider.RecalculateAsync(
                        new RefreshPayrollResponsibilityAllowanceAbcRequest(
                            row.Year,
                            row.Month,
                            row.EmployeeId,
                            GetConcurrencyTimestamp(row)),
                        disposalTokenSource.Token);
                    await ReloadAsync();
                });

            if (result is not null && !HasLoadError)
            {
                if (result.Refresh.SkippedMissingSource > 0)
                {
                    ToastService.ShowWarning($"Chưa làm mới {row.EmployeeCode} vì chưa có nguồn gán trách nhiệm hợp lệ.");
                }
                else if (result.Calculate.SkippedLocked > 0)
                {
                    ToastService.ShowInfo($"Dòng trách nhiệm của {row.EmployeeCode} đã bị khóa nên được giữ nguyên.");
                }
                else if (result.Refresh.Inserted + result.Refresh.Updated == 0
                         && result.Calculate.Updated == 0)
                {
                    ToastService.ShowInfo($"Dữ liệu phụ cấp trách nhiệm của {row.EmployeeCode} không có thay đổi.");
                }
                else
                {
                    ToastService.ShowSuccess($"Đã làm mới dữ liệu phụ cấp trách nhiệm cho {row.EmployeeCode}.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            ToastService.ShowError(
                $"Không thể làm mới dữ liệu phụ cấp trách nhiệm của {row.EmployeeCode}. Vui lòng tải lại và thử lại.");
        }
    }

    /// <summary>Chuyển đổi trạng thái cho luồng <c>ToggleLockAsync</c>.</summary>
    private async Task ToggleLockAsync(PayrollResponsibilityAllowanceAbcItemDto row)
    {
        if (!CanUseLoadedDataActions)
        {
            return;
        }

        var shouldLock = !row.IsLocked;
        try
        {
            PayrollResponsibilityAllowanceAbcItemDto? updatedRow = null;
            await RunBusyAsync(
                shouldLock
                    ? $"Đang khóa dòng trách nhiệm của {row.EmployeeCode}..."
                    : $"Đang mở khóa dòng trách nhiệm của {row.EmployeeCode}...",
                async () =>
                {
                    updatedRow = await AbcCommandProvider.SetLockStateAsync(
                        row.EmployeeId,
                        row.Year,
                        row.Month,
                        shouldLock,
                        GetConcurrencyTimestamp(row),
                        disposalTokenSource.Token);
                });

            if (updatedRow is not null && !HasLoadError)
            {
                ApplyUpdatedAbcRow(updatedRow);
                ToastService.ShowSuccess(shouldLock ? "Đã khóa dòng trách nhiệm." : "Đã mở khóa dòng trách nhiệm.");
            }
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            ToastService.ShowError(
                shouldLock
                    ? $"Không thể khóa phụ cấp trách nhiệm của {row.EmployeeCode}."
                    : $"Không thể mở khóa phụ cấp trách nhiệm của {row.EmployeeCode}.");
        }
    }

    /// <summary>Mở cho luồng <c>OpenLockActionPopupAsync</c>.</summary>
    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if (!CanOperateOnCurrentDataset)
        {
            return Task.CompletedTask;
        }

        PendingLockActionState = shouldLock;
        PendingLockActionMonth = AppliedMonth;
        PendingLockActionYear = AppliedYear;
        SelectedLockActionScope = CanChooseSelectedRowsScope
            ? LockScopeSelectedRows
            : LockScopeWholePeriod;
        IsLockActionPopupVisible = true;
        return Task.CompletedTask;
    }

    /// <summary>Đóng cho luồng <c>CloseLockActionPopup</c>.</summary>
    private void CloseLockActionPopup()
    {
        if (IsExecutingCommand)
        {
            return;
        }

        IsLockActionPopupVisible = false;
    }

    /// <summary>Cập nhật lựa chọn cho luồng <c>SelectLockActionScope</c>.</summary>
    private void SelectLockActionScope(string scope)
    {
        if (IsExecutingCommand)
        {
            return;
        }

        if (string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal) && CanChooseSelectedRowsScope)
        {
            SelectedLockActionScope = LockScopeSelectedRows;
            return;
        }

        if (string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal))
        {
            SelectedLockActionScope = LockScopeWholePeriod;
        }
    }

    /// <summary>Xác nhận cho luồng <c>ConfirmLockActionAsync</c>.</summary>
    private async Task ConfirmLockActionAsync()
    {
        var shouldLock = PendingLockActionState;
        var actionScope = SelectedLockActionScope;
        if (!CanConfirmLockAction)
        {
            return;
        }

        Guid[]? employeeIds = null;
        PayrollResponsibilityAllowanceAbcItemDto[] lockTargetRows;
        var selectedCount = 0;
        if (!IsWholePeriodLockActionScope(actionScope))
        {
            var selectedRows = GetSelectedAbcRows()
                .DistinctBy(row => row.EmployeeId)
                .ToArray();
            if (selectedRows.Length == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một nhân viên hoặc chuyển sang phạm vi toàn bộ kỳ.");
                return;
            }

            employeeIds = selectedRows.Select(row => row.EmployeeId).ToArray();
            lockTargetRows = selectedRows;
            selectedCount = employeeIds.Length;
        }
        else
        {
            // Whole-period commands must validate every target row, not only the
            // current server-paged grid page.
            lockTargetRows = (await AbcQueryProvider.LoadAllAsync(
                PendingLockActionYear,
                PendingLockActionMonth,
                disposalTokenSource.Token)).ToArray();
        }

        var concurrencyTokens = lockTargetRows
            .Select(row => new PayrollResponsibilityAllowanceAbcConcurrencyToken(
                row.EmployeeId,
                GetConcurrencyTimestamp(row)))
            .ToArray();

        CloseLockActionPopup();
        var actionText = shouldLock ? "khóa" : "mở khóa";
        SetPayrollResponsibilityAllowanceAbcBatchLockStateResult? result = null;
        try
        {
            await RunBusyAsync(
                BuildLockActionLoadingText(actionText, actionScope, selectedCount),
                async () =>
                {
                    result = await AbcCommandProvider.SetLockStateBatchAsync(
                        new SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest(
                            PendingLockActionYear,
                            PendingLockActionMonth,
                            shouldLock,
                            employeeIds,
                            concurrencyTokens),
                        disposalTokenSource.Token);
                    if (result.UpdatedCount > 0)
                    {
                        await ReloadAsync();
                    }
                });

            if (result is null || HasLoadError)
            {
                return;
            }

            if (result.TargetRowCount == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu phụ cấp trách nhiệm phù hợp để {actionText}.");
                return;
            }

            if (result.UpdatedCount == 0)
            {
                ToastService.ShowInfo(
                    shouldLock
                        ? "Các dữ liệu trong phạm vi đã được khóa trước đó."
                        : "Các dữ liệu trong phạm vi đã được mở khóa trước đó.");
                return;
            }

            ToastService.ShowSuccess(
                $"Đã {actionText} {result.UpdatedCount:N0} trên {result.TargetRowCount:N0} dữ liệu phụ cấp trách nhiệm.");
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (HrmApiException ex) when (ex.Kind == HrmApiErrorKind.Conflict)
        {
            // Không tự gửi lại command; tải snapshot mới để người dùng xác nhận thao tác lần nữa.
            await ReloadAsync();
            if (!HasLoadError)
            {
                ToastService.ShowWarning(
                    $"Dữ liệu phụ cấp trách nhiệm đã thay đổi. Đã tải lại kỳ {CurrentPeriodLabel}; hãy kiểm tra và {actionText} lại.");
            }
        }
        catch (HrmApiException ex)
        {
            ToastService.ShowError($"Không thể {actionText} dữ liệu phụ cấp trách nhiệm. {ex.UserMessage}");
        }
        catch (Exception)
        {
            ToastService.ShowError($"Không thể {actionText} dữ liệu phụ cấp trách nhiệm. Vui lòng thử lại.");
        }
    }

    #endregion
}
