namespace Vnta.Hrm.Application.Common.Security;

/// <summary>
/// Server-side authorization boundary for read access to monthly attendance data.
/// </summary>
public interface IAttendanceMonthlyWorkReadAuthorizer
{
    Task DemandAsync(CancellationToken cancellationToken = default);
}
