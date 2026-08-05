using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;

/// <summary>Deletes an editable other-allowance row.</summary>
public interface IOtherAllowanceDeleteService
{
    Task DeleteAsync(
        DeleteOtherAllowanceRequest request,
        CancellationToken cancellationToken = default);
}
