using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("device_user_profiles")]
public sealed class ZktecoDeviceUserProfile
{
    [Key]
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DeviceSn { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(100)]
    public string? Password { get; set; }

    [MaxLength(100)]
    public string? CardNumber { get; set; }

    [MaxLength(50)]
    public string? GroupCode { get; set; }

    [MaxLength(50)]
    public string? TimeZoneCode { get; set; }

    [MaxLength(20)]
    public string? PrivilegeCode { get; set; }

    [MaxLength(20)]
    public string? VerifyMode { get; set; }

    [MaxLength(100)]
    public string? ViceCard { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
