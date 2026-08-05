using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Domain;
using Vnta.AttendanceGateway.Protocol.Parsers;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Vnta.AttendanceGateway.Integration;

public sealed class AttendanceLogSyncService
{
    private const string UnassignedDepartmentName = "Phòng ban chưa đặt tên";
    private const string UnassignedDepartmentCode = "AUTO-UNASSIGNED-DEPARTMENT";
    private const string UnassignedPositionName = "Chưa xác định chức vụ";
    private const string UnassignedPositionCode = "AUTO-UNASSIGNED-POSITION";
    private const string SourceLabel = "ATTLOG";
    private readonly AttendanceGatewayEmployeeIdentityResolver _employeeIdentityResolver;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttendanceLogSyncService> _logger;

    private sealed record PendingAttendanceInsert(
        ZktecoAttendanceLog AttendanceLog,
        ZktecoOutboundAttendanceLog OutboundAttendanceLog);

    public AttendanceLogSyncService(
        IServiceScopeFactory scopeFactory,
        AttendanceGatewayEmployeeIdentityResolver employeeIdentityResolver,
        ILogger<AttendanceLogSyncService> logger)
    {
        _employeeIdentityResolver = employeeIdentityResolver;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<AttendanceLogSyncResult> ProcessAsync(string deviceSn, string url, string rawBody, string? flowId, CancellationToken cancellationToken)
    {
        var receivedLines = AttendanceLogBodyParser.SplitLines(rawBody);
        if (receivedLines.Count == 0)
        {
            return new AttendanceLogSyncResult(0, 0);
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
        var normalizedSerial = deviceSn.Trim().ToUpperInvariant();

        var device = await dbContext.Devices
            .SingleOrDefaultAsync(x => x.SerialNumber == normalizedSerial, cancellationToken);

        if (device is null)
        {
            _logger.LogWarning("VNTA Attendance Gateway FLOW DB [{FlowId}] Could not resolve ATTLOG device in database. DeviceSn={DeviceSn}", flowId ?? "<none>", normalizedSerial);
            return new AttendanceLogSyncResult(receivedLines.Count, 0);
        }

        await EnsureAttendanceDailySummaryTableExistsAsync(dbContext, cancellationToken);

        var stamp = HeaderParser.ExtractQueryParam(url, "Stamp");
        if (!string.IsNullOrWhiteSpace(stamp))
        {
            device.AttendanceLogStamp = stamp.Trim();
            device.UpdatedAtUtc = VietnamTime.Now.DateTime;
        }

        var parsedLines = receivedLines
            .Select(AttendanceLogBodyParser.ParseLine)
            .Where(x => x is not null)
            .Cast<AttendanceLogLine>()
            .ToArray();

        if (parsedLines.Length == 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new AttendanceLogSyncResult(receivedLines.Count, 0);
        }

        var devicePins = parsedLines
            .Select(x => x.Pin.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var normalizedPins = devicePins
            .Select(_employeeIdentityResolver.NormalizePinToEmployeeCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var employees = await dbContext.Employees
            .Where(x => normalizedPins.Contains(x.EmployeeCode))
            .ToListAsync(cancellationToken);

        var employeeCodeLookup = employees
            .ToDictionary(x => x.EmployeeCode.Trim().ToUpperInvariant(), x => x, StringComparer.OrdinalIgnoreCase);

        var resolvedEmployeeCodes = normalizedPins
            .Where(x => employeeCodeLookup.ContainsKey(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingEmployeeCodes = normalizedPins
            .Where(x => !employeeCodeLookup.ContainsKey(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missingEmployeeCodes.Length > 0)
        {
            await _employeeIdentityResolver.EnsureEmployeesForCodesAsync(
                dbContext,
                employeeCodeLookup,
                missingEmployeeCodes,
                SourceLabel,
                VietnamTime.Now.DateTime,
                cancellationToken);
        }

        var now = VietnamTime.Now.DateTime;
        var pendingInserts = new List<PendingAttendanceInsert>();
        var candidateKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parsedLine in parsedLines)
        {
            var resolvedEmployeeCode = _employeeIdentityResolver.NormalizePinToEmployeeCode(parsedLine.Pin);
            if (!employeeCodeLookup.ContainsKey(resolvedEmployeeCode))
            {
                _logger.LogWarning("Skipping ATTLOG line because employee code could not be resolved and VNTA Attendance Gateway auto-create is disabled. DeviceSn={DeviceSn}, PIN={Pin}, RawLine={RawLine}",
                    normalizedSerial, parsedLine.Pin, parsedLine.RawLine);
                continue;
            }

            var employee = employeeCodeLookup[resolvedEmployeeCode];

            var dedupKey = BuildDedupKey(device.Id, resolvedEmployeeCode, parsedLine);
            if (!candidateKeys.Add(dedupKey))
            {
                _logger.LogInformation("Skipping duplicate ATTLOG line inside the same payload. DeviceSn={DeviceSn}, PIN={Pin}, DedupKey={DedupKey}",
                    normalizedSerial, parsedLine.Pin, dedupKey);
                continue;
            }

            var attendanceLog = new ZktecoAttendanceLog
            {
                Id = Guid.CreateVersion7(),
                DeviceId = device.Id,
                EmployeeId = employee.Id,
                DeviceCode = device.Code,
                AttTime = ToVietnamLocalTimestamp(parsedLine.AttTime),
                Status = parsedLine.Status,
                Verify = parsedLine.Verify,
                WorkCode = parsedLine.WorkCode,
                Reserved1 = parsedLine.Reserved1,
                Reserved2 = parsedLine.Reserved2,
                MaskFlag = parsedLine.MaskFlag,
                Temperature = parsedLine.Temperature,
                DedupKey = dedupKey,
                UpdateTime = now,
                CreatedAtUtc = now
            };

            pendingInserts.Add(new PendingAttendanceInsert(
                attendanceLog,
                new ZktecoOutboundAttendanceLog
                {
                    Id = Guid.NewGuid(),
                    AttendanceLogId = attendanceLog.Id,
                    DeviceSn = normalizedSerial,
                    EmployeeCode = resolvedEmployeeCode,
                    TapTime = ToVietnamLocalTimestamp(parsedLine.AttTime),
                    VerificationMode = ParseIntOrZero(parsedLine.Verify),
                    InOutMode = ParseIntOrZero(parsedLine.Status),
                    AttemptCount = 0,
                    Status = OutboundDeliveryStatuses.Pending,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                    NextAttemptAtUtc = DateTimeOffset.UtcNow
                }));
        }

        if (pendingInserts.Count == 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new AttendanceLogSyncResult(receivedLines.Count, 0);
        }

        var existingKeys = await dbContext.AttendanceLogs
            .AsNoTracking()
            .Where(x => candidateKeys.Contains(x.DedupKey))
            .Select(x => x.DedupKey)
            .ToListAsync(cancellationToken);

        if (existingKeys.Count > 0)
        {
            var existingKeySet = existingKeys.ToHashSet(StringComparer.Ordinal);
            pendingInserts = pendingInserts
                .Where(x => !existingKeySet.Contains(x.AttendanceLog.DedupKey))
                .ToList();
        }

        if (pendingInserts.Count > 0)
        {
            var itemsToInsert = pendingInserts.Select(x => x.AttendanceLog).ToList();
            dbContext.AttendanceLogs.AddRange(itemsToInsert);
            await UpsertAttendanceDailySummariesAsync(dbContext, itemsToInsert, now, cancellationToken);
            dbContext.OutboundAttendanceLogs.AddRange(pendingInserts.Select(x => x.OutboundAttendanceLog));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("VNTA Attendance Gateway FLOW DB [{FlowId}] Processed ATTLOG payload. DeviceSn={DeviceSn}, ReceivedLines={ReceivedLines}, SavedLines={SavedLines}, Stamp={Stamp}",
            flowId ?? "<none>", normalizedSerial, receivedLines.Count, pendingInserts.Count, string.IsNullOrWhiteSpace(stamp) ? "<empty>" : stamp);

        return new AttendanceLogSyncResult(receivedLines.Count, pendingInserts.Count);
    }

    private static string BuildDedupKey(Guid deviceId, string normalizedEmployeeCode, AttendanceLogLine parsedLine)
    {
        var attTimeTicks = ToVietnamLocalTimestamp(parsedLine.AttTime).Ticks;
        var canonicalValue = string.Join("|",
            deviceId.ToString("N"),
            normalizedEmployeeCode,
            attTimeTicks.ToString(),
            NormalizeForDedup(parsedLine.Status),
            NormalizeForDedup(parsedLine.Verify),
            NormalizeForDedup(parsedLine.WorkCode));

        var bytes = Encoding.UTF8.GetBytes(canonicalValue);
        var hashBytes = MD5.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }

    // Thiết bị gửi thời gian không kèm offset. Quy ước hiểu theo múi giờ máy chủ rồi chuẩn hóa sang UTC để lưu timestamptz.
    private static DateTime ToVietnamLocalTimestamp(DateTime value)
    {
        return VietnamTime.ToVietnamLocalTimestamp(value);
    }

    private static async Task EnsureAttendanceDailySummaryTableExistsAsync(ZktecoDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS attendance_daily_summaries
            (
                "Id" uuid NOT NULL,
                "EmployeeId" uuid NULL,
                "WorkDate" date NOT NULL,
                "PunchCount" integer NOT NULL,
                "PunchMomentsText" text NOT NULL,
                "FirstPunchTime" timestamp without time zone NULL,
                "LastPunchTime" timestamp without time zone NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL,
                CONSTRAINT "PK_attendance_daily_summaries" PRIMARY KEY ("Id")
            );
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE attendance_daily_summaries
            ADD COLUMN IF NOT EXISTS "EmployeeId" uuid NULL;
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE attendance_daily_summaries
            DROP COLUMN IF EXISTS "EmployeeCode";
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_attendance_daily_summaries_EmployeeId_WorkDate"
            ON attendance_daily_summaries ("EmployeeId", "WorkDate");
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_attendance_daily_summaries_EmployeeId"
            ON attendance_daily_summaries ("EmployeeId");
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS "IX_attendance_daily_summaries_WorkDate"
            ON attendance_daily_summaries ("WorkDate");
            """,
            cancellationToken);

        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(
            dbContext,
            "attendance_daily_summaries",
            "FK_attendance_daily_summaries_employees_EmployeeId",
            cancellationToken);

        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(
            dbContext,
            "attendance_logs",
            "FK_attendance_logs_employees_EmployeeId",
            cancellationToken);
    }

    private static async Task UpsertAttendanceDailySummariesAsync(
        ZktecoDbContext dbContext,
        IReadOnlyCollection<ZktecoAttendanceLog> itemsToInsert,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var groupedItems = itemsToInsert
            .Where(x => x.AttTime.HasValue && x.EmployeeId.HasValue)
            .GroupBy(x => new
            {
                EmployeeId = x.EmployeeId,
                WorkDate = DateOnly.FromDateTime(x.AttTime!.Value),
            })
            .ToArray();

        if (groupedItems.Length == 0)
        {
            return;
        }

        var employeeIds = groupedItems.Where(x => x.Key.EmployeeId.HasValue).Select(x => x.Key.EmployeeId!.Value).Distinct().ToArray();
        var workDates = groupedItems.Select(x => x.Key.WorkDate).Distinct().ToArray();
        var minWorkDate = workDates.Min();
        var maxWorkDate = workDates.Max();

        var existingSummaries = await dbContext.AttendanceDailySummaries
            .Where(x => x.EmployeeId.HasValue && employeeIds.Contains(x.EmployeeId.Value) && x.WorkDate >= minWorkDate && x.WorkDate <= maxWorkDate)
            .ToListAsync(cancellationToken);

        var summaryLookup = existingSummaries.ToDictionary(
            x => BuildSummaryKey(x.EmployeeId, x.WorkDate),
            x => x,
            StringComparer.OrdinalIgnoreCase);

        foreach (var group in groupedItems)
        {
            var summaryKey = BuildSummaryKey(group.Key.EmployeeId, group.Key.WorkDate);
            if (!summaryLookup.TryGetValue(summaryKey, out var summary))
            {
                summary = new ZktecoAttendanceDailySummary
                {
                    Id = Guid.CreateVersion7(),
                    EmployeeId = group.Key.EmployeeId,
                    WorkDate = group.Key.WorkDate,
                    CreatedAtUtc = now,
                };

                summaryLookup[summaryKey] = summary;
                dbContext.AttendanceDailySummaries.Add(summary);
            }

            var mergedMoments = ParsePunchMoments(summary.PunchMomentsText);
            foreach (var item in group)
            {
                if (!item.AttTime.HasValue)
                {
                    continue;
                }

                mergedMoments.Add(FormatPunchMoment(item.AttTime.Value));
            }

            mergedMoments.Sort(StringComparer.Ordinal);

            var groupFirstPunch = group.Min(x => x.AttTime);
            var groupLastPunch = group.Max(x => x.AttTime);

            summary.PunchMomentsText = string.Join("|", mergedMoments);
            summary.PunchCount = mergedMoments.Count;
            summary.FirstPunchTime = MinDateTime(summary.FirstPunchTime, groupFirstPunch);
            summary.LastPunchTime = MaxDateTime(summary.LastPunchTime, groupLastPunch);
            summary.UpdatedAtUtc = now;
        }
    }

    private static List<string> ParsePunchMoments(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string FormatPunchMoment(DateTime value)
        => value.ToString("HH:mm:ss");

    private static string BuildSummaryKey(Guid? employeeId, DateOnly workDate)
        => $"{employeeId?.ToString("D") ?? "null"}|{workDate:yyyy-MM-dd}";

    private static DateTime? MinDateTime(DateTime? current, DateTime? candidate)
    {
        if (!candidate.HasValue)
        {
            return current;
        }

        if (!current.HasValue || candidate.Value < current.Value)
        {
            return candidate.Value;
        }

        return current;
    }

    private static DateTime? MaxDateTime(DateTime? current, DateTime? candidate)
    {
        if (!candidate.HasValue)
        {
            return current;
        }

        if (!current.HasValue || candidate.Value > current.Value)
        {
            return candidate.Value;
        }

        return current;
    }

    private static string NormalizeForDedup(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private static int ParseIntOrZero(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : 0;
    }








}

public sealed record AttendanceLogSyncResult(int ReceivedLineCount, int SavedLineCount);


