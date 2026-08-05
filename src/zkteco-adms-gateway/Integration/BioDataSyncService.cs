using System.Text;
using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Domain;
using Vnta.AttendanceGateway.Protocol.Parsers;
using Microsoft.EntityFrameworkCore;

namespace Vnta.AttendanceGateway.Integration;

public sealed class BioDataSyncService
{
    private const string UnassignedDepartmentName = "Phòng ban chưa đặt tên";
    private const string UnassignedDepartmentCode = "AUTO-UNASSIGNED-DEPARTMENT";
    private const string UnassignedPositionName = "Chưa xác định chức vụ";
    private const string UnassignedPositionCode = "AUTO-UNASSIGNED-POSITION";
    private const string SourceLabel = "BIODATA";
    private static volatile bool _bioDataTablesEnsured;
    private readonly AttendanceGatewayEmployeeIdentityResolver _employeeIdentityResolver;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BioDataSyncService> _logger;

    public BioDataSyncService(
        IServiceScopeFactory scopeFactory,
        AttendanceGatewayEmployeeIdentityResolver employeeIdentityResolver,
        ILogger<BioDataSyncService> logger)
    {
        _employeeIdentityResolver = employeeIdentityResolver;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<BioDataSyncResult> ProcessAsync(
        string deviceSn,
        string url,
        string rawBody,
        string? flowId,
        CancellationToken cancellationToken)
    {
        var receivedLines = AttendanceLogBodyParser.SplitLines(rawBody);
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZktecoDbContext>();
        var normalizedSerial = deviceSn.Trim().ToUpperInvariant();

        var device = await dbContext.Devices
            .SingleOrDefaultAsync(x => x.SerialNumber == normalizedSerial, cancellationToken);

        if (device is null)
        {
            _logger.LogWarning("VNTA Attendance Gateway FLOW DB [{FlowId}] Could not resolve BIODATA device in database. DeviceSn={DeviceSn}", flowId ?? "<none>", normalizedSerial);
            return new BioDataSyncResult(receivedLines.Count, 0, false, null);
        }

        var stamp = HeaderParser.ExtractQueryParam(url, "Stamp");
        if (!string.IsNullOrWhiteSpace(stamp))
        {
            device.OperationLogStamp = stamp.Trim();
            device.UpdatedAtUtc = DateTime.UtcNow;
        }

        await EnsureBioDataTablesExistAsync(dbContext, cancellationToken);

        var employeeLookup = new Dictionary<string, ZktecoEmployee>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var savedLines = 0;

        foreach (var rawLine in receivedLines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            if (!GetLineToken(rawLine).Equals("BIODATA", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "VNTA Attendance Gateway FLOW DB [{FlowId}] Skipping BIODATA line because token is not BIODATA. DeviceSn={DeviceSn}, RawLine={RawLine}",
                    flowId ?? "<none>",
                    normalizedSerial,
                    rawLine);
                continue;
            }

            var handled = await HandleBioDataLineAsync(
                dbContext,
                employeeLookup,
                rawLine,
                normalizedSerial,
                now,
                cancellationToken);

            if (handled)
            {
                savedLines++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "VNTA Attendance Gateway FLOW DB [{FlowId}] Processed BIODATA payload. DeviceSn={DeviceSn}, ReceivedLines={ReceivedLines}, SavedLines={SavedLines}, Stamp={Stamp}",
            flowId ?? "<none>",
            normalizedSerial,
            receivedLines.Count,
            savedLines,
            string.IsNullOrWhiteSpace(stamp) ? "<empty>" : stamp.Trim());

        return new BioDataSyncResult(receivedLines.Count, savedLines, true, string.IsNullOrWhiteSpace(stamp) ? null : stamp.Trim());
    }

    private async Task<bool> HandleBioDataLineAsync(
        ZktecoDbContext dbContext,
        IDictionary<string, ZktecoEmployee> employeeLookup,
        string rawLine,
        string deviceSn,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "BIODATA");
        var pin = GetValue(values, "PIN");
        var template = GetValue(values, "TMP");
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(template))
        {
            return false;
        }

        var employee = await _employeeIdentityResolver.EnsureEmployeeFromPinAsync(
            dbContext,
            employeeLookup,
            pin,
            null,
            SourceLabel,
            now,
            allowAutoCreate: false,
            cancellationToken);
        if (employee is null)
        {
            return false;
        }

        var bioNo = NullIfWhiteSpace(GetValue(values, "NO"));
        var bioIndex = NullIfWhiteSpace(GetValue(values, "INDEX"));
        var bioType = NullIfWhiteSpace(GetValue(values, "TYPE"));

        var existingBioData = dbContext.BioDataRecords
            .Local
            .FirstOrDefault(
                x => x.EmployeeId == employee.Id
                     && x.BioNo == bioNo
                     && x.BioIndex == bioIndex
                     && x.BioType == bioType);

        if (existingBioData is null)
        {
            existingBioData = await dbContext.BioDataRecords
                .SingleOrDefaultAsync(
                    x => x.EmployeeId == employee.Id
                         && x.BioNo == bioNo
                         && x.BioIndex == bioIndex
                         && x.BioType == bioType,
                    cancellationToken);
        }

        if (existingBioData is null)
        {
            existingBioData = new ZktecoBioData
            {
                Id = Guid.CreateVersion7(),
                EmployeeId = employee.Id,
                DeviceSn = deviceSn,
                Pin = employee.EmployeeCode,
                BioNo = bioNo,
                BioIndex = bioIndex,
                BioType = bioType,
                CreatedAtUtc = now
            };

            dbContext.BioDataRecords.Add(existingBioData);
        }
        else
        {
            existingBioData.UpdatedAtUtc = now;
        }

        existingBioData.EmployeeId = employee.Id;
        existingBioData.Pin = employee.EmployeeCode;
        existingBioData.Valid = NullIfWhiteSpace(GetValue(values, "VALID"));
        existingBioData.Duress = NullIfWhiteSpace(GetValue(values, "DURESS"));
        existingBioData.MajorVersion = NullIfWhiteSpace(GetValue(values, "MAJORVER"));
        existingBioData.MinorVersion = NullIfWhiteSpace(GetValue(values, "MINORVER"));
        existingBioData.Format = NullIfWhiteSpace(GetValue(values, "FORMAT"));
        existingBioData.TemplateData = template.Trim();

        return await ProcessByBioTypeAsync(
            dbContext,
            employee,
            deviceSn,
            existingBioData,
            now,
            cancellationToken);
    }

