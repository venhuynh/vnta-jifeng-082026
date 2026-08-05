using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;

/// <summary>Reads other-allowance rows for a payroll period.</summary>
public interface IOtherAllowanceReadService
{
    Task<OtherAllowancePageDto> SearchPageAsync(
        OtherAllowanceFilter filter,
        CancellationToken cancellationToken = default);
}
