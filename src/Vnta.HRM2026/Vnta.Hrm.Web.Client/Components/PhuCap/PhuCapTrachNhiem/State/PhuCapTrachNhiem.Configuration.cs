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
    #region Thao tác popup cấu hình

    /// <summary>Thực hiện xử lý cho luồng <c>HandleRouteFocusAsync</c>.</summary>
    private async Task HandleRouteFocusAsync()
    {
        var focus = NormalizeFocus(Focus);
        if (string.IsNullOrWhiteSpace(focus))
        {
            handledFocus = null;
            return;
        }

        if (string.Equals(handledFocus, focus, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        handledFocus = focus;

        if (string.Equals(focus, FocusPositionAssignments, StringComparison.OrdinalIgnoreCase))
        {
            await OpenConfigPopupAsync(ConfigMappingsTabIndex);
            ClearRouteFocus();
            return;
        }

        if (string.Equals(focus, FocusEmployeeAssignments, StringComparison.OrdinalIgnoreCase))
        {
            await OpenAssignmentsPopupAsync();
            ClearRouteFocus();
            return;
        }

        handledFocus = null;
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ClearRouteFocus</c>.</summary>
    private void ClearRouteFocus()
    {
        NavigationManager.NavigateTo("/payroll/responsibility-allowance", replace: true);
    }

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeFocus</c>.</summary>
    private static string? NormalizeFocus(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized.ToLowerInvariant();
    }

    /// <summary>Mở cho luồng <c>OpenConfigPopupAsync</c>.</summary>
    private async Task OpenConfigPopupAsync(int activeTabIndex)
    {
        ConfigPopupErrorMessage = null;
        ConfigPopupPeriod = GetRequestedPeriod();
        ConfigPopupActiveTabIndex = activeTabIndex;
        ResetGradeForm();
        ResetMappingForm();

        try
        {
            await RunBusyAsync(
                $"Đang tải cấu hình trách nhiệm kỳ {ConfigPopupPeriodLabel}...",
                async () =>
                {
                    await Task.WhenAll(
                        EnsureLookupDataAsync(includeEmployees: false, includePositions: true),
                        EnsureConfigLoadedAsync(ConfigPopupPeriod));
                });
            IsConfigPopupVisible = true;
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
            ConfigPopupErrorMessage = ex.Message;
            ToastService.ShowError("Không thể tải cấu hình trách nhiệm.");
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ReloadConfigAsync</c>.</summary>
    private Task ReloadConfigAsync() => ReloadConfigAsync(ConfigPopupPeriod);

    /// <summary>Thực hiện xử lý cho luồng <c>ReloadConfigAsync</c>.</summary>
    private async Task ReloadConfigAsync(ResponsibilityAllowancePeriodKey period)
    {
        var config = await ConfigurationProvider.GetAsync(period.Year, period.Month, disposalTokenSource.Token);
        GradeRows = config.Grades;
        MappingRows = config.Mappings;
        EmployeeAssignmentRows = config.EmployeeAssignments;
        LoadedConfigPeriod = period;
    }

    /// <summary>Thực hiện xử lý cho luồng <c>EnsureConfigLoadedAsync</c>.</summary>
    private Task EnsureConfigLoadedAsync(ResponsibilityAllowancePeriodKey period) =>
        LoadedConfigPeriod == period
            ? Task.CompletedTask
            : ReloadConfigAsync(period);

    /// <summary>Đặt lại cho luồng <c>ResetGradeForm</c>.</summary>
    private void ResetGradeForm()
    {
        EditingGradeId = null;
        GradeForm = GradeFormModel.CreateDefault();
    }

    /// <summary>Thực hiện xử lý cho luồng <c>StartEditGrade</c>.</summary>
    private void StartEditGrade(PayrollResponsibilityAllowanceGradeDto grade)
    {
        EditingGradeId = grade.Id;
        GradeForm = new GradeFormModel
        {
            Code = grade.Code,
            Name = grade.Name,
            StandardResponsibilityAllowanceAmount = grade.StandardResponsibilityAllowanceAmount,
            DisplayOrder = grade.DisplayOrder,
            IsActive = grade.IsActive,
            Note = grade.Note ?? string.Empty
        };
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnGradeAmountChanged</c>.</summary>
    private void OnGradeAmountChanged(decimal value) => GradeForm.StandardResponsibilityAllowanceAmount = value;

    /// <summary>Xử lý sự kiện cho luồng <c>OnGradeDisplayOrderChanged</c>.</summary>
    private void OnGradeDisplayOrderChanged(int value) => GradeForm.DisplayOrder = value;

    /// <summary>Lưu cho luồng <c>SaveGradeAsync</c>.</summary>
    private async Task SaveGradeAsync()
    {
        ConfigPopupErrorMessage = null;

        try
        {
            var period = ConfigPopupPeriod;
            await ConfigurationProvider.SaveGradeAsync(
                new SavePayrollResponsibilityAllowanceGradeRequest(
                    EditingGradeId,
                    period.Year,
                    period.Month,
                    GradeForm.Code,
                    GradeForm.Name,
                    GradeForm.StandardResponsibilityAllowanceAmount,
                    GradeForm.DisplayOrder,
                    GradeForm.IsActive,
                    GradeForm.Note),
                disposalTokenSource.Token);

            await ReloadConfigAsync();
            ResetGradeForm();
            ToastService.ShowSuccess("Đã lưu cấp bậc trách nhiệm.");
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
            ConfigPopupErrorMessage = ex.Message;
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>CopyGradesFromPreviousMonthAsync</c>.</summary>
    private async Task CopyGradesFromPreviousMonthAsync()
    {
        ConfigPopupErrorMessage = null;

        try
        {
            var previous = ConfigPopupPeriod.GetPreviousPeriod();
            var previousConfig = await ConfigurationProvider.GetAsync(previous.Year, previous.Month, disposalTokenSource.Token);
            if (previousConfig.Grades.Count == 0)
            {
                ConfigPopupErrorMessage = $"Kỳ {previous.Month:00}/{previous.Year} chưa có cấp bậc để lấy.";
                return;
            }

            var period = ConfigPopupPeriod;
            var result = await ConfigurationProvider.CopyFromPreviousMonthAsync(
                period.Year,
                period.Month,
                copyMappings: false,
                disposalTokenSource.Token);
            await ReloadConfigAsync();
            ToastService.ShowSuccess($"Đã lấy cấp bậc từ tháng trước: thêm {result.CreatedCount}, bỏ qua {result.SkippedCount} bản ghi đã có.");
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
            ConfigPopupErrorMessage = ex.Message;
        }
    }

    /// <summary>Đặt lại cho luồng <c>ResetMappingForm</c>.</summary>
    private void ResetMappingForm()
    {
        EditingMappingId = null;
        MappingForm = MappingFormModel.CreateDefault();
    }

    /// <summary>Thực hiện xử lý cho luồng <c>StartEditMapping</c>.</summary>
    private void StartEditMapping(PayrollResponsibilityAllowanceGradePositionDto mapping)
    {
        EditingMappingId = mapping.Id;
        MappingForm = new MappingFormModel
        {
            PositionIdText = mapping.PositionId.ToString(),
            GradeIdText = mapping.GradeId.ToString(),
            IsActive = mapping.IsActive,
            Note = mapping.Note ?? string.Empty
        };
    }

    /// <summary>Lưu cho luồng <c>SaveMappingAsync</c>.</summary>
    private async Task SaveMappingAsync()
    {
        ConfigPopupErrorMessage = null;

        try
        {
            var positionId = ParseRequiredGuid(MappingForm.PositionIdText, "Chức vụ");
            var gradeId = ParseRequiredGuid(MappingForm.GradeIdText, "Cấp bậc");
            var period = ConfigPopupPeriod;

            await ConfigurationProvider.SaveMappingAsync(
                new SavePayrollResponsibilityAllowanceGradePositionRequest(
                    EditingMappingId,
                    period.Year,
                    period.Month,
                    gradeId,
                    positionId,
                    MappingForm.IsActive,
                    MappingForm.Note),
                disposalTokenSource.Token);

            await ReloadConfigAsync();
            ResetMappingForm();
            ToastService.ShowSuccess("Đã lưu mapping chức vụ.");
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
            ConfigPopupErrorMessage = ex.Message;
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>DeactivateMappingAsync</c>.</summary>
    private async Task DeactivateMappingAsync(PayrollResponsibilityAllowanceGradePositionDto mapping)
    {
        ConfigPopupErrorMessage = null;

        try
        {
            await ConfigurationProvider.DeactivateMappingAsync(mapping.Id, disposalTokenSource.Token);
            await ReloadConfigAsync();
            ToastService.ShowSuccess("Đã ngừng mapping chức vụ.");
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
            ConfigPopupErrorMessage = ex.Message;
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>CopyMappingsFromPreviousMonthAsync</c>.</summary>
    private async Task CopyMappingsFromPreviousMonthAsync()
    {
        ConfigPopupErrorMessage = null;

        try
        {
            var previous = ConfigPopupPeriod.GetPreviousPeriod();
            var previousConfig = await ConfigurationProvider.GetAsync(previous.Year, previous.Month, disposalTokenSource.Token);
            if (previousConfig.Mappings.Count == 0)
            {
                ConfigPopupErrorMessage = $"Kỳ {previous.Month:00}/{previous.Year} chưa có mapping chức vụ để lấy.";
                return;
            }

            var period = ConfigPopupPeriod;
            var result = await ConfigurationProvider.CopyFromPreviousMonthAsync(
                period.Year,
                period.Month,
                copyMappings: true,
                disposalTokenSource.Token);
            await ReloadConfigAsync();
            ToastService.ShowSuccess($"Đã lấy mapping từ tháng trước: thêm {result.CreatedCount}, bỏ qua {result.SkippedCount} bản ghi đã có.");
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
            ConfigPopupErrorMessage = ex.Message;
        }
    }

    #endregion
}