    private async Task<bool> ProcessByBioTypeAsync(
        ZktecoDbContext dbContext,
        ZktecoEmployee employee,
        string deviceSn,
        ZktecoBioData bioData,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(bioData.BioType, out var bioTypeInt))
        {
            return true;
        }

        return bioTypeInt switch
        {
            1 => await UpsertFingerprintFromBioDataAsync(dbContext, employee, deviceSn, bioData, now, cancellationToken),
            2 => await UpsertFaceFromBioDataAsync(dbContext, employee, deviceSn, bioData, now, cancellationToken),
            _ => true
        };
    }

    private async Task<bool> UpsertFingerprintFromBioDataAsync(
        ZktecoDbContext dbContext,
        ZktecoEmployee employee,
        string deviceSn,
        ZktecoBioData bioData,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var fid = bioData.BioIndex ?? bioData.BioNo;
        if (string.IsNullOrWhiteSpace(fid))
        {
            return false;
        }

        var existingTemplate = dbContext.FingerprintTemplates
            .Local
            .FirstOrDefault(x =>
                x.EmployeeCode == employee.EmployeeCode &&
                x.Fid == fid);

        if (existingTemplate is null)
        {
            existingTemplate = await dbContext.FingerprintTemplates
                .SingleOrDefaultAsync(x =>
                    x.EmployeeCode == employee.EmployeeCode &&
                    x.Fid == fid, cancellationToken);
        }

        if (existingTemplate is null)
        {
            existingTemplate = new ZktecoFingerprintTemplate
            {
                Id = Guid.CreateVersion7(),
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                DeviceSn = deviceSn,
                Fid = fid,
                CreatedAtUtc = now
            };

            dbContext.FingerprintTemplates.Add(existingTemplate);
        }
        else
        {
            existingTemplate.UpdatedAtUtc = now;
        }

        existingTemplate.EmployeeId = employee.Id;
        existingTemplate.EmployeeCode = employee.EmployeeCode;
        existingTemplate.Size = bioData.TemplateData.Length;
        existingTemplate.Valid = bioData.Valid;
        existingTemplate.TemplateData = bioData.TemplateData;
        existingTemplate.MajorVersion = bioData.MajorVersion;
        existingTemplate.MinorVersion = bioData.MinorVersion;
        existingTemplate.Duress = bioData.Duress;
        return true;
    }

    private async Task<bool> UpsertFaceFromBioDataAsync(
        ZktecoDbContext dbContext,
        ZktecoEmployee employee,
        string deviceSn,
        ZktecoBioData bioData,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var fid = bioData.BioIndex ?? bioData.BioNo;
        if (string.IsNullOrWhiteSpace(fid))
        {
            return false;
        }

        var existingTemplate = dbContext.FaceTemplates
            .Local
            .FirstOrDefault(x => x.EmployeeId == employee.Id && x.Fid == fid);

        if (existingTemplate is null)
        {
            existingTemplate = await dbContext.FaceTemplates
                .SingleOrDefaultAsync(x => x.EmployeeId == employee.Id && x.Fid == fid, cancellationToken);
        }

        if (existingTemplate is null)
        {
            existingTemplate = new ZktecoFaceTemplate
            {
                Id = Guid.CreateVersion7(),
                EmployeeId = employee.Id,
                DeviceSn = deviceSn,
                Fid = fid,
                CreatedAtUtc = now
            };

            dbContext.FaceTemplates.Add(existingTemplate);
        }
        else
        {
            existingTemplate.UpdatedAtUtc = now;
        }

        existingTemplate.EmployeeId = employee.Id;
        existingTemplate.Size = bioData.TemplateData.Length;
        existingTemplate.Valid = bioData.Valid;
        existingTemplate.TemplateData = bioData.TemplateData;
        existingTemplate.Version = BuildFaceVersion(bioData.MajorVersion, bioData.MinorVersion);
        return true;
    }

    private async Task EnsureBioDataTablesExistAsync(ZktecoDbContext dbContext, CancellationToken cancellationToken)
    {
        if (_bioDataTablesEnsured)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS biodata (
                "Id" uuid PRIMARY KEY,
                "EmployeeId" uuid NOT NULL,
                "DeviceSn" character varying(50) NOT NULL,
                "Pin" character varying(50) NOT NULL,
                "BioNo" character varying(20) NULL,
                "BioIndex" character varying(20) NULL,
                "Valid" character varying(20) NULL,
                "Duress" character varying(20) NULL,
                "BioType" character varying(20) NULL,
                "MajorVersion" character varying(20) NULL,
                "MinorVersion" character varying(20) NULL,
                "Format" character varying(20) NULL,
                "TemplateData" text NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL
            );
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS fingerprint_templates (
                "Id" uuid PRIMARY KEY,
                "EmployeeId" uuid NOT NULL,
                "EmployeeCode" character varying(50) NOT NULL,
                "DeviceSn" character varying(50) NOT NULL,
                "Fid" character varying(20) NOT NULL,
                "Size" integer NULL,
                "Valid" character varying(20) NULL,
                "TemplateData" text NOT NULL,
                "MajorVersion" character varying(20) NULL,
                "MinorVersion" character varying(20) NULL,
                "Duress" character varying(20) NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL
            );
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS face_templates (
                "Id" uuid PRIMARY KEY,
                "EmployeeId" uuid NOT NULL,
                "DeviceSn" character varying(50) NOT NULL,
                "Fid" character varying(20) NOT NULL,
                "Size" integer NULL,
                "Valid" character varying(20) NULL,
                "TemplateData" text NOT NULL,
                "Version" character varying(20) NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL
            );
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_biodata_EmployeeId_DeviceSn_BioNo_BioIndex_BioType";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_biodata_EmployeeCode_BioNo_BioIndex_BioType";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_biodata_EmployeeCode";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_biodata_EmployeeId_BioNo_BioIndex_BioType" ON biodata ("EmployeeId", "BioNo", "BioIndex", "BioType");""", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceIndexAsync(dbContext, "biodata", "IX_biodata_EmployeeId", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_fingerprint_templates_EmployeeId_DeviceSn_Fid";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_fingerprint_templates_EmployeeCode_Fid" ON fingerprint_templates ("EmployeeCode", "Fid");""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_fingerprint_templates_EmployeeCode" ON fingerprint_templates ("EmployeeCode");""", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceIndexAsync(dbContext, "fingerprint_templates", "IX_fingerprint_templates_EmployeeId", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_face_templates_EmployeeId_DeviceSn_Fid";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_face_templates_EmployeeCode_Fid";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_face_templates_EmployeeCode";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_face_templates_EmployeeId_Fid" ON face_templates ("EmployeeId", "Fid");""", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceIndexAsync(dbContext, "face_templates", "IX_face_templates_EmployeeId", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(dbContext, "biodata", "FK_biodata_employees_EmployeeId", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(dbContext, "fingerprint_templates", "FK_fingerprint_templates_employees_EmployeeId", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(dbContext, "face_templates", "FK_face_templates_employees_EmployeeId", cancellationToken);

        _bioDataTablesEnsured = true;
        _logger.LogInformation("Ensured PostgreSQL tables for BIODATA processing exist before ingesting payloads.");
    }











    private static Dictionary<string, string> ParseKeyValuesAfterPrefix(string rawLine, string prefix)
    {
        var payload = rawLine.Length > prefix.Length
            ? rawLine[prefix.Length..].Trim()
            : string.Empty;

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in payload.Split('\t', StringSplitOptions.RemoveEmptyEntries))
        {
            var delimiterIndex = segment.IndexOf('=');
            if (delimiterIndex <= 0)
            {
                continue;
            }

            var key = segment[..delimiterIndex].Trim().ToUpperInvariant();
            var value = segment[(delimiterIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static string GetLineToken(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return string.Empty;
        }

        var trimmed = rawLine.TrimStart();
        var tokenBuilder = new StringBuilder();
        foreach (var ch in trimmed)
        {
            if (char.IsWhiteSpace(ch))
            {
                break;
            }

            tokenBuilder.Append(char.ToUpperInvariant(ch));
        }

        return tokenBuilder.ToString();
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) ? value : null;

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? BuildFaceVersion(string? majorVersion, string? minorVersion)
    {
        if (string.IsNullOrWhiteSpace(majorVersion) && string.IsNullOrWhiteSpace(minorVersion))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(minorVersion))
        {
            return majorVersion?.Trim();
        }

        if (string.IsNullOrWhiteSpace(majorVersion))
        {
            return minorVersion?.Trim();
        }

        return $"{majorVersion.Trim()}.{minorVersion.Trim()}";
    }
}

public sealed record BioDataSyncResult(int ReceivedLineCount, int SavedLineCount, bool DeviceResolved, string? Stamp);


