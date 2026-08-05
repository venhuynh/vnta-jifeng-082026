using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vnta.AttendanceGateway.Domain;

[Table("fvein_templates")]
public sealed class ZktecoFveinTemplate
{
    [Key]
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DeviceSn { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Fid { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Index { get; set; } = string.Empty;

    public int? Size { get; set; }

    [MaxLength(20)]
    public string? Valid { get; set; }

    [Required]
    public string TemplateData { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Version { get; set; }

    [MaxLength(20)]
    public string? Duress { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
