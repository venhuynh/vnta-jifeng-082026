using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapTrachNhiem;

public sealed class HttpPayrollResponsibilityAllowanceWorkflowService(NavigationManager navigationManager)
    : IPayrollResponsibilityAllowanceGradeConfigurationReadService,
      IPayrollResponsibilityAllowanceGradeConfigurationWriteService,
      IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService,
      IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService,
      IPayrollResponsibilityAllowanceEmployeeAssignmentExportService,
      IPayrollResponsibilityAllowanceMonthlyAbcQueryService,
      IPayrollResponsibilityAllowanceMonthlyAbcExportService,
      IPayrollResponsibilityAllowanceMonthlyAbcCommandService,
      IPayrollResponsibilityAllowanceRecalculationService
{
    #region Dependencies

    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    #endregion

    #region Grade Configuration Workflow

    public async Task<PayrollResponsibilityAllowanceGradeConfigDto> GetGradeConfigAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"api/payroll/responsibility-allowance/grade-config?year={year}&month={month}",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceGradeConfigDto>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceGradeDto> SaveGradeAsync(
        SavePayrollResponsibilityAllowanceGradeRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/grade-config/grades",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceGradeDto>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceGradePositionDto> SaveMappingAsync(
        SavePayrollResponsibilityAllowanceGradePositionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/grade-config/mappings",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceGradePositionDto>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceGradePositionDto> DeactivateMappingAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"api/payroll/responsibility-allowance/grade-config/mappings/{id}/deactivate",
            content: null,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceGradePositionDto>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceConfigCopyResult> CopyFromPreviousMonthAsync(
        int year,
        int month,
        bool copyMappings,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/grade-config/copy-from-previous",
            new { year, month, copyMappings },
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceConfigCopyResult>(cancellationToken);
    }

    #endregion

    #region Employee Assignment Workflow

    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentDto> SaveEmployeeAssignmentAsync(
        SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/grade-config/employee-assignments",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceEmployeeAssignmentDto>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> EnsureEmployeeAssignmentsForSummariesAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/grade-config/employee-assignments/synchronize-summaries",
            new { year, month },
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> LoadEmployeeAssignmentsFromPreviousMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/employee-assignments/load-from-previous-month",
            new { year, month },
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> RecalculateEmployeeAssignmentsAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/employee-assignments/recalculate",
            new { year, month },
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> ApplyPositionDefaultsToEmployeeAssignmentsAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/grade-config/employee-assignments/apply-position-defaults",
            new { year, month },
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult>(cancellationToken);
    }

    public async Task<UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult> UpdateAndRefreshEmployeeAssignmentAsync(
        UpdatePayrollResponsibilityAllowanceEmployeeAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/employee-assignments/update-and-refresh",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto> SearchEmployeeAssignmentsAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentQuery query,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/employee-assignments/search",
            query,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>> ExportEmployeeAssignmentsAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/employee-assignments/export",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>>(cancellationToken);
    }

    #endregion

    #region Monthly ABC Workflow

    public async Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto>> GetAbcAsync(
        PayrollResponsibilityAllowanceAbcFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"year={filter.Year}",
            $"month={filter.Month}"
        };

        if (filter.IsLocked.HasValue)
        {
            query.Add($"isLocked={filter.IsLocked.Value}");
        }

        var response = await httpClient.GetAsync(
            $"api/payroll/responsibility-allowance?{string.Join("&", query)}",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<PayrollResponsibilityAllowanceAbcItemDto>>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceAbcPageDto> SearchAbcAsync(
        PayrollResponsibilityAllowanceAbcQuery query,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/search",
            query,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceAbcPageDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>> ExportAsync(
        PayrollResponsibilityAllowanceAbcExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/export",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto>>(cancellationToken);
    }

    public async Task<RefreshPayrollResponsibilityAllowanceAbcResult> RefreshAbcAsync(
        RefreshPayrollResponsibilityAllowanceAbcRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/refresh",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RefreshPayrollResponsibilityAllowanceAbcResult>(cancellationToken);
    }

    public async Task<CalculatePayrollResponsibilityAllowanceAbcResult> CalculateAbcAsync(
        RefreshPayrollResponsibilityAllowanceAbcRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/calculate-abc",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<CalculatePayrollResponsibilityAllowanceAbcResult>(cancellationToken);
    }

    public async Task<RecalculatePayrollResponsibilityAllowanceAbcResult> RecalculateAbcAsync(
        RefreshPayrollResponsibilityAllowanceAbcRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/recalculate",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RecalculatePayrollResponsibilityAllowanceAbcResult>(cancellationToken);
    }

    public async Task<CopyPayrollResponsibilityAllowanceAbcFromPreviousResult> CopyAbcFromPreviousMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"api/payroll/responsibility-allowance/{year}/{month}/copy-from-previous",
            content: null,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<CopyPayrollResponsibilityAllowanceAbcFromPreviousResult>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceAbcItemDto> SetLockStateAsync(
        Guid employeeId,
        int year,
        int month,
        bool isLocked,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var action = isLocked ? "lock" : "unlock";
        var concurrencyQuery = originalUpdatedAtUtc.HasValue
            ? $"?originalUpdatedAtUtc={Uri.EscapeDataString(originalUpdatedAtUtc.Value.ToString("O"))}"
            : string.Empty;
        var response = await httpClient.PostAsync(
            $"api/payroll/responsibility-allowance/{employeeId}/{year}/{month}/{action}{concurrencyQuery}",
            content: null,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceAbcItemDto>(cancellationToken);
    }

    public async Task<SetPayrollResponsibilityAllowanceAbcBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetPayrollResponsibilityAllowanceAbcBatchLockStateResult>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceAbcItemDto> SaveAdjustmentAsync(
        SavePayrollResponsibilityAllowanceAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/adjustments",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceAbcItemDto>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceUpdateContextDto> GetUpdateContextAsync(
        Guid employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"api/payroll/responsibility-allowance/update-context?employeeId={employeeId}&year={year}&month={month}",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceUpdateContextDto>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceAbcItemDto> UpdatePerformanceBonusAsync(
        Guid employeeId,
        int year,
        int month,
        decimal monthlyPerformanceBonusAmount,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/payroll/responsibility-allowance/{employeeId}/{year}/{month}/performance-bonus",
            new { monthlyPerformanceBonusAmount, originalUpdatedAtUtc },
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceAbcItemDto>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceAbcItemDto> UpdatePerformanceBonusExclusionAsync(
        Guid employeeId,
        int year,
        int month,
        bool isPerformanceBonusExcluded,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/payroll/responsibility-allowance/{employeeId}/{year}/{month}/performance-bonus-exclusion",
            new { isPerformanceBonusExcluded, originalUpdatedAtUtc },
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceAbcItemDto>(cancellationToken);
    }

    public async Task<UpdatePayrollResponsibilityPerformanceBonusForPeriodResult> UpdatePerformanceBonusForPeriodAsync(
        int year,
        int month,
        decimal monthlyPerformanceBonusAmount,
        IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? concurrencyTokens,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/payroll/responsibility-allowance/{year}/{month}/performance-bonus",
            new { monthlyPerformanceBonusAmount, concurrencyTokens },
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<UpdatePayrollResponsibilityPerformanceBonusForPeriodResult>(cancellationToken);
    }

    #endregion
}
