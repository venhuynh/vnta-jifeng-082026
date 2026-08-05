using System.Text.Json;
using System.Text.Json.Serialization;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Models;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.Persistence;
using Vnta.Hrm.Web.Client.Components.TienDoTrienKhai.State;

namespace Vnta.Hrm.Web.Services.TienDoTrienKhai;

/// <summary>Lưu tạm tiến độ triển khai trong tệp JSON phía máy chủ.</summary>
public sealed class JsonFileProjectImplementationProgressStore : IProjectImplementationProgressStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly SemaphoreSlim operationGate = new(1, 1);
    private readonly string storageDirectory;
    private readonly string storagePath;

    public JsonFileProjectImplementationProgressStore(IHostEnvironment hostEnvironment)
    {
        storageDirectory = Path.Combine(hostEnvironment.ContentRootPath, "App_Data");
        storagePath = Path.Combine(storageDirectory, "project-implementation-progress.json");
    }

    public async Task<ProjectImplementationProgressSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        await operationGate.WaitAsync(cancellationToken);
        try
        {
            return await ReadOrCreateSnapshotAsync(cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task UpdateTaskAsync(
        UpdateProjectImplementationTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        await operationGate.WaitAsync(cancellationToken);
        try
        {
            var snapshot = await ReadOrCreateSnapshotAsync(cancellationToken);
            var task = snapshot.Phases
                .SelectMany(phase => phase.Milestones)
                .SelectMany(milestone => milestone.Tasks)
                .SingleOrDefault(candidate => candidate.Id == request.TaskId);

            if(task is null)
            {
                throw new InvalidOperationException("Không tìm thấy đầu việc cần lưu.");
            }

            task.WorkItem = request.WorkItem.Trim();
            task.Owner = request.Owner;
            task.StartDate = request.StartDate;
            task.EndDate = request.EndDate;
            task.Status = request.Status;
            task.CompletionPercent = request.CompletionPercent;

            await WriteSnapshotAsync(snapshot, cancellationToken);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task<ProjectImplementationProgressSnapshot> ReadOrCreateSnapshotAsync(CancellationToken cancellationToken)
    {
        if(!File.Exists(storagePath))
        {
            var initialSnapshot = ProjectImplementationProgressDefaults.CreateSnapshot();
            await WriteSnapshotAsync(initialSnapshot, cancellationToken);
            return initialSnapshot;
        }

        await using var stream = new FileStream(
            storagePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous);

        var persistedSnapshot = await JsonSerializer.DeserializeAsync<ProjectImplementationProgressSnapshot>(
            stream,
            SerializerOptions,
            cancellationToken);

        if(persistedSnapshot is null || persistedSnapshot.Phases is null || persistedSnapshot.Phases.Count == 0)
        {
            throw new InvalidDataException("Tệp lưu tiến độ triển khai không có dữ liệu hợp lệ.");
        }

        if(persistedSnapshot.SchemaVersion != ProjectImplementationProgressSnapshot.CurrentSchemaVersion)
        {
            throw new InvalidDataException("Phiên bản tệp lưu tiến độ triển khai không được hỗ trợ.");
        }

        return persistedSnapshot;
    }

    private async Task WriteSnapshotAsync(
        ProjectImplementationProgressSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(storageDirectory);
        var temporaryPath = $"{storagePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using(var stream = new FileStream(
                            temporaryPath,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None,
                            bufferSize: 4096,
                            FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, snapshot, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, storagePath, overwrite: true);
        }
        finally
        {
            if(File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void Validate(UpdateProjectImplementationTaskRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkItem);

        if(request.EndDate < request.StartDate)
        {
            throw new ArgumentException("Ngày kết thúc không thể trước ngày bắt đầu.", nameof(request));
        }

        if(request.CompletionPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Phần trăm hoàn thành phải nằm trong khoảng từ 0 đến 100.");
        }

        if(!Enum.IsDefined(request.Owner) || !Enum.IsDefined(request.Status))
        {
            throw new ArgumentException("Đơn vị phụ trách hoặc tình trạng không hợp lệ.", nameof(request));
        }
    }
}
