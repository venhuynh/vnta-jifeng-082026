namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Queries;

/// <summary>Read-only capability for the other responsibility allowance snapshot.</summary>
public interface IOtherResponsibilityAllowanceReadService
{
    Task<IReadOnlyList<OtherResponsibilityAllowanceListItemDto>> SearchAsync(
        OtherResponsibilityAllowanceFilter filter,
        CancellationToken cancellationToken = default);
}
