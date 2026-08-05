using System.Globalization;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Employees;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Đại diện kiểu <c>PhuCapTrachNhiem</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public partial class PhuCapTrachNhiem
{
    #region Thao tác popup gán nhân viên

    /// <summary>Mở cho luồng <c>OpenAssignmentsPopupAsync</c>.</summary>
    private async Task OpenAssignmentsPopupAsync()
    {
        AssignmentsPopupErrorMessage = null;
        AssignmentsPopupPeriod = GetRequestedPeriod();
        AssignmentSearchText = string.Empty;

        try
        {
            await RunBusyAsync(
                $"Đang tải dữ liệu gán nhân viên kỳ {AssignmentsPopupPeriodLabel}...",
                async () =>
                {
                    await EnsureRequestedPeriodLoadedAsync();
                    if (HasLoadError)
                    {
                        return;
                    }

                    await Task.WhenAll(
                        EnsureLookupDataAsync(includeEmployees: true, includePositions: true),
                        EnsureConfigLoadedAsync(AssignmentsPopupPeriod));
                });

            if (HasLoadError)
            {
                return;
            }

            BuildAssignmentEditorRows();
            IsAssignmentsPopupVisible = true;
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
            AssignmentsPopupErrorMessage = ex.Message;
            ToastService.ShowError("Không thể tải dữ liệu gán trách nhiệm theo nhân viên.");
        }
    }

    private decimal GetEditorStandardAmount(EmployeeAssignmentEditorModel editor)
    {
        return TryParseGuid(editor.GradeIdText, out var gradeId)
            ? GradeRows.FirstOrDefault(row => row.Id == gradeId)?.StandardResponsibilityAllowanceAmount ?? 0m
            : 0m;
    }

    /// <summary>Mở cho luồng <c>OpenAssignmentsFromConfigAsync</c>.</summary>
    private async Task OpenAssignmentsFromConfigAsync()
    {
        IsConfigPopupVisible = false;
        await OpenAssignmentsPopupAsync();
    }

    /// <summary>Tạo cho luồng <c>BuildAssignmentEditorRows</c>.</summary>
    private void BuildAssignmentEditorRows()
    {
        if (EmployeeRows.Count == 0)
        {
            AssignmentEditorRows = [];
            return;
        }

        var assignmentsByEmployeeId = EmployeeAssignmentRows.ToDictionary(row => row.EmployeeId);
        var lockedEmployeeIds = AbcRows
            .Where(row => row.IsLocked)
            .Select(row => row.EmployeeId)
            .ToHashSet();

        AssignmentEditorRows = EmployeeRows
            .Select(employee =>
            {
                assignmentsByEmployeeId.TryGetValue(employee.Id, out var assignment);
                return new EmployeeAssignmentEditorRow(
                    employee,
                    new EmployeeAssignmentEditorModel
                    {
                        GradeIdText = assignment?.GradeId?.ToString() ?? string.Empty,
                        Note = assignment?.Note ?? string.Empty,
                        AssignmentSource = assignment?.AssignmentSource ?? string.Empty
                    },
                    lockedEmployeeIds.Contains(employee.Id));
            })
            .ToList();
    }

    /// <summary>Lưu cho luồng <c>SaveEmployeeAssignmentAsync</c>.</summary>
    private async Task SaveEmployeeAssignmentAsync(EmployeeAssignmentEditorRow row)
    {
        AssignmentsPopupErrorMessage = null;

        if (row.IsLocked)
        {
            AssignmentsPopupErrorMessage = "Dòng trách nhiệm đã bị khóa, không thể cập nhật gán trách nhiệm.";
            return;
        }

        try
        {
            await RunBusyAsync(
                $"Đang lưu gán trách nhiệm cho {row.Employee.EmployeeCode}...",
                async () =>
                {
                    var period = AssignmentsPopupPeriod;
                    if (!TryParseGuid(row.Editor.GradeIdText, out var gradeId))
                    {
                        throw new InvalidOperationException("Hãy chọn bậc trách nhiệm trước khi lưu.");
                    }

                    await ConfigurationProvider.SaveEmployeeAssignmentAsync(
                        new SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest(
                            EmployeeAssignmentRows.FirstOrDefault(item => item.EmployeeId == row.Employee.Id)?.Id,
                            period.Year,
                            period.Month,
                            row.Employee.Id,
                            gradeId,
                            row.Editor.Note),
                        disposalTokenSource.Token);

                    await AbcCommandProvider.RefreshAsync(
                        new RefreshPayrollResponsibilityAllowanceAbcRequest(period.Year, period.Month, row.Employee.Id),
                        disposalTokenSource.Token);

                    await ReloadAsync();
                    await ReloadConfigAsync(period);
                });

            if (!HasLoadError)
            {
                BuildAssignmentEditorRows();
                ToastService.ShowSuccess($"Đã lưu gán trách nhiệm cho {row.Employee.EmployeeCode}.");
            }
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
            AssignmentsPopupErrorMessage = ex.Message;
        }
    }

    /// <summary>Áp dụng cho luồng <c>ApplyPositionDefaultsAsync</c>.</summary>
    private async Task ApplyPositionDefaultsAsync()
    {
        AssignmentsPopupErrorMessage = null;

        try
        {
            PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult? result = null;
            await RunBusyAsync(
                $"Đang áp dụng mặc định chức vụ cho kỳ {AssignmentsPopupPeriodLabel}...",
                async () =>
                {
                    var period = AssignmentsPopupPeriod;
                    result = await ConfigurationProvider.ApplyPositionDefaultsAsync(
                        period.Year,
                        period.Month,
                        disposalTokenSource.Token);
                    await AbcCommandProvider.RefreshAsync(
                        new RefreshPayrollResponsibilityAllowanceAbcRequest(period.Year, period.Month, null),
                        disposalTokenSource.Token);
                    await ReloadAsync();
                    await ReloadConfigAsync(period);
                });

            if (result is not null && !HasLoadError)
            {
                BuildAssignmentEditorRows();
                ToastService.ShowSuccess($"Đã áp dụng mặc định chức vụ cho {result.Updated} nhân viên.");
            }
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
            AssignmentsPopupErrorMessage = ex.Message;
        }
    }

    #endregion
}
