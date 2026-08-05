using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.DangTrienKhai.DuLieuSinhTracHoc;

public sealed class DatabaseAttendanceBiometricDeviceQueueService(ApplicationDbContext dbContext)
    : IAttendanceBiometricDeviceQueueService
{
    private const string DefaultGroupCode = "1";
    private const string DefaultTimeZoneCode = "0000000100000000";
    private const string DefaultPrivilegeCode = "0";

    private static readonly string[] EmployeeIdColumns = ["employeeid", "employee_id"];
    private static readonly string[] PayloadColumns = ["templatedata", "template_data", "tmp", "template", "fingerprint", "finger_data", "content", "data", "blob"];
    private static readonly string[] BioDataTypeColumns = ["biotype", "bio_type", "type"];
    private static readonly string[] BioDataNoColumns = ["biono", "bio_no", "no"];
    private static readonly string[] BioDataIndexColumns = ["bioindex", "bio_index", "index"];
    private static readonly string[] BioDataValidColumns = ["valid"];
    private static readonly string[] BioDataDuressColumns = ["duress"];
    private static readonly string[] BioDataMajorVersionColumns = ["majorver", "major_ver"];
    private static readonly string[] BioDataMinorVersionColumns = ["minorver", "minor_ver"];
    private static readonly string[] BioDataFormatColumns = ["format"];
    private static readonly string[] UpdatedAtColumns = ["updatedatutc", "updated_at_utc", "updatedat", "updated_at"];
    private static readonly string[] CreatedAtColumns = ["createdatutc", "created_at_utc", "createdat", "created_at"];

    public async Task<AttendanceBiometricDeviceCommandBatchResult> CreatePushCommandsAsync(
        AttendanceBiometricDeviceCommandBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var employeeById = await LoadMatchedEmployeesAsync(
            request,
            "Hãy chọn ít nhất một nhân viên hợp lệ để tạo lệnh cập nhật.",
            "Không tìm thấy nhân viên hợp lệ có mã chấm công để tạo lệnh cập nhật.",
            cancellationToken);
        var deviceSerialNumbers = GetRequiredDeviceSerialNumbers(
            request,
            "Hãy chọn ít nhất một máy chấm công hợp lệ để tạo lệnh cập nhật.");

        var profiles = await dbContext.DeviceUserProfiles
            .AsNoTracking()
            .Where(profile => employeeById.Keys.Contains(profile.EmployeeId))
            .ToListAsync(cancellationToken);
        var profileByEmployeeId = profiles
            .GroupBy(static profile => profile.EmployeeId)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderByDescending(static profile => profile.UpdatedAtUtc ?? profile.CreatedAtUtc)
                    .ThenByDescending(static profile => profile.CreatedAtUtc)
                    .ThenByDescending(static profile => profile.Id)
                    .First());

        var fingerprintTemplates = await dbContext.FingerprintTemplates
            .AsNoTracking()
            .Where(template =>
                employeeById.Keys.Contains(template.EmployeeId)
                && template.TemplateData != null
                && template.TemplateData.Trim() != string.Empty)
            .ToListAsync(cancellationToken);
        var fingerprintTemplatesByEmployeeId = fingerprintTemplates
            .GroupBy(static template => template.EmployeeId)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<AttendanceFingerprintTemplateRow>)group
                    .GroupBy(template => NormalizeOptional(template.Fid) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .Select(static templateGroup => templateGroup
                        .OrderByDescending(static template => template.UpdatedAtUtc ?? template.CreatedAtUtc)
                        .ThenByDescending(static template => template.CreatedAtUtc)
                        .ThenByDescending(static template => template.Id)
                        .First())
                    .OrderBy(static template => ParseFidOrder(template.Fid))
                    .ThenBy(static template => NormalizeOptional(template.Fid), StringComparer.OrdinalIgnoreCase)
                    .ToArray());

        var userPictures = await dbContext.UserPictures
            .AsNoTracking()
            .Where(picture =>
                employeeById.Keys.Contains(picture.EmployeeId)
                && picture.Content != null
                && picture.Content.Trim() != string.Empty)
            .ToListAsync(cancellationToken);
        var userPictureByEmployeeId = userPictures
            .GroupBy(static picture => picture.EmployeeId)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderByDescending(static picture => picture.UpdatedAtUtc ?? picture.CreatedAtUtc)
                    .ThenByDescending(static picture => picture.CreatedAtUtc)
                    .ThenByDescending(static picture => picture.Id)
                    .First());

        var bioPhotos = await dbContext.BioPhotos
            .AsNoTracking()
            .Where(photo =>
                employeeById.Keys.Contains(photo.EmployeeId)
                && photo.Content != null
                && photo.Content.Trim() != string.Empty)
            .ToListAsync(cancellationToken);
        var bioPhotoByEmployeeId = bioPhotos
            .GroupBy(static photo => photo.EmployeeId)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderByDescending(static photo => photo.UpdatedAtUtc ?? photo.CreatedAtUtc)
                    .ThenByDescending(static photo => photo.CreatedAtUtc)
                    .ThenByDescending(static photo => photo.Id)
                    .First());

        var bioDataByEmployeeId = await LoadBioDataPayloadsAsync(employeeById.Keys.ToArray(), cancellationToken);
        var commitTime = NormalizeDatabaseTimestamp(DateTime.UtcNow);
        var commandRows = BuildPushCommandRows(
            employeeById,
            profileByEmployeeId,
            fingerprintTemplatesByEmployeeId,
            bioDataByEmployeeId,
            userPictureByEmployeeId,
            bioPhotoByEmployeeId,
            deviceSerialNumbers,
            commitTime);

        if (commandRows.Count == 0)
        {
            throw new InvalidOperationException("Không có dữ liệu sinh trắc học nguồn phù hợp để tạo lệnh cập nhật.");
        }

        dbContext.DeviceCommands.AddRange(commandRows);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AttendanceBiometricDeviceCommandBatchResult(
            commandRows.Count,
            employeeById.Count,
            deviceSerialNumbers,
            employeeById.Keys.ToArray());
    }

    public async Task<AttendanceBiometricDeviceCommandBatchResult> CreateDeleteCommandsAsync(
        AttendanceBiometricDeviceCommandBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var employeeById = await LoadMatchedEmployeesAsync(
            request,
            "Hãy chọn ít nhất một nhân viên hợp lệ để tạo lệnh xóa.",
            "Không tìm thấy nhân viên hợp lệ có mã chấm công để tạo lệnh xóa.",
            cancellationToken);
        var deviceSerialNumbers = GetRequiredDeviceSerialNumbers(
            request,
            "Hãy chọn ít nhất một máy chấm công hợp lệ để tạo lệnh xóa.");
        var commitTime = NormalizeDatabaseTimestamp(DateTime.UtcNow);
        var commandRows = BuildDeleteCommandRows(employeeById, deviceSerialNumbers, commitTime);

        dbContext.DeviceCommands.AddRange(commandRows);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AttendanceBiometricDeviceCommandBatchResult(
            commandRows.Count,
            employeeById.Count,
            deviceSerialNumbers,
            employeeById.Keys.ToArray());
    }

    private static List<AdmsDeviceCommandRow> BuildPushCommandRows(
        IReadOnlyDictionary<Guid, EmployeeSnapshot> employees,
        IReadOnlyDictionary<Guid, AttendanceDeviceUserProfileRow> profiles,
        IReadOnlyDictionary<Guid, IReadOnlyList<AttendanceFingerprintTemplateRow>> fingerprintTemplatesByEmployeeId,
        IReadOnlyDictionary<Guid, IReadOnlyList<BioDataPayload>> bioDataByEmployeeId,
        IReadOnlyDictionary<Guid, AttendanceUserPictureRow> userPictureByEmployeeId,
        IReadOnlyDictionary<Guid, AttendanceBioPhotoRow> bioPhotoByEmployeeId,
        IReadOnlyList<string> deviceSerialNumbers,
        DateTime commitTime)
    {
        var commandRows = new List<AdmsDeviceCommandRow>();

        foreach (var deviceSerialNumber in deviceSerialNumbers)
        {
            foreach (var employee in employees.Values.OrderBy(static employee => employee.EmployeeCode, StringComparer.OrdinalIgnoreCase))
            {
                profiles.TryGetValue(employee.Id, out var profile);
                var employeeCode = employee.EmployeeCode!;

                commandRows.Add(new AdmsDeviceCommandRow
                {
                    DeviceSn = deviceSerialNumber,
                    Content = BuildUpdateUserInfoContent(employeeCode, employee.FullName, profile),
                    CommitTime = commitTime,
                    Description = "Update user info",
                    ReturnValue = string.Empty
                });

                if (fingerprintTemplatesByEmployeeId.TryGetValue(employee.Id, out var templates))
                {
                    foreach (var template in templates)
                    {
                        var templateData = NormalizeOptional(template.TemplateData);
                        if (templateData is null)
                        {
                            continue;
                        }

                        commandRows.Add(new AdmsDeviceCommandRow
                        {
                            DeviceSn = deviceSerialNumber,
                            Content = BuildUpdateFingerprintContent(employeeCode, template, templateData),
                            CommitTime = commitTime,
                            Description = $"Update Fingerprint Pin={employeeCode}",
                            ReturnValue = string.Empty
                        });
                    }
                }

                if (bioDataByEmployeeId.TryGetValue(employee.Id, out var bioDataPayloads))
                {
                    foreach (var payload in bioDataPayloads)
                    {
                        commandRows.Add(new AdmsDeviceCommandRow
                        {
                            DeviceSn = deviceSerialNumber,
                            Content = BuildUpdateBioDataContent(employeeCode, payload),
                            CommitTime = commitTime,
                            Description = $"Update BioData Pin={employeeCode} Type={payload.Type}",
                            ReturnValue = string.Empty
                        });
                    }
                }

                if (userPictureByEmployeeId.TryGetValue(employee.Id, out var userPicture))
                {
                    var content = NormalizeOptional(userPicture.Content);
                    if (content is not null)
                    {
                        commandRows.Add(new AdmsDeviceCommandRow
                        {
                            DeviceSn = deviceSerialNumber,
                            Content = BuildUpdateUserPictureContent(employeeCode, userPicture, content),
                            CommitTime = commitTime,
                            Description = $"Update UserPic Pin={employeeCode}",
                            ReturnValue = string.Empty
                        });
                    }
                }

                if (bioPhotoByEmployeeId.TryGetValue(employee.Id, out var bioPhoto))
                {
                    var content = NormalizeOptional(bioPhoto.Content);
                    if (content is not null)
                    {
                        commandRows.Add(new AdmsDeviceCommandRow
                        {
                            DeviceSn = deviceSerialNumber,
                            Content = BuildUpdateBioPhotoContent(employeeCode, bioPhoto, content),
                            CommitTime = commitTime,
                            Description = $"Update BioPhoto Pin={employeeCode}",
                            ReturnValue = string.Empty
                        });
                    }
                }
            }
        }

        return commandRows;
    }

    private static List<AdmsDeviceCommandRow> BuildDeleteCommandRows(
        IReadOnlyDictionary<Guid, EmployeeSnapshot> employees,
        IReadOnlyList<string> deviceSerialNumbers,
        DateTime commitTime)
    {
        var commandRows = new List<AdmsDeviceCommandRow>();

        foreach (var deviceSerialNumber in deviceSerialNumbers)
        {
            foreach (var employee in employees.Values.OrderBy(static employee => employee.EmployeeCode, StringComparer.OrdinalIgnoreCase))
            {
                var employeeCode = employee.EmployeeCode!;

                commandRows.Add(new AdmsDeviceCommandRow
                {
                    DeviceSn = deviceSerialNumber,
                    Content = $"DATA DELETE USERINFO PIN={employeeCode}",
                    CommitTime = commitTime,
                    Description = "Delete User Info",
                    ReturnValue = string.Empty
                });

                for (var fid = 0; fid < 10; fid += 1)
                {
                    commandRows.Add(new AdmsDeviceCommandRow
                    {
                        DeviceSn = deviceSerialNumber,
                        Content = $"DATA DELETE FINGERTMP PIN={employeeCode}\tFID={fid}",
                        CommitTime = commitTime,
                        Description = $"Delete Fingerprint Pin={employeeCode}",
                        ReturnValue = string.Empty
                    });
                }

                commandRows.Add(new AdmsDeviceCommandRow
                {
                    DeviceSn = deviceSerialNumber,
                    Content = $"DATA DELETE FACE PIN={employeeCode}",
                    CommitTime = commitTime,
                    Description = $"Delete Face Pin={employeeCode}",
                    ReturnValue = string.Empty
                });

                commandRows.Add(new AdmsDeviceCommandRow
                {
                    DeviceSn = deviceSerialNumber,
                    Content = $"DATA DELETE FVEIN Pin={employeeCode}",
                    CommitTime = commitTime,
                    Description = $"Delete FVEIN Pin={employeeCode}",
                    ReturnValue = string.Empty
                });

                for (var fid = 0; fid < 10; fid += 1)
                {
                    commandRows.Add(new AdmsDeviceCommandRow
                    {
                        DeviceSn = deviceSerialNumber,
                        Content = $"DATA DELETE FVEIN Pin={employeeCode}\tFID={fid}",
                        CommitTime = commitTime,
                        Description = $"Delete FVEIN Pin={employeeCode}",
                        ReturnValue = string.Empty
                    });
                }

                foreach (var content in BuildDeleteBioDataCommandContents(employeeCode))
                {
                    commandRows.Add(new AdmsDeviceCommandRow
                    {
                        DeviceSn = deviceSerialNumber,
                        Content = content,
                        CommitTime = commitTime,
                        Description = $"Delete Biodata Pin={employeeCode}",
                        ReturnValue = string.Empty
                    });
                }

                commandRows.Add(new AdmsDeviceCommandRow
                {
                    DeviceSn = deviceSerialNumber,
                    Content = $"DATA DELETE USERPIC PIN={employeeCode}",
                    CommitTime = commitTime,
                    Description = $"Delete UserPic Pin={employeeCode}",
                    ReturnValue = string.Empty
                });

                commandRows.Add(new AdmsDeviceCommandRow
                {
                    DeviceSn = deviceSerialNumber,
                    Content = $"DATA DELETE BIOPHOTO PIN={employeeCode}",
                    CommitTime = commitTime,
                    Description = $"Delete BioPhoto Pin={employeeCode}",
                    ReturnValue = string.Empty
                });
            }
        }

        return commandRows;
    }

    private static string BuildUpdateUserInfoContent(
        string employeeCode,
        string? fullName,
        AttendanceDeviceUserProfileRow? profile)
    {
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"DATA UPDATE USERINFO PIN={employeeCode}\tName={NormalizeCommandField(fullName)}\tPri={NormalizeCommandField(profile?.PrivilegeCode) ?? DefaultPrivilegeCode}\tPasswd={NormalizeCommandField(profile?.Password)}\tCard={NormalizeCommandField(profile?.CardNumber)}\tGrp={NormalizeCommandField(profile?.GroupCode) ?? DefaultGroupCode}\tTZ={NormalizeCommandField(profile?.TimeZoneCode) ?? DefaultTimeZoneCode}");
    }

    private static string BuildUpdateFingerprintContent(
        string employeeCode,
        AttendanceFingerprintTemplateRow template,
        string templateData)
    {
        var size = template.Size.GetValueOrDefault(templateData.Length);
        var valid = NormalizeCommandField(template.Valid) ?? "1";
        var fid = NormalizeCommandField(template.Fid) ?? "0";

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"DATA UPDATE FINGERTMP PIN={employeeCode}\tFID={fid}\tSize={size}\tValid={valid}\tTMP={templateData}");
    }

    private static string BuildUpdateBioDataContent(string employeeCode, BioDataPayload payload)
    {
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"DATA UPDATE BIODATA Pin={employeeCode}\tNo={payload.No}\tIndex={payload.Index}\tValid={payload.Valid}\tDuress={payload.Duress}\tType={payload.Type}\tMajorVer={payload.MajorVersion}\tMinorVer={payload.MinorVersion}\tFormat={payload.Format}\tTmp={payload.TemplateData}");
    }

    private static string BuildUpdateUserPictureContent(
        string employeeCode,
        AttendanceUserPictureRow userPicture,
        string content)
    {
        var size = userPicture.Size.GetValueOrDefault(content.Length);

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"DATA UPDATE USERPIC PIN={employeeCode}\tSize={size}\tContent={content}");
    }

    private static string BuildUpdateBioPhotoContent(
        string employeeCode,
        AttendanceBioPhotoRow bioPhoto,
        string content)
    {
        var type = NormalizeCommandField(bioPhoto.Type) ?? "0";

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"DATA UPDATE BIOPHOTO PIN={employeeCode}\tType={type}\tSize={content.Length}\tContent={content}\tFormat=0\tUrl=0\tPostBackTmpFlag=0");
    }

    private static IEnumerable<string> BuildDeleteBioDataCommandContents(string employeeCode)
    {
        yield return $"DATA DELETE BIODATA Pin={employeeCode}";

        for (var type = 0; type < 10; type += 1)
        {
            if (type is 1 or 7)
            {
                for (var no = 0; no < 9; no += 1)
                {
                    yield return $"DATA DELETE BIODATA Pin={employeeCode}\tType={type}\tNo={no}";
                }

                continue;
            }

            if (type == 2)
            {
                yield return $"DATA DELETE BIODATA Pin={employeeCode}\tType={type}\tNo=0";
                continue;
            }

            if (type is 4 or 8)
            {
                for (var no = 0; no < 2; no += 1)
                {
                    yield return $"DATA DELETE BIODATA Pin={employeeCode}\tType={type}\tNo={no}";
                }

                continue;
            }

            yield return $"DATA DELETE BIODATA Pin={employeeCode}\tType={type}";
        }
    }

    private async Task<Dictionary<Guid, IReadOnlyList<BioDataPayload>>> LoadBioDataPayloadsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return [];
        }

        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var resolvedTable = await ResolveTableNameAsync(connection, "biodata", cancellationToken);
            if (resolvedTable is null)
            {
                return [];
            }

            var columns = await GetColumnNamesAsync(connection, resolvedTable, cancellationToken);
            var employeeIdColumn = PickFirst(columns, EmployeeIdColumns);
            var payloadColumn = PickFirst(columns, PayloadColumns);
            var typeColumn = PickFirst(columns, BioDataTypeColumns);
            var noColumn = PickFirst(columns, BioDataNoColumns);
            var indexColumn = PickFirst(columns, BioDataIndexColumns);
            var validColumn = PickFirst(columns, BioDataValidColumns);
            var duressColumn = PickFirst(columns, BioDataDuressColumns);
            var majorVersionColumn = PickFirst(columns, BioDataMajorVersionColumns);
            var minorVersionColumn = PickFirst(columns, BioDataMinorVersionColumns);
            var formatColumn = PickFirst(columns, BioDataFormatColumns);
            var updatedAtColumn = PickFirst(columns, UpdatedAtColumns);
            var createdAtColumn = PickFirst(columns, CreatedAtColumns);

            if (employeeIdColumn is null || payloadColumn is null || typeColumn is null)
            {
                return [];
            }

            var sql = $"""
                select
                    cast({QuoteIdentifier(employeeIdColumn)} as text) as employee_id,
                    cast({QuoteIdentifier(typeColumn)} as text) as bio_type,
                    {BuildNullableTextProjection(noColumn, "bio_no")},
                    {BuildNullableTextProjection(indexColumn, "bio_index")},
                    {BuildNullableTextProjection(validColumn, "valid")},
                    {BuildNullableTextProjection(duressColumn, "duress")},
                    {BuildNullableTextProjection(majorVersionColumn, "major_ver")},
                    {BuildNullableTextProjection(minorVersionColumn, "minor_ver")},
                    {BuildNullableTextProjection(formatColumn, "format")},
                    cast({QuoteIdentifier(payloadColumn)} as text) as template_data,
                    {BuildNullableTimestampProjection(updatedAtColumn, "updated_at")},
                    {BuildNullableTimestampProjection(createdAtColumn, "created_at")}
                from {QuoteIdentifier(resolvedTable)}
                where cast({QuoteIdentifier(employeeIdColumn)} as text) = any(@employeeIds)
                  and {QuoteIdentifier(payloadColumn)} is not null
                  and btrim(cast({QuoteIdentifier(payloadColumn)} as text)) <> ''
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("employeeIds", employeeIds.Select(static employeeId => employeeId.ToString()).ToArray());

            var rows = new List<BioDataProjection>();
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                var employeeIdText = reader.GetString(0)?.Trim();
                if (!Guid.TryParse(employeeIdText, out var employeeId))
                {
                    continue;
                }

                var type = NormalizeOptional(reader.IsDBNull(1) ? null : reader.GetString(1));
                var no = NormalizeOptional(reader.IsDBNull(2) ? null : reader.GetString(2));
                var index = NormalizeOptional(reader.IsDBNull(3) ? null : reader.GetString(3));
                var valid = NormalizeOptional(reader.IsDBNull(4) ? null : reader.GetString(4));
                var duress = NormalizeOptional(reader.IsDBNull(5) ? null : reader.GetString(5));
                var majorVersion = NormalizeOptional(reader.IsDBNull(6) ? null : reader.GetString(6));
                var minorVersion = NormalizeOptional(reader.IsDBNull(7) ? null : reader.GetString(7));
                var format = NormalizeOptional(reader.IsDBNull(8) ? null : reader.GetString(8));
                var templateData = reader.IsDBNull(9)
                    ? null
                    : NormalizeOptional(reader.GetString(9));
                if (templateData is null)
                {
                    continue;
                }

                var updatedAt = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10);
                var createdAt = reader.IsDBNull(11) ? (DateTime?)null : reader.GetDateTime(11);

                rows.Add(new BioDataProjection(
                    employeeId,
                    type,
                    no,
                    index,
                    valid,
                    duress,
                    majorVersion,
                    minorVersion,
                    format,
                    templateData,
                    updatedAt,
                    createdAt));
            }

            return rows
                .GroupBy(static row => new
                {
                    row.EmployeeId,
                    Type = row.Type ?? string.Empty,
                    No = row.No ?? string.Empty,
                    Index = row.Index ?? string.Empty
                })
                .Select(static group => group
                    .OrderByDescending(static row => row.UpdatedAt)
                    .ThenByDescending(static row => row.CreatedAt)
                    .First())
                .GroupBy(static row => row.EmployeeId)
                .ToDictionary(
                    static group => group.Key,
                    static group => (IReadOnlyList<BioDataPayload>)group
                        .OrderBy(static row => ParseFidOrder(row.Type))
                        .ThenBy(static row => row.No, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(static row => row.Index, StringComparer.OrdinalIgnoreCase)
                        .Select(static row => new BioDataPayload(
                            row.Type ?? "0",
                            row.No ?? "0",
                            row.Index ?? "0",
                            row.Valid ?? "1",
                            row.Duress ?? "0",
                            row.MajorVersion ?? "0",
                            row.MinorVersion ?? "0",
                            row.Format ?? "0",
                            row.TemplateData))
                        .ToArray());
        }
        finally
        {
            if (shouldCloseConnection && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<Dictionary<Guid, EmployeeSnapshot>> LoadMatchedEmployeesAsync(
        AttendanceBiometricDeviceCommandBatchRequest request,
        string emptyEmployeeMessage,
        string noMatchMessage,
        CancellationToken cancellationToken)
    {
        var employeeIds = request.EmployeeIds
            .Where(static employeeId => employeeId != Guid.Empty)
            .Distinct()
            .ToArray();
        if (employeeIds.Length == 0)
        {
            throw new InvalidOperationException(emptyEmployeeMessage);
        }

        var employees = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employeeIds.Contains(employee.Id))
            .Select(employee => new EmployeeSnapshot(
                employee.Id,
                NormalizeEmployeeCode(employee.EmployeeCode),
                BuildFullName(employee.LastName, employee.FirstName)))
            .ToListAsync(cancellationToken);

        var employeeById = employees
            .Where(static employee => !string.IsNullOrWhiteSpace(employee.EmployeeCode))
            .ToDictionary(static employee => employee.Id);
        if (employeeById.Count == 0)
        {
            throw new InvalidOperationException(noMatchMessage);
        }

        return employeeById;
    }

    private static string[] GetRequiredDeviceSerialNumbers(
        AttendanceBiometricDeviceCommandBatchRequest request,
        string emptyDeviceMessage)
    {
        var deviceSerialNumbers = request.DeviceSerialNumbers
            .Select(static serialNumber => NormalizeDeviceSerial(serialNumber))
            .Where(static serialNumber => !string.IsNullOrWhiteSpace(serialNumber))
            .Select(static serialNumber => serialNumber!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (deviceSerialNumbers.Length == 0)
        {
            throw new InvalidOperationException(emptyDeviceMessage);
        }

        return deviceSerialNumbers;
    }

    private static async Task<string?> ResolveTableNameAsync(
        NpgsqlConnection connection,
        string logicalTableName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select table_name
            from information_schema.tables
            where table_schema = 'public'
              and lower(table_name) = lower(@logicalTableName)
            limit 1
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("logicalTableName", logicalTableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    private static async Task<Dictionary<string, string>> GetColumnNamesAsync(
        NpgsqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select column_name
            from information_schema.columns
            where table_schema = 'public'
              and table_name = @tableName
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tableName", tableName);

        var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            var columnName = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(columnName))
            {
                columns[columnName.Trim()] = columnName.Trim();
            }
        }

        return columns;
    }

    private static string? PickFirst(
        IReadOnlyDictionary<string, string> columns,
        IReadOnlyList<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (columns.TryGetValue(candidate, out var actual))
            {
                return actual;
            }
        }

        return null;
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string BuildNullableTextProjection(string? columnName, string alias)
    {
        return columnName is null
            ? $"null::text as {alias}"
            : $"cast({QuoteIdentifier(columnName)} as text) as {alias}";
    }

    private static string BuildNullableTimestampProjection(string? columnName, string alias)
    {
        return columnName is null
            ? $"null::timestamp without time zone as {alias}"
            : $"cast({QuoteIdentifier(columnName)} as timestamp without time zone) as {alias}";
    }

    private static string? NormalizeEmployeeCode(string? employeeCode)
    {
        return NormalizeOptional(employeeCode);
    }

    private static string? NormalizeDeviceSerial(string? serialNumber)
    {
        return string.IsNullOrWhiteSpace(serialNumber)
            ? null
            : serialNumber.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeCommandField(string? value)
    {
        var normalized = NormalizeOptional(value);
        return normalized?.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    }

    private static string BuildFullName(string? lastName, string? firstName)
    {
        var parts = new[] { NormalizeOptional(lastName), NormalizeOptional(firstName) }
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? string.Empty : string.Join(" ", parts);
    }

    private static int ParseFidOrder(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : int.MaxValue;
    }

    private static DateTime NormalizeDatabaseTimestamp(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
            DateTimeKind.Local => DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Unspecified),
            _ => value
        };
    }

    private sealed record EmployeeSnapshot(
        Guid Id,
        string? EmployeeCode,
        string FullName);

    private sealed record BioDataProjection(
        Guid EmployeeId,
        string? Type,
        string? No,
        string? Index,
        string? Valid,
        string? Duress,
        string? MajorVersion,
        string? MinorVersion,
        string? Format,
        string TemplateData,
        DateTime? UpdatedAt,
        DateTime? CreatedAt);

    private sealed record BioDataPayload(
        string Type,
        string No,
        string Index,
        string Valid,
        string Duress,
        string MajorVersion,
        string MinorVersion,
        string Format,
        string TemplateData);
}
