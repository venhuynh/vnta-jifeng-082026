using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;

namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;

/// <summary>Creates an other-allowance row.</summary>
public interface IOtherAllowanceCreateService
{
    Task<OtherAllowanceCommandResult> CreateAsync(
        CreateOtherAllowanceRequest request,
        CancellationToken cancellationToken = default);
}
