using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;

namespace Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Persistence;

/// <summary>Kho lưu tạm cho dữ liệu tiến độ triển khai được cập nhật trên giao diện.</summary>
public interface IProjectImplementationProgressStore
{
    Task<ProjectImplementationProgressSnapshot> LoadAsync(CancellationToken cancellationToken = default);

    Task UpdateTaskAsync(
        UpdateProjectImplementationTaskRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Bản ghi toàn bộ lộ trình được lưu trong tệp JSON.</summary>
public sealed record ProjectImplementationProgressSnapshot(
    int SchemaVersion,
    IReadOnlyList<ProjectImplementationPhase> Phases)
{
    public const int CurrentSchemaVersion = 1;
}

/// <summary>Thay đổi cho một đầu việc được người dùng lưu từ DxGrid.</summary>
public sealed record UpdateProjectImplementationTaskRequest(
    Guid TaskId,
    string WorkItem,
    ProjectImplementationTaskOwner Owner,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectImplementationTaskStatus Status,
    int CompletionPercent);
