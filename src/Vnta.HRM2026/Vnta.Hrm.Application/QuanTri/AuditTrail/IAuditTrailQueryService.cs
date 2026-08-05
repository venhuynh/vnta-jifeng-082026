namespace Vnta.Hrm.Application.QuanTri.AuditTrail;

/// <summary>
/// Reads audit data after the caller has established immutable audit-read access.
/// </summary>
public interface IAuditTrailQueryService
{
    Task<AuditEventPageDto> GetPageAsync(
        AuditEventFilter filter,
        AuditReadAccess access,
        CancellationToken cancellationToken = default);

    Task<AuditEventDetailDto?> GetDetailAsync(
        Guid id,
        AuditReadAccess access,
        CancellationToken cancellationToken = default);

    Task<AuditEventContextDto?> GetContextAsync(
        Guid eventId,
        AuditReadAccess access,
        int take = 50,
        CancellationToken cancellationToken = default);
}
