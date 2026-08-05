using Vnta.AttendanceGateway.Data;
using Vnta.AttendanceGateway.Domain;
using Vnta.AttendanceGateway.Protocol.Parsers;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Vnta.AttendanceGateway.Integration;

public sealed class OperationalLogSyncService
{
    private const string SourceLabel = "OPERLOG";
    private const string OpLogPrefix = "OPLOG ";
    private const string UnassignedDepartmentName = "Phòng ban chưa đặt tên";
    private const string UnassignedDepartmentCode = "AUTO-UNASSIGNED-DEPARTMENT";
    private const string UnassignedPositionName = "Chưa xác định chức vụ";
    private const string UnassignedPositionCode = "AUTO-UNASSIGNED-POSITION";
    private static volatile bool _operationalTablesEnsured;
    private readonly AttendanceGatewayEmployeeIdentityResolver _employeeIdentityResolver;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperationalLogSyncService> _logger;

    public OperationalLogSyncService(
        IServiceScopeFactory scopeFactory,
        AttendanceGatewayEmployeeIdentityResolver employeeIdentityResolver,
        ILogger<OperationalLogSyncService> logger)
    {
        _employeeIdentityResolver = employeeIdentityResolver;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<OperationalLogSyncResult> ProcessAsync(
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

        var device = await LoadLatestDeviceAsync(dbContext, normalizedSerial, flowId, cancellationToken);

        if (device is null)
        {
            _logger.LogWarning("VNTA Attendance Gateway FLOW DB [{FlowId}] Could not resolve OPERLOG device in database. DeviceSn={DeviceSn}", flowId ?? "<none>", normalizedSerial);
            return new OperationalLogSyncResult(receivedLines.Count, 0, false, null, []);
        }

        var stamp = HeaderParser.ExtractQueryParam(url, "Stamp");
        if (!string.IsNullOrWhiteSpace(stamp))
        {
            device.OperationLogStamp = stamp.Trim();
            device.UpdatedAtUtc = VietnamTime.Now.DateTime;
        }

        await EnsureOperationalTablesExistAsync(dbContext, cancellationToken);

        var firstLineToken = receivedLines.Count > 0
            ? GetLineToken(receivedLines[0])
            : string.Empty;

        var savedLines = 0;
        var employeeLookup = new Dictionary<string, ZktecoEmployee>(StringComparer.OrdinalIgnoreCase);
        var semanticActivities = new List<OperationalLogSemanticActivity>();
        var now = VietnamTime.Now.DateTime;

        foreach (var rawLine in receivedLines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var token = GetLineToken(rawLine);
            var handled = token switch
            {
                "OPLOG" => HandleOpLogLine(dbContext, rawLine, normalizedSerial),
                "USER" => await HandleUserLineAsync(dbContext, employeeLookup, rawLine, normalizedSerial, now, cancellationToken),
                "FP" => await HandleFingerprintLineAsync(dbContext, employeeLookup, rawLine, normalizedSerial, now, cancellationToken),
                "FACE" => await HandleFaceLineAsync(dbContext, employeeLookup, rawLine, normalizedSerial, now, cancellationToken),
                "BIOPHOTO" => await HandleBioPhotoLineAsync(dbContext, employeeLookup, rawLine, normalizedSerial, now, cancellationToken),
                "FVEIN" => await HandleFveinLineAsync(dbContext, employeeLookup, rawLine, normalizedSerial, now, cancellationToken),
                "USERPIC" => await HandleUserPictureLineAsync(dbContext, employeeLookup, rawLine, normalizedSerial, now, cancellationToken),
                _ => false
            };

            if (handled)
            {
                savedLines++;
            }
            else if (!string.IsNullOrWhiteSpace(token))
            {
                _logger.LogDebug(
                    "Skipping OPERLOG line because token is not yet handled or data is incomplete. DeviceSn={DeviceSn}, Token={Token}, RawLine={RawLine}",
                    normalizedSerial,
                    token,
                    rawLine);
            }

            var semanticActivity = BuildSemanticActivity(token, rawLine, handled);
            if (semanticActivity is not null)
            {
                semanticActivities.Add(semanticActivity);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "VNTA Attendance Gateway FLOW DB [{FlowId}] Processed OPERLOG payload. DeviceSn={DeviceSn}, FirstLineToken={FirstLineToken}, ReceivedLines={ReceivedLines}, SavedLines={SavedLines}, Stamp={Stamp}",
            flowId ?? "<none>",
            normalizedSerial,
            string.IsNullOrWhiteSpace(firstLineToken) ? "<empty>" : firstLineToken,
            receivedLines.Count,
            savedLines,
            string.IsNullOrWhiteSpace(stamp) ? "<empty>" : stamp.Trim());

        return new OperationalLogSyncResult(
            receivedLines.Count,
            savedLines,
            true,
            string.IsNullOrWhiteSpace(stamp) ? null : stamp.Trim(),
            semanticActivities);
    }

    private bool HandleOpLogLine(ZktecoDbContext dbContext, string rawLine, string deviceSn)
    {
        var opLog = ParseOpLogLine(rawLine, deviceSn);
        if (opLog is null)
        {
            return false;
        }

        dbContext.OpLogs.Add(opLog);
        return true;
    }

    private async Task<bool> HandleUserLineAsync(
        ZktecoDbContext dbContext,
        IDictionary<string, ZktecoEmployee> employeeLookup,
        string rawLine,
        string deviceSn,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "USER");
        var pin = GetValue(values, "PIN");
        if (string.IsNullOrWhiteSpace(pin))
        {
            return false;
        }

        var fullName = GetValue(values, "NAME");
        var employee = await _employeeIdentityResolver.EnsureEmployeeFromPinAsync(
            dbContext,
            employeeLookup,
            pin,
            fullName,
            SourceLabel,
            now,
            allowAutoCreate: true,
            cancellationToken);
        if (employee is null)
        {
            return false;
        }

        var existingProfile = await LoadLatestDeviceUserProfileAsync(
            dbContext,
            employee.EmployeeCode,
            cancellationToken);

        if (existingProfile is null)
        {
            existingProfile = new ZktecoDeviceUserProfile
            {
                Id = Guid.CreateVersion7(),
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                DeviceSn = deviceSn,
                CreatedAtUtc = now
            };

            dbContext.DeviceUserProfiles.Add(existingProfile);
        }
        else
        {
            existingProfile.UpdatedAtUtc = now;
        }

        existingProfile.EmployeeId = employee.Id;
        existingProfile.EmployeeCode = employee.EmployeeCode;
        existingProfile.Password = NullIfWhiteSpace(GetValue(values, "PASSWD"));
        existingProfile.CardNumber = NullIfWhiteSpace(GetValue(values, "CARD"));
        existingProfile.FullName ??= NullIfWhiteSpace(fullName);
        existingProfile.GroupCode ??= NullIfWhiteSpace(GetValue(values, "GRP"));
        existingProfile.TimeZoneCode ??= NullIfWhiteSpace(GetValue(values, "TZ"));
        existingProfile.PrivilegeCode ??= NullIfWhiteSpace(GetValue(values, "PRI"));
        existingProfile.VerifyMode ??= NullIfWhiteSpace(GetValue(values, "VERIFY"));
        existingProfile.ViceCard ??= NullIfWhiteSpace(GetValue(values, "VICECARD"));

        return true;
    }

    private async Task<bool> HandleFingerprintLineAsync(
        ZktecoDbContext dbContext,
        IDictionary<string, ZktecoEmployee> employeeLookup,
        string rawLine,
        string deviceSn,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "FP");
        var pin = GetValue(values, "PIN");
        var fid = GetValue(values, "FID");
        var template = GetValue(values, "TMP");
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(fid) || string.IsNullOrWhiteSpace(template))
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

