using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;

/// <summary>Updates an editable other-allowance row.</summary>
public interface IOtherAllowanceUpdateService
{
    Task<OtherAllowanceCommandResult> UpdateAsync(
        UpdateOtherAllowanceRequest request,
        CancellationToken cancellationToken = default);
}
