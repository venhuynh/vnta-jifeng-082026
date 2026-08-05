using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class AttendanceDeviceDataProvider(IAttendanceDeviceService attendanceDeviceService)
{
    public Task<IReadOnlyList<AttendanceDeviceRecord>> GetAsync(CancellationToken cancellationToken = default)
    {
        return GetFromServiceAsync(cancellationToken);
    }

    public Task<string?> ValidateAsync(
        AttendanceDeviceRecord device,
        CancellationToken cancellationToken = default)
    {
        return attendanceDeviceService.ValidateAsync(MapRequest(device), cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceDeviceRecord>> SaveAsync(
        AttendanceDeviceRecord device,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        await attendanceDeviceService.SaveAsync(MapRequest(device), isNew, cancellationToken);
        return await GetFromServiceAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceDeviceRecord>> DeleteAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        await attendanceDeviceService.DeleteAsync(ids.ToArray(), cancellationToken);
        return await GetFromServiceAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AttendanceDeviceRecord>> GetFromServiceAsync(CancellationToken cancellationToken)
    {
        var devices = await attendanceDeviceService.GetAsync(cancellationToken);
        return devices
            .Select(MapRecord)
            .ToList();
    }

    private static UpsertAttendanceDeviceRequest MapRequest(AttendanceDeviceRecord source) =>
        new()
        {
            Id = source.Id,
            Code = source.Code,
            Name = source.Name,
            SerialNumber = source.SerialNumber,
            IpAddress = source.IpAddress,
            MacAddress = source.MacAddress,
            Port = source.Port,
            Location = source.Location,
            ActivationCode = source.ActivationCode,
            VendorName = source.VendorName,
            DeviceModel = source.DeviceModel,
            FirmwareVersion = source.FirmwareVersion,
            FingerprintVersion = source.FingerprintVersion,
            TimeZone = source.TimeZone,
            Status = source.Status,
            IsInUse = source.IsInUse,
            UserCount = source.UserCount,
            AttendanceLogCount = source.AttendanceLogCount,
            FingerprintCount = source.FingerprintCount,
            AttendanceLogStamp = source.AttendanceLogStamp,
            AttendancePhotoStamp = source.AttendancePhotoStamp,
            OperationLogStamp = source.OperationLogStamp,
            ErrorLogStamp = source.ErrorLogStamp,
            TransferFlag = source.TransferFlag,
            Delay = source.Delay,
            Realtime = source.Realtime,
            TransInterval = source.TransInterval,
            TransTimes = source.TransTimes,
            Encrypt = source.Encrypt,
            ErrorDelay = source.ErrorDelay,
            Timeout = source.Timeout,
            SyncTime = source.SyncTime,
            LastRequestTime = source.LastRequestTime,
            IrTempDetectionFunOn = source.IrTempDetectionFunOn,
            MaskDetectionFunOn = source.MaskDetectionFunOn,
            MultiBioDataSupport = source.MultiBioDataSupport,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

    private static AttendanceDeviceRecord MapRecord(AttendanceDeviceDto source) =>
        new()
        {
            Id = source.Id,
            Code = source.Code,
            Name = source.Name,
            SerialNumber = source.SerialNumber,
            IpAddress = source.IpAddress,
            MacAddress = source.MacAddress,
            Port = source.Port,
            Location = source.Location,
            ActivationCode = source.ActivationCode,
            VendorName = source.VendorName,
            DeviceModel = source.DeviceModel,
            FirmwareVersion = source.FirmwareVersion,
            FingerprintVersion = source.FingerprintVersion,
            TimeZone = source.TimeZone,
            Status = source.Status,
            IsInUse = source.IsInUse,
            UserCount = source.UserCount,
            AttendanceLogCount = source.AttendanceLogCount,
            FingerprintCount = source.FingerprintCount,
            AttendanceLogStamp = source.AttendanceLogStamp,
            AttendancePhotoStamp = source.AttendancePhotoStamp,
            OperationLogStamp = source.OperationLogStamp,
            ErrorLogStamp = source.ErrorLogStamp,
            TransferFlag = source.TransferFlag,
            Delay = source.Delay,
            Realtime = source.Realtime,
            TransInterval = source.TransInterval,
            TransTimes = source.TransTimes,
            Encrypt = source.Encrypt,
            ErrorDelay = source.ErrorDelay,
            Timeout = source.Timeout,
            SyncTime = source.SyncTime,
            LastRequestTime = source.LastRequestTime,
            IrTempDetectionFunOn = source.IrTempDetectionFunOn,
            MaskDetectionFunOn = source.MaskDetectionFunOn,
            MultiBioDataSupport = source.MultiBioDataSupport,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
}
