namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

public enum HazardAllowanceExportJobStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

public sealed record CreateHazardAllowanceExportJobRequest(
    HazardAllowanceFilter Filter,
    string RequestedBy);

public sealed record HazardAllowanceExportJobDto(
    Guid Id,
    HazardAllowanceExportJobStatus Status,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? FileName,
    string? ErrorMessage);

public sealed record HazardAllowanceExportJobFileDto(
    Stream Content,
    string FileName,
    string ContentType);

/// <summary>Durable, user-owned export jobs for large hazard allowance datasets.</summary>
public interface IHazardAllowanceExportJobService
{
    Task<HazardAllowanceExportJobDto> QueueAsync(
        CreateHazardAllowanceExportJobRequest request,
        CancellationToken cancellationToken = default);

    Task<HazardAllowanceExportJobDto?> GetAsync(
        Guid jobId,
        string requestedBy,
        CancellationToken cancellationToken = default);

    Task<HazardAllowanceExportJobFileDto?> OpenCompletedFileAsync(
        Guid jobId,
        string requestedBy,
        CancellationToken cancellationToken = default);
}

/// <summary>Processes durable export jobs from a hosted worker without coupling the host to Infrastructure.</summary>
public interface IHazardAllowanceExportJobProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default);

    Task DeleteExpiredAsync(CancellationToken cancellationToken = default);
}
