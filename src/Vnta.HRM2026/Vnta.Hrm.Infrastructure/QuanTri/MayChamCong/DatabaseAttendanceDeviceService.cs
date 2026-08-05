using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.QuanTri.MayChamCong;

public sealed class DatabaseAttendanceDeviceService(ApplicationDbContext dbContext)
    : IAttendanceDeviceService
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private const string DeviceSerialNumberUniqueIndexName = "ux_devices_serial_number_not_empty";

    public async Task<IReadOnlyList<AttendanceDeviceDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Devices
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToDto).ToArray();
    }

    public async Task<string?> ValidateAsync(
        UpsertAttendanceDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCode = Normalize(request.Code);
        var normalizedName = Normalize(request.Name);
        var normalizedLocation = Normalize(request.Location);
        var normalizedActivationCode = Normalize(request.ActivationCode);
        var originalSerial = Normalize(request.SerialNumber);
        var normalizedSerial = AttendanceDeviceActivationCode.NormalizeSerial(originalSerial ?? string.Empty);

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return "Mã máy chưa được khởi tạo hợp lệ.";
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return "Tên máy không được để trống.";
        }

        if (string.IsNullOrWhiteSpace(normalizedSerial))
        {
            return "Số serial không được để trống.";
        }

        if (!IsSupportedSerial(normalizedSerial))
        {
            return "Số serial chỉ được gồm chữ, số và các ký tự ., _, -.";
        }

        if (request.Location is not null && string.IsNullOrWhiteSpace(normalizedLocation))
        {
            return "Vị trí không được chỉ gồm khoảng trắng.";
        }

        if (string.IsNullOrWhiteSpace(normalizedActivationCode))
        {
            return "Mã kích hoạt không được để trống.";
        }

        if (!AttendanceDeviceActivationCode.HasExpectedShape(normalizedActivationCode))
        {
            return "Mã kích hoạt phải đúng dạng VN1-XXXX-XXXX-XXXX-XXXX.";
        }

        if (!AttendanceDeviceActivationCode.Validate(normalizedSerial, normalizedActivationCode))
        {
            return "Mã kích hoạt không đúng với số serial này. Hãy nhập mã kích hoạt hợp lệ trước khi lưu.";
        }

        var existingRows = await dbContext.Devices
            .AsNoTracking()
            .Select(x => new ExistingDeviceSnapshot
            {
                Id = x.Id,
                Code = x.Code,
                SerialNumber = x.SerialNumber,
                ActivationCode = x.ActivationCode
            })
            .ToListAsync(cancellationToken);

        var duplicateCode = existingRows.Any(existing =>
            existing.Id != request.Id
            && string.Equals(existing.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));

        if (duplicateCode)
        {
            return "Mã máy đã tồn tại. Hãy dùng mã khác.";
        }

        var duplicateSerial = existingRows.Any(existing =>
            existing.Id != request.Id
            && string.Equals(
                AttendanceDeviceActivationCode.NormalizeSerial(existing.SerialNumber ?? string.Empty),
                normalizedSerial,
                StringComparison.OrdinalIgnoreCase));

        if (duplicateSerial)
        {
            return "Số serial đã được gắn cho máy khác.";
        }

        if (!string.IsNullOrWhiteSpace(normalizedActivationCode))
        {
            var duplicateActivationCode = existingRows.Any(existing =>
                existing.Id != request.Id
                && string.Equals(
                    AttendanceDeviceActivationCode.NormalizeActivationCode(existing.ActivationCode ?? string.Empty),
                    AttendanceDeviceActivationCode.NormalizeActivationCode(normalizedActivationCode),
                    StringComparison.OrdinalIgnoreCase));

            if (duplicateActivationCode)
            {
                return "Mã kích hoạt đã được gắn cho máy khác.";
            }
        }

        var existingDevice = existingRows.FirstOrDefault(existing => existing.Id == request.Id);
        if (existingDevice is not null)
        {
            var hasSerialChanged = !string.Equals(
                AttendanceDeviceActivationCode.NormalizeSerial(existingDevice.SerialNumber ?? string.Empty),
                normalizedSerial,
                StringComparison.OrdinalIgnoreCase);

            if (hasSerialChanged)
            {
                return "Số serial không được phép điều chỉnh sau khi đã tạo thiết bị.";
            }
        }

        return null;
    }

    public async Task<AttendanceDeviceDto> SaveAsync(
        UpsertAttendanceDeviceRequest request,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = await ValidateAsync(request, cancellationToken);
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            throw new InvalidOperationException(validationMessage);
        }

        var normalizedId = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
        var normalizedCreatedAt = request.CreatedAtUtc == default
            ? DateTime.UtcNow
            : request.CreatedAtUtc;
        var normalizedUpdatedAt = request.UpdatedAtUtc ?? DateTime.UtcNow;

        AttendanceDeviceRow row;
        if (isNew)
        {
            row = new AttendanceDeviceRow
            {
                Id = normalizedId,
                CreatedAtUtc = ToDatabaseLocalTimestamp(normalizedCreatedAt)
            };

            dbContext.Devices.Add(row);
        }
        else
        {
            row = await dbContext.Devices.SingleOrDefaultAsync(x => x.Id == normalizedId, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy máy chấm công để cập nhật.");
        }

        Apply(row, request, normalizedId);

        if (!isNew)
        {
            row.CreatedAtUtc = row.CreatedAtUtc == default
                ? ToDatabaseLocalTimestamp(normalizedCreatedAt)
                : DateTime.SpecifyKind(row.CreatedAtUtc, DateTimeKind.Unspecified);
        }

        row.UpdatedAtUtc = ToDatabaseLocalTimestamp(normalizedUpdatedAt);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateSerialNumberViolation(ex))
        {
            throw new InvalidOperationException("Số serial đã được gắn cho máy chấm công khác.", ex);
        }

        return MapToDto(row);
    }

    public async Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ids.Count == 0)
        {
            return;
        }

        var rows = await dbContext.Devices
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return;
        }

        dbContext.Devices.RemoveRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(
        AttendanceDeviceRow row,
        UpsertAttendanceDeviceRequest request,
        Guid id)
    {
        row.Id = id;
        row.Code = Normalize(request.Code) ?? string.Empty;
        row.Name = Normalize(request.Name) ?? string.Empty;
        row.SerialNumber = NormalizeSerial(request.SerialNumber);
        row.IpAddress = Normalize(request.IpAddress);
        row.MacAddress = Normalize(request.MacAddress);
        row.Port = request.Port > 0 ? request.Port : null;
        row.Location = Normalize(request.Location);
        row.ActivationCode = Normalize(request.ActivationCode);
        row.VendorName = Normalize(request.VendorName);
        row.DeviceModel = Normalize(request.DeviceModel);
        row.FirmwareVersion = Normalize(request.FirmwareVersion);
        row.FingerprintVersion = Normalize(request.FingerprintVersion);
        row.TimeZone = Normalize(request.TimeZone);
        row.Status = request.Status;
        row.IsInUse = request.IsInUse;
        row.UserCount = request.UserCount ?? 0;
        row.AttendanceLogCount = request.AttendanceLogCount ?? 0;
        row.FingerprintCount = request.FingerprintCount ?? 0;
        row.AttendanceLogStamp = Normalize(request.AttendanceLogStamp);
        row.AttendancePhotoStamp = Normalize(request.AttendancePhotoStamp);
        row.OperationLogStamp = Normalize(request.OperationLogStamp);
        row.ErrorLogStamp = Normalize(request.ErrorLogStamp);
        row.TransferFlag = Normalize(request.TransferFlag);
        row.Delay = Normalize(request.Delay);
        row.Realtime = Normalize(request.Realtime);
        row.TransInterval = Normalize(request.TransInterval);
        row.TransTimes = Normalize(request.TransTimes);
        row.Encrypt = Normalize(request.Encrypt);
        row.ErrorDelay = Normalize(request.ErrorDelay);
        row.Timeout = request.Timeout;
        row.SyncTime = request.SyncTime;
        row.LastRequestTime = ToDatabaseLocalTimestamp(request.LastRequestTime);
        row.IrTempDetectionFunOn = Normalize(request.IrTempDetectionFunOn);
        row.MaskDetectionFunOn = Normalize(request.MaskDetectionFunOn);
        row.MultiBioDataSupport = Normalize(request.MultiBioDataSupport);
    }

    private static AttendanceDeviceDto MapToDto(AttendanceDeviceRow row)
    {
        return new AttendanceDeviceDto
        {
            Id = row.Id,
            Code = row.Code,
            Name = row.Name,
            SerialNumber = row.SerialNumber,
            IpAddress = row.IpAddress,
            MacAddress = row.MacAddress,
            Port = row.Port,
            Location = row.Location,
            ActivationCode = row.ActivationCode,
            VendorName = row.VendorName,
            DeviceModel = row.DeviceModel,
            FirmwareVersion = row.FirmwareVersion,
            FingerprintVersion = row.FingerprintVersion,
            TimeZone = row.TimeZone,
            Status = row.Status,
            IsInUse = row.IsInUse,
            UserCount = row.UserCount,
            AttendanceLogCount = row.AttendanceLogCount,
            FingerprintCount = row.FingerprintCount,
            AttendanceLogStamp = row.AttendanceLogStamp,
            AttendancePhotoStamp = row.AttendancePhotoStamp,
            OperationLogStamp = row.OperationLogStamp,
            ErrorLogStamp = row.ErrorLogStamp,
            TransferFlag = row.TransferFlag,
            Delay = row.Delay,
            Realtime = row.Realtime,
            TransInterval = row.TransInterval,
            TransTimes = row.TransTimes,
            Encrypt = row.Encrypt,
            ErrorDelay = row.ErrorDelay,
            Timeout = row.Timeout,
            SyncTime = row.SyncTime,
            LastRequestTime = FromDatabaseLocalTimestamp(row.LastRequestTime),
            IrTempDetectionFunOn = row.IrTempDetectionFunOn,
            MaskDetectionFunOn = row.MaskDetectionFunOn,
            MultiBioDataSupport = row.MultiBioDataSupport,
            CreatedAtUtc = FromDatabaseLocalTimestamp(row.CreatedAtUtc) ?? default,
            UpdatedAtUtc = FromDatabaseLocalTimestamp(row.UpdatedAtUtc)
        };
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeSerial(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = AttendanceDeviceActivationCode.NormalizeSerial(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool IsSupportedSerial(string normalizedSerial) =>
        normalizedSerial.All(character =>
            (character >= 'A' && character <= 'Z')
            || (character >= '0' && character <= '9'));

    private static bool IsDuplicateSerialNumberViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
        && string.Equals(
            postgresException.ConstraintName,
            DeviceSerialNumberUniqueIndexName,
            StringComparison.OrdinalIgnoreCase);

    private static DateTime? ToDatabaseLocalTimestamp(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return ToDatabaseLocalTimestamp(value.Value);
    }

    private static DateTime ToDatabaseLocalTimestamp(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            return value;
        }

        var utcValue = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        var vietnamValue = TimeZoneInfo.ConvertTimeFromUtc(utcValue, VietnamTimeZone);
        return DateTime.SpecifyKind(vietnamValue, DateTimeKind.Unspecified);
    }

    private static DateTime? FromDatabaseLocalTimestamp(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);
    }

    private sealed class ExistingDeviceSnapshot
    {
        public Guid Id { get; init; }
        public string? Code { get; init; }
        public string? SerialNumber { get; init; }
        public string? ActivationCode { get; init; }
    }
}
