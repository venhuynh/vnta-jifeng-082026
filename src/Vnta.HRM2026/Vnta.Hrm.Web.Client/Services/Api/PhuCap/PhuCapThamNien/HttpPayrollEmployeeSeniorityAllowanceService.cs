using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapThamNien;

/// <summary>
/// HTTP transport cho contract phụ cấp thâm niên.
/// Lớp này chỉ truyền request/response; không được sao chép quy tắc tính từ server.
/// </summary>
public sealed class HttpPayrollEmployeeSeniorityAllowanceService(NavigationManager navigationManager)
    : IPayrollEmployeeSeniorityAllowanceReadService,
      IPayrollEmployeeSeniorityAllowanceRangeSummaryService,
      IPayrollEmployeeSeniorityAllowancePeriodPreparationService,
      IPayrollEmployeeSeniorityAllowanceRefreshService,
      IPayrollEmployeeSeniorityAllowanceManualAdjustmentService,
      IPayrollEmployeeSeniorityAllowanceLockService
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };

    public async Task PreparePeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"api/payroll/seniority-allowance/prepare-period?year={year}&month={month}",
            content: null,
            cancellationToken);

        await response.EnsureSuccessAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceListItemDto>> SearchAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var querySegments = new List<string>
        {
            $"year={filter.PayrollYear}",
            $"month={filter.PayrollMonth}",
            $"take={filter.Take}"
        };

        if(!string.IsNullOrWhiteSpace(filter.DepartmentName))
        {
            querySegments.Add($"departmentName={Uri.EscapeDataString(filter.DepartmentName)}");
        }

        if(!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            querySegments.Add($"searchText={Uri.EscapeDataString(filter.SearchText)}");
        }

        if(filter.IsLocked.HasValue)
        {
            querySegments.Add($"isLocked={filter.IsLocked.Value}");
        }

        if(!string.IsNullOrWhiteSpace(filter.SeniorityRangeKey))
        {
            querySegments.Add($"seniorityRangeKey={Uri.EscapeDataString(filter.SeniorityRangeKey)}");
        }

        var response = await httpClient.GetAsync(
            $"api/payroll/seniority-allowance?{string.Join("&", querySegments)}",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<PayrollEmployeeSeniorityAllowanceListItemDto>>(cancellationToken);
    }

    public async Task<PayrollEmployeeSeniorityAllowancePageDto> SearchPageAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var querySegments = new List<string>
        {
            $"year={filter.PayrollYear}",
            $"month={filter.PayrollMonth}",
            $"skip={filter.Skip}",
            $"take={filter.Take}"
        };

        if(!string.IsNullOrWhiteSpace(filter.DepartmentName))
        {
            querySegments.Add($"departmentName={Uri.EscapeDataString(filter.DepartmentName)}");
        }

        if(!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            querySegments.Add($"searchText={Uri.EscapeDataString(filter.SearchText)}");
        }

        if(filter.IsLocked.HasValue)
        {
            querySegments.Add($"isLocked={filter.IsLocked.Value}");
        }

        if(!string.IsNullOrWhiteSpace(filter.SeniorityRangeKey))
        {
            querySegments.Add($"seniorityRangeKey={Uri.EscapeDataString(filter.SeniorityRangeKey)}");
        }

        var response = await httpClient.GetAsync(
            $"api/payroll/seniority-allowance/search-page?{string.Join("&", querySegments)}",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollEmployeeSeniorityAllowancePageDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>> GetRangeSummariesAsync(
        PayrollEmployeeSeniorityAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var querySegments = new List<string>
        {
            $"year={filter.PayrollYear}",
            $"month={filter.PayrollMonth}"
        };

        if(!string.IsNullOrWhiteSpace(filter.DepartmentName))
        {
            querySegments.Add($"departmentName={Uri.EscapeDataString(filter.DepartmentName)}");
        }

        if(!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            querySegments.Add($"searchText={Uri.EscapeDataString(filter.SearchText)}");
        }

        if(filter.IsLocked.HasValue)
        {
            querySegments.Add($"isLocked={filter.IsLocked.Value}");
        }

        var response = await httpClient.GetAsync(
            $"api/payroll/seniority-allowance/range-summaries?{string.Join("&", querySegments)}",
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<PayrollEmployeeSeniorityAllowanceRangeSummaryDto>>(cancellationToken);
    }

    /// <summary>
    /// Gửi request đến HTTP boundary; server là nguồn xác nhận cuối cùng của công tính lương, khóa và số tiền.
    /// </summary>
    public async Task<RefreshPayrollEmployeeSeniorityAllowanceResult> RefreshAsync(
        RefreshPayrollEmployeeSeniorityAllowanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/seniority-allowance/refresh",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<RefreshPayrollEmployeeSeniorityAllowanceResult>(cancellationToken);
    }

    public async Task<PayrollEmployeeSeniorityAllowanceListItemDto> UpdateManualValuesAsync(
        UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/seniority-allowance/manual-values",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollEmployeeSeniorityAllowanceListItemDto>(cancellationToken);
    }

    public async Task<PayrollEmployeeSeniorityAllowanceListItemDto> SetLockStateAsync(
        SetPayrollEmployeeSeniorityAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/seniority-allowance/lock-state",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<PayrollEmployeeSeniorityAllowanceListItemDto>(cancellationToken);
    }

    public async Task<SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/seniority-allowance/lock-state/batch",
            request,
            cancellationToken);

        return await response.ReadRequiredFromJsonAsync<SetPayrollEmployeeSeniorityAllowanceBatchLockStateResult>(cancellationToken);
    }
}
