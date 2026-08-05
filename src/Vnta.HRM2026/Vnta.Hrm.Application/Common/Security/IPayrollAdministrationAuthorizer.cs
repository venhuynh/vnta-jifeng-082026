namespace Vnta.Hrm.Application.Common.Security;

/// <summary>
/// Server-side capability boundary shared by every transport that can execute
/// a payroll-administration command.
/// </summary>
public interface IPayrollAdministrationAuthorizer
{
    Task DemandAsync(CancellationToken cancellationToken = default);
}
