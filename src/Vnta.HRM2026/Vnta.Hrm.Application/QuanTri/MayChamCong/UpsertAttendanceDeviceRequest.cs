namespace Vnta.Hrm.Application.QuanTri.MayChamCong;

public sealed class UpsertAttendanceDeviceRequest
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? SerialNumber { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public int? Port { get; set; }
    public string? Location { get; set; }
    public string? ActivationCode { get; set; }
    public string? VendorName { get; set; }
    public string? DeviceModel { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? FingerprintVersion { get; set; }
    public string? TimeZone { get; set; }
    public int Status { get; set; }
    public bool IsInUse { get; set; }
    public int? UserCount { get; set; }
    public int? AttendanceLogCount { get; set; }
    public int? FingerprintCount { get; set; }
    public string? AttendanceLogStamp { get; set; }
    public string? AttendancePhotoStamp { get; set; }
    public string? OperationLogStamp { get; set; }
    public string? ErrorLogStamp { get; set; }
    public string? TransferFlag { get; set; }
    public string? Delay { get; set; }
    public string? Realtime { get; set; }
    public string? TransInterval { get; set; }
    public string? TransTimes { get; set; }
    public string? Encrypt { get; set; }
    public string? ErrorDelay { get; set; }
    public int? Timeout { get; set; }
    public int SyncTime { get; set; }
    public DateTime? LastRequestTime { get; set; }
    public string? IrTempDetectionFunOn { get; set; }
    public string? MaskDetectionFunOn { get; set; }
    public string? MultiBioDataSupport { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
