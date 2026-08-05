using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("devices")]
public sealed class ZktecoDevice
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SerialNumber { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(50)]
    public string? MacAddress { get; set; }

    public int? Port { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }

    [MaxLength(200)]
    public string? ActivationCode { get; set; }

    [MaxLength(100)]
    public string? VendorName { get; set; }

    [MaxLength(200)]
    public string? DeviceModel { get; set; }

    [MaxLength(100)]
    public string? FirmwareVersion { get; set; }

    [MaxLength(100)]
    public string? FingerprintVersion { get; set; }

    [MaxLength(50)]
    public string? TimeZone { get; set; }

    public int Status { get; set; }

    public bool IsInUse { get; set; }

    public int UserCount { get; set; }

    public int AttendanceLogCount { get; set; }

    public int FingerprintCount { get; set; }

    [MaxLength(100)]
    public string? AttendanceLogStamp { get; set; }

    [MaxLength(100)]
    public string? AttendancePhotoStamp { get; set; }

    [MaxLength(100)]
    public string? OperationLogStamp { get; set; }

    [MaxLength(100)]
    public string? ErrorLogStamp { get; set; }

    [MaxLength(1000)]
    public string? TransferFlag { get; set; }

    [MaxLength(100)]
    public string? Delay { get; set; }

    [MaxLength(20)]
    public string? Realtime { get; set; }

    [MaxLength(100)]
    public string? TransInterval { get; set; }

    [MaxLength(100)]
    public string? TransTimes { get; set; }

    [MaxLength(20)]
    public string? Encrypt { get; set; }

    [MaxLength(100)]
    public string? ErrorDelay { get; set; }

    public int? Timeout { get; set; }

    public int SyncTime { get; set; }

    public DateTime? LastRequestTime { get; set; }

    [MaxLength(20)]
    public string? IrTempDetectionFunOn { get; set; }

    [MaxLength(20)]
    public string? MaskDetectionFunOn { get; set; }

    [MaxLength(200)]
    public string? MultiBioDataSupport { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
