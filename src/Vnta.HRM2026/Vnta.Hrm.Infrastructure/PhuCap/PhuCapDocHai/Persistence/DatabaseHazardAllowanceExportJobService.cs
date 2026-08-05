using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapDocHai;

public sealed class DatabaseHazardAllowanceExportJobService(
    ApplicationDbContext dbContext,
    IHazardAllowanceExportService hazardAllowanceExportService,
    IOptions<HazardAllowanceExportJobOptions> optionsAccessor,
    ILogger<DatabaseHazardAllowanceExportJobService> logger,
    IHazardAllowanceRequestValidator requestValidator)
    : IHazardAllowanceExportJobService, IHazardAllowanceExportJobProcessor
{
    private const int MaximumActiveJobsPerUser = 3;
    private readonly HazardAllowanceExportJobOptions options = optionsAccessor.Value;

    public async Task<HazardAllowanceExportJobDto> QueueAsync(
        CreateHazardAllowanceExportJobRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        requestValidator.Validate(request).ThrowIfInvalid();
        var requestedBy = NormalizeRequired(request.RequestedBy, nameof(request.RequestedBy));
        var activeJobs = await dbContext.HazardAllowanceExportJobs.CountAsync(
            job => job.RequestedBy == requestedBy
                && (job.Status == HazardAllowanceExportJobStatus.Queued || job.Status == HazardAllowanceExportJobStatus.Running),
            cancellationToken);
        if(activeJobs >= MaximumActiveJobsPerUser)
        {
            throw new InvalidOperationException("Mỗi người dùng chỉ có thể có tối đa 3 export phụ cấp độc hại đang chờ xử lý.");
        }

        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        var job = new HazardAllowanceExportJobRow
        {
            Id = Guid.NewGuid(),
            FilterJson = JsonSerializer.Serialize(request.Filter),
            RequestedBy = requestedBy,
            Status = HazardAllowanceExportJobStatus.Queued,
            CreatedAtUtc = now
        };
        dbContext.HazardAllowanceExportJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(job);
    }

    public async Task<HazardAllowanceExportJobDto?> GetAsync(
        Guid jobId,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        var owner = NormalizeRequired(requestedBy, nameof(requestedBy));
        var job = await dbContext.HazardAllowanceExportJobs.AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == jobId && row.RequestedBy == owner, cancellationToken);
        return job is null ? null : Map(job);
    }

    public async Task<HazardAllowanceExportJobFileDto?> OpenCompletedFileAsync(
        Guid jobId,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        var owner = NormalizeRequired(requestedBy, nameof(requestedBy));
        var job = await dbContext.HazardAllowanceExportJobs.AsNoTracking()
            .SingleOrDefaultAsync(
                row => row.Id == jobId
                    && row.RequestedBy == owner
                    && row.Status == HazardAllowanceExportJobStatus.Completed,
                cancellationToken);
        if(job?.OutputPath is null || job.FileName is null)
        {
            return null;
        }

        var root = GetOutputDirectory();
        var path = Path.GetFullPath(job.OutputPath);
        if(!IsWithinOutputDirectory(root, path) || !File.Exists(path))
        {
            return null;
        }

        return new HazardAllowanceExportJobFileDto(
            new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read),
            job.FileName,
            "text/csv; charset=utf-8");
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        var runningTimeoutMinutes = Math.Clamp(options.RunningJobTimeoutMinutes, 1, 24 * 60);
        var staleCutoff = ToDatabaseTimestamp(now.AddMinutes(-runningTimeoutMinutes));
        await dbContext.HazardAllowanceExportJobs
            .Where(job => job.Status == HazardAllowanceExportJobStatus.Running
                && job.StartedAtUtc != null
                && job.StartedAtUtc < staleCutoff)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, HazardAllowanceExportJobStatus.Queued)
                    .SetProperty(job => job.StartedAtUtc, (DateTime?)null)
                    .SetProperty(job => job.ErrorMessage, "Worker trước đó quá thời hạn; job được xếp lại."),
                cancellationToken);

        var candidateId = await dbContext.HazardAllowanceExportJobs.AsNoTracking()
            .Where(job => job.Status == HazardAllowanceExportJobStatus.Queued)
            .OrderBy(job => job.CreatedAtUtc)
            .Select(job => (Guid?)job.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if(candidateId is not Guid jobId)
        {
            return false;
        }

        var startedAtUtc = ToDatabaseTimestamp(DateTime.UtcNow);
        var claimed = await dbContext.HazardAllowanceExportJobs
            .Where(job => job.Id == jobId && job.Status == HazardAllowanceExportJobStatus.Queued)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, HazardAllowanceExportJobStatus.Running)
                    .SetProperty(job => job.StartedAtUtc, startedAtUtc),
                cancellationToken);
        if(claimed != 1)
        {
            return true;
        }

        var job = await dbContext.HazardAllowanceExportJobs.SingleAsync(row => row.Id == jobId, cancellationToken);
        try
        {
            var filter = JsonSerializer.Deserialize<HazardAllowanceFilter>(job.FilterJson)
                ?? throw new InvalidOperationException("Điều kiện export phụ cấp độc hại không hợp lệ.");
            var rows = await hazardAllowanceExportService.ExportAsync(filter, cancellationToken);
            var root = GetOutputDirectory();
            Directory.CreateDirectory(root);
            var fileName = $"hazard-allowance-{filter.PayrollYear:D4}-{filter.PayrollMonth:D2}-{job.Id:N}.csv";
            var outputPath = Path.Combine(root, fileName);
            await WriteCsvAsync(outputPath, rows, cancellationToken);

            job.Status = HazardAllowanceExportJobStatus.Completed;
            job.FileName = fileName;
            job.OutputPath = outputPath;
            job.CompletedAtUtc = ToDatabaseTimestamp(DateTime.UtcNow);
            job.ErrorMessage = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch(Exception ex) when(ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Hazard allowance export job {JobId} failed.", jobId);
            job.Status = HazardAllowanceExportJobStatus.Failed;
            job.CompletedAtUtc = ToDatabaseTimestamp(DateTime.UtcNow);
            job.ErrorMessage = Truncate(ex.Message, 2000);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        return true;
    }

    public async Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var retentionHours = Math.Clamp(options.RetentionHours, 1, 24 * 30);
        var cutoff = ToDatabaseTimestamp(DateTime.UtcNow.AddHours(-retentionHours));
        var expired = await dbContext.HazardAllowanceExportJobs
            .Where(job => (job.Status == HazardAllowanceExportJobStatus.Completed || job.Status == HazardAllowanceExportJobStatus.Failed)
                && job.CompletedAtUtc != null && job.CompletedAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        foreach(var job in expired)
        {
            if(!string.IsNullOrWhiteSpace(job.OutputPath)
                && IsWithinOutputDirectory(GetOutputDirectory(), job.OutputPath))
            {
                TryDeleteFile(job.OutputPath);
            }
        }

        if(expired.Count > 0)
        {
            dbContext.HazardAllowanceExportJobs.RemoveRange(expired);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task WriteCsvAsync(
        string outputPath,
        IReadOnlyList<HazardAllowanceListItemDto> rows,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, useAsync: true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await writer.WriteLineAsync("Mã nhân viên,Họ tên,Kỳ lương,Công hợp lệ,Khấu trừ đi trễ/về sớm,Công tính phụ cấp,Đơn giá/ngày,Phụ cấp độc hại,Trạng thái hưởng,Lý do loại trừ,Trạng thái khóa");
        foreach(var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(string.Join(",",
                Escape(row.EmployeeCode),
                Escape(row.EmployeeName),
                Escape($"{row.PayrollMonth:00}/{row.PayrollYear}"),
                row.QualifiedWorkdayCount.ToString(CultureInfo.InvariantCulture),
                row.LateEarlyDeductionDays.ToString(CultureInfo.InvariantCulture),
                row.PayableWorkdayCount.ToString(CultureInfo.InvariantCulture),
                row.HazardAllowancePerDay.ToString(CultureInfo.InvariantCulture),
                row.HazardAllowanceAmount.ToString(CultureInfo.InvariantCulture),
                row.IsEligibleForAllowance ? "Hưởng PC" : "Ngoại lệ",
                Escape(row.ExclusionReason),
                row.IsLocked ? "Đã khóa" : "Đang mở"));
        }
    }

    private string GetOutputDirectory() => Path.GetFullPath(options.OutputDirectory);

    private static HazardAllowanceExportJobDto Map(HazardAllowanceExportJobRow row) =>
        new(row.Id, row.Status, row.CreatedAtUtc, row.StartedAtUtc, row.CompletedAtUtc, row.FileName, row.ErrorMessage);

    internal static string Escape(string? value)
    {
        var normalized = value ?? string.Empty;
        if(normalized.Length > 0 && normalized[0] is '=' or '+' or '-' or '@')
        {
            normalized = $"'{normalized}";
        }
        return $"\"{normalized.Replace("\"", "\"\"")}\"";
    }

    private static string NormalizeRequired(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Người yêu cầu export là bắt buộc.", parameterName)
            : value.Trim();

    private static DateTime ToDatabaseTimestamp(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private static bool IsWithinOutputDirectory(string root, string candidate)
    {
        var normalizedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedCandidate = Path.GetFullPath(candidate);
        return normalizedCandidate.StartsWith(
            normalizedRoot + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDeleteFile(string outputPath)
    {
        try
        {
            if(File.Exists(outputPath)) File.Delete(outputPath);
        }
        catch(IOException)
        {
            // A currently downloading file is retried by the next cleanup interval.
        }
    }
}