        var existingTemplate = await LoadLatestFingerprintTemplateAsync(
            dbContext,
            employee.EmployeeCode,
            fid,
            cancellationToken);

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
        existingTemplate.Size = TryParseInt(GetValue(values, "SIZE"));
        existingTemplate.Valid = NullIfWhiteSpace(GetValue(values, "VALID"));
        existingTemplate.TemplateData = template.Trim();
        existingTemplate.MajorVersion = template.StartsWith("oco", StringComparison.OrdinalIgnoreCase) ? "9" : "10";
        existingTemplate.MinorVersion = NullIfWhiteSpace(GetValue(values, "MINORVER"));
        existingTemplate.Duress = NullIfWhiteSpace(GetValue(values, "DURESS"));

        return true;
    }

    private async Task<bool> HandleFaceLineAsync(
        ZktecoDbContext dbContext,
        IDictionary<string, ZktecoEmployee> employeeLookup,
        string rawLine,
        string deviceSn,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "FACE");
        var pin = GetValue(values, "PIN");
        var fid = GetValue(values, "FID");
        var template = GetValue(values, "TMP");
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(fid) || string.IsNullOrWhiteSpace(template))
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

        var existingTemplate = await LoadLatestFaceTemplateAsync(
            dbContext,
            employee.Id,
            fid,
            cancellationToken);

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
        existingTemplate.Size = TryParseInt(GetValue(values, "SIZE"));
        existingTemplate.Valid = NullIfWhiteSpace(GetValue(values, "VALID"));
        existingTemplate.TemplateData = template.Trim();
        existingTemplate.Version = NullIfWhiteSpace(GetValue(values, "VER"));

        return true;
    }

    private async Task<bool> HandleBioPhotoLineAsync(
        ZktecoDbContext dbContext,
        IDictionary<string, ZktecoEmployee> employeeLookup,
        string rawLine,
        string deviceSn,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "BIOPHOTO");
        var pin = GetValue(values, "PIN");
        var fileName = GetValue(values, "FILENAME");
        var content = GetValue(values, "CONTENT");
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(content))
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

        var existingPhoto = await LoadLatestBioPhotoAsync(
            dbContext,
            employee.Id,
            cancellationToken);

        if (existingPhoto is null)
        {
            existingPhoto = new ZktecoBioPhoto
            {
                Id = Guid.CreateVersion7(),
                EmployeeId = employee.Id,
                DeviceSn = deviceSn,
                FileName = BuildCanonicalImageFileName(employee.EmployeeCode),
                CreatedAtUtc = now
            };

            dbContext.BioPhotos.Add(existingPhoto);
        }
        else
        {
            existingPhoto.UpdatedAtUtc = now;
        }

        existingPhoto.EmployeeId = employee.Id;
        existingPhoto.FileName = BuildCanonicalImageFileName(employee.EmployeeCode);
        existingPhoto.Type = NullIfWhiteSpace(GetValue(values, "TYPE"));
        existingPhoto.Size = TryParseInt(GetValue(values, "SIZE")) ?? content.Trim().Length;
        existingPhoto.Content = content.Trim();
        UpdateEmployeeAvatar(employee, existingPhoto.Content, now);

        return true;
    }

    private async Task<bool> HandleFveinLineAsync(
        ZktecoDbContext dbContext,
        IDictionary<string, ZktecoEmployee> employeeLookup,
        string rawLine,
        string deviceSn,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "FVEIN");
        var pin = GetValue(values, "PIN");
        var fid = GetValue(values, "FID");
        var index = GetValue(values, "INDEX");
        var template = GetValue(values, "TMP");
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(fid) || string.IsNullOrWhiteSpace(index) || string.IsNullOrWhiteSpace(template))
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

        var existingTemplate = await LoadLatestFveinTemplateAsync(
            dbContext,
            employee.Id,
            fid,
            index,
            cancellationToken);

        if (existingTemplate is null)
        {
            existingTemplate = new ZktecoFveinTemplate
            {
                Id = Guid.CreateVersion7(),
                EmployeeId = employee.Id,
                DeviceSn = deviceSn,
                Fid = fid.Trim(),
                Index = index.Trim(),
                CreatedAtUtc = now
            };

            dbContext.FveinTemplates.Add(existingTemplate);
        }
        else
        {
            existingTemplate.UpdatedAtUtc = now;
        }

        existingTemplate.EmployeeId = employee.Id;
        existingTemplate.Size = TryParseInt(GetValue(values, "SIZE"));
        existingTemplate.Valid = NullIfWhiteSpace(GetValue(values, "VALID"));
        existingTemplate.TemplateData = template.Trim();
        existingTemplate.Version = NullIfWhiteSpace(GetValue(values, "VER"));
        existingTemplate.Duress = NullIfWhiteSpace(GetValue(values, "DURESS"));

        return true;
    }

    private async Task<bool> HandleUserPictureLineAsync(
        ZktecoDbContext dbContext,
        IDictionary<string, ZktecoEmployee> employeeLookup,
        string rawLine,
        string deviceSn,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "USERPIC");
        var pin = GetValue(values, "PIN");
        var fileName = GetValue(values, "FILENAME");
        var content = GetValue(values, "CONTENT");
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(content))
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

        var existingPicture = await LoadLatestUserPictureAsync(
            dbContext,
            employee.Id,
            cancellationToken);

        if (existingPicture is null)
        {
            existingPicture = new ZktecoUserPicture
            {
                Id = Guid.CreateVersion7(),
                EmployeeId = employee.Id,
                DeviceSn = deviceSn,
                FileName = BuildCanonicalImageFileName(employee.EmployeeCode),
                CreatedAtUtc = now
            };

            dbContext.UserPictures.Add(existingPicture);
        }
        else
        {
            existingPicture.UpdatedAtUtc = now;
        }

        existingPicture.EmployeeId = employee.Id;
        existingPicture.FileName = BuildCanonicalImageFileName(employee.EmployeeCode);
        existingPicture.Size = TryParseInt(GetValue(values, "SIZE")) ?? content.Trim().Length;
        existingPicture.Content = content.Trim();
        UpdateEmployeeAvatar(employee, existingPicture.Content, now);

        return true;
    }

    private async Task<ZktecoDevice?> LoadLatestDeviceAsync(
        ZktecoDbContext dbContext,
        string normalizedSerial,
        string? flowId,
        CancellationToken cancellationToken)
    {
        var devices = await dbContext.Devices
            .Where(x => x.SerialNumber == normalizedSerial)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (devices.Count > 1)
        {
            _logger.LogWarning(
                "VNTA Attendance Gateway FLOW DB [{FlowId}] Found duplicate devices for serial number. SerialNumber={SerialNumber}, SelectedDeviceId={SelectedDeviceId}",
                flowId ?? "<none>",
                normalizedSerial,
                devices[0].Id);
        }

        return devices.FirstOrDefault();
    }

    private async Task<ZktecoDeviceUserProfile?> LoadLatestDeviceUserProfileAsync(
        ZktecoDbContext dbContext,
        string employeeCode,
        CancellationToken cancellationToken)
    {
        var profiles = await dbContext.DeviceUserProfiles
            .Where(x => x.EmployeeCode == employeeCode)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (profiles.Count > 1)
        {
            _logger.LogWarning(
                "Found duplicate device_user_profiles rows while processing OPERLOG USER line. EmployeeCode={EmployeeCode}, SelectedProfileId={SelectedProfileId}",
                employeeCode,
                profiles[0].Id);
        }

        return profiles.FirstOrDefault();
    }

    private async Task<ZktecoFingerprintTemplate?> LoadLatestFingerprintTemplateAsync(
        ZktecoDbContext dbContext,
        string employeeCode,
        string fid,
        CancellationToken cancellationToken)
    {
        var templates = await dbContext.FingerprintTemplates
            .Where(x => x.EmployeeCode == employeeCode && x.Fid == fid)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (templates.Count > 1)
        {
            _logger.LogWarning(
                "Found duplicate fingerprint_templates rows while processing OPERLOG FP line. EmployeeCode={EmployeeCode}, Fid={Fid}, SelectedTemplateId={SelectedTemplateId}",
                employeeCode,
                fid,
                templates[0].Id);
        }

        return templates.FirstOrDefault();
    }

    private async Task<ZktecoFaceTemplate?> LoadLatestFaceTemplateAsync(
        ZktecoDbContext dbContext,
        Guid employeeId,
        string fid,
        CancellationToken cancellationToken)
    {
        var templates = await dbContext.FaceTemplates
            .Where(x => x.EmployeeId == employeeId && x.Fid == fid)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (templates.Count > 1)
        {
            _logger.LogWarning(
                "Found duplicate face_templates rows while processing OPERLOG FACE line. EmployeeId={EmployeeId}, Fid={Fid}, SelectedTemplateId={SelectedTemplateId}",
                employeeId,
                fid,
                templates[0].Id);
        }

        return templates.FirstOrDefault();
    }

    private async Task<ZktecoBioPhoto?> LoadLatestBioPhotoAsync(
        ZktecoDbContext dbContext,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var photos = await dbContext.BioPhotos
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (photos.Count > 1)
        {
            _logger.LogWarning(
                "Found duplicate bio_photos rows while processing OPERLOG BIOPHOTO line. EmployeeId={EmployeeId}, SelectedPhotoId={SelectedPhotoId}",
                employeeId,
                photos[0].Id);
        }

        return photos.FirstOrDefault();
    }

    private async Task<ZktecoFveinTemplate?> LoadLatestFveinTemplateAsync(
        ZktecoDbContext dbContext,
        Guid employeeId,
        string fid,
        string index,
        CancellationToken cancellationToken)
    {
        var templates = await dbContext.FveinTemplates
            .Where(x => x.EmployeeId == employeeId && x.Fid == fid && x.Index == index)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (templates.Count > 1)
        {
            _logger.LogWarning(
                "Found duplicate fvein_templates rows while processing OPERLOG FVEIN line. EmployeeId={EmployeeId}, Fid={Fid}, Index={Index}, SelectedTemplateId={SelectedTemplateId}",
                employeeId,
                fid,
                index,
                templates[0].Id);
        }

        return templates.FirstOrDefault();
    }

    private async Task<ZktecoUserPicture?> LoadLatestUserPictureAsync(
        ZktecoDbContext dbContext,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var pictures = await dbContext.UserPictures
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (pictures.Count > 1)
        {
            _logger.LogWarning(
                "Found duplicate user_pictures rows while processing OPERLOG USERPIC line. EmployeeId={EmployeeId}, SelectedPictureId={SelectedPictureId}",
                employeeId,
                pictures[0].Id);
        }

        return pictures.FirstOrDefault();
    }

    private async Task EnsureOperationalTablesExistAsync(ZktecoDbContext dbContext, CancellationToken cancellationToken)
    {
        if (_operationalTablesEnsured)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS oplog (
                "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "Operator" character varying(500) NULL,
                "OpTime" timestamp without time zone NULL,
                "OpType" character varying(500) NULL,
                "User" character varying(50) NULL,
                "Obj1" character varying(500) NULL,
                "Obj2" character varying(500) NULL,
                "Obj3" character varying(500) NULL,
                "Obj4" character varying(500) NULL,
                "DeviceId" character varying(500) NULL
            );
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS device_user_profiles (
                "Id" uuid PRIMARY KEY,
                "EmployeeId" uuid NOT NULL,
                "EmployeeCode" character varying(50) NOT NULL,
                "DeviceSn" character varying(50) NOT NULL,
                "FullName" character varying(200) NULL,
                "Password" character varying(100) NULL,
                "CardNumber" character varying(100) NULL,
                "GroupCode" character varying(50) NULL,
                "TimeZoneCode" character varying(50) NULL,
                "PrivilegeCode" character varying(20) NULL,
                "VerifyMode" character varying(20) NULL,
                "ViceCard" character varying(100) NULL,
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

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS bio_photos (
                "Id" uuid PRIMARY KEY,
                "EmployeeId" uuid NOT NULL,
                "DeviceSn" character varying(50) NOT NULL,
                "FileName" character varying(255) NOT NULL,
                "Type" character varying(50) NULL,
                "Size" integer NULL,
                "Content" text NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL
            );
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS fvein_templates (
                "Id" uuid PRIMARY KEY,
                "EmployeeId" uuid NOT NULL,
                "DeviceSn" character varying(50) NOT NULL,
                "Fid" character varying(20) NOT NULL,
                "Index" character varying(20) NOT NULL,
                "Size" integer NULL,
                "Valid" character varying(20) NULL,
                "TemplateData" text NOT NULL,
                "Version" character varying(20) NULL,
                "Duress" character varying(20) NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL
            );
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS user_pictures (
                "Id" uuid PRIMARY KEY,
                "EmployeeId" uuid NOT NULL,
                "DeviceSn" character varying(50) NOT NULL,
                "FileName" character varying(255) NOT NULL,
                "Size" integer NULL,
                "Content" text NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL
            );
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_oplog_DeviceId" ON oplog ("DeviceId");""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_oplog_OpTime" ON oplog ("OpTime");""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_device_user_profiles_EmployeeId_DeviceSn";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_device_user_profiles_EmployeeCode" ON device_user_profiles ("EmployeeCode");""", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceIndexAsync(dbContext, "device_user_profiles", "IX_device_user_profiles_EmployeeId", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_fingerprint_templates_EmployeeId_DeviceSn_Fid";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_fingerprint_templates_EmployeeCode_Fid" ON fingerprint_templates ("EmployeeCode", "Fid");""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_fingerprint_templates_EmployeeCode" ON fingerprint_templates ("EmployeeCode");""", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceIndexAsync(dbContext, "fingerprint_templates", "IX_fingerprint_templates_EmployeeId", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_face_templates_EmployeeId_DeviceSn_Fid";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_face_templates_EmployeeCode_Fid";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_face_templates_EmployeeCode";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_face_templates_EmployeeId_Fid" ON face_templates ("EmployeeId", "Fid");""", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceIndexAsync(dbContext, "face_templates", "IX_face_templates_EmployeeId", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_bio_photos_EmployeeId_DeviceSn_FileName";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_bio_photos_EmployeeCode";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_bio_photos_EmployeeId";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_bio_photos_EmployeeId" ON bio_photos ("EmployeeId");""", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceIndexAsync(dbContext, "bio_photos", "IX_bio_photos_EmployeeId", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_fvein_templates_EmployeeId_DeviceSn_Fid_Index";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_fvein_templates_EmployeeCode_Fid";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_fvein_templates_EmployeeCode";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_fvein_templates_EmployeeId_Fid_Index" ON fvein_templates ("EmployeeId", "Fid", "Index");""", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceIndexAsync(dbContext, "fvein_templates", "IX_fvein_templates_EmployeeId", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_user_pictures_EmployeeId_DeviceSn_FileName";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_user_pictures_EmployeeCode";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""DROP INDEX IF EXISTS "IX_user_pictures_EmployeeId";""", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_pictures_EmployeeId" ON user_pictures ("EmployeeId");""", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceIndexAsync(dbContext, "user_pictures", "IX_user_pictures_EmployeeId", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(dbContext, "device_user_profiles", "FK_device_user_profiles_employees_EmployeeId", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(dbContext, "fingerprint_templates", "FK_fingerprint_templates_employees_EmployeeId", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(dbContext, "face_templates", "FK_face_templates_employees_EmployeeId", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(dbContext, "bio_photos", "FK_bio_photos_employees_EmployeeId", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(dbContext, "fvein_templates", "FK_fvein_templates_employees_EmployeeId", cancellationToken);
        await ZktecoSchemaGuard.EnsureEmployeeReferenceConstraintAsync(dbContext, "user_pictures", "FK_user_pictures_employees_EmployeeId", cancellationToken);

        _operationalTablesEnsured = true;
        _logger.LogInformation("Ensured PostgreSQL tables for OPERLOG processing exist before ingesting payloads.");
    }











    private ZktecoOpLog? ParseOpLogLine(string rawLine, string deviceSn)
    {
        if (string.IsNullOrWhiteSpace(rawLine) || !rawLine.StartsWith(OpLogPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var payload = rawLine[OpLogPrefix.Length..].Trim();
        var parts = payload
            .Split('\t')
            .Select(x => x.Trim())
            .ToArray();

        if (parts.Length < 7)
        {
            _logger.LogWarning(
                "Skipping OPERLOG OPLOG line because it does not contain enough fields. DeviceSn={DeviceSn}, RawLine={RawLine}",
                deviceSn,
                rawLine);
            return null;
        }

        if (!DateTime.TryParse(parts[2], out var opTime))
        {
            _logger.LogWarning(
                "Skipping OPERLOG OPLOG line because OpTime could not be parsed. DeviceSn={DeviceSn}, RawLine={RawLine}",
                deviceSn,
                rawLine);
            return null;
        }

        opTime = DateTime.SpecifyKind(opTime, DateTimeKind.Unspecified);

        return new ZktecoOpLog
        {
            Operator = NullIfWhiteSpace(parts[1]),
            OpTime = VietnamTime.ToVietnamLocalTimestamp(opTime),
            OpType = NullIfWhiteSpace(parts[0]),
            User = "0",
            Obj1 = NullIfWhiteSpace(parts[3]),
            Obj2 = NullIfWhiteSpace(parts[4]),
            Obj3 = NullIfWhiteSpace(parts[5]),
            Obj4 = NullIfWhiteSpace(parts[6]),
            DeviceId = deviceSn
        };
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

    private static string BuildCanonicalImageFileName(string employeeCode)
        => $"{employeeCode.Trim()}.jpg";

    private static void UpdateEmployeeAvatar(ZktecoEmployee employee, string avatar, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(avatar))
        {
            return;
        }

        var normalizedAvatar = avatar.Trim();
        if (string.Equals(employee.Avatar, normalizedAvatar, StringComparison.Ordinal))
        {
            return;
        }

        employee.Avatar = normalizedAvatar;
        employee.UpdatedAtUtc = now;
    }

    private static int? TryParseInt(string? value)
        => int.TryParse(value, out var parsedValue) ? parsedValue : null;

    private static OperationalLogSemanticActivity? BuildSemanticActivity(
        string token,
        string rawLine,
        bool handled) =>
        token switch
        {
            "OPLOG" => BuildOpLogSemanticActivity(rawLine, handled),
            "USER" => BuildUserSemanticActivity(rawLine, handled),
            "FP" => BuildFingerprintSemanticActivity(rawLine, handled),
            "FACE" => BuildFaceSemanticActivity(rawLine, handled),
            "BIOPHOTO" => BuildBioPhotoSemanticActivity(rawLine, handled),
            "FVEIN" => BuildFveinSemanticActivity(rawLine, handled),
            "USERPIC" => BuildUserPictureSemanticActivity(rawLine, handled),
            _ when !string.IsNullOrWhiteSpace(token) => new OperationalLogSemanticActivity(
                "operational-log-unmapped-token",
                "received",
                $"Đã nhận dòng OPERLOG token `{token}` nhưng gateway chưa diễn giải ngữ nghĩa riêng.",
                rawLine),
            _ => null
        };

    private static OperationalLogSemanticActivity BuildUserSemanticActivity(string rawLine, bool handled)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "USER");
        var pin = GetValue(values, "PIN") ?? "<trống>";
        var fullName = NullIfWhiteSpace(GetValue(values, "NAME"));
        var cardNumber = NullIfWhiteSpace(GetValue(values, "CARD"));
        var privilege = NullIfWhiteSpace(GetValue(values, "PRI"));
        var verifyMode = NullIfWhiteSpace(GetValue(values, "VERIFY"));

        var summary = handled
            ? $"Đã đồng bộ hồ sơ người dùng {FormatPinLabel(pin, fullName)}.{AppendDetail("Thẻ", cardNumber)}{AppendDetail("Quyền", privilege)}{AppendDetail("Xác thực", verifyMode)}"
            : $"Nhận dòng OPERLOG USER cho PIN={pin} nhưng thiếu dữ liệu bắt buộc nên chưa xử lý.";

        return new OperationalLogSemanticActivity(
            "operational-log-user-profile-upserted",
            handled ? "processed" : "ignored",
            summary,
            rawLine);
    }

    private static OperationalLogSemanticActivity BuildFingerprintSemanticActivity(string rawLine, bool handled)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "FP");
        var pin = GetValue(values, "PIN") ?? "<trống>";
        var fid = GetValue(values, "FID") ?? "<trống>";
        var size = NullIfWhiteSpace(GetValue(values, "SIZE"));
        var valid = NullIfWhiteSpace(GetValue(values, "VALID"));
        var duress = NullIfWhiteSpace(GetValue(values, "DURESS"));

        var summary = handled
            ? $"Đã đồng bộ mẫu vân tay cho PIN={pin}, FID={fid}.{AppendDetail("Size", size)}{AppendDetail("Valid", valid)}{AppendDetail("Duress", duress)}"
            : $"Nhận dòng OPERLOG FP cho PIN={pin}, FID={fid} nhưng chưa đủ dữ liệu để lưu mẫu vân tay.";

        return new OperationalLogSemanticActivity(
            "operational-log-fingerprint-template-upserted",
            handled ? "processed" : "ignored",
            summary,
            rawLine);
    }

    private static OperationalLogSemanticActivity BuildFaceSemanticActivity(string rawLine, bool handled)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "FACE");
        var pin = GetValue(values, "PIN") ?? "<trống>";
        var fid = GetValue(values, "FID") ?? "<trống>";
        var size = NullIfWhiteSpace(GetValue(values, "SIZE"));
        var valid = NullIfWhiteSpace(GetValue(values, "VALID"));
        var version = NullIfWhiteSpace(GetValue(values, "VER"));

        var summary = handled
            ? $"Đã đồng bộ mẫu khuôn mặt cho PIN={pin}, FID={fid}.{AppendDetail("Size", size)}{AppendDetail("Valid", valid)}{AppendDetail("Version", version)}"
            : $"Nhận dòng OPERLOG FACE cho PIN={pin}, FID={fid} nhưng chưa đủ dữ liệu để lưu mẫu khuôn mặt.";

        return new OperationalLogSemanticActivity(
            "operational-log-face-template-upserted",
            handled ? "processed" : "ignored",
            summary,
            rawLine);
    }

    private static OperationalLogSemanticActivity BuildBioPhotoSemanticActivity(string rawLine, bool handled)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "BIOPHOTO");
        var pin = GetValue(values, "PIN") ?? "<trống>";
        var fileName = NullIfWhiteSpace(GetValue(values, "FILENAME"));
        var size = NullIfWhiteSpace(GetValue(values, "SIZE"));
        var type = NullIfWhiteSpace(GetValue(values, "TYPE"));

        var summary = handled
            ? $"Đã đồng bộ ảnh sinh trắc học cho PIN={pin}.{AppendDetail("File", fileName)}{AppendDetail("Type", type)}{AppendDetail("Size", size)}"
            : $"Nhận dòng OPERLOG BIOPHOTO cho PIN={pin} nhưng thiếu nội dung ảnh nên chưa xử lý.";

        return new OperationalLogSemanticActivity(
            "operational-log-biophoto-upserted",
            handled ? "processed" : "ignored",
            summary,
            rawLine);
    }

    private static OperationalLogSemanticActivity BuildFveinSemanticActivity(string rawLine, bool handled)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "FVEIN");
        var pin = GetValue(values, "PIN") ?? "<trống>";
        var fid = GetValue(values, "FID") ?? "<trống>";
        var index = GetValue(values, "INDEX") ?? "<trống>";
        var size = NullIfWhiteSpace(GetValue(values, "SIZE"));
        var version = NullIfWhiteSpace(GetValue(values, "VER"));

        var summary = handled
            ? $"Đã đồng bộ mẫu tĩnh mạch ngón tay cho PIN={pin}, FID={fid}, INDEX={index}.{AppendDetail("Size", size)}{AppendDetail("Version", version)}"
            : $"Nhận dòng OPERLOG FVEIN cho PIN={pin}, FID={fid}, INDEX={index} nhưng chưa đủ dữ liệu để lưu mẫu tĩnh mạch.";

        return new OperationalLogSemanticActivity(
            "operational-log-fvein-template-upserted",
            handled ? "processed" : "ignored",
            summary,
            rawLine);
    }

    private static OperationalLogSemanticActivity BuildUserPictureSemanticActivity(string rawLine, bool handled)
    {
        var values = ParseKeyValuesAfterPrefix(rawLine, "USERPIC");
        var pin = GetValue(values, "PIN") ?? "<trống>";
        var fileName = NullIfWhiteSpace(GetValue(values, "FILENAME"));
        var size = NullIfWhiteSpace(GetValue(values, "SIZE"));

        var summary = handled
            ? $"Đã đồng bộ ảnh hồ sơ người dùng cho PIN={pin}.{AppendDetail("File", fileName)}{AppendDetail("Size", size)}"
            : $"Nhận dòng OPERLOG USERPIC cho PIN={pin} nhưng thiếu nội dung ảnh nên chưa xử lý.";

        return new OperationalLogSemanticActivity(
            "operational-log-user-picture-upserted",
            handled ? "processed" : "ignored",
            summary,
            rawLine);
    }

    private static OperationalLogSemanticActivity BuildOpLogSemanticActivity(string rawLine, bool handled)
    {
        var summary = TryBuildOpLogSummary(rawLine, out var opType)
            ?? "Nhận dòng OPERLOG OPLOG nhưng không đủ dữ liệu để diễn giải chi tiết.";

        return new OperationalLogSemanticActivity(
            $"operational-log-oplog-{NormalizeEventSegment(opType)}",
            handled ? "processed" : "ignored",
            summary,
            rawLine);
    }

    private static string? TryBuildOpLogSummary(string rawLine, out string? opType)
    {
        opType = null;

        if (string.IsNullOrWhiteSpace(rawLine) || !rawLine.StartsWith(OpLogPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var payload = rawLine[OpLogPrefix.Length..].Trim();
        var parts = payload
            .Split('\t')
            .Select(static x => x.Trim())
            .ToArray();

        if (parts.Length < 7)
        {
            return null;
        }

        opType = NullIfWhiteSpace(parts[0]);
        var opTime = NullIfWhiteSpace(parts[2]);
        var operatorName = NullIfWhiteSpace(parts[1]);
        var obj1 = NullIfWhiteSpace(parts[3]);
        var obj2 = NullIfWhiteSpace(parts[4]);
        var obj3 = NullIfWhiteSpace(parts[5]);
        var obj4 = NullIfWhiteSpace(parts[6]);

        return $"Thiết bị báo thao tác {opType ?? "<không rõ loại>"}.{AppendDetail("Thời điểm", opTime)}{AppendDetail("Operator", operatorName)}{AppendDetail("Obj1", obj1)}{AppendDetail("Obj2", obj2)}{AppendDetail("Obj3", obj3)}{AppendDetail("Obj4", obj4)}";
    }

    private static string FormatPinLabel(string pin, string? fullName) =>
        string.IsNullOrWhiteSpace(fullName)
            ? $"PIN={pin}"
            : $"PIN={pin} ({fullName})";

    private static string AppendDetail(string label, string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : $" {label}={value}.";

    private static string NormalizeEventSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var builder = new StringBuilder();
        var lastWasSeparator = false;

        foreach (var ch in value.Trim())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                lastWasSeparator = false;
                continue;
            }

            if (lastWasSeparator)
            {
                continue;
            }

            builder.Append('-');
            lastWasSeparator = true;
        }

        var normalized = builder
            .ToString()
            .Trim('-');

        return string.IsNullOrWhiteSpace(normalized) ? "unknown" : normalized;
    }
}

public sealed record OperationalLogSyncResult(
    int ReceivedLineCount,
    int SavedLineCount,
    bool DeviceResolved,
    string? Stamp,
    IReadOnlyList<OperationalLogSemanticActivity> SemanticActivities);

public sealed record OperationalLogSemanticActivity(
    string EventType,
    string LogStatus,
    string SummaryText,
    string RawBody);


