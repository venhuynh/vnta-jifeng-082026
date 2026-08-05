using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("fingerprint_templates")]
public sealed class ZktecoFingerprintTemplate
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

    [Required]
    [MaxLength(20)]
    public string Fid { get; set; } = string.Empty;

    public int? Size { get; set; }

    [MaxLength(20)]
    public string? Valid { get; set; }

    [Required]
    public string TemplateData { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? MajorVersion { get; set; }

    [MaxLength(20)]
    public string? MinorVersion { get; set; }

    [MaxLength(20)]
    public string? Duress { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
