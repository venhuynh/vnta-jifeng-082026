using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.QuanTri.LenhMayChamCong;

public sealed class DatabaseAdmsDeviceCommandService(ApplicationDbContext dbContext)
    : IAdmsDeviceCommandService
{
    private const int CommandTimeoutSeconds = 30;

    public Task<AdmsDeviceCommandLookupOptionsDto> GetLookupOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new AdmsDeviceCommandLookupOptionsDto(
        [
            new(AdmsDeviceCommandStatus.Pending, "Chưa gửi"),
            new(AdmsDeviceCommandStatus.Transmitted, "Đã gửi"),
            new(AdmsDeviceCommandStatus.Success, "Thành công"),
            new(AdmsDeviceCommandStatus.Error, "Lỗi")
        ]));
    }

    public async Task<IReadOnlyList<AdmsDeviceCommandSummaryDto>> SearchAsync(
        AdmsDeviceCommandFilter filter,
        CancellationToken cancellationToken = default)
    {
        var now = GetCurrentDatabaseTimestamp();
        var timeoutThreshold = now.AddSeconds(-CommandTimeoutSeconds);
        var query = dbContext.DeviceCommands.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.DeviceSn))
        {
            var deviceSn = $"%{filter.DeviceSn.Trim()}%";
            query = query.Where(x => x.DeviceSn != null && EF.Functions.ILike(x.DeviceSn, deviceSn));
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = $"%{filter.SearchTerm.Trim()}%";
            query = query.Where(x =>
                (x.DeviceSn != null && EF.Functions.ILike(x.DeviceSn, search))
                || (x.Content != null && EF.Functions.ILike(x.Content, search))
                || (x.Description != null && EF.Functions.ILike(x.Description, search))
                || (x.ReturnValue != null && EF.Functions.ILike(x.ReturnValue, search)));
        }

        if (filter.CommitFrom.HasValue)
        {
            var from = NormalizeDatabaseTimestamp(filter.CommitFrom.Value);
            query = query.Where(x => x.CommitTime.HasValue && x.CommitTime.Value >= from);
        }

        if (filter.CommitTo.HasValue)
        {
            var to = NormalizeDatabaseTimestamp(filter.CommitTo.Value);
            query = query.Where(x => x.CommitTime.HasValue && x.CommitTime.Value <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = ApplyStatusFilter(query, filter.Status.Trim(), timeoutThreshold);
        }

        var rows = await query
            .OrderByDescending(x => x.CommitTime ?? DateTime.MinValue)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(x => ToSummary(x, now)).ToArray();
    }

    public async Task<AdmsDeviceCommandDetailDto?> GetDetailAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.DeviceCommands
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return row is null ? null : ToDetail(row, GetCurrentDatabaseTimestamp());
    }

    public async Task<AdmsDeviceInfoResponseDto?> GetLatestInfoResponseAsync(
        string deviceSn,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceSn))
        {
            throw new InvalidOperationException("Số serial thiết bị là bắt buộc.");
        }

        var normalizedSerial = deviceSn.Trim().ToUpperInvariant();
        var row = await dbContext.DeviceCommands
            .AsNoTracking()
            .Where(x =>
                x.DeviceSn == normalizedSerial
                && x.Content == "INFO"
                && x.ResponseTime.HasValue)
            .OrderByDescending(x => x.ResponseTime)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.DeviceSn,
                x.ResponseTime,
                x.ReturnValue
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null || !row.ResponseTime.HasValue)
        {
            return null;
        }

        return new AdmsDeviceInfoResponseDto(
            row.Id,
            row.DeviceSn ?? normalizedSerial,
            row.ResponseTime.Value,
            AdmsDeviceInfoResponseParser.Parse(row.ReturnValue));
    }

    public async Task<AdmsDeviceCommandDetailDto> CreateAsync(
        UpsertAdmsDeviceCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var row = new AdmsDeviceCommandRow
        {
            DeviceSn = request.DeviceSn.Trim().ToUpperInvariant(),
            Content = request.Content.Trim(),
            CommitTime = NormalizeDatabaseTimestamp(request.CommitTime ?? GetCurrentDatabaseTimestamp()),
            TransTime = null,
            ResponseTime = null,
            ReturnValue = string.Empty,
            Description = NormalizeOptional(request.Description)
        };

        dbContext.DeviceCommands.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetail(row, GetCurrentDatabaseTimestamp());
    }

    public async Task<AdmsDeviceCommandDetailDto> UpdateAsync(
        int id,
        UpsertAdmsDeviceCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var row = await dbContext.DeviceCommands.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy lệnh thiết bị để cập nhật.");

        EnsureEditable(row, GetCurrentDatabaseTimestamp());

        row.DeviceSn = request.DeviceSn.Trim().ToUpperInvariant();
        row.Content = request.Content.Trim();
        row.CommitTime = NormalizeDatabaseTimestamp(request.CommitTime ?? row.CommitTime ?? GetCurrentDatabaseTimestamp());
        row.Description = NormalizeOptional(request.Description);

        // Nếu người dùng sửa một lệnh chưa phản hồi, trả nó về pending để gateway phát lại nội dung mới.
        row.TransTime = null;
        row.ResponseTime = null;
        row.ReturnValue = string.Empty;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDetail(row, GetCurrentDatabaseTimestamp());
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.DeviceCommands.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy lệnh thiết bị để xóa.");

        EnsureEditable(row, GetCurrentDatabaseTimestamp());
        dbContext.DeviceCommands.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE device_cmd RESTART IDENTITY",
            cancellationToken);
    }

    private static IQueryable<AdmsDeviceCommandRow> ApplyStatusFilter(
        IQueryable<AdmsDeviceCommandRow> query,
        string status,
        DateTime timeoutThreshold)
    {
        return status.ToLowerInvariant() switch
        {
            AdmsDeviceCommandStatus.Pending => query.Where(x => x.TransTime == null && x.ResponseTime == null),
            AdmsDeviceCommandStatus.Transmitted => query.Where(x => x.TransTime != null && x.ResponseTime == null),
            AdmsDeviceCommandStatus.Success => query.Where(x =>
                x.ResponseTime != null
                && x.ReturnValue != null
                && EF.Functions.ILike(x.ReturnValue, "%Return=0%")),
            AdmsDeviceCommandStatus.Error => query.Where(x =>
                x.ResponseTime != null
                && (x.ReturnValue == null || !EF.Functions.ILike(x.ReturnValue, "%Return=0%"))),
            AdmsDeviceCommandStatus.Responded => query.Where(x => x.ResponseTime != null),
            AdmsDeviceCommandStatus.NoResponse => query.Where(x => x.TransTime != null && x.ResponseTime == null),
            AdmsDeviceCommandStatus.Cancelled => query.Where(_ => false),
            _ => query
        };
    }

    private static AdmsDeviceCommandSummaryDto ToSummary(AdmsDeviceCommandRow row, DateTime now)
    {
        var isTimedOut = IsTimedOut(row, now);
        var status = ResolveStatus(row, isTimedOut);

        return new AdmsDeviceCommandSummaryDto(
            row.Id,
            row.DeviceSn ?? string.Empty,
            row.Content ?? string.Empty,
            row.CommitTime,
            row.TransTime,
            row.ResponseTime,
            status,
            AdmsDeviceCommandStatus.ToDisplayText(status),
            row.ReturnValue ?? string.Empty,
            row.Description ?? string.Empty,
            isTimedOut);
    }

    private static AdmsDeviceCommandDetailDto ToDetail(AdmsDeviceCommandRow row, DateTime now)
    {
        var isTimedOut = IsTimedOut(row, now);
        var status = ResolveStatus(row, isTimedOut);

        return new AdmsDeviceCommandDetailDto(
            row.Id,
            row.DeviceSn ?? string.Empty,
            row.Content ?? string.Empty,
            row.CommitTime,
            row.TransTime,
            row.ResponseTime,
            status,
            AdmsDeviceCommandStatus.ToDisplayText(status),
            row.ReturnValue ?? string.Empty,
            row.Description ?? string.Empty,
            isTimedOut);
    }

    private static void Validate(UpsertAdmsDeviceCommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceSn))
        {
            throw new InvalidOperationException("Số serial thiết bị là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new InvalidOperationException("Nội dung lệnh là bắt buộc.");
        }
    }

    private static void EnsureEditable(AdmsDeviceCommandRow row, DateTime now)
    {
        var status = ResolveStatus(row, IsTimedOut(row, now));
        if (row.ResponseTime.HasValue
            || string.Equals(status, AdmsDeviceCommandStatus.Responded, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Lệnh đã phản hồi nên không thể thay đổi.");
        }
    }

    private static string ResolveStatus(AdmsDeviceCommandRow row, bool isTimedOut)
    {
        if (!row.TransTime.HasValue)
        {
            return AdmsDeviceCommandStatus.Pending;
        }

        if (!row.ResponseTime.HasValue)
        {
            return AdmsDeviceCommandStatus.Transmitted;
        }

        if (HasSuccessfulReturnValue(row.ReturnValue))
        {
            return AdmsDeviceCommandStatus.Success;
        }

        return AdmsDeviceCommandStatus.Error;
    }

    private static bool IsTimedOut(AdmsDeviceCommandRow row, DateTime now)
    {
        return row.TransTime.HasValue
            && !row.ResponseTime.HasValue
            && now.Subtract(row.TransTime.Value).TotalSeconds >= CommandTimeoutSeconds;
    }

    private static bool HasSuccessfulReturnValue(string? returnValue)
    {
        if (string.IsNullOrWhiteSpace(returnValue))
        {
            return false;
        }

        var firstLine = returnValue
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return false;
        }

        foreach (var segment in firstLine.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!segment.StartsWith("Return=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = segment["Return=".Length..].Trim();
            return string.Equals(value, "0", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime NormalizeDatabaseTimestamp(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }

    private static DateTime GetCurrentDatabaseTimestamp()
    {
        return DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified);
    }
}
